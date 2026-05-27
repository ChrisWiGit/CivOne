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
		private readonly TranslationFileRepositoryImpl _testee;
		private bool _disposed;

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