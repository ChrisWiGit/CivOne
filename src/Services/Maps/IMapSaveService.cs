// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Saves the current map (terrain and, where set, per-civilization start positions)
	/// as a standalone <c>*.comap</c> file, independent of a full game save.
	/// </summary>
	public interface IMapSaveService
	{
		/// <summary>
		/// Writes the current map to <paramref name="filePath"/> as a <c>.comap</c> YAML file.
		/// </summary>
		/// <param name="filePath">Destination file path.</param>
		void SaveCivOneMap(string filePath);
	}
}
