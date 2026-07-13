// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CivOne.Agents;
using CivOne.Civilizations;
using CivOne.Persistence.Game;

namespace CivOne.Screens
{
	internal sealed class NewGameAiSelectionState
	{
		public required int Difficulty { get; init; }

		public required int Competition { get; init; }

		public required ICivilization[] TribesAvailable { get; init; }

		public required string[] MenuItemsTribes { get; init; }

		public required int TribeIndex { get; init; }

		public required string LeaderName { get; init; }

		public required string TribeName { get; init; }

		public required string TribeNamePlural { get; init; }
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1822:Mark members as static", Justification = "Instance methods are required by design.")]
	internal sealed class NewGameAiSelectionDelegate
	{
		public NewGameAiSelectionState BuildState(NewGameAiSelectionResult result, IEnumerable<ICivilization> civilizations)
		{
			ArgumentNullException.ThrowIfNull(result);
			ArgumentNullException.ThrowIfNull(civilizations);

			ICivilization[] tribesAvailable =
			[
				.. civilizations.Where(c => c.PreferredPlayerNumber > 0 && c.PreferredPlayerNumber <= result.Competition)
			];
			string[] menuItemsTribes = [.. tribesAvailable.Select(c => c.Name)];

			int tribeIndex = Array.FindIndex(tribesAvailable, civ => civ == result.Human.Civilization);
			if (tribeIndex < 0)
			{
				tribeIndex = 0;
			}

			return new NewGameAiSelectionState
			{
				Difficulty = result.Difficulty,
				Competition = result.Competition,
				TribesAvailable = tribesAvailable,
				MenuItemsTribes = menuItemsTribes,
				TribeIndex = tribeIndex,
				LeaderName = result.Human.Name,
				TribeName = result.Human.Civilization.Name,
				TribeNamePlural = result.Human.Civilization.NamePlural
			};
		}

		public void LogSelectionSummary(
			NewGameAiSelectionResult? aiSelectionResult,
			IReadOnlyList<string> menuItemsDifficulty,
			Func<int, string> difficultyNameResolver,
			Action<string, object?[]> log)
		{
			if (aiSelectionResult is null)
			{
				return;
			}

			string selectedDifficultyLabel = aiSelectionResult.Difficulty >= 0 && aiSelectionResult.Difficulty < menuItemsDifficulty.Count
				? menuItemsDifficulty[aiSelectionResult.Difficulty]
				: aiSelectionResult.Difficulty.ToString(CultureInfo.InvariantCulture);

			log(
				"AI selection mode active: Difficulty={0} ({1}), Competition={2}, Opponents={3}",
				new object?[]
				{
					aiSelectionResult.Difficulty,
					selectedDifficultyLabel,
					aiSelectionResult.Competition,
					aiSelectionResult.Opponents.Count
				});

			log(
				"AI selection human: Name={0}, Civilization={1}, Color={2}, Team={3}",
				new object?[]
				{
					aiSelectionResult.Human.Name,
					aiSelectionResult.Human.Civilization.Name,
					aiSelectionResult.Human.ColorSlot,
					aiSelectionResult.Human.TeamSlot
				});

			foreach (NewGamePlayerSelection selection in aiSelectionResult.Opponents)
			{
				int slot = selection.Civilization.PreferredPlayerNumber;
				string difficultyLabel = selection.DifficultyIndex >= 0
					? difficultyNameResolver(selection.DifficultyIndex)
					: "<unset>";

				log(
					"AI selection opponent: Slot={0}, Name={1}, Civilization={2}, AiId={3}, AiName={4}, Difficulty={5} ({6}), Color={7}, Team={8}",
					new object?[]
					{
						slot,
						selection.Name,
						selection.Civilization.Name,
						selection.AiId?.ToString() ?? "<null>",
						selection.AiName,
						selection.DifficultyIndex,
						difficultyLabel,
						selection.ColorSlot,
						selection.TeamSlot
					});
			}
		}

		public IReadOnlyDictionary<int, int>? BuildDifficultyOverrides(NewGameAiSelectionResult? aiSelectionResult)
		{
			if (aiSelectionResult is null)
			{
				return null;
			}

			Dictionary<int, int> overrides = [];
			foreach (NewGamePlayerSelection selection in aiSelectionResult.Opponents)
			{
				if (selection.DifficultyIndex < 0)
				{
					continue;
				}

				int slot = selection.Civilization.PreferredPlayerNumber;
				if (slot < 0)
				{
					// includes barbarian (which is index 0)
					continue;
				}

				overrides[slot] = selection.DifficultyIndex;
			}

			return overrides;
		}

		public void ApplySelectionsToCreatedGame(
			NewGameAiSelectionResult? aiSelectionResult,
			int maxDifficulty,
			Func<byte, Player?> getPlayer,
			Action<string, object?[]> log)
		{
			if (aiSelectionResult is null)
			{
				return;
			}

			ArgumentNullException.ThrowIfNull(getPlayer);
			ArgumentNullException.ThrowIfNull(log);

			foreach (NewGamePlayerSelection selection in aiSelectionResult.Opponents)
			{
				int slot = selection.Civilization.PreferredPlayerNumber;
				if (slot < 0 || slot > byte.MaxValue)
				{
					continue;
				}

				Player? player = getPlayer((byte)slot);
				if (player is null || player.IsHuman)
				{
					continue;
				}

				IPlayerRestorable restorable = player;
				restorable.AiId = selection.AiId;
				if (!string.IsNullOrWhiteSpace(selection.Name))
				{
					string trimmedName = selection.Name.Trim();
					restorable.TribeName = trimmedName;
					restorable.TribeNamePlural = trimmedName;
				}

				if (selection.DifficultyIndex >= 0 && selection.DifficultyIndex <= maxDifficulty)
				{
					player.AiDifficulty = (AiDifficulty)selection.DifficultyIndex;
				} 
				else
				{
					log(
						"Warning: Ignoring invalid difficulty index {0} for slot {1}. Valid range is 0..{2}.",
						[
							selection.DifficultyIndex,
							slot,
							maxDifficulty
						]);
				}

				log(
					"Applied AI selection for slot {0}: Name={1}, AiId={2}, AiName={3}, DifficultyIndex={4}, Color={5}, Team={6}",
					[
						slot,
						player.TribeName,
						selection.AiId?.ToString() ?? "<null>",
						selection.AiName,
						selection.DifficultyIndex,
						selection.ColorSlot,
						selection.TeamSlot
					]);
			}
		}
	}
}
