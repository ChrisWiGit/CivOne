using System;
using System.Collections.Generic;
using System.IO;
using CivTranslateInteractive;
using Xunit;

#nullable enable

namespace CivOne.UnitTests
{
	public sealed class CivTranslateInteractiveWorkflowTests : IDisposable
	{
		private readonly string _tempDirectory;
		private readonly string _translationFilePath;
		private readonly ITranslationDocumentRepository _translationDocumentRepository;
		private readonly IValuesFileRepository _valuesFileRepository;

		public CivTranslateInteractiveWorkflowTests()
		{
			_tempDirectory = Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_tempDirectory);
			_translationFilePath = Path.Combine(_tempDirectory, "civ_german.txt");
			_translationDocumentRepository = new TranslationDocumentRepository();
			_valuesFileRepository = new ValuesFileRepository();
		}

		[Fact]
		public void RunWithExistingValuesFileReturnsErrorAndKeepsTranslationFile()
		{
			// Arrange
			string originalContent = "# Header\nHELLO=Hallo\nBYE=Tschuess\n";
			File.WriteAllText(_translationFilePath, originalContent);
			string valuesFilePath = TranslationRoundtripWorkflow.BuildValuesFilePath(_translationFilePath);
			File.WriteAllText(valuesFilePath, "already-there");
			TestInteractiveConsole console = new();
			TranslationRoundtripWorkflow testee = new(console, _translationDocumentRepository, _valuesFileRepository);

			// Act
			int actual = testee.Run(_translationFilePath);

			// Assert
			Assert.Equal(2, actual);
			Assert.Contains("already exists", string.Join('\n', console.Output), StringComparison.OrdinalIgnoreCase);
			Assert.Equal(NormalizeLineEndings(originalContent), NormalizeLineEndings(File.ReadAllText(_translationFilePath)));
		}

		[Fact]
		public void RunWithTranslatedValuesWritesValuesBackAndPreservesStructure()
		{
			// Arrange
			File.WriteAllText(_translationFilePath, "# Header\n\nHELLO=Hallo\nA[EQ]B=X[EQ]Y\n");
			string valuesFilePath = TranslationRoundtripWorkflow.BuildValuesFilePath(_translationFilePath);
			TestInteractiveConsole console = new(
				onReadLine: () =>
				{
					File.WriteAllLines(valuesFilePath, ["Hello there", "XX=YY"]);
					return string.Empty;
				});
			TranslationRoundtripWorkflow testee = new(console, _translationDocumentRepository, _valuesFileRepository);

			// Act
			int actual = testee.Run(_translationFilePath);

			// Assert
			Assert.Equal(0, actual);
			string[] actualLines = File.ReadAllLines(_translationFilePath);
			Assert.Equal("# Header", actualLines[0]);
			Assert.Equal(string.Empty, actualLines[1]);
			Assert.Equal("HELLO=Hello there", actualLines[2]);
			Assert.Equal("A[EQ]B=XX[EQ]YY", actualLines[3]);
		}

		[Fact]
		public void RunWithMismatchedTranslatedValuesCountReturnsErrorAndKeepsOriginalFile()
		{
			// Arrange
			string originalContent = "HELLO=Hallo\nBYE=Tschuess\n";
			File.WriteAllText(_translationFilePath, originalContent);
			string valuesFilePath = TranslationRoundtripWorkflow.BuildValuesFilePath(_translationFilePath);
			TestInteractiveConsole console = new(
				onReadLine: () =>
				{
					File.WriteAllLines(valuesFilePath, ["Only one line"]);
					return string.Empty;
				});
			TranslationRoundtripWorkflow testee = new(console, _translationDocumentRepository, _valuesFileRepository);

			// Act
			int actual = testee.Run(_translationFilePath);

			// Assert
			Assert.Equal(3, actual);
			Assert.Contains("Value count mismatch", string.Join('\n', console.Output), StringComparison.OrdinalIgnoreCase);
			Assert.Equal(NormalizeLineEndings(originalContent), NormalizeLineEndings(File.ReadAllText(_translationFilePath)));
		}

		public void Dispose()
		{
			if (Directory.Exists(_tempDirectory))
			{
				Directory.Delete(_tempDirectory, true);
			}
		}

		private sealed class TestInteractiveConsole(Func<string?>? onReadLine = null) : IInteractiveConsole
		{
			private readonly Func<string?> _onReadLine = onReadLine ?? (() => string.Empty);

			public List<string> Output { get; } = [];

			public void WriteLine(string message)
			{
				Output.Add(message);
			}

			public string? ReadLine() => _onReadLine();
		}

		private static string NormalizeLineEndings(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal);
	}
}
