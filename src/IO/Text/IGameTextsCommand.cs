// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.
using CivOne.Services;

namespace CivOne.IO.Text
{
	/// <summary>
	/// Mutable game text component with reload and language observer behavior.
	/// Used by the factory-managed singleton instance.
	/// </summary>
	internal interface IGameTextsCommand : IGameTexts, ITranslationLanguageObserver
	{
		/// <summary>
		/// Reloads all configured text files and rebuilds key mappings.
		/// Call this after changing language or file contents.
		/// </summary>
		void Reset();
	}
}