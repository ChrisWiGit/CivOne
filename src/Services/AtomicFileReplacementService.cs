using System;
using System.IO;

namespace CivOne.Services
{
	public class AtomicFileReplacementService(IAtomicFileOperations? fileOperations = null) : IAtomicFileReplacementService
	{
		private readonly IAtomicFileOperations _fileOperations = fileOperations ?? new AtomicFileOperations();

		public void ReplaceFile(string destinationPath, Action<Stream> writeAction)
		{
			ArgumentNullException.ThrowIfNull(writeAction);
			if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path is required.", nameof(destinationPath));

			string? directory = _fileOperations.GetDirectoryName(destinationPath);
			if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Cannot determine destination directory.");

			_fileOperations.CreateDirectory(directory);

			string fileName = _fileOperations.GetFileName(destinationPath);
			string tempFilePath = _fileOperations.CombinePath(directory, $".{fileName}.{Guid.NewGuid():N}.tmp");

			try
			{
				using (Stream stream = _fileOperations.OpenWriteCreateNew(tempFilePath))
				{
					writeAction(stream);
					stream.Flush();
				}

				if (_fileOperations.FileExists(destinationPath))
				{
					_fileOperations.DeleteFile(destinationPath);
				}

				_fileOperations.MoveFile(tempFilePath, destinationPath);
			}
			catch
			{
				if (_fileOperations.FileExists(tempFilePath))
				{
					_fileOperations.DeleteFile(tempFilePath);
				}

				throw;
			}
		}
	}
}
