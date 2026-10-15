namespace CivOne.src
{
	using System.Linq;
	using System.Reflection;
	using CivOne.Services;
	using Xunit;

	public class PeaceTurnsTests : TestsBase
	{
		// fact Game.Instance is of IGameSnapshotSource
		[Fact]
		public void GameInstanceImplementsIGameSnapshotSource()
		{
			var gameInstance = Game.Instance;
			Assert.IsAssignableFrom<IGameSnapshotSource>(gameInstance);
		}

		[Fact]
		public void EndTurnWhenNoHostileActionOccurredIncrementsOncePerNewTurn()
		{
			// Arrange
			var game = Game.Instance;
			SetPeaceTurns(game, 3);
			var expected = GameCalendarServiceFactory.Current.TurnToYear((ushort)(game.GameTurn + 1)) > 0 ? (ushort)4 : (ushort)3;

			// Act
			AdvanceToNextTurn(game);
			var actual = (game as IGameSnapshotSource).PeaceTurns;

			// Assert
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void EndTurnWhenHostileActionOccurredResetsToZero()
		{
			// Arrange
			var game = Game.Instance;
			SetPeaceTurns(game, 5);
			game.RegisterHostileAction(1, 2);
			// Reset and increment share the turn wrap, so the next turn already reads 1 in AD years.
			var expected = GameCalendarServiceFactory.Current.TurnToYear((ushort)(game.GameTurn + 1)) > 0 ? (ushort)1 : (ushort)0;

			// Act
			AdvanceToNextTurn(game);
			var actual = (game as IGameSnapshotSource).PeaceTurns;

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
			var property = typeof(Game).GetProperty("PeaceTurnService", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.NotNull(property);
			var service = property!.GetValue(game) as CivOne.Services.Peace.IPeaceTurnService;
			Assert.NotNull(service);
			service!.Restore(value);
		}
	}
}
