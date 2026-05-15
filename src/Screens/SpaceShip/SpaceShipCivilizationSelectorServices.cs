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
	public readonly record struct SpaceShipCivilizationListItem(Player Player, bool IsEnabled);

	public interface ISpaceShipCivilizationSelectorService
	{
		SpaceShipCivilizationListItem[] GetCivilizations();
	}

	public interface ISpaceShipCivilizationEligibilityEvaluator
	{
		bool IsEnabled(Player player);
	}

	public sealed class SpaceShipCivilizationSelectorServices
	{
		public required ISpaceShipCivilizationSelectorService SelectorService { get; init; }
		public required ITranslationService TranslationService { get; init; }
	}

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

	internal sealed class SpaceShipCivilizationSelectorService(ISpaceShipCivilizationEligibilityEvaluator eligibilityEvaluator) : ISpaceShipCivilizationSelectorService
	{
		private readonly ISpaceShipCivilizationEligibilityEvaluator _eligibilityEvaluator = eligibilityEvaluator ?? throw new ArgumentNullException(nameof(eligibilityEvaluator));

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
				if (player == null || player.Civilization is Barbarian || player.IsDestroyed)
				{
					continue;
				}

				items.Add(new SpaceShipCivilizationListItem(player, _eligibilityEvaluator.IsEnabled(player)));
			}

			return [.. items];
		}
	}
}
