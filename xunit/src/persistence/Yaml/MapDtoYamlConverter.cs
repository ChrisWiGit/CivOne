using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Persistence.Model.Attributes;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Model
{
    /* für YAML ein converter, welche eine MapDto anders serialisiert. wie geht das?
    */
    public class MapDtoYamlConverter : YamlDotNet.Serialization.IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(MapDto);
        }

		public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
		{
			throw new NotImplementedException();
		}

		public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}
}