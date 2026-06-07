using System;
using System.Collections.Generic;
using System.IO;
using CivOne.Services;
using CivOne.Services.Translation;
using Xunit;

namespace CivOne.UnitTests
{
	public class TranslationServiceFactoryTests : IDisposable
	{
		private readonly string _storageDirectory;
		private readonly string _translationDirectory;
		private bool _disposed;

		public TranslationServiceFactoryTests()
		{
			_storageDirectory = Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"));
			_translationDirectory = Path.Combine(_storageDirectory, "translations");
			Directory.CreateDirectory(_translationDirectory);
			TranslationServiceFactory.UseIdentity();
		}

		[Fact]
		public void TryUseLanguageWhenLanguageFileExistsActivatesFileTranslation()
		{
			File.WriteAllText(Path.Combine(_translationDirectory, "civ_neutraltest.txt"), "HELLO=Hallo");

			bool success = TranslationServiceFactory.TryUseLanguage(_storageDirectory, "neutraltest", out var error);
			var actual = TranslationServiceFactory.CreateDefault().Translate("hello");

			Assert.True(success);
			Assert.Null(error);
			Assert.Equal("Hallo", actual);
			Assert.Equal("neutraltest", TranslationServiceFactory.ActiveLanguagePostfix);
		}

		[Fact]
		public void TryUseLanguageWhenTranslationFileHasUppercaseNameIsIgnored()
		{
			File.WriteAllText(Path.Combine(_translationDirectory, "CIV_GERMAN.TXT"), "HELLO=Hallo");

			bool success = TranslationServiceFactory.TryUseLanguage(_storageDirectory, "german", out var error);

			Assert.False(success);
			Assert.NotNull(error);
		}

		[Fact]
		public void TryUseLanguageWhenSelectedAgainReloadsFileFromDisk()
		{
			string filePath = Path.Combine(_translationDirectory, "civ_neutraltest.txt");
			File.WriteAllText(filePath, "HELLO=Hallo1");

			bool firstSuccess = TranslationServiceFactory.TryUseLanguage(_storageDirectory, "neutraltest", out _);
			string firstTranslation = TranslationServiceFactory.CreateDefault().Translate("HELLO");

			File.WriteAllText(filePath, "HELLO=Hallo2");
			bool secondSuccess = TranslationServiceFactory.TryUseLanguage(_storageDirectory, "neutraltest", out _);
			string secondTranslation = TranslationServiceFactory.CreateDefault().Translate("HELLO");

			Assert.True(firstSuccess);
			Assert.True(secondSuccess);
			Assert.Equal("Hallo1", firstTranslation);
			Assert.Equal("Hallo2", secondTranslation);
		}

		[Fact]
		public void TryUseLanguageWhenLanguageMissingLeavesCurrentLanguageUntouched()
		{
			File.WriteAllText(Path.Combine(_translationDirectory, "civ_neutraltest.txt"), "HELLO=Hallo");
			bool initialSuccess = TranslationServiceFactory.TryUseLanguage(_storageDirectory, "neutraltest", out _);

			bool missingSuccess = TranslationServiceFactory.TryUseLanguage(_storageDirectory, "missing", out var error);
			string translationAfterFailure = TranslationServiceFactory.CreateDefault().Translate("HELLO");

			Assert.True(initialSuccess);
			Assert.False(missingSuccess);
			Assert.Contains("not found", error, StringComparison.Ordinal);
			Assert.Equal("Hallo", translationAfterFailure);
			Assert.Equal("neutraltest", TranslationServiceFactory.ActiveLanguagePostfix);
		}

		[Fact]
		public void TryUseLanguageWhenLanguageChangesNotifiesRegisteredObserver()
		{
			File.WriteAllText(Path.Combine(_translationDirectory, "civ_neutraltest.txt"), "HELLO=Hallo");
			var observer = new TestTranslationLanguageObserver();

			TranslationServiceFactory.RegisterLanguageObserver(observer);
			bool success = TranslationServiceFactory.TryUseLanguage(_storageDirectory, "neutraltest", out _);
			TranslationServiceFactory.UnregisterLanguageObserver(observer);

			Assert.True(success);
			Assert.Single(observer.Notifications);
			Assert.Equal("neutraltest", observer.Notifications[0]);
		}

		[Fact]
		public void UnregisterLanguageObserverWhenCalledStopsFurtherNotifications()
		{
			File.WriteAllText(Path.Combine(_translationDirectory, "civ_neutraltest.txt"), "HELLO=Hallo");
			var observer = new TestTranslationLanguageObserver();

			TranslationServiceFactory.RegisterLanguageObserver(observer);
			bool firstSuccess = TranslationServiceFactory.TryUseLanguage(_storageDirectory, "neutraltest", out _);
			TranslationServiceFactory.UnregisterLanguageObserver(observer);
			TranslationServiceFactory.UseIdentity();

			Assert.True(firstSuccess);
			Assert.Single(observer.Notifications);
			Assert.Equal("neutraltest", observer.Notifications[0]);
		}

		[Fact]
		public void GetLanguageDisplayNamePrefersDisplayNameFromLanguageFile()
		{
			TranslationLanguageInfo language = new("german", "civ_german.txt", "Deutsch");

			string actual = TranslationServiceFactory.GetLanguageDisplayName(language, _ => "German");

			Assert.Equal("Deutsch", actual);
		}

		[Fact]
		public void GetLanguageDisplayNameUsesTranslatorWhenDisplayNameMissing()
		{
			TranslationLanguageInfo language = new("german", "civ_german.txt");

			string actual = TranslationServiceFactory.GetLanguageDisplayName(language, _ => "German");

			Assert.Equal("German", actual);
		}

		[Fact]
		public void GetLanguageDisplayNameWhenTranslatorReturnsEmptyUsesPostfix()
		{
			TranslationLanguageInfo language = new("german", "civ_german.txt");

			string actual = TranslationServiceFactory.GetLanguageDisplayName(language, _ => string.Empty);

			Assert.Equal("german", actual);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
			{
				return;
			}

			if (disposing)
			{
				TranslationServiceFactory.UseIdentity();
				if (Directory.Exists(_storageDirectory))
				{
					Directory.Delete(_storageDirectory, true);
				}
			}

			_disposed = true;
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		~TranslationServiceFactoryTests()
		{
			Dispose(disposing: false);
		}

		private sealed class TestTranslationLanguageObserver : ITranslationLanguageObserver
		{
			public List<string> Notifications { get; } = [];

			public void OnLanguageChanged(string activeLanguagePostfix)
			{
				Notifications.Add(activeLanguagePostfix);
			}
		}
	}
}