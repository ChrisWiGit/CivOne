using System.Linq;
using CivOne.Buildings;
using CivOne.Persistence.Game;
using CivOne.Screens;
using CivOne.Tasks;
using Xunit;

namespace CivOne.src
{
	/// <summary>
	/// Checks the palace upgrade offer at game level: once per turn, not once per player.
	/// </summary>
	public class PalaceUpgradeTurnTests : TestsBase
	{
		[Fact]
		public void FullTurnAboveThresholdEnqueuesPalaceBuilderExactlyOnce()
		{
			// Arrange
			Game game = Game.Instance;
			PrepareHumanAboveThreshold(game);

			// Act
			AdvanceOneFullTurn(game);

			// Assert
			Assert.Equal(1, CountPalaceBuilderTasks());
		}

		[Fact]
		public void FullTurnWithPalaceOptionOffEnqueuesNoPalaceBuilder()
		{
			// Arrange
			Game game = Game.Instance;
			PrepareHumanAboveThreshold(game);
			game.Palace = false;

			// Act
			AdvanceOneFullTurn(game);

			// Assert
			Assert.Equal(0, CountPalaceBuilderTasks());
		}

		[Fact]
		public void FullTurnAfterScoringCompletedEnqueuesNoPalaceBuilder()
		{
			// Arrange
			Game game = Game.Instance;
			PrepareHumanAboveThreshold(game);
			game.ScoringCompleted = true;

			// Act
			AdvanceOneFullTurn(game);

			// Assert
			Assert.Equal(0, CountPalaceBuilderTasks());
		}

		private void PrepareHumanAboveThreshold(Game game)
		{
			game.SetCurrentPlayerForTesting(game.PlayerNumber(playa));
			var unit = game.GetUnits().First(x => x.Owner == playa.Civilization.Id);
			City? city = game.AddCity(playa, 1, unit.X, unit.Y);
			Assert.NotNull(city);
			city!.AddBuilding(new Palace());
			city.Size = 8;
			game.Palace = true;
			GameTask.ClearAll();

			// Threshold for the first upgrade at difficulty 3: (7 - 3) * 1 * 2 / 2 / 2 + 1 = 3.
			Assert.True(((IPlayer)playa).CivilizationScore >= 3, "Test setup must put the human score above the threshold.");
		}

		private static int CountPalaceBuilderTasks() => GameTask.Count(task => task is Show show && show.Content is PalaceView);

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
