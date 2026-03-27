using System;
using CivOne.Persistence.Model;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Yaml
{
    class Map2dYamlTypeConverter<T> : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(Map2d<T>);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            var rows = (T[][])rootDeserializer(typeof(T[][]));
            return new Map2d<T>(rows);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            var map = (Map2d<T>)value;
            var rows = new T[map.Height()][];

            for (int y = 0; y < map.Height(); y++)
                rows[y] = map[y];

            serializer(rows, typeof(T[][]));
        }
    }
}
