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
using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;
using CivOne.IO;
using CivOne.Enums;

namespace CivOne.Services.Map
{
	public class MapSaveService : IMapSaveService
	{
		private static Resources Resources => Resources.Instance;

		public ushort Save(CivOne.Map map, string filename)
		{
			ValidateBinaryMapDimensions(map);
			Log($"Map: Saving {filename} - Random seed: {map.TerrainMasterWord}");

			using (Bytemap bitmap = Resources["SP299"].Bitmap)
			{
				SaveTerrainLayer(map, bitmap);
				SaveImprovementLayer(map, bitmap);
				SaveImprovementLayer2(map, bitmap);
				SaveExploredLayer(map, bitmap);
				SaveContinentLayer(map, bitmap);

				using (Picture picture = new Picture(bitmap, Resources["SP299"].Palette))
				{
					PicFile picFile = new PicFile(picture);
					using (BinaryWriter bw = new BinaryWriter(File.Open(filename, FileMode.Create)))
					{
						bw.Write(picFile.GetBytes());
					}

					return (ushort)map.TerrainMasterWord;
				}
			}
		}

		private void SaveTerrainLayer(CivOne.Map map, Bytemap bitmap)
		{
			for (int x = 0; x < map.Width; x++)
			for (int y = 0; y < map.Height; y++)
			{
				byte b;
				switch (map[x, y].Type)
				{
					case Terrain.Forest: b = 2; break;
					case Terrain.Swamp: b = 3; break;
					case Terrain.Plains: b = 6; break;
					case Terrain.Tundra: b = 7; break;
					case Terrain.River: b = 9; break;
					case Terrain.Grassland1:
					case Terrain.Grassland2: b = 10; break;
					case Terrain.Jungle: b = 11; break;
					case Terrain.Hills: b = 12; break;
					case Terrain.Mountains: b = 13; break;
					case Terrain.Desert: b = 14; break;
					case Terrain.Arctic: b = 15; break;
					default: b = 1; break; // Ocean
				}
				bitmap[x, y] = b;
			}
		}

		private void SaveImprovementLayer(CivOne.Map map, Bytemap bitmap)
		{
			for (int x = 0; x < map.Width; x++)
			for (int y = 0; y < map.Height; y++)
			{
				byte b = 0;
				if (map[x, y].City != null) b |= 0x01;
				if (map[x, y].Irrigation) b |= 0x02;
				if (map[x, y].Mine) b |= 0x04;
				if (map[x, y].Road) b |= 0x08;

				bitmap[x, y + (map.Height * 2)] = b;
				bitmap[x + map.Width, y + (map.Height * 2)] = b; // Visibility layer
			}
		}

		private void SaveImprovementLayer2(CivOne.Map map, Bytemap bitmap)
		{
			for (int x = 0; x < map.Width; x++)
			for (int y = 0; y < map.Height; y++)
			{
				byte b = 0;
				if (map[x, y].RailRoad) b |= 0x01;

				bitmap[x, y + (map.Height * 3)] = b;
				bitmap[x + map.Width, y + (map.Height * 3)] = b; // Visibility layer
			}
		}

		private void SaveExploredLayer(CivOne.Map map, Bytemap bitmap)
		{
			for (int x = 0; x < map.Width; x++)
			for (int y = 0; y < map.Height; y++)
			{
				bitmap[x + (map.Width * 2), y] = map[x, y].Visited;
			}
		}

		private void SaveContinentLayer(CivOne.Map map, Bytemap bitmap)
		{
			for (int x = 0; x < map.Width; x++)
			for (int y = 0; y < map.Height; y++)
			{
				bitmap[x, y + map.Height] = map[x, y].ContinentId;
				bitmap[x + map.Width, y + map.Height] = 0;
			}
		}

		private static void ValidateBinaryMapDimensions(CivOne.Map map)
		{
			ArgumentNullException.ThrowIfNull(map);

			if (map.Width == CivOne.Map.BinaryFormatWidth && map.Height == CivOne.Map.BinaryFormatHeight)
			{
				return;
			}

			throw new ArgumentException(
				$"Binary map format only supports {CivOne.Map.BinaryFormatWidth}x{CivOne.Map.BinaryFormatHeight}. Current map size is {map.Width}x{map.Height}.",
				nameof(map));
		}

		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);
	}
}
