namespace CivOne.Services.Maps
{
	/// <summary>
	/// Provides the start path for the map file chooser and remembers the last used directory.
	/// </summary>
	/// <remarks>
	/// The last used directory is stored as a runtime setting, so it survives a restart.
	/// </remarks>
	public interface IMapDialogPathProvider
	{
		/// <summary>
		/// Returns the file name the map file chooser should start with.
		/// </summary>
		/// <returns>
		/// A full path inside the last used map directory, or inside the maps directory
		/// of the profile when no directory was used before.
		/// </returns>
		string EnsureInitialMapFilePath();

		/// <summary>
		/// Stores the directory of the given file as the last used map directory.
		/// </summary>
		/// <param name="filePath">Full path of the file the user selected.</param>
		void SetLastUsedMapPath(string filePath);
	}
}
