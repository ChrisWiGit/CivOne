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
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Services;
using CivOne.Wonders;

namespace CivOne.Screens
{
	/// <summary>
	/// One entry shown in <see cref="SpaceShipCivilizationSelectorDialog"/>.
	/// Contains player reference and whether selection is currently enabled.
	/// </summary>
	public readonly record struct SpaceShipCivilizationListItem(Player Player, bool IsEnabled);

	/// <summary>
	/// Provides civilization entries for <see cref="SpaceShipCivilizationSelectorDialog"/>.
	/// </summary>
	public interface ISpaceShipCivilizationSelectorService
	{
		SpaceShipCivilizationListItem[] GetCivilizations();
	}

	/// <summary>
	/// Evaluates if a specific civilization is selectable in the spaceship selector dialog.
	/// </summary>
	public interface ISpaceShipCivilizationEligibilityEvaluator
	{
		bool IsEnabled(Player player);
	}

	/// <summary>
	/// Service bundle required by <see cref="SpaceShipCivilizationSelectorDialog"/>.
	/// </summary>
	public sealed class SpaceShipCivilizationSelectorServices
	{
		public required ISpaceShipCivilizationSelectorService SelectorService { get; init; }
		public required ITranslationService TranslationService { get; init; }
	}

	/// <summary>
	/// Default eligibility evaluator for the civilization selector.
	/// Delegates checks to <see cref="SpaceShipCivilizationSelectionRules"/>.
	/// </summary>
	public sealed class SpaceShipCivilizationEligibilityEvaluator : ISpaceShipCivilizationEligibilityEvaluator
	{
		public bool IsEnabled(Player player)
		{
			if (player == null)
			{
				return false;
			}

			bool hasApolloProgram = player.HasWonder<ApolloProgram>();
			return SpaceShipCivilizationSelectionRules.IsEnabled(hasApolloProgram, player.SpaceShipGrid);
		}
	}

	/// <summary>
	/// Shared rule set used to decide whether a civilization can be opened in <see cref="SpaceShipView"/>.
	/// </summary>
	public static class SpaceShipCivilizationSelectionRules
	{
		internal static bool IsEnabled(bool hasApolloProgram, SpaceShipComponentType[,] spaceShipGrid)
		{
			if (hasApolloProgram)
			{
				return true;
			}

			return HasAnySpaceShipPart(spaceShipGrid);
		}

		internal static bool HasAnySpaceShipPart(SpaceShipComponentType[,] spaceShipGrid)
		{
			if (spaceShipGrid == null)
			{
				return false;
			}

			for (int x = 0; x < spaceShipGrid.GetLength(0); x++)
			{
				for (int y = 0; y < spaceShipGrid.GetLength(1); y++)
				{
					if (spaceShipGrid[x, y] != SpaceShipComponentType.Empty)
					{
						return true;
					}
				}
			}

			return false;
		}
	}

	/// <summary>
	/// Default selector implementation that reads players from <see cref="Game"/> and applies eligibility filters.
	/// </summary>
	internal sealed class SpaceShipCivilizationSelectorService(
		ISpaceShipCivilizationEligibilityEvaluator eligibilityEvaluator,
		bool includeDestroyed = false,
		bool includeDisabled = false) : ISpaceShipCivilizationSelectorService
	{
		private readonly ISpaceShipCivilizationEligibilityEvaluator _eligibilityEvaluator = eligibilityEvaluator ?? throw new ArgumentNullException(nameof(eligibilityEvaluator));
		private readonly bool _includeDestroyed = includeDestroyed;
		private readonly bool _includeDisabled = includeDisabled;

		public SpaceShipCivilizationListItem[] GetCivilizations()
		{
			if (!Game.Started)
			{
				return [];
			}

			Game game = Game.Instance;
			if (game?.Players == null)
			{
				return [];
			}

			List<SpaceShipCivilizationListItem> items = [];
			foreach (Player player in game.Players)
			{
				if (player == null || player.Civilization is Barbarian)
				{
					continue;
				}

				if (!_includeDestroyed && player.IsDestroyed)
				{
					continue;
				}

				bool isEnabled = _includeDisabled || _eligibilityEvaluator.IsEnabled(player);
				items.Add(new SpaceShipCivilizationListItem(player, isEnabled));
			}

			return [.. items];
		}
	}
}
