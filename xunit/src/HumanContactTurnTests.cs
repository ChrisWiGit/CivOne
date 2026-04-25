namespace CivOne.src
{
	using System.Linq;
	using CivOne.Civilizations;
	using Xunit;

	public class HumanContactTurnTests : TestsBase
	{
		[Fact]
		public void Explore_AiSeeingHumanUnit_SetsHumanContactTurnToCurrentTurn()
		{
			var game = Game.Instance;
			var aiPlayer = game.Players.First(player => player != playa && !(player.Civilization is Barbarian));
			var humanUnit = game.GetUnits().First(unit => unit.Owner == game.PlayerNumber(playa));

			aiPlayer.HumanContactTurn = 0;
			aiPlayer.Explore(humanUnit.X, humanUnit.Y, 0);

			Assert.Equal(game.GameTurn, aiPlayer.HumanContactTurn);
		}

		[Fact]
		public void Explore_HumanSeeingAiUnit_DoesNotSetAiHumanContactTurn()
		{
			var game = Game.Instance;
			var aiPlayer = game.Players.First(player => player != playa && !(player.Civilization is Barbarian));
			var aiUnit = game.GetUnits().First(unit => unit.Owner == game.PlayerNumber(aiPlayer));

			aiPlayer.HumanContactTurn = 0;
			playa.Explore(aiUnit.X, aiUnit.Y, 0);

			Assert.Equal((ushort)0, aiPlayer.HumanContactTurn);
		}
	}
}
