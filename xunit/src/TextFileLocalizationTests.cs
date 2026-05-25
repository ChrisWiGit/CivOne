using System.Collections.Generic;
using CivOne.IO.Text;
using Xunit;

namespace CivOne.UnitTests
{
	public class TextFileLocalizationTests
	{
		private sealed class StubLoader(Dictionary<string, string[]> map) : ITextFileLoader
		{
			private readonly Dictionary<string, string[]> _map = map;

			public string[] LoadArray(string filename)
				=> _map.TryGetValue(filename, out var lines) ? lines : [];
		}

		private static TextFile CreateTextFile(ITextFileLoader loader)
		{
			var textFile = new TextFile(loader);
			textFile.Reset();
			return textFile;
		}

		[Fact]
		public void GetGameText_WhenLoaderReturnsUmlautLines_PreservesInternationalCharacters()
		{
			// Arrange
			var loader = new StubLoader(new Dictionary<string, string[]>
			{
				["ERROR"] = ["*DEMOCRACY", "Angriff auf verbündete Einheit nicht erlaubt.", ""]
			});

			// Act
			var _testee = CreateTextFile(loader);
			var actual = _testee.GetGameText("ERROR/DEMOCRACY");

			// Assert
			Assert.Single(actual);
			Assert.Equal("Angriff auf verbündete Einheit nicht erlaubt.", actual[0]);
		}

		[Fact]
		public void GetGameText_WhenLoaderReturnsEmptyForMissingFile_ReturnsEmpty()
		{
			// Arrange – loader returns nothing (simulates "localized file not found, data file also missing")
			var loader = new StubLoader(new Dictionary<string, string[]>());

			// Act
			var _testee = CreateTextFile(loader);
			var actual = _testee.GetGameText("ERROR/ZOC");

			// Assert
			Assert.Empty(actual);
		}

		[Fact]
		public void OnLanguageChanged_WhenTriggered_ReloadsFromLoader()
		{
			// Arrange – first load returns English content
			var loaderMap = new Dictionary<string, string[]>
			{
				["ERROR"] = ["*DEMO", "English text.", ""]
			};
			var loader = new StubLoader(loaderMap);
			var _testee = CreateTextFile(loader);

			// Act – simulate language change (update loader map, then notify)
			loaderMap["ERROR"] = ["*DEMO", "Deutscher Text.", ""];
			_testee.OnLanguageChanged("german");
			var actual = _testee.GetGameText("ERROR/DEMO");

			// Assert
			Assert.Single(actual);
			Assert.Equal("Deutscher Text.", actual[0]);
		}

		[Fact]
		public void Reset_WhenCalledTwice_UsesLatestLoaderData()
		{
			// Arrange
			var loaderMap = new Dictionary<string, string[]>
			{
				["HELP"] = ["*FIRSTMOVE", "First line.", ""]
			};
			var loader = new StubLoader(loaderMap);
			var _testee = CreateTextFile(loader);

			// Act
			loaderMap["HELP"] = ["*FIRSTMOVE", "Updated line.", ""];
			_testee.Reset();
			var actual = _testee.GetGameText("HELP/FIRSTMOVE");

			// Assert
			Assert.Single(actual);
			Assert.Equal("Updated line.", actual[0]);
		}
	}
}
