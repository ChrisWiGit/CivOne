// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CivOne.Services;
using Xunit;

namespace CivOne.UnitTests
{
	public class AtomicFileReplacementServiceTests
	{
		[Fact]
		public void ReplaceFileWhenDestinationExistsDeletesOldFileAndMovesTempFile()
		{
			string destinationPath = Path.Combine("saves", "cos", "savegame.cos");
			var fileOps = new FakeAtomicFileOperations();
			fileOps.ExistingFiles.Add(destinationPath);
			var testee = new AtomicFileReplacementService(fileOps);

			testee.ReplaceFile(destinationPath, stream =>
			{
				byte[] payload = Encoding.UTF8.GetBytes("test");
				stream.Write(payload, 0, payload.Length);
			});

			Assert.Single(fileOps.CreatedDirectories);
			Assert.Equal(Path.Combine("saves", "cos"), fileOps.CreatedDirectories[0]);
			Assert.Single(fileOps.DeletedFiles);
			Assert.Equal(destinationPath, fileOps.DeletedFiles[0]);
			Assert.Single(fileOps.MoveOperations);
			Assert.Equal(fileOps.OpenedWritePaths[0], fileOps.MoveOperations[0].SourcePath);
			Assert.Equal(destinationPath, fileOps.MoveOperations[0].DestinationPath);
			Assert.StartsWith(Path.Combine("saves", "cos", ".savegame.cos."), fileOps.OpenedWritePaths[0]);
			Assert.EndsWith(".tmp", fileOps.OpenedWritePaths[0]);
		}

		[Fact]
		public void ReplaceFileWhenWriteThrowsDeletesTempFileAndRethrows()
		{
			string destinationPath = Path.Combine("saves", "cos", "savegame.cos");
			var fileOps = new FakeAtomicFileOperations();
			var testee = new AtomicFileReplacementService(fileOps);

			Assert.Throws<InvalidOperationException>(() =>
				testee.ReplaceFile(destinationPath, _ => throw new InvalidOperationException("write failed")));

			Assert.Single(fileOps.OpenedWritePaths);
			Assert.Single(fileOps.DeletedFiles);
			Assert.Equal(fileOps.OpenedWritePaths[0], fileOps.DeletedFiles[0]);
			Assert.Empty(fileOps.MoveOperations);
			Assert.DoesNotContain(destinationPath, fileOps.DeletedFiles);
		}

		private sealed class FakeAtomicFileOperations : IAtomicFileOperations
		{
			public HashSet<string> ExistingFiles { get; } = [];
			public List<string> CreatedDirectories { get; } = [];
			public List<string> DeletedFiles { get; } = [];
			public List<string> OpenedWritePaths { get; } = [];
			public List<(string SourcePath, string DestinationPath)> MoveOperations { get; } = [];

			public bool FileExists(string path) => ExistingFiles.Contains(path);

			public void DeleteFile(string path)
			{
				DeletedFiles.Add(path);
				ExistingFiles.Remove(path);
			}

			public void MoveFile(string sourcePath, string destinationPath)
			{
				MoveOperations.Add((sourcePath, destinationPath));
				ExistingFiles.Remove(sourcePath);
				ExistingFiles.Add(destinationPath);
			}

			public Stream OpenWriteCreateNew(string path)
			{
				OpenedWritePaths.Add(path);
				ExistingFiles.Add(path);
				return new MemoryStream();
			}

			public void CreateDirectory(string path)
			{
				CreatedDirectories.Add(path);
			}

			public string GetDirectoryName(string path) => Path.GetDirectoryName(path);

			public string GetFileName(string path) => Path.GetFileName(path);

			public string CombinePath(string left, string right) => Path.Combine(left, right);
		}
	}
}
