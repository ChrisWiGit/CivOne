using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CivOne.IO.Text;
using CivOne.Services;
using Xunit;

namespace CivOne.UnitTests
{
	public sealed class TextFileLoaderTests : IDisposable
	{
		private readonly MockRuntime _runtime;
		private readonly List<string> _createdFiles = [];

		public TextFileLoaderTests()
		{
			_runtime = new MockRuntime(new RuntimeSettings());
			TranslationServiceFactory.ResetForTests();
			TranslationServiceFactory.UseIdentity();
			Settings.Instance.LanguagePostfix = string.Empty;
		}

		[Fact]
		public void LoadArrayWhenOriginalFallbackContainsCp437UmlautDecodesUmlaut()
		{
			// Arrange
			const string fileName = "ZZZ_CP437.TXT";
			string path = CreateDataFilePath(fileName);
			byte[] bytes =
			[
				.. Encoding.ASCII.GetBytes("*DEMO\r\nStandort f"),
				0x81,
				.. Encoding.ASCII.GetBytes("r Hauptstadt finden.\r\n\r\n")
			];
			File.WriteAllBytes(path, bytes);

			var _testee = new TextFileLoader();

			// Act
			string[] actual = _testee.LoadArray("ZZZ_CP437");

			// Assert
			Assert.Contains("Standort für Hauptstadt finden.", actual);
		}

		[Fact]
		public void LoadArrayWhenOriginalFallbackContainsValidUtf8KeepsUtf8Text()
		{
			// Arrange
			const string fileName = "ZZZ_UTF8.TXT";
			string path = CreateDataFilePath(fileName);
			File.WriteAllText(path, "*DEMO\nÜbergröße\n\n", Encoding.UTF8);

			var _testee = new TextFileLoader();

			// Act
			string[] actual = _testee.LoadArray("ZZZ_UTF8");

			// Assert
			Assert.Contains("Übergröße", actual);
		}

		[Fact]
		public void LoadArrayWhenLocalizedFileExistsUsesLocalizedUtf8File()
		{
			// Arrange
			const string postfix = "zztest";
			const string logicalName = "ZZZ_LOCALIZED";
			string translationDirectory = Path.Combine(RuntimeHandler.Runtime.StorageDirectory, "translations");
			Directory.CreateDirectory(translationDirectory);

			string languageFile = RegisterTrackedFile(Path.Combine(translationDirectory, $"civ_{postfix}.txt"));
			File.WriteAllText(languageFile, "__LANGUAGE_DISPLAYNAME__=Test\n", Encoding.UTF8);

			bool languageActivated = TranslationServiceFactory.TryUseLanguage(RuntimeHandler.Runtime.StorageDirectory, postfix, out string? errorMessage);
			Assert.True(languageActivated, errorMessage);
			Settings.Instance.LanguagePostfix = postfix;

			string localizedFile = RegisterTrackedFile(Path.Combine(translationDirectory, $"{logicalName}_{postfix}.txt"));
			File.WriteAllText(localizedFile, "*DEMO\nÜbergröße aus Lokalisierung\n\n", Encoding.UTF8);

			string fallbackPath = CreateDataFilePath($"{logicalName}.TXT");
			File.WriteAllText(fallbackPath, "*DEMO\nFallback text\n\n", Encoding.UTF8);

			var _testee = new TextFileLoader();

			// Act
			string[] actual = _testee.LoadArray(logicalName);

			// Assert
			Assert.Contains("Übergröße aus Lokalisierung", actual);
			Assert.DoesNotContain("Fallback text", actual);
		}

		private string CreateDataFilePath(string fileName)
		{
			string path = Path.Combine(Settings.Instance.DataDirectory, fileName);
			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Settings.Instance.DataDirectory);
			return RegisterTrackedFile(path);
		}

		private string RegisterTrackedFile(string path)
		{
			_createdFiles.Add(path);
			return path;
		}

		public void Dispose()
		{
			TranslationServiceFactory.UseIdentity();
			Settings.Instance.LanguagePostfix = string.Empty;

			foreach (string file in _createdFiles)
			{
				if (File.Exists(file))
				{
					File.Delete(file);
				}
			}

			_runtime.Dispose();
			RuntimeHandler.Wipe();
		}
	}
}