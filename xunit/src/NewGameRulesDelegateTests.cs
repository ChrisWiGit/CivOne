using System.Linq;
using CivOne.Civilizations;
using CivOne.Screens.NewGamePanels;
using CivOne.Services;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Covers the rules of the new game setup: the mapping from opponent count to player count, the
	/// civilizations offered for a given game size, and the difficulty entries.
	///
	/// The rules are kept free of screen state, so these tests run without a runtime or a game.
	/// </summary>
	public class NewGameRulesDelegateTests
	{
		/// <summary>
		/// Puts the translation service into a known identity state, so menu texts equal their keys.
		/// </summary>
		public NewGameRulesDelegateTests()
		{
			TranslationServiceFactory.ResetForTests();
		}

		private static MockedICivilization Civilization(byte id, byte preferredPlayerNumber)
			=> new MockedICivilization(1, id) { PreferredPlayerNumber = preferredPlayerNumber };

		private static NewGameRulesDelegate CreateRules(bool deityEnabled = false, params ICivilization[] civilizations)
			=> new(civilizations, () => deityEnabled);

		/// <summary>
		/// The menus count opponents, a game is created with the human player on top.
		/// </summary>
		[Theory]
		[InlineData(1, 2)]
		[InlineData(2, 3)]
		[InlineData(6, 7)]
		public void OpponentsToCompetitionAddsHumanPlayer(int opponents, int expectedCompetition)
		{
			NewGameRulesDelegate rules = CreateRules();

			Assert.Equal(expectedCompetition, rules.OpponentsToCompetition(opponents));
		}

		/// <summary>
		/// Values outside the supported range are pulled back into it instead of reaching game creation.
		/// </summary>
		[Fact]
		public void OpponentsToCompetitionClampsToGameLimits()
		{
			NewGameRulesDelegate rules = CreateRules();

			Assert.Equal(Game.MinCompetition, rules.OpponentsToCompetition(0));
			Assert.Equal(Game.MinCompetition, rules.OpponentsToCompetition(-5));
			Assert.Equal(Game.MaxCompetition, rules.OpponentsToCompetition(Game.MaxCompetition + 10));
		}

		/// <summary>
		/// The offered opponent counts stay inside the range a game can be created with.
		/// </summary>
		[Fact]
		public void OpponentBoundsFollowGameLimits()
		{
			NewGameRulesDelegate rules = CreateRules();

			Assert.Equal(Game.MinCompetition - 1, rules.MinOpponents);
			Assert.Equal(Game.MaxCompetition - 1, rules.MaxOpponents);
			Assert.Equal(rules.MaxOpponents - rules.MinOpponents + 1, rules.OpponentCount);
			Assert.Equal(Game.MinCompetition, rules.OpponentsToCompetition(rules.MinOpponents));
			Assert.Equal(Game.MaxCompetition, rules.OpponentsToCompetition(rules.MaxOpponents));
		}

		/// <summary>
		/// Only civilizations above the highest id of the original game count as an addition.
		/// </summary>
		[Fact]
		public void IsExtendedCivilizationSplitsAtClassicMaxId()
		{
			NewGameRulesDelegate rules = CreateRules();

			Assert.False(rules.IsExtendedCivilization(Civilization(NewGameRulesDelegate.ClassicCivilizationMaxId, 1)));
			Assert.True(rules.IsExtendedCivilization(Civilization(NewGameRulesDelegate.ClassicCivilizationMaxId + 1, 1)));
		}

		/// <summary>
		/// A classic sized game only offers the civilizations of the original game.
		/// </summary>
		[Fact]
		public void ClassicGameOffersClassicCivilizationsOnly()
		{
			NewGameRulesDelegate rules = CreateRules(false,
				Civilization(3, 1),
				Civilization(1, 1),
				Civilization(20, 1));

			int[] ids = [.. rules.GetSelectableCivilizations(NewGameRulesDelegate.ClassicMaxOpponents).Select(c => c.Id)];

			int[] expected = [1, 3];
			Assert.Equal(expected, ids);
		}

		/// <summary>
		/// A larger game offers every civilization, the classic ones first, each group ordered by id.
		/// </summary>
		[Fact]
		public void LargeGameOffersExtendedCivilizationsAfterClassicOnes()
		{
			NewGameRulesDelegate rules = CreateRules(false,
				Civilization(20, 1),
				Civilization(3, 1),
				Civilization(15, 1),
				Civilization(1, 1));

			int[] ids = [.. rules.GetSelectableCivilizations(NewGameRulesDelegate.ClassicMaxOpponents + 1).Select(c => c.Id)];

			int[] expected = [1, 3, 15, 20];
			Assert.Equal(expected, ids);
		}

		/// <summary>
		/// Civilizations without a preferred player number are not playable and never offered.
		/// </summary>
		[Fact]
		public void CivilizationsWithoutPreferredPlayerNumberAreSkipped()
		{
			NewGameRulesDelegate rules = CreateRules(false,
				Civilization(1, 0),
				Civilization(2, 1));

			int[] ids = [.. rules.GetSelectableCivilizations(4).Select(c => c.Id)];

			int[] expected = [2];
			Assert.Equal(expected, ids);
		}

		/// <summary>
		/// The deity difficulty is only offered when it is unlocked.
		/// </summary>
		[Fact]
		public void DifficultyMenuItemsFollowDeitySetting()
		{
			string[] withoutDeity = CreateRules(deityEnabled: false).BuildDifficultyMenuItems();
			string[] withDeity = CreateRules(deityEnabled: true).BuildDifficultyMenuItems();

			Assert.Equal(5, withoutDeity.Length);
			Assert.Equal(6, withDeity.Length);
			Assert.Contains("Emperor", withoutDeity[^1], System.StringComparison.Ordinal);
			Assert.Contains("Deity", withDeity[^1], System.StringComparison.Ordinal);
			Assert.Contains("Chieftain", withoutDeity[0], System.StringComparison.Ordinal);
		}

		/// <summary>
		/// The menu text shows the resulting number of civilizations, not the opponent count.
		/// </summary>
		[Fact]
		public void CivilizationCountTextShowsPlayerCount()
		{
			NewGameRulesDelegate rules = CreateRules();

			Assert.Equal("7 Civilizations", rules.FormatCivilizationCount(6));
		}
	}
}
