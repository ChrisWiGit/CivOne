using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CivOne.Persistence.Model;
using CivOne.Persistence.Model.Attributes;
using Xunit;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace CivOne.Persistence.YamlConverter
{
    /**
     * Custom event emitter that adds comments to the emitted YAML based on the DocAttribute applied to properties.
     * It also supports emitting allowed values for properties if specified in the DocAttribute.
     * Usage:
     * var serializer = new YamlDotNet.Serialization.SerializerBuilder()
     *     .WithEventEmitter(next => new DocCommentEventEmitter(next))
     *     .Build();
     */
    public class DocCommentEventEmitter : ChainedEventEmitter
    {
        private readonly Stack<Type> typeStack = new();

        private Dictionary<string, bool> commentForProperties = new();

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

        public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
        {
            var propName = eventInfo.Source?.Value?.ToString();

            if (propName != null && CurrentType != null && !commentForProperties.ContainsKey(propName))
            {
                PropertyInfo property = CurrentType.GetProperty(propName);

                DocAttribute docAttribute = property?.GetCustomAttribute<DocAttribute>();

                if (docAttribute != null)
                {
                    commentForProperties[propName] = true;
                    emitter.Emit(new Comment(docAttribute.Description, false));

                    String allowedValues = docAttribute.AllowedValues;

                    if (docAttribute.AllowedValuesPropertyName != null)
                    {
                        var allowedValuesProperty = CurrentType.GetFields().FirstOrDefault(p => p.Name == docAttribute.AllowedValuesPropertyName);
                        var value = allowedValuesProperty?.GetValue(eventInfo.Source);

                        if (value is IEnumerable allowedValuesEnumerable)
                        {
                            emitter.Emit(new Comment($"Allowed values: {string.Join(", ", allowedValuesEnumerable.Cast<object>())}", false));
                        }
                    }

                    if (allowedValues != null)
                    {
                        emitter.Emit(new Comment($"Allowed values: {allowedValues}", false));
                    }
                }
            }
            base.Emit(eventInfo, emitter);
        }
    }
}

//[AttributeUsage(AttributeTargets.Property)]
//     public sealed class DocAttribute(
// 		string description,
// 		string allowedValuesPropertyName = null) : Attribute
//     {
// 		public string Description { get; } = description;
// 		public string AllowedValuesPropertyName { get; } = allowedValuesPropertyName;

//         public string AllowedValues { get; }

//         public DocAttribute(string description, string[] allowedValues) : this(description)
//         {
//             AllowedValues = string.Join(", ", allowedValues);
//         }

//         public DocAttribute(string description, long minValue, long maxValue) : this(description)
//         {
//             AllowedValues = $"[{minValue}, {maxValue}]";
//         }
// 	}

// }