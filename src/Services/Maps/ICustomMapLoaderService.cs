// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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
		/// Scans for <c>*.comap</c> and <c>*.cos</c> files.
		/// Returns an empty list when the directory does not exist.
		/// </remarks>
		IReadOnlyList<string> GetMapFiles();

		/// <summary>
		/// Loads the map from the given file path into <see cref="Map.Instance"/>.
		/// </summary>
		/// <param name="filePath">Full path to the map file.</param>
		/// <returns><see langword="true"/> on success; <see langword="false"/> if the file could not be loaded.</returns>
		bool LoadMapFile(string filePath);
	}
}
