using System;
using System.IO;

namespace CivOne.Services
{
	/// <summary>
	/// Default <see cref="IAtomicFileReplacementService"/> implementation.
	///
	/// Writes the new content to a temporary file in the destination directory first, then swaps it
	/// into place. This guarantees the destination is always either the previous complete file or the
	/// new complete file - it is never left half-written, even if the write throws, the disk fills up,
	/// or the process crashes mid-write.
	///
	/// The temporary file lives in the same directory as the destination so that the final move stays
	/// on the same volume (a same-volume move is a fast rename rather than a copy) and so that any
	/// partial temp file left behind by a crash sits next to its target for easy cleanup.
	/// </summary>
	public class AtomicFileReplacementService(IAtomicFileOperations? fileOperations = null) : IAtomicFileReplacementService
	{
		private readonly IAtomicFileOperations _fileOperations = fileOperations ?? new AtomicFileOperations();

		/// <summary>
		/// Replaces <paramref name="destinationPath"/> with content produced by <paramref name="writeAction"/>.
		///
		/// Ensures the destination directory exists, writes to a uniquely named temporary file, flushes and
		/// closes it, then removes any existing destination and moves the temporary file into place.
		/// If any step fails, the temporary file is deleted and the exception is rethrown, leaving the
		/// original destination untouched.
		/// </summary>
		/// <param name="destinationPath">The path of the file to replace. Its directory is created if missing.</param>
		/// <param name="writeAction">Callback that writes the new content to the provided temporary stream.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="writeAction"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="destinationPath"/> is null or whitespace.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the destination directory cannot be determined.</exception>
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
				DeleteFailBestEffort(tempFilePath);

				throw;
			}
		}

		private void DeleteFailBestEffort(string tempFilePath)
		{
			// Best-effort cleanup: never let a failure to delete the temp file
			// mask the original exception that caused us to enter this block.
			try
			{
				if (_fileOperations.FileExists(tempFilePath))
				{
					_fileOperations.DeleteFile(tempFilePath);
				}
			}
			catch (Exception cleanupException) when (cleanupException is IOException or UnauthorizedAccessException)
			{
				// Swallow cleanup failures; a leftover temp file is harmless.
			}
		}
	}
}
