using System.IO;

namespace CivOne.Services
{
	/// <summary>
	/// Default <see cref="IDirectoryService"/> implementation, backed by <see cref="Directory"/>.
	/// </summary>
	public class DirectoryService : IDirectoryService
	{
		/// <inheritdoc/>
		public void CreateDirectory(string path) => Directory.CreateDirectory(path);
	}
}