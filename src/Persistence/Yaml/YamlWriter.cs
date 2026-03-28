using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CivOne.Persistence.Yaml
{
    /// <summary>
    /// Fluent builder for serializing a DTO to YAML.
    /// <para>
    /// Start with <see cref="Of"/> to capture the object, then optionally configure
    /// the serializer via <see cref="WithStandard"/>, <see cref="WithNamingConvention"/>,
    /// <see cref="WithTypeConverter"/> or <see cref="WithEventEmitter"/>, and finally
    /// materialize the result with <see cref="AsString"/> / <see cref="ToFile"/>.
    /// <see cref="ToString"/> is equivalent to <see cref="AsString"/>.
    /// </para>
    /// <example>
    /// <code>
    /// // Standard defaults, write to file:
    /// YamlWriter.Of(dto).WithStandard().ToFile("output.yaml");
    ///
    /// // Standard defaults, obtain as string:
    /// string yaml = YamlWriter.Of(dto).WithStandard().AsString();
    ///
    /// // Custom configuration:
    /// string yaml = YamlWriter.Of(dto)
    ///     .WithNamingConvention(CamelCaseNamingConvention.Instance)
    ///     .AsString();
    /// </code>
    /// </example>
    /// </summary>
    class YamlWriter
    {
        private readonly object _dto;
        private INamingConvention _namingConvention;
        private readonly List<IYamlTypeConverter> _typeConverters = [];
        private readonly List<Func<IEventEmitter, IEventEmitter>> _eventEmitterFactories = [];

        private YamlWriter(object dto)
        {
            _dto = dto;
        }

        /// <summary>
        /// No serialization options are pre-applied; use <see cref="WithStandard"/> or
        /// Creates a <see cref="YamlWriter"/> for <paramref name="dto"/>.
        /// the individual <c>With*</c> methods to configure the serializer before
        /// calling <see cref="AsString"/> or <see cref="ToFile"/>.
        /// </summary>
        /// <param name="dto">The object to serialize.</param>
        public static YamlWriter Of(object dto) => new(dto);

        /// <summary>
        /// Applies the standard serialization defaults:
        /// <list type="bullet">
        ///   <item><see cref="PascalCaseNamingConvention"/></item>
        ///   <item><see cref="Bool2dMapYamlTypeConverter"/></item>
        ///   <item><see cref="DocCommentEventEmitter"/></item>
        /// </list>
        /// Further <c>With*</c> calls can supplement or override individual settings.
        /// </summary>
        public YamlWriter WithStandard()
            => WithNamingConvention(PascalCaseNamingConvention.Instance)
               .WithTypeConverter(new Bool2dMapYamlTypeConverter())
               .WithEventEmitter(next => new DocCommentEventEmitter(next));

        /// <summary>
        /// Sets the naming convention used when serializing property names.
        /// Calling this method multiple times replaces the previously set convention.
        /// </summary>
        /// <param name="namingConvention">
        /// The naming convention to apply, e.g.
        /// <see cref="PascalCaseNamingConvention.Instance"/> or
        /// <see cref="CamelCaseNamingConvention.Instance"/>.
        /// </param>
        public YamlWriter WithNamingConvention(INamingConvention namingConvention)
        {
            _namingConvention = namingConvention;
            return this;
        }

        /// <summary>
        /// Registers a custom <see cref="IYamlTypeConverter"/> for types that require
        /// special serialization logic. Multiple converters can be added; they are
        /// applied in the order they were registered.
        /// </summary>
        /// <param name="typeConverter">The converter to add, e.g. <see cref="Bool2dMapYamlTypeConverter"/>.</param>
        public YamlWriter WithTypeConverter(IYamlTypeConverter typeConverter)
        {
            _typeConverters.Add(typeConverter);
            return this;
        }

        /// <summary>
        /// Registers a custom event emitter factory that wraps the next emitter in the
        /// pipeline. Multiple emitters can be chained; each factory receives the
        /// previously configured emitter as its argument. Use this to inject comments,
        /// anchors, or other YAML stream customisations.
        /// </summary>
        /// <param name="eventEmitterFactory">
        /// A factory delegate, e.g. <c>next =&gt; new DocCommentEventEmitter(next)</c>.
        /// </param>
        public YamlWriter WithEventEmitter(Func<IEventEmitter, IEventEmitter> eventEmitterFactory)
        {
            _eventEmitterFactories.Add(eventEmitterFactory);
            return this;
        }

        /// <summary>
        /// Serializes the DTO using the configured options and returns the result as a
        /// <see cref="string"/>. Equivalent to <see cref="ToString"/>.
        /// </summary>
        public string AsString()
        {
            var builder = new SerializerBuilder();

            if (_namingConvention != null)
                builder = builder.WithNamingConvention(_namingConvention);

            foreach (var converter in _typeConverters)
                builder = builder.WithTypeConverter(converter);

            foreach (var factory in _eventEmitterFactories)
                builder = builder.WithEventEmitter(factory);

            return builder.Build().Serialize(_dto);
        }

        /// <summary>
        /// Serializes the DTO using the configured options and writes the resulting YAML
        /// to <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">Path of the target file.</param>
        public void ToFile(string filename)
            => System.IO.File.WriteAllText(System.IO.Path.Combine(BasePath, filename), AsString());

        /// <inheritdoc cref="AsString"/>
        public override string ToString() => AsString();

        /// <summary>
		/// Base path for output files. 
        /// This will output all files to folder 'CivOne/xunit'.
		/// </summary>
        public static string BasePath { get; set; } = "../../..";
    }
}