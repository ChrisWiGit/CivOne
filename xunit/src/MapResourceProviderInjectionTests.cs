// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Linq;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Services.Maps;
using CivOne.Tiles;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Proves that <see cref="Map"/> loads picture resources via the injected
	/// <see cref="IMapResourceProvider"/> rather than the global <c>Resources</c>
	/// singleton. Locks down the resource seam introduced in Phase C1.
	///
	/// The fixture supplies an all-zero bitmap of the required dimensions
	/// (3 * <see cref="Map.WIDTH"/>, 4 * <see cref="Map.HEIGHT"/>) so every tile
	/// resolves to <see cref="Terrain.Ocean"/> after <see cref="Map.LoadMap(string,int)"/>.
	/// </summary>
	public class MapResourceProviderInjectionTests : src.TestsBase
	{
		private CapturingMapResourceProvider testee = null!;

		[Fact]
		public void ConstructorWithProviderDoesNotInvokeIt()
		{
			testee = new CapturingMapResourceProvider();

			Map actual = new(null, testee);

			Assert.Empty(testee.RequestedFilenames);
			Assert.NotNull(actual);
		}

		[Fact]
		public void LoadMapFetchesPictureFromInjectedProvider()
		{
			const string expected = "TEST.MAP";
			testee = new CapturingMapResourceProvider();
			Map.Reset(new Map(null, testee));

			Map.Instance.LoadMap(expected, 5);

			Assert.Contains(expected, testee.RequestedFilenames);
		}

		[Fact]
		public void LoadMapWithEmptyBitmapProducesAllOceanTiles()
		{
			testee = new CapturingMapResourceProvider();
			Map.Reset(new Map(null, testee));

			Map.Instance.LoadMap("ANY.MAP", 0);

			Assert.All(Map.Instance.AllTiles(), t => Assert.Equal(Terrain.Ocean, t.Type));
		}

		[Fact]
		public void LoadMapSetsTerrainMasterWordToProvidedSeed()
		{
			const int expected = 9;
			testee = new CapturingMapResourceProvider();
			Map.Reset(new Map(null, testee));

			Map.Instance.LoadMap("ANY.MAP", expected);

			Assert.Equal(expected, Map.Instance.TerrainMasterWord);
		}

		/// <summary>
		/// Stub provider that records every requested filename and returns a fresh
		/// all-zero <see cref="Picture"/> sized to satisfy <see cref="Map.LoadMap(string,int)"/>:
		/// width = 3 * <see cref="Map.WIDTH"/> (terrain + improvement + huts columns),
		/// height = 4 * <see cref="Map.HEIGHT"/> (terrain + segmentation + 2 improvement rows).
		/// </summary>
		private sealed class CapturingMapResourceProvider : IMapResourceProvider
		{
			public List<string> RequestedFilenames { get; } = new();

			public Picture GetPicture(string filename)
			{
				RequestedFilenames.Add(filename);
				return new Picture(Map.WIDTH * 3, Map.HEIGHT * 4);
			}
		}
	}
}
