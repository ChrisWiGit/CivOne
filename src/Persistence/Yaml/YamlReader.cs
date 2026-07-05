using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CivOne.Persistence.Yaml
{
    /// <summary>
    /// Fluent builder for deserializing YAML from a string or file into a DTO.
    /// <para>
    /// Start with <see cref="OfString"/> to load YAML from a string or <see cref="OfFile"/>
    /// to load from a file, then optionally configure the deserializer via
    /// <see cref="WithStandard"/>, <see cref="WithNamingConvention"/>,
    /// <see cref="WithTypeConverter"/> or <see cref="WithNodeDeserializer"/>, and finally
    /// materialize the result with <see cref="As{T}()"/>.
    /// </para>
    /// <example>
    /// <code>
    /// // Standard defaults, read from file:
    /// var dto = YamlReader.OfFile("input.yaml").WithStandard().As&lt;MyDto&gt;();
    ///
    /// // Standard defaults, read from string:
    /// var dto = YamlReader.OfString(yamlContent).WithStandard().As&lt;MyDto&gt;();
    ///
    /// // Custom configuration:
    /// var dto = YamlReader.OfString(yamlContent)
    ///     .WithNamingConvention(CamelCaseNamingConvention.Instance)
    ///     .As&lt;MyDto&gt;();
    /// </code>
    /// </example>
    /// </summary>
    class YamlReader
    {
        private readonly string _yaml;
        private INamingConvention? _namingConvention;
        private readonly List<IYamlTypeConverter> _typeConverters = new List<IYamlTypeConverter>();
        private readonly List<INodeDeserializer> _nodeDeserializerFactories = new List<INodeDeserializer>();

        private YamlReader(string yaml)
        {
            _yaml = yaml;
        }

        /// <summary>
        /// Creates a <see cref="YamlReader"/> for YAML content provided as a string.
        /// No deserialization options are pre-applied; use <see cref="WithStandard"/> or
        /// the individual <c>With*</c> methods to configure the deserializer before
        /// calling <see cref="As{T}()"/>.
        /// </summary>
        /// <param name="yaml">The YAML content to deserialize.</param>
        public static YamlReader OfString(string yaml) => new(yaml);

        /// <summary>
        /// Creates a <see cref="YamlReader"/> for YAML content loaded from a file.
        /// No deserialization options are pre-applied; use <see cref="WithStandard"/> or
        /// the individual <c>With*</c> methods to configure the deserializer before
        /// calling <see cref="As{T}()"/>.
        /// </summary>
        /// <param name="filename">Path to the YAML file to read.</param>
        public static YamlReader OfFile(string filename)
        {
            var fullPath = Path.Combine(BasePath, filename);
            var yaml = File.ReadAllText(fullPath);
            return new(yaml);
        }

        /// <summary>
        /// Applies the standard deserialization defaults:
        /// <list type="bullet">
        ///   <item><see cref="PascalCaseNamingConvention"/></item>
        ///   <item><see cref="Bool2dMapYamlTypeConverter"/></item>
        ///   <item><see cref="SpaceShipGridMapYamlTypeConverter"/></item>
        /// </list>
        /// Further <c>With*</c> calls can supplement or override individual settings.
        /// </summary>
        public YamlReader WithStandard()
            => WithNamingConvention(PascalCaseNamingConvention.Instance)
                .WithTypeConverter(new Bool2dMapYamlTypeConverter())
                .WithTypeConverter(new SpaceShipGridMapYamlTypeConverter())
                .WithTypeConverter(new MapLocationYamlConverter());

        /// <summary>
        /// Sets the naming convention used when deserializing property names.
        /// Calling this method multiple times replaces the previously set convention.
        /// </summary>
        /// <param name="namingConvention">
        /// The naming convention to apply, e.g.
        /// <see cref="PascalCaseNamingConvention.Instance"/> or
        /// <see cref="CamelCaseNamingConvention.Instance"/>.
        /// </param>
        public YamlReader WithNamingConvention(INamingConvention namingConvention)
        {
            _namingConvention = namingConvention;
            return this;
        }

        /// <summary>
        /// Registers a custom <see cref="IYamlTypeConverter"/> for types that require
        /// special deserialization logic. Multiple converters can be added; they are
        /// applied in the order they were registered.
        /// </summary>
        /// <param name="typeConverter">The converter to add, e.g. <see cref="Bool2dMapYamlTypeConverter"/>.</param>
        public YamlReader WithTypeConverter(IYamlTypeConverter typeConverter)
        {
            _typeConverters.Add(typeConverter);
            return this;
        }

        /// <summary>
        /// Registers a custom node deserializer factory that wraps the next deserializer in the
        /// pipeline. Multiple deserializers can be chained; each factory receives the
        /// previously configured deserializer as its argument. Use this to inject custom
        /// deserialization logic for specific types or node types.
        /// </summary>
        /// <param name="nodeDeserializerFactory">
        /// A factory delegate, e.g. <c>next =&gt; new CustomNodeDeserializer(next)</c>.
        /// </param>
        public YamlReader WithNodeDeserializer(INodeDeserializer nodeDeserializer)
        {
            _nodeDeserializerFactories.Add(nodeDeserializer);
            return this;
        }

        /// <summary>
        /// Deserializes the YAML content using the configured options into an instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The target type to deserialize into.</typeparam>
        /// <returns>An instance of <typeparamref name="T"/> populated from the YAML content.</returns>
        public T As<T>()
        {
            var builder = new DeserializerBuilder();

            if (_namingConvention != null)
                builder = builder.WithNamingConvention(_namingConvention);

            foreach (var converter in _typeConverters)
                builder = builder.WithTypeConverter(converter);

            foreach (var factory in _nodeDeserializerFactories)
                builder = builder.WithNodeDeserializer(factory);

            var deserializer = builder.Build();
            return deserializer.Deserialize<T>(_yaml);
        }

        /// <summary>
        /// Base path for input files.
        /// This will read all files from folder 'CivOne/xunit'.
        /// </summary>
        public static string BasePath { get; set; } = "../../..";
    }
}
