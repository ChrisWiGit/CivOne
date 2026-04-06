using System;
using CivOne.Persistence.Model;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Yaml
{
    /// <summary>
    /// YAML converter for <see cref="MapLocation"/>.
    /// Required because MapLocation's X and Y are readonly fields, which YamlDotNet
    /// cannot set via reflection. This converter reads them into a settable helper
    /// and constructs MapLocation via its two-argument constructor.
    /// </summary>
    internal class MapLocationYamlConverter : IYamlTypeConverter
    {
        // Settable helper so YamlDotNet's default mapping deserializer can populate it.
        private sealed class MapLocationData
        {
            public uint X { get; set; }
            public uint Y { get; set; }
        }

        public bool Accepts(Type type) => type == typeof(MapLocation);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            var data = (MapLocationData)rootDeserializer(typeof(MapLocationData));
            return new MapLocation(data.X, data.Y);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            var loc = (MapLocation)value;
            serializer(new MapLocationData { X = loc.X, Y = loc.Y }, typeof(MapLocationData));
        }
    }
}
