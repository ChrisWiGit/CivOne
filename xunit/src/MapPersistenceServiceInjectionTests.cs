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
using CivOne.Graphics;
using CivOne.Services.Maps;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Locks down the persistence seam introduced in Phase C3. <see cref="Map.SaveMap(string)"/>
	/// must route its byte stream through the injected <see cref="IMapPersistenceService"/>
	/// instead of writing directly to disk via <see cref="File"/>.
	/// </summary>
	public class MapPersistenceServiceInjectionTests : src.TestsBase
	{
		private CapturingMapPersistenceService testee;

		[Fact]
		public void SaveMapRoutesBytesThroughInjectedPersistenceService()
		{
			const string expectedFilename = "TEST.MAP";
			testee = new CapturingMapPersistenceService();
			InMemoryMapResourceProvider resources = new();
			Map.Reset(new Map(null, resources, null, testee));
			Map.Instance.LoadMap("ANY.MAP", 7);

			Map.Instance.SaveMap(expectedFilename);

			Assert.Single(testee.Writes);
			Assert.Equal(expectedFilename, testee.Writes[0].Filename);
			Assert.NotEmpty(testee.Writes[0].Bytes);
		}

		[Fact]
		public void SaveMapReturnsTerrainMasterWord()
		{
			const int expected = 11;
			testee = new CapturingMapPersistenceService();
			InMemoryMapResourceProvider resources = new();
			Map.Reset(new Map(null, resources, null, testee));
			Map.Instance.LoadMap("ANY.MAP", expected);

			ushort actual = Map.Instance.SaveMap("OUT.MAP");

			Assert.Equal((ushort)expected, actual);
		}

		[Fact]
		public void DefaultMapPersistenceServiceWritesBytesToDisk()
		{
			string filename = Path.Combine(Path.GetTempPath(), $"civone-c3-{Path.GetRandomFileName()}.bin");
			byte[] expected = [1, 2, 3, 4, 5];
			DefaultMapPersistenceService testee = new();

			try
			{
				testee.WriteAllBytes(filename, expected);

				byte[] actual = File.ReadAllBytes(filename);
				Assert.Equal(expected, actual);
			}
			finally
			{
				if (File.Exists(filename)) File.Delete(filename);
			}
		}

		private sealed class CapturingMapPersistenceService : IMapPersistenceService
		{
			public List<(string Filename, byte[] Bytes)> Writes { get; } = new();

			public void WriteAllBytes(string filename, byte[] bytes) => Writes.Add((filename, bytes));
		}

		/// <summary>
		/// Returns a fresh all-zero <see cref="Picture"/> sized for the load/save
		/// pipeline (3*WIDTH x 4*HEIGHT). The palette has 256 entries so the
		/// <see cref="Picture(Bytemap, Palette)"/> constructor inside <c>SaveMap</c>
		/// succeeds.
		/// </summary>
		private sealed class InMemoryMapResourceProvider : IMapResourceProvider
		{
			public Picture GetPicture(string filename) => new(Map.WIDTH * 3, Map.HEIGHT * 4);
		}
	}
}
