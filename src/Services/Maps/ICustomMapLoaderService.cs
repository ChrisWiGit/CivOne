using System.Collections.Generic;

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Provides operations for discovering and loading custom map files.
	/// </summary>
	public interface ICustomMapLoaderService
	{
		/// <summary>
		/// Returns the sorted full paths of all loadable map files in the maps directory.
		/// </summary>
		/// <remarks>
		/// Scans for <c>*.comap</c> and legacy <c>*.map</c> files.
		/// Returns an empty list when the directory does not exist.
		/// </remarks>
		IReadOnlyList<string> GetMapFiles();

		/// <summary>
		/// Loads the map from the given file path into <see cref="Map.Instance"/>.
		/// </summary>
		/// <remarks>
		/// The format is chosen by file extension: <c>*.map</c> is read as a legacy
		/// Civilization I map, anything else as a <c>*.comap</c> YAML map.
		/// </remarks>
		/// <param name="filePath">Full path to the map file.</param>
		/// <returns><see langword="true"/> on success; <see langword="false"/> if the file could not be loaded.</returns>
		bool LoadMapFile(string filePath);
	}
}