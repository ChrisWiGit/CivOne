using System;
using CivOne.Persistence.Model;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Yaml
{
    class Bool2dMapYamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(Bool2dMap);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            var rows = (string[]?)rootDeserializer(typeof(string[]));

            if (rows == null || rows.Length == 0)
            {
                return new Bool2dMap();
            }

            return new Bool2dMap(rows);
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value is not Bool2dMap bool2dMap)
            {
                throw new ArgumentException($"WriteYaml expected a {nameof(Bool2dMap)} but received {value.GetType().FullName}.");
            }

            serializer(bool2dMap.Rows, typeof(string[]));
        }

    }
}