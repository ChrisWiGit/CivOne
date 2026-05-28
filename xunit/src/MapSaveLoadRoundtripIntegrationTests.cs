// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.IO;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;
using CivOne.Services.Maps;
using CivOne.Tiles;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Phase D integration tests. Exercise the full Save→Load pipeline through the
	/// real PIC encoder/decoder, but route every external collaborator
	/// (random, picture resources, persistence) through stubs so the test stays
	/// hermetic and reproducible. Locks down that:
	/// <list type="bullet">
	///   <item>The bytes produced by <see cref="Map.SaveMap(string)"/> can be parsed
	///         back by <see cref="PicFile"/>.</item>
	///   <item>The terrain types of every tile survive the roundtrip.</item>
	///   <item>The terrain master word is preserved.</item>
	/// </list>
	/// </summary>
	public class MapSaveLoadRoundtripIntegrationTests : src.TestsBase
	{
		[Fact]
		public void SaveMapAndLoadMapRoundtripPreservesTerrainTypes()
		{
			const int seed = 7;
			Terrain[,] expected = BuildDeterministicTerrainPattern();

			string tempFile = Path.Combine(Path.GetTempPath(), $"civone-d-{Path.GetRandomFileName()}.map");
			try
			{
				CapturingMapPersistenceService capture = new();
				Map source = new(null, new InMemoryMapResourceProvider(), null, capture);
				PopulateTiles(source, expected, seed);
				source.SaveMap(tempFile);

				File.WriteAllBytes(tempFile, capture.Writes[0].Bytes);

				Map reloaded = new(null, new FilePicResourceProvider(tempFile), null, null);
				reloaded.LoadMap("ignored", seed);

				for (int x = 0; x < Map.WIDTH; x++)
				for (int y = 0; y < Map.HEIGHT; y++)
				{
					Assert.Equal(expected[x, y], reloaded[x, y].Type);
				}
			}
			finally
			{
				if (File.Exists(tempFile)) File.Delete(tempFile);
			}
		}

		[Fact]
		public void SaveMapAndLoadMapRoundtripPreservesTerrainMasterWord()
		{
			const int expected = 11;
			Terrain[,] terrain = BuildDeterministicTerrainPattern();

			string tempFile = Path.Combine(Path.GetTempPath(), $"civone-d-{Path.GetRandomFileName()}.map");
			try
			{
				CapturingMapPersistenceService capture = new();
				Map source = new(null, new InMemoryMapResourceProvider(), null, capture);
				PopulateTiles(source, terrain, expected);
				ushort returned = source.SaveMap(tempFile);

				File.WriteAllBytes(tempFile, capture.Writes[0].Bytes);

				Map reloaded = new(null, new FilePicResourceProvider(tempFile), null, null);
				reloaded.LoadMap("ignored", expected);

				Assert.Equal((ushort)expected, returned);
				Assert.Equal(expected, reloaded.TerrainMasterWord);
			}
			finally
			{
				if (File.Exists(tempFile)) File.Delete(tempFile);
			}
		}

		/// <summary>
		/// Builds a deterministic per-coordinate pattern that exercises every
		/// terrain code handled by <see cref="Map.LoadMap(string,int)"/> and
		/// <see cref="Map.SaveMap(string)"/>.
		/// </summary>
		private static Terrain[,] BuildDeterministicTerrainPattern()
		{
			Terrain[] palette =
			[
				Terrain.Ocean, Terrain.Forest, Terrain.Swamp, Terrain.Plains,
				Terrain.Tundra, Terrain.River, Terrain.Grassland1, Terrain.Jungle,
				Terrain.Hills, Terrain.Mountains, Terrain.Desert, Terrain.Arctic,
			];
			Terrain[,] result = new Terrain[Map.WIDTH, Map.HEIGHT];
			for (int x = 0; x < Map.WIDTH; x++)
			for (int y = 0; y < Map.HEIGHT; y++)
			{
				result[x, y] = palette[(x * 7 + y * 3) % palette.Length];
			}
			return result;
		}

		private static void PopulateTiles(Map target, Terrain[,] terrain, int seed)
		{
			target.InitializeForYamlLoad(Map.WIDTH, Map.HEIGHT, seed);
			for (int x = 0; x < Map.WIDTH; x++)
			for (int y = 0; y < Map.HEIGHT; y++)
			{
				target.SetTileInternal(x, y, CreateTile(terrain[x, y], x, y));
			}
			target.FinalizeYamlLoad();
		}

		private static ITile CreateTile(Terrain type, int x, int y) => type switch
		{
			Terrain.Forest => new Forest(x, y, false),
			Terrain.Swamp => new Swamp(x, y, false),
			Terrain.Plains => new Plains(x, y, false),
			Terrain.Tundra => new Tundra(x, y, false),
			Terrain.River => new River(x, y),
			Terrain.Grassland1 or Terrain.Grassland2 => new Grassland(x, y),
			Terrain.Jungle => new Jungle(x, y, false),
			Terrain.Hills => new Hills(x, y, false),
			Terrain.Mountains => new Mountains(x, y, false),
			Terrain.Desert => new Desert(x, y, false),
			Terrain.Arctic => new Arctic(x, y, false),
			_ => new Ocean(x, y, false),
		};

		/// <summary>
		/// Returns a synthetic SP299-shaped <see cref="Picture"/> sized for the
		/// save/load pipeline (3*WIDTH x 4*HEIGHT). The palette has 256 entries so
		/// <see cref="PicFile"/> can encode it.
		/// </summary>
		private sealed class InMemoryMapResourceProvider : IMapResourceProvider
		{
			public Picture GetPicture(string filename) => new(Map.WIDTH * 3, Map.HEIGHT * 4);
		}

		/// <summary>
		/// Resource provider that parses a saved .MAP file from disk via
		/// <see cref="PicFile"/>, mirroring the way <c>Resources.Instance</c>
		/// resolves picture resources in production.
		/// </summary>
		private sealed class FilePicResourceProvider(string filename) : IMapResourceProvider
		{
			public Picture GetPicture(string _)
			{
				PicFile pf = new(filename);
				return new Picture(pf.GetPicture256, pf.GetPalette256);
			}
		}

		private sealed class CapturingMapPersistenceService : IMapPersistenceService
		{
			public List<(string Filename, byte[] Bytes)> Writes { get; } = new();

			public void WriteAllBytes(string filename, byte[] bytes) => Writes.Add((filename, bytes));
		}
	}
}
