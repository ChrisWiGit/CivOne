using System;
using System.IO;

namespace CivOne.Services
{
	/// <summary>
	/// Provides a method to replace a file atomically, ensuring that the file is either fully replaced or not modified at all, even in the event of an error during the write operation.
	/// The typical implementation would look like this:
	/// 1. Write the new content to a temporary file in the same directory as the destination file.
	/// 2. Flush and close the temporary file to ensure all data is written to disk.
	/// 3. Delete the original file if it exists.
	/// 4. Move the temporary file to the destination path, effectively replacing the original file
	/// Usually access any file also makes sure the directory exists, so this service will also ensure the destination directory is created if it doesn't exist.
	/// </summary>
	public interface IAtomicFileReplacementService
	{
		/// <summary>
		/// Replaces the specified file with the content provided by the write action, ensuring atomicity.
		/// </summary>
		/// <param name="destinationPath">The path of the file to be replaced.</param>
		/// <param name="writeAction">The action that writes the new content to the file.</param>
		void ReplaceFile(string destinationPath, Action<Stream> writeAction);
	}
}
