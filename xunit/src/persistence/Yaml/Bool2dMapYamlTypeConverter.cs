using System;
using CivOne.Persistence.Model;
using Xunit;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.YamlConverter
{
    class Bool2dMapYamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(Bool2dMap);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            var rows = (string[])rootDeserializer(typeof(string[]));
            return new Bool2dMap(rows);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            var bool2dMap = (Bool2dMap)value;
            serializer(bool2dMap.Rows, typeof(string[]));
        }

    }
}