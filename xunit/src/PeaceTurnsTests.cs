namespace CivOne.src
{
	using System.Linq;
	using System.Reflection;
	using Xunit;

	public class PeaceTurnsTests : TestsBase
	{
		[Fact]
		public void EndTurn_WhenNoHostileActionOccurred_IncrementsOncePerNewTurn()
		{
			// Arrange
			var game = Game.Instance;
			SetPeaceTurns(game, 3);
			var expected = (ushort)4;

			// Act
			AdvanceToNextTurn(game);
			var actual = game.PeaceTurns;

			// Assert
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void EndTurn_WhenHostileActionOccurred_ResetsToZero()
		{
			// Arrange
			var game = Game.Instance;
			SetPeaceTurns(game, 5);
			game.RegisterHostileAction();
			var expected = (ushort)0;

			// Act
			AdvanceToNextTurn(game);
			var actual = game.PeaceTurns;

			// Assert
			Assert.Equal(expected, actual);
		}

		private static void AdvanceToNextTurn(Game game)
		{
			var initialTurn = game.GameTurn;
			var maxSteps = game.Players.Count() + 1;
			for (var i = 0; i < maxSteps && game.GameTurn == initialTurn; i++)
			{
				game.EndTurn(0);
			}
		}

		private static void SetPeaceTurns(Game game, ushort value)
		{
			var field = typeof(Game).GetField("_peaceTurns", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.NotNull(field);
			field!.SetValue(game, value);
		}
	}
}
