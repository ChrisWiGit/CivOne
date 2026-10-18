// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Linq;
using CivOne.Agents;
using CivOne.Enums;
using CivOne.IO;

namespace CivOne.Screens
{
	/// <summary>
	/// Default implementation for providing AI catalog data to the selection screen.
	/// </summary>
	internal sealed class DefaultNewGameAiCatalogService : INewGameAiCatalogService
	{
		private static readonly string[] BaseDifficultyLabels =
		[
			// not translated here, since these are already translated elsewhere
			"Chieftain",
			"Warlord",
			"Prince",
			"King",
			"Emperor"
		];

		/// <summary>
		/// Gets the selectable AI catalog entries.
		/// </summary>
		/// <returns>The available AI catalog entries.</returns>
		public IReadOnlyList<AiCatalogEntry> GetAiEntries()
		{
			return
			[
				.. AgentLoaderEntry
					.GetAvailableDefinitions()
					.Where(definition => definition.Id != AiDefinitionIds.BarbarianDisabled)
					.Select(definition => new AiCatalogEntry(definition.Id, definition.DisplayName, definition.Provider, definition.Difficulty))
			];
		}

		/// <summary>
		/// Gets the difficulty labels available for the current settings.
		/// </summary>
		/// <returns>The available difficulty labels.</returns>
		public IReadOnlyList<string> GetDifficultyLabels()
		{
			return Settings.Instance.DeityEnabled
				? [.. BaseDifficultyLabels, "Deity"]
				: BaseDifficultyLabels;
		}
	}
}