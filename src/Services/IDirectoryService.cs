namespace CivOne.Services
{
	/// <summary>
	/// Abstracts <see cref="System.IO.Directory"/> for dependency injection scenarios.
	/// </summary>
	public interface IDirectoryService
	{
		/// <summary>
		/// Creates all directories and subdirectories in <paramref name="path"/> unless they already exist.
		/// </summary>
		/// <param name="path">The directory path to create.</param>
		void CreateDirectory(string path);
	}
}