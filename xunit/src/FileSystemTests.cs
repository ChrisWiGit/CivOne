using System;
using System.IO;
using CivOne.IO;
using Xunit;

namespace CivOne.UnitTests
{
	public class FileSystemTests : IDisposable
	{
		private readonly string _tempDirectory;
		private readonly string _sourceDirectory;
		private readonly string _targetDirectory;

		public FileSystemTests()
		{
			_tempDirectory = Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"));
			_sourceDirectory = Path.Combine(_tempDirectory, "source");
			_targetDirectory = Path.Combine(_tempDirectory, "target");
			Directory.CreateDirectory(_sourceDirectory);
		}

		[Fact]
		public void CopyFilesWhenSourceFileUsesDifferentCasingCopiesUsingCanonicalTargetName()
		{
			File.WriteAllText(Path.Combine(_sourceDirectory, "fonts.cv"), "font-data");

			bool actual = FileSystem.CopyFiles(_sourceDirectory, _targetDirectory, ["FONTS.CV"], out string? missingFile);

			Assert.True(actual);
			Assert.Null(missingFile);
			Assert.Equal("font-data", File.ReadAllText(Path.Combine(_targetDirectory, "FONTS.CV")));
		}

		[Fact]
		public void CopyFilesWhenFileIsMissingReturnsMissingCanonicalFileName()
		{
			bool actual = FileSystem.CopyFiles(_sourceDirectory, _targetDirectory, ["FONTS.CV"], out string? missingFile);

			Assert.False(actual);
			Assert.Equal("FONTS.CV", missingFile);
		}

		public void Dispose()
		{
			if (Directory.Exists(_tempDirectory))
			{
				Directory.Delete(_tempDirectory, true);
			}
			GC.SuppressFinalize(this);
		}
	}
}