using System;
using System.IO;
using CivOne.Services.Translation;
using Xunit;

namespace CivOne.UnitTests
{
	public class TranslationFileRepositoryImplTests : IDisposable
	{
		private readonly string _storageDirectory;
		private readonly string _translationDirectory;
		private readonly TranslationFileRepositoryImpl _testee;

		public TranslationFileRepositoryImplTests()
		{
			_storageDirectory = Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"));
			_translationDirectory = Path.Combine(_storageDirectory, "translations");
			Directory.CreateDirectory(_translationDirectory);
			_testee = new TranslationFileRepositoryImpl();
		}

		[Fact]
		public void GetAvailableLanguages_ReturnsOnlyValidCivFiles()
		{
			File.WriteAllText(Path.Combine(_translationDirectory, "civ_german.txt"), "HELLO=Hallo");
			File.WriteAllText(Path.Combine(_translationDirectory, "civ_invalid.txt"), "malformed-line");
			File.WriteAllText(Path.Combine(_translationDirectory, "all.txt"), "HELLO=Hallo");
			File.WriteAllText(Path.Combine(_translationDirectory, "CIV_upper.txt"), "HELLO=Hallo");

			var actual = _testee.GetAvailableLanguages(_storageDirectory);

			Assert.Single(actual);
			Assert.Equal("german", actual[0].Postfix);
		}

		[Fact]
		public void TryLoadTranslations_NormalizesKeyAndUnescapesEqualsPlaceholder()
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
		public void TryLoadTranslations_UnescapesControlCharactersInKeyAndValue()
		{
			string filePath = Path.Combine(_translationDirectory, "civ_german.txt");
			File.WriteAllText(filePath, "FOO\\nBAR=Line1\\nLine2\n");

			bool success = _testee.TryLoadTranslations(filePath, out var translations, out var error);

			Assert.True(success);
			Assert.Null(error);
			Assert.Equal("Line1\nLine2", translations["FOO\nBAR"]);
		}

		[Fact]
		public void TryLoadTranslations_UnescapesUppercaseControlCharactersInKeyAndValue()
		{
			string filePath = Path.Combine(_translationDirectory, "civ_german.txt");
			File.WriteAllText(filePath, "ROME\\NCAESAREA=Rom\\NCaesarea\n");

			bool success = _testee.TryLoadTranslations(filePath, out var translations, out var error);

			Assert.True(success);
			Assert.Null(error);
			Assert.Equal("Rom\nCaesarea", translations["ROME\nCAESAREA"]);
		}

		[Fact]
		public void TryLoadTranslations_WithMalformedLine_ReturnsFalse()
		{
			string filePath = Path.Combine(_translationDirectory, "civ_german.txt");
			File.WriteAllText(filePath, "HELLO=Hallo\nmalformed\n");

			bool success = _testee.TryLoadTranslations(filePath, out _, out var error);

			Assert.False(success);
			Assert.Contains("Malformed line", error, StringComparison.Ordinal);
		}

		[Fact]
		public void SyncFiles_WhenSourceFileUsesUppercaseName_SkipsIt()
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
		public void SyncFiles_WhenSourceContainsLowercaseFile_CopiesIt()
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
		public void SyncFiles_SkipsObsoleteKeysAndAllTxt()
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
			if (Directory.Exists(_storageDirectory))
			{
				Directory.Delete(_storageDirectory, true);
			}
		}
	}
}