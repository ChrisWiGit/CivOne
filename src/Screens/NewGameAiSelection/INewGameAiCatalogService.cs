// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;

namespace CivOne.Screens
{
	/// <summary>
	/// Provides available AI catalog entries and difficulty labels for the selection screen.
	/// </summary>
	internal interface INewGameAiCatalogService
	{
		/// <summary>
		/// Gets the selectable AI catalog entries.
		/// </summary>
		/// <returns>The available AI catalog entries.</returns>
		IReadOnlyList<AiCatalogEntry> GetAiEntries();

		/// <summary>
		/// Gets the selectable difficulty labels.
		/// </summary>
		/// <returns>The available difficulty labels.</returns>
		IReadOnlyList<string> GetDifficultyLabels();
	}
}