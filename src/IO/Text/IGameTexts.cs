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
	/// Read-only access to parsed Civilization text entries.
	/// Keys use file/marker format, for example ERROR/DEMOCRACY.
	/// </summary>
	internal interface IGameTexts
	{
		/// <summary>
		/// Returns text lines for a single game text key.
		/// </summary>
		/// <param name="key">
		/// Composite key in format FILE/MARKER.
		/// Example: HELP/HELP1.
		/// </param>
		/// <returns>
		/// Matching lines.
		/// Returns an empty array when key is unknown.
		/// </returns>
		string[] GetGameText(string key);
	}
}