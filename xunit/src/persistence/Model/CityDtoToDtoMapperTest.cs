namespace CivOne.Persistence.Model
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;
    using CivOne;
    using CivOne.Buildings;
    using CivOne.Enums;
    using CivOne.Persistence.Model.Attributes;
    using CivOne.Persistence.Yaml;
    using CivOne.UnitTests;
    using CivOne.Wonders;
    using Xunit;
    using YamlDotNet.Core;
    using YamlDotNet.Core.Events;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.EventEmitters;
    using YamlDotNet.Serialization.NamingConventions;
    using CityId = System.UInt32;

    public class CityDtoToDtoMapperTest
    {
        private readonly CityDtoMapper _testee;
        public List<ICity> _cities = [];

        public CityDtoToDtoMapperTest()
        {
            _testee = new CityDtoMapper(
                new ProductionDtoMapper(new MockedReflect()),
				new TestCityDefinitionResolver(),
				new ValueSanitizer(new NoOpLogger()));

            var city2 = new MockedICity(2);
            _cities.Add(city2);
            var city3 = new MockedICity(3);
            _cities.Add(city3);

            var city = new MockedICity(1)
            {
                TradingCities = [city2, city3],
                Player = new MockedIPlayer()
                {
                    Cities = [city2, city3]
                }
            };
            _cities.Add(city);


        }

        [Fact]
        public void TestToDto()
        {
            var dto = _testee.ToDto(_cities.Last());
            // testee.ToDto	
            Assert.NotNull(dto);

            WriteToFile("citydto.yaml", dto);
        }

        private void WriteToFile(string filename, CityDto dto)
        {
            var serializer = new YamlDotNet.Serialization.SerializerBuilder()
                // .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .WithTypeConverter(new Bool2dMapYamlTypeConverter())
                .WithEventEmitter(next => new DocCommentEventEmitter(next))
                .Build();

            string yaml = serializer.Serialize(dto);
            System.IO.File.WriteAllText(filename, yaml);
        }

        private sealed class TestCityDefinitionResolver : ICityDefinitionResolver
        {
            public IBuilding[] ResolveBuildings(IEnumerable<Building> buildingTypes)
                => [.. (buildingTypes ?? []).Select(type => new MockedIBuilding { Type = type })];

            public IWonder[] ResolveWonders(IEnumerable<Wonder> wonderTypes)
                => [.. (wonderTypes ?? []).Select(type => new MockedIWonder { Type = type })];
        }
    }
}