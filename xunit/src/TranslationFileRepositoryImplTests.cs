using System;
using System.IO;
using CivOne.Services.Translation;
using Xunit;

namespace CivOne.UnitTests
{
	public sealed class TranslationFileRepositoryImplTests : IDisposable
	{
		private readonly string _storageDirectory;
		private readonly string _translationDirectory;
		private readonly TranslationFileRepository _testee;
		private bool _disposed;

		public TranslationFileRepositoryImplTests()
		{
			_storageDirectory = Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"));
			_translationDirectory = Path.Combine(_storageDirectory, "translations");
			Directory.CreateDirectory(_translationDirectory);
			_testee = new TranslationFileRepository();
		}

		[Fact]
		public void GetAvailableLanguagesReturnsOnlyValidCivFiles()
		{
			File.WriteAllText(Path.Combine(_translationDirectory, "civ_german.txt"), "__LANGUAGE_DISPLAYNAME__=Deutsch\nGERMAN=GermanLegacy\nHELLO=Hallo");
			File.WriteAllText(Path.Combine(_translationDirectory, "civ_invalid.txt"), "malformed-line");
			File.WriteAllText(Path.Combine(_translationDirectory, "all.txt"), "HELLO=Hallo");
			File.WriteAllText(Path.Combine(_translationDirectory, "CIV_upper.txt"), "HELLO=Hallo");

			var actual = _testee.GetAvailableLanguages(_storageDirectory);

			Assert.Single(actual);
			Assert.Equal("german", actual[0].Postfix);
			Assert.Equal("Deutsch", actual[0].DisplayName);
		}

		[Fact]
		public void GetAvailableLanguagesWhenMetaDisplayNameMissingFallsBackToLegacyPostfixKey()
		{
			File.WriteAllText(Path.Combine(_translationDirectory, "civ_german.txt"), "GERMAN=DeutschLegacy");

			var actual = _testee.GetAvailableLanguages(_storageDirectory);

			Assert.Single(actual);
			Assert.Equal("german", actual[0].Postfix);
			Assert.Equal("DeutschLegacy", actual[0].DisplayName);
		}

		[Fact]
		public void GetAvailableLanguagesWhenSelfNameKeyMissingLeavesDisplayNameEmpty()
		{
			File.WriteAllText(Path.Combine(_translationDirectory, "civ_french.txt"), "HELLO=Bonjour");

			var actual = _testee.GetAvailableLanguages(_storageDirectory);

			Assert.Single(actual);
			Assert.Equal("french", actual[0].Postfix);
			Assert.Null(actual[0].DisplayName);
		}

		[Fact]
		public void GetAvailableLanguagesWhenSelfNameKeyWhitespaceLeavesDisplayNameEmpty()
		{
			File.WriteAllText(Path.Combine(_translationDirectory, "civ_french.txt"), "FRENCH=   \nHELLO=Bonjour");

			var actual = _testee.GetAvailableLanguages(_storageDirectory);

			Assert.Single(actual);
			Assert.Equal("french", actual[0].Postfix);
			Assert.Null(actual[0].DisplayName);
		}

		[Fact]
		public void TryLoadTranslationsNormalizesKeyAndUnescapesEqualsPlaceholder()
		{
			string filePath = Path.Combine(_translationDirectory, "civ_german.txt");
			File.WriteAllText(filePath, "quick save/load=Quick Save/Load\nA[EQ]B=X[EQ]Y\n");

			bool success = _testee.TryLoadTranslations(filePath, out var translations, out var error);

			Assert.True(success);
			Assert.Null(error);
			Assert.Equal("Quick Save/Load", translations["QUICK SAVE/LOAD"]);
			Assert.Equal("X=Y", translations["A=B"]);
		}

		[Fact]
		public void TryLoadTranslationsUnescapesControlCharactersInKeyAndValue()
		{
			string filePath = Path.Combine(_translationDirectory, "civ_german.txt");
			File.WriteAllText(filePath, "FOO\\nBAR=Line1\\nLine2\n");

			bool success = _testee.TryLoadTranslations(filePath, out var translations, out var error);

			Assert.True(success);
			Assert.Null(error);
			Assert.Equal("Line1\nLine2", translations["FOO\nBAR"]);
		}

		[Fact]
		public void TryLoadTranslationsUnescapesUppercaseControlCharactersInKeyAndValue()
		{
			string filePath = Path.Combine(_translationDirectory, "civ_german.txt");
			File.WriteAllText(filePath, "ROME\\NCAESAREA=Rom\\NCaesarea\n");

			bool success = _testee.TryLoadTranslations(filePath, out var translations, out var error);

			Assert.True(success);
			Assert.Null(error);
			Assert.Equal("Rom\nCaesarea", translations["ROME\nCAESAREA"]);
		}

		[Fact]
		public void TryLoadTranslationsWithMalformedLineReturnsFalse()
		{
			string filePath = Path.Combine(_translationDirectory, "civ_german.txt");
			File.WriteAllText(filePath, "HELLO=Hallo\nmalformed\n");

			bool success = _testee.TryLoadTranslations(filePath, out _, out var error);

			Assert.False(success);
			Assert.Contains("Malformed line", error, StringComparison.Ordinal);
		}

		[Fact]
		public void SyncFilesWhenSourceFileUsesUppercaseNameSkipsIt()
		{
			string sourceDirectory = Path.Combine(_storageDirectory, "source");
			string targetDirectory = Path.Combine(_storageDirectory, "target");
			Directory.CreateDirectory(sourceDirectory);
			File.WriteAllText(Path.Combine(sourceDirectory, "CIV_GERMAN.TXT"), "HELLO=Hallo");

			int actual = _testee.SyncFiles(sourceDirectory, targetDirectory);

			Assert.Equal(0, actual);
			Assert.False(File.Exists(Path.Combine(targetDirectory, "civ_german.txt")));
			Assert.False(File.Exists(Path.Combine(targetDirectory, "CIV_GERMAN.TXT")));
		}

		[Fact]
		public void SyncFilesWhenSourceContainsLowercaseFileCopiesIt()
		{
			string sourceDirectory = Path.Combine(_storageDirectory, "source");
			string targetDirectory = Path.Combine(_storageDirectory, "target");
			Directory.CreateDirectory(sourceDirectory);
			File.WriteAllText(Path.Combine(sourceDirectory, "civ_german.txt"), "HELLO=Hallo");

			int actual = _testee.SyncFiles(sourceDirectory, targetDirectory);

			Assert.Equal(1, actual);
			Assert.Equal("HELLO=Hallo", File.ReadAllText(Path.Combine(targetDirectory, "civ_german.txt")));
		}

		[Fact]
		public void SyncFilesSkipsObsoleteKeysAndAllTxt()
		{
			string sourceDirectory = Path.Combine(_storageDirectory, "source");
			string targetDirectory = Path.Combine(_storageDirectory, "target");
			Directory.CreateDirectory(sourceDirectory);
			File.WriteAllText(Path.Combine(sourceDirectory, "civ_german.txt"), "HELLO=Hallo");
			File.WriteAllText(Path.Combine(sourceDirectory, "all.txt"), "X=Y");
			File.WriteAllText(Path.Combine(sourceDirectory, "obsoletekeys.txt"), "OLD=Y");

			int actual = _testee.SyncFiles(sourceDirectory, targetDirectory);

			Assert.Equal(1, actual);
			Assert.True(File.Exists(Path.Combine(targetDirectory, "civ_german.txt")));
			Assert.False(File.Exists(Path.Combine(targetDirectory, "all.txt")));
			Assert.False(File.Exists(Path.Combine(targetDirectory, "obsoletekeys.txt")));
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (_disposed) return;
			_disposed = true;

			if (disposing && Directory.Exists(_storageDirectory))
			{
				Directory.Delete(_storageDirectory, true);
			}
		}

		~TranslationFileRepositoryImplTests() => Dispose(false);
	}
}