using System.Linq;
using CivOne.Tasks;
using Xunit;

namespace CivOne.src
{
	/// <summary>
	/// Checks the end-of-game check at game level: once per turn, only in the exact years.
	/// </summary>
	public class GameEndTurnTests : TestsBase
	{
		// The test game runs at difficulty 3: warning in 2020, end in 2040. From 1850 a turn is one year.
		private const ushort TurnBefore2020 = 2019 - 1450;
		private const ushort TurnBefore2040 = 2039 - 1450;

		[Fact]
		public void FinalYearEnqueuesTheEndGameFlowOnceAndNotAgainNextTurn()
		{
			Game game = Game.Instance;
			game.GameTurn = TurnBefore2040;
			GameTask.ClearAll();

			AdvanceOneFullTurn(game);
			Assert.Equal(1, GameTask.Count(task => task is GameEndTask));

			GameTask.ClearAll();
			AdvanceOneFullTurn(game);
			Assert.Equal(0, GameTask.Count(task => task is GameEndTask));
		}

		[Fact]
		public void RetirementWarningYearDoesNotEndTheGame()
		{
			Game game = Game.Instance;
			game.GameTurn = TurnBefore2020;
			GameTask.ClearAll();

			AdvanceOneFullTurn(game);

			Assert.Equal(0, GameTask.Count(task => task is GameEndTask));
		}

		[Fact]
		public void ResearchDoublesOnceWhenTheCalendarReachesOneAd()
		{
			Game game = Game.Instance;
			game.GameTurn = 199;
			playa.Science = 21;
			GameTask.ClearAll();

			AdvanceOneFullTurn(game);
			short afterAnnoDomini = playa.Science;
			AdvanceOneFullTurn(game);

			Assert.True(afterAnnoDomini >= 42, "Research progress must double at 1 AD.");
			Assert.True(playa.Science < afterAnnoDomini * 2, "Research progress must double only once.");
		}

		/// <summary>
		/// Ends the turn of every player once, so exactly one turn change happens.
		/// </summary>
		private static void AdvanceOneFullTurn(Game game)
		{
			var initialTurn = game.GameTurn;
			var players = game.Players.Count();
			for (var i = 0; i < players; i++)
			{
				game.EndTurn(0);
			}

			Assert.Equal(initialTurn + 1, game.GameTurn);
		}
	}
}
