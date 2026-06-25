// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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
