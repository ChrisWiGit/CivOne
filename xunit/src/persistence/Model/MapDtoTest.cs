using System;
using CivOne.Enums;
using Xunit;

namespace CivOne.Persistence.Model
{
	public class MapDtoTest		
	{
		private readonly MapDtoMapper _testee;

		public MapDtoTest()
		{
			_testee = new MapDtoMapper();
		}

		// _testee.ToDto() and _testee.FromDto() 
	}
}

//  private void WriteToFile(string filename, CityDto dto)
        // {
        //     var serializer = new YamlDotNet.Serialization.SerializerBuilder()
        //         // .WithNamingConvention(PascalCaseNamingConvention.Instance)
        //         .WithTypeConverter(new Bool2dMapYamlTypeConverter())
        //         .WithEventEmitter(next => new DocCommentEventEmitter(next))
        //         .Build();

        //     string yaml = serializer.Serialize(dto);
        //     System.IO.File.WriteAllText(filename, yaml);
        // }

// 