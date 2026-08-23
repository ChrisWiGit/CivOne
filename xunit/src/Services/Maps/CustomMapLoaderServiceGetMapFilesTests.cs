using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Tests for <see cref="CustomMapLoaderService.GetMapFiles"/>, the map directory scan that feeds
	/// the map list of the load map screen.
	/// Uses a temporary directory and a fake <see cref="ISettings"/>, so no game engine is involved.
	/// </summary>
	public sealed class CustomMapLoaderServiceGetMapFilesTests : IDisposable
	{
		private readonly string _mapsDirectory = Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"), "maps");

		private CustomMapLoaderService CreateTestee() => new(new FakeSettings(_mapsDirectory));

		private void WriteMapFile(string fileName)
		{
			Directory.CreateDirectory(_mapsDirectory);
			File.WriteAllText(Path.Combine(_mapsDirectory, fileName), string.Empty);
		}

		[Fact]
		public void GetMapFilesReturnsEmptyListWhenDirectoryDoesNotExist()
		{
			IReadOnlyList<string> actual = CreateTestee().GetMapFiles();

			Assert.Empty(actual);
		}

		[Fact]
		public void GetMapFilesReturnsComapAndLegacyMapFilesOnly()
		{
			WriteMapFile("alpha.comap");
			WriteMapFile("beta.map");
			WriteMapFile("gamma.cos");
			WriteMapFile("delta.txt");

			List<string> actual = [.. CreateTestee().GetMapFiles().Select(path => Path.GetFileName(path))];

			Assert.Equal(new List<string> { "alpha.comap", "beta.map" }, actual);
		}

		[Fact]
		public void GetMapFilesReturnsFilesSortedByName()
		{
			WriteMapFile("zulu.comap");
			WriteMapFile("alpha.map");
			WriteMapFile("mike.comap");

			List<string> actual = [.. CreateTestee().GetMapFiles().Select(path => Path.GetFileName(path))];

			Assert.Equal(new List<string> { "alpha.map", "mike.comap", "zulu.comap" }, actual);
		}

		[Fact]
		public void GetMapFilesIgnoresExtensionWhenComparingPrefixedNames()
		{
			// The '.' before the extension must not outrank the ' ' in "Earth Standard" —
			// "earth" is a prefix of "Earth Standard" once extensions are stripped, so it sorts first.
			WriteMapFile("Earth Standard.comap");
			WriteMapFile("earth.map");

			List<string> actual = [.. CreateTestee().GetMapFiles().Select(path => Path.GetFileName(path))];

			Assert.Equal(new List<string> { "earth.map", "Earth Standard.comap" }, actual);
		}

		[Fact]
		public void GetMapFilesSortsEmbeddedNumbersNumerically()
		{
			WriteMapFile("map10.comap");
			WriteMapFile("map2.comap");
			WriteMapFile("map1.comap");
			WriteMapFile("map0.comap");
			WriteMapFile("map99.comap");

			List<string> actual = [.. CreateTestee().GetMapFiles().Select(path => Path.GetFileName(path))];

			Assert.Equal(
				new List<string> { "map0.comap", "map1.comap", "map2.comap", "map10.comap", "map99.comap" },
				actual);
		}

		[Fact]
		public void GetMapFilesIgnoresSubDirectories()
		{
			WriteMapFile("alpha.comap");
			string subDirectory = Path.Combine(_mapsDirectory, "sub");
			Directory.CreateDirectory(subDirectory);
			File.WriteAllText(Path.Combine(subDirectory, "beta.comap"), string.Empty);

			List<string> actual = [.. CreateTestee().GetMapFiles().Select(path => Path.GetFileName(path))];

			Assert.Equal(new List<string> { "alpha.comap" }, actual);
		}

		public void Dispose()
		{
			string? root = Path.GetDirectoryName(_mapsDirectory);
			if (root != null && Directory.Exists(root))
			{
				Directory.Delete(root, recursive: true);
			}
		}

		private sealed class FakeSettings(string mapsDirectory) : ISettings
		{
			public string MapsDirectory { get; } = mapsDirectory;
			public string PicturesDirectory => throw new NotImplementedException();
			public string SavesDirectory => throw new NotImplementedException();
			public string CosSavesDirectory => throw new NotImplementedException();
			public string StorageDirectory => throw new NotImplementedException();
			public string CaptureDirectory => throw new NotImplementedException();
			public string DataDirectory => throw new NotImplementedException();
			public string PluginsDirectory => throw new NotImplementedException();
			public string SoundsDirectory => throw new NotImplementedException();
			public bool RevealWorld => throw new NotImplementedException();
		}
	}
}
