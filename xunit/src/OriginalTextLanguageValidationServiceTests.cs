using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CivOne.IO.Text;
using Xunit;

namespace CivOne.UnitTests
{
	public sealed class OriginalTextLanguageValidationServiceTests : IDisposable
	{
		private readonly string _tempDirectory;

		public OriginalTextLanguageValidationServiceTests()
		{
			_tempDirectory = Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_tempDirectory);
		}

		[Fact]
		public void ValidateWhenAllSegmentsMatchReturnsVerified()
		{
			// Arrange
			WriteTextFile("ERROR.TXT", BuildSampleFile(["alpha", "bravo", "charlie", "delta", "echo"]));
			var definitions = CreateSingleFileDefinition(minimumMatches: 3, maximumMismatches: 1);
			var _testee = new OriginalTextLanguageValidationService(definitions);

			// Act
			var actual = _testee.Validate(_tempDirectory);

			// Assert
			Assert.True(actual.IsVerified);
			Assert.Empty(actual.FilesWithMismatches);
			Assert.Single(actual.Files);
			Assert.Equal(5, actual.Files[0].MatchedSegments);
		}

		[Fact]
		public void ValidateWhenSingleMismatchExistsKeepsFileVerified()
		{
			// Arrange
			WriteTextFile("ERROR.TXT", BuildSampleFile(["alpha", "bravo", "charlie", "delta", "different"]));
			var definitions = CreateSingleFileDefinition(minimumMatches: 3, maximumMismatches: 1);
			var _testee = new OriginalTextLanguageValidationService(definitions);

			// Act
			var actual = _testee.Validate(_tempDirectory);

			// Assert
			Assert.True(actual.IsVerified);
			Assert.Empty(actual.FilesWithMismatches);
			Assert.Equal(4, actual.Files[0].MatchedSegments);
			Assert.Equal(1, actual.Files[0].MismatchedSegments);
		}

		[Fact]
		public void ValidateWhenTwoMismatchesExistMarksFileAsNotVerified()
		{
			// Arrange
			WriteTextFile("ERROR.TXT", BuildSampleFile(["alpha", "bravo", "different-one", "delta", "different-two"]));
			var definitions = CreateSingleFileDefinition(minimumMatches: 3, maximumMismatches: 1);
			var _testee = new OriginalTextLanguageValidationService(definitions);

			// Act
			var actual = _testee.Validate(_tempDirectory);

			// Assert
			Assert.False(actual.IsVerified);
			Assert.Single(actual.FilesWithMismatches);
			Assert.Equal("ERROR.TXT", actual.FilesWithMismatches[0].FileName);
			Assert.Equal(2, actual.FilesWithMismatches[0].MismatchedSegments);
		}

		[Fact]
		public void ValidateWhenMultipleFilesAreNotVerifiedReturnsAllInMismatchList()
		{
			// Arrange
			WriteTextFile("ERROR.TXT", BuildSampleFile(["wrong-a", "bravo", "wrong-b", "delta", "echo"]));
			WriteTextFile("HELP.TXT", BuildSampleFile(["one", "wrong-c", "three", "wrong-d", "five"]));
			OriginalTextLanguageValidationFileDefinition[] definitions =
			[
				CreateFileDefinition("ERROR.TXT", ["alpha", "bravo", "charlie", "delta", "echo"], minimumMatches: 3, maximumMismatches: 1),
				CreateFileDefinition("HELP.TXT", ["one", "two", "three", "four", "five"], minimumMatches: 3, maximumMismatches: 1)
			];
			var _testee = new OriginalTextLanguageValidationService(definitions);

			// Act
			var actual = _testee.Validate(_tempDirectory);

			// Assert
			Assert.False(actual.IsVerified);
			Assert.Equal(2, actual.FilesWithMismatches.Count);
			Assert.Contains(actual.FilesWithMismatches, x => x.FileName == "ERROR.TXT");
			Assert.Contains(actual.FilesWithMismatches, x => x.FileName == "HELP.TXT");
		}

		[Fact]
		public void ValidateWhenNotEnoughSegmentsAreReadableReturnsNotVerified()
		{
			// Arrange
			WriteTextFile("ERROR.TXT", "*M1\nalpha\n\n*END\n");
			var definitions = CreateSingleFileDefinition(minimumMatches: 3, maximumMismatches: 1);
			var _testee = new OriginalTextLanguageValidationService(definitions);

			// Act
			var actual = _testee.Validate(_tempDirectory);

			// Assert
			Assert.False(actual.IsVerified);
			Assert.Single(actual.FilesWithMismatches);
			Assert.Equal(1, actual.FilesWithMismatches[0].ReadableSegments);
		}

		[Fact]
		public void ValidateWhenDataDirectoryIsWhiteSpaceThrowsArgumentException()
		{
			// Arrange
			var definitions = CreateSingleFileDefinition(minimumMatches: 3, maximumMismatches: 1);
			var _testee = new OriginalTextLanguageValidationService(definitions);

			// Act
			Action action = () => _testee.Validate(" ");

			// Assert
			Assert.Throws<ArgumentException>(action);
		}

		[Fact]
		public void ValidateWhenConfiguredFileIsMissingReturnsNotVerifiedFileResult()
		{
			// Arrange
			var definitions = CreateSingleFileDefinition(minimumMatches: 3, maximumMismatches: 1);
			var _testee = new OriginalTextLanguageValidationService(definitions);

			// Act
			var actual = _testee.Validate(_tempDirectory);

			// Assert
			Assert.False(actual.IsVerified);
			Assert.Single(actual.FilesWithMismatches);
			Assert.Equal("ERROR.TXT", actual.FilesWithMismatches[0].FileName);
			Assert.Equal(0, actual.FilesWithMismatches[0].ReadableSegments);
			Assert.Equal(0, actual.FilesWithMismatches[0].MatchedSegments);
			Assert.Equal(0, actual.FilesWithMismatches[0].MismatchedSegments);
		}

		[Fact]
		public void ValidateWhenByteWindowSegmentMatchesReturnsVerified()
		{
			// Arrange
			byte[] bytes = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06];
			WriteBinaryFile("ERROR.TXT", bytes);
			OriginalTextLanguageValidationFileDefinition[] definitions =
			[
				new(
					FileName: "ERROR.TXT",
					MinimumMatches: 1,
					MaximumMismatches: 0,
					Segments:
					[
						new(
							SegmentId: "BYTE_SEGMENT_1",
							ExpectedSha256: HashBytes(bytes[1..4]),
							Source: OriginalTextLanguageSegmentSource.ByteWindow,
							Offset: 1,
							Length: 3)
					])
			];
			var _testee = new OriginalTextLanguageValidationService(definitions);

			// Act
			var actual = _testee.Validate(_tempDirectory);

			// Assert
			Assert.True(actual.IsVerified);
			Assert.Empty(actual.FilesWithMismatches);
			Assert.Single(actual.Files);
			Assert.Equal(1, actual.Files[0].MatchedSegments);
		}

		private void WriteTextFile(string fileName, string content)
		{
			File.WriteAllText(Path.Combine(_tempDirectory, fileName), content, Encoding.UTF8);
		}

		private void WriteBinaryFile(string fileName, byte[] content)
		{
			File.WriteAllBytes(Path.Combine(_tempDirectory, fileName), content);
		}

		private static OriginalTextLanguageValidationFileDefinition[] CreateSingleFileDefinition(int minimumMatches, int maximumMismatches)
		{
			return
			[
				CreateFileDefinition("ERROR.TXT", ["alpha", "bravo", "charlie", "delta", "echo"], minimumMatches, maximumMismatches)
			];
		}

		private static OriginalTextLanguageValidationFileDefinition CreateFileDefinition(string fileName, string[] values, int minimumMatches, int maximumMismatches)
		{
			return new OriginalTextLanguageValidationFileDefinition(
				FileName: fileName,
				MinimumMatches: minimumMatches,
				MaximumMismatches: maximumMismatches,
				Segments:
				[
					CreateSegment("M1", values[0]),
					CreateSegment("M2", values[1]),
					CreateSegment("M3", values[2]),
					CreateSegment("M4", values[3]),
					CreateSegment("M5", values[4])
				]);
		}

		private static OriginalTextLanguageValidationSegmentDefinition CreateSegment(string marker, string value)
		{
			return new OriginalTextLanguageValidationSegmentDefinition(
				SegmentId: marker,
				ExpectedSha256: Hash(value),
				Source: OriginalTextLanguageSegmentSource.MarkerBodyLine,
				Marker: marker,
				BodyLineIndex: 0);
		}

		private static string BuildSampleFile(string[] markerValues)
		{
			return $"*M1\n{markerValues[0]}\n\n*M2\n{markerValues[1]}\n\n*M3\n{markerValues[2]}\n\n*M4\n{markerValues[3]}\n\n*M5\n{markerValues[4]}\n\n*END\n";
		}

		private static string Hash(string value)
		{
			string normalized = value.Trim().ToUpperInvariant();
			return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
		}

		private static string HashBytes(byte[] value)
		{
			return Convert.ToHexString(SHA256.HashData(value));
		}

		public void Dispose()
		{
			if (Directory.Exists(_tempDirectory))
			{
				Directory.Delete(_tempDirectory, true);
			}
		}
	}
}