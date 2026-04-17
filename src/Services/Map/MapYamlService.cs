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
using CivOne.Tiles;

namespace CivOne.Services.Map
{
	public class MapYamlService : IMapYamlService
	{
		public void LoadEarthMapInThread(CivOne.Map map)
		{
			if (map.Ready || map.Tiles != null)
			{
				Log("ERROR: Map is already load{0}/generat{0}", (map.Ready ? "ed" : "ing"));
				return;
			}

			map.LandMassInternal = -1;
			map.TemperatureInternal = -1;
			map.ClimateInternal = -1;
			map.AgeInternal = -1;
			map.FixedStartPositions = true;

			RunEarthMapThread(map);
		}

		public void RunEarthMapThread(CivOne.Map map)
		{
			Log("Map: Loading Earth map from earth.yml");
			
			try
			{
				var yamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "earth.yml");
				
				// If not found in BaseDirectory, try resources folder
				if (!File.Exists(yamlPath))
				{
					yamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "earth.yml");
				}
				
				if (!File.Exists(yamlPath))
				{
					Log("ERROR: earth.yml not found at {0} or {1}", 
						Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "earth.yml"),
						Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "earth.yml"));
					Log("Current BaseDirectory: {0}", AppDomain.CurrentDomain.BaseDirectory);
					throw new FileNotFoundException("earth.yml not found");
				}
				
				Log("Map: Loading from {0}", yamlPath);
				var yaml = File.ReadAllText(yamlPath);

				var dto = YamlReader.OfString(yaml)
					.WithStandard()
					.WithTypeConverter(new MapDtoTileDtoYamlConverter())
					.As<GameStateDto>();

				Log("Map: YAML loaded successfully, starting map initialization");
				var mapMapper = new MapDtoMapper(
					new RuntimeMapFactory(map),
					new RuntimeTileDtoMapper(map, new RuntimeTerrainFactory()));

				mapMapper.FromDto(dto.Map);
				Log("Map: Tiles loaded ({0} x {1})", map.Width, map.Height);
				map.FinalizeYamlLoad();
				Log("Map: Ready");
			}
			catch (Exception ex)
			{
				Log("ERROR: Failed to load earth.yml: {0}", ex.Message);
				Log("ERROR: Stack trace: {0}", ex.StackTrace);
				Log("ERROR: Inner exception: {0}", ex.InnerException?.Message);
				
				// Initialize empty map to prevent crashes
				if (map.Tiles == null)
				{
					Log("ERROR: Initializing empty map as fallback");
					map.InitializeForYamlLoad(map.Width, map.Height, 0);
					for (int x = 0; x < map.Width; x++)
					for (int y = 0; y < map.Height; y++)
					{
						map.SetTileInternal(x, y, new Ocean(x, y, false));
					}
					map.FinalizeYamlLoad();
				}
			}
		}

		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);
	}
}
