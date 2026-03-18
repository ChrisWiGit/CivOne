using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CivOne.Persistence.Model;
using CivOne.Persistence.Model.Attributes;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace CivOne.Persistence.Yaml
{
    /// <summary>
    /// Custom event emitter that adds comments to the emitted YAML based on the DocAttribute applied to properties.
    /// It also supports emitting allowed values for properties if specified in the DocAttribute.
    /// Usage:
    /// var serializer = new YamlDotNet.Serialization.SerializerBuilder()
    ///     .WithEventEmitter(next => new DocCommentEventEmitter(next))
    ///     .Build();
    /// This event emitter does the following:
    /// - If a DocAttribute is found on a property, it adds a comment with the description before emitting the property (in front of yaml key).
    /// - If the DocAttribute specifies an AllowedValuesPropertyName, it looks for a property with that name and emits a comment listing the allowed values.
    /// - If the DocAttribute specifies a CommentValuesPropertyName, it looks for a property with that name which should be a dictionary mapping possible values to comments. When emitting a sequence of scalar values for the original property, it emits the corresponding comment for each value based on the dictionary.
    /// Why?
    /// This allows us to include human-readable documentation and allowed values directly in the generated YAML files, which can be very helpful for users who are editing these files manually. It also keeps the documentation close to the code, making it easier to maintain.
    /// Numbers will be more readable if we can include the corresponding advance or government name as a comment, and for lists of advances or governments it will be very helpful to have comments for each element indicating which advance or government it corresponds to.
    /// </summary>
    /// <example>
    /// Example of output YAML with comments:
    /// <code>
    /// # The number of turns the player is in anarchy.
    /// Anarchy: 3
    /// # The id of the advance currently being researched, or null if no research is in progress.
    /// # Allowed values: [Advance:0, Advance:1, Advance:2, Advance:3]
    /// CurrentResearch: 2
    /// # A list of explored advances
    /// # Allowed values: Advance:0, Advance:1, Advance:2, Advance
    /// Advances:
    /// # Advance:0
    /// - 0
    /// # Advance:1
    /// - 1
    /// # Advance:2
    /// - 2
    /// </code>
    /// </example>
    public class DocCommentEventEmitter : ChainedEventEmitter
    {
        private readonly Stack<Type> typeStack = new();

        private Dictionary<string, bool> commentForProperties = new();

        private Dictionary<int, string> activeCommentValues;
        private bool nextSequenceHasComments = false;
        private bool inCommentedSequence = false;

        private Type CurrentType => typeStack.Count > 0 ? typeStack.Peek() : null;

        public DocCommentEventEmitter(IEventEmitter next) : base(next) { }

        public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter)
        {
            var type = eventInfo.Source?.Type;

            if (type != null)
                typeStack.Push(type);

            base.Emit(eventInfo, emitter);
        }

        public override void Emit(MappingEndEventInfo eventInfo, IEmitter emitter)
        {
            if (typeStack.Count > 0)
                typeStack.Pop();

            base.Emit(eventInfo, emitter);
        }

        public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter)
        {
            if (nextSequenceHasComments)
            {
                inCommentedSequence = true;
                nextSequenceHasComments = false;
            }
            base.Emit(eventInfo, emitter);
        }

        public override void Emit(SequenceEndEventInfo eventInfo, IEmitter emitter)
        {
            if (inCommentedSequence)
            {
                inCommentedSequence = false;
                activeCommentValues = null;
            }
            base.Emit(eventInfo, emitter);
        }

        public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
        {
            var propName = eventInfo.Source?.Value?.ToString();

            if (inCommentedSequence)
                EmitSequenceElementComment(propName, emitter);
            else
                TryEmitPropertyDocComment(eventInfo, emitter, propName);

            base.Emit(eventInfo, emitter);
        }

        private void EmitSequenceElementComment(string propName, IEmitter emitter)
        {
            if (activeCommentValues == null || propName == null)
            {
                return;
            }
            if (int.TryParse(propName, out int intValue) && activeCommentValues.TryGetValue(intValue, out string comment))
            {
                emitter.Emit(new Comment(comment, false));
            }
        }

        private void TryEmitPropertyDocComment(ScalarEventInfo eventInfo, IEmitter emitter, string propName)
        {
            if (propName == null || CurrentType == null || commentForProperties.ContainsKey(propName))
            {
                return;
            }

            var docAttribute = CurrentType.GetProperty(propName)?.GetCustomAttribute<DocAttribute>();
            if (docAttribute == null)
            {
                return;
            }

            DoNotCreateCommentForPropertyNextTime(propName);
            emitter.Emit(new Comment(docAttribute.Description, false));
            EmitAllowedValuesComment(eventInfo, emitter, docAttribute);
            PrepareSequenceElementComments(eventInfo, docAttribute, propName);
        }

        private void EmitAllowedValuesComment(ScalarEventInfo eventInfo, IEmitter emitter, DocAttribute docAttribute)
        {
            if (docAttribute.AllowedValuesPropertyName != null)
            {
                var field = CurrentType.GetFields().FirstOrDefault(p => p.Name == docAttribute.AllowedValuesPropertyName);
                if (field?.GetValue(eventInfo.Source) is IEnumerable values)
                    emitter.Emit(new Comment($"Allowed values: {string.Join(", ", values.Cast<object>())}", false));
            }

            if (docAttribute.AllowedValues != null)
            {
                emitter.Emit(new Comment($"Allowed values: {docAttribute.AllowedValues}", false));
            }
        }

        private void PrepareSequenceElementComments(ScalarEventInfo eventInfo, DocAttribute docAttribute, string propName)
        {
            if (docAttribute.CommentValuesPropertyName == null) return;

            // Allow re-commenting each time this property is serialized, since comments depend on instance data
            DoNotCreateCommentForPropertyNextTime(propName, ignore: false);

            var field = CurrentType
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                .FirstOrDefault(p => p.Name == docAttribute.CommentValuesPropertyName);
            if (field == null)
            {
                return;
            }

            object dictValue = field.IsStatic ? field.GetValue(null) : field.GetValue(eventInfo.Source?.Value);
            activeCommentValues = ConvertToDictionaryIntString(dictValue);
            if (activeCommentValues != null)
            {
                nextSequenceHasComments = true;
            }
        }

        private void DoNotCreateCommentForPropertyNextTime(string propName, bool ignore = true)
        {
            commentForProperties[propName] = ignore;
        }

        private static Dictionary<int, string> ConvertToDictionaryIntString(object value)
        {
            if (value is Dictionary<int, string> dict)
                return dict;

            if (value is IDictionary idictionary)
            {
                var result = new Dictionary<int, string>();
                foreach (DictionaryEntry entry in idictionary)
                {
                    try
                    {
                        int key = Convert.ToInt32(entry.Key);
                        result[key] = entry.Value?.ToString();
                    }
                    catch { /* skip unconvertible keys */ }
                }
                return result.Count > 0 ? result : null;
            }

            return null;
        }
    }
}