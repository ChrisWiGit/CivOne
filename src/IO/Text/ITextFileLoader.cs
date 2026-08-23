// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.IO.Text
{
	/// <summary>
	/// Loads raw text resources by logical file name.
	/// Implementations resolve language-specific and fallback paths.
	/// </summary>
	internal interface ITextFileLoader
	{
		/// <summary>
		/// Loads a text resource as cleaned line array.
		/// </summary>
		/// <param name="filename">
		/// Logical file name without extension.
		/// Example: ERROR.
		/// </param>
		/// <returns>
		/// All lines from the resolved file.
		/// Returns an empty array when no file exists.
		/// </returns>
		string[] LoadArray(string filename);
	}
}