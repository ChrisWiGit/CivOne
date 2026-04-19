using System.IO;

namespace CivOne.Services
{
	public interface IAtomicFileOperations
	{
		bool FileExists(string path);
		void DeleteFile(string path);
		void MoveFile(string sourcePath, string destinationPath);
		Stream OpenWriteCreateNew(string path);
		void CreateDirectory(string path);
		string GetDirectoryName(string path);
		string GetFileName(string path);
		string CombinePath(string left, string right);
	}
}
