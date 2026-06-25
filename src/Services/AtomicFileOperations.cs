using System.IO;

namespace CivOne.Services
{
	public class AtomicFileOperations : IAtomicFileOperations
	{
		public bool FileExists(string path) => File.Exists(path);

		public void DeleteFile(string path) => File.Delete(path);

		public void MoveFile(string sourcePath, string destinationPath) => File.Move(sourcePath, destinationPath);

		public Stream OpenWriteCreateNew(string path) => File.Open(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);

		public void CreateDirectory(string path) => Directory.CreateDirectory(path);

		public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);

		public string GetFileName(string path) => Path.GetFileName(path);

		public string CombinePath(string left, string right) => Path.Combine(left, right);
	}
}
