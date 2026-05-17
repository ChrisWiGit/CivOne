using System;
using System.IO;
using CivOne.Services;
using Xunit;

namespace CivOne.UnitTests
{
	public class TranslationServiceFactoryTests : IDisposable
	{
		private readonly string _storageDirectory;
		private readonly string _translationDirectory;

		public TranslationServiceFactoryTests()
		{
			_storageDirectory = Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"));
			_translationDirectory = Path.Combine(_storageDirectory, "translations");
			Directory.CreateDirectory(_translationDirectory);
			TranslationServiceFactory.UseIdentity();
		}

		[Fact]
		public void TryUseLanguage_WhenLanguageFileExists_ActivatesFileTranslation()
		{
			File.WriteAllText(Path.Combine(_translationDirectory, "civ_german.txt"), "HELLO=Hallo");

			bool success = TranslationServiceFactory.TryUseLanguage(_storageDirectory, "german", out var error);
			var actual = TranslationServiceFactory.CreateDefault().Translate("hello");

			Assert.True(success);
			Assert.Null(error);
			Assert.Equal("Hallo", actual);
			Assert.Equal("german", TranslationServiceFactory.ActiveLanguagePostfix);
		}

		[Fact]
		public void TryUseLanguage_WhenSelectedAgain_ReloadsFileFromDisk()
		{
			string filePath = Path.Combine(_translationDirectory, "civ_german.txt");
			File.WriteAllText(filePath, "HELLO=Hallo1");

			bool firstSuccess = TranslationServiceFactory.TryUseLanguage(_storageDirectory, "german", out _);
			string firstTranslation = TranslationServiceFactory.CreateDefault().Translate("HELLO");

			File.WriteAllText(filePath, "HELLO=Hallo2");
			bool secondSuccess = TranslationServiceFactory.TryUseLanguage(_storageDirectory, "german", out _);
			string secondTranslation = TranslationServiceFactory.CreateDefault().Translate("HELLO");

			Assert.True(firstSuccess);
			Assert.True(secondSuccess);
			Assert.Equal("Hallo1", firstTranslation);
			Assert.Equal("Hallo2", secondTranslation);
		}

		[Fact]
		public void TryUseLanguage_WhenLanguageMissing_LeavesCurrentLanguageUntouched()
		{
			File.WriteAllText(Path.Combine(_translationDirectory, "civ_german.txt"), "HELLO=Hallo");
			bool initialSuccess = TranslationServiceFactory.TryUseLanguage(_storageDirectory, "german", out _);

			bool missingSuccess = TranslationServiceFactory.TryUseLanguage(_storageDirectory, "missing", out var error);
			string translationAfterFailure = TranslationServiceFactory.CreateDefault().Translate("HELLO");

			Assert.True(initialSuccess);
			Assert.False(missingSuccess);
			Assert.Contains("not found", error, StringComparison.Ordinal);
			Assert.Equal("Hallo", translationAfterFailure);
			Assert.Equal("german", TranslationServiceFactory.ActiveLanguagePostfix);
		}

		public void Dispose()
		{
			TranslationServiceFactory.UseIdentity();
			if (Directory.Exists(_storageDirectory))
			{
				Directory.Delete(_storageDirectory, true);
			}
		}
	}
}