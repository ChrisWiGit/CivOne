using System;
using System.IO;
using CivOne.Persistence.Factories;
using CivOne.Persistence.Model;
using CivOne.Persistence.Yaml;

namespace CivOne.src
{
    /// <summary>
    /// Loads the Earth map from the bundled <c>earth.yml</c> test-data file instead of the
    /// proprietary MAP.PIC. Overrides <see cref="Map.TaskRunEarthMapGeneration"/> so that
    /// <see cref="Map.LoadEarthMapInThread"/> works synchronously in tests without any
    /// proprietary files present.
    /// </summary>
    sealed class MapGenerationFromYaml : Map
    {
        protected override void TaskRunEarthMapGeneration()
        {
            Log("Map: Loading Earth map from YAML (test data)");

            var yamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "earth.yml");
            var yaml = File.ReadAllText(yamlPath);

            var dto = YamlReader.OfString(yaml)
                .WithStandard()
                .WithTypeConverter(new MapDtoTileDtoYamlConverter())
                .As<GameStateDto>();

            var map = Map.Instance;
            var mapMapper = new MapDtoMapper(
                new RuntimeMapFactory(map),
                new RuntimeTileDtoMapper(map, new RuntimeTerrainFactory()));

            mapMapper.FromDto(dto.Map);
            map.FinalizeYamlLoad();
        }
    }
}