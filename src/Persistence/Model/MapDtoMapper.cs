using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Persistence.Model.Attributes;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Model
{
    public class MapDtoMapper(
    ) : DtoMapper<MapDto, Map>
    {
        public Map FromDto(MapDto dto)
        {
            throw new NotImplementedException();
        }

        public MapDto ToDto(Map map)
		{
			return new MapDto
            {
               
			};
		}
    }
}