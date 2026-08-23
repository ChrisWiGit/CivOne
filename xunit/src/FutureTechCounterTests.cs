namespace CivOne.src
{
	using System.Linq;
	using CivOne.Tasks;
	using Xunit;

	public class FutureTechCounterTests : TestsBase
	{
		[Fact]
		public void ProcessScienceIncrementsPlayerAndGlobalCounterForHumanFutureTech()
		{
			foreach (var advance in Common.Advances.ToArray())
			{
				if (!playa.HasAdvance(advance))
				{
					playa.AddAdvance(advance, false);
				}
			}

			playa.CurrentResearch = null;
			playa.Science = playa.ScienceCost;
			playa.FutureTechCount = 0;

			var game = Game.Instance;
			game.SetCurrentPlayerForTesting(game.PlayerNumber(playa));
			Assert.Equal((ushort)0, game.PlayerFutureTech);

			new ProcessScience(playa).Run();

			Assert.Equal((ushort)1, playa.FutureTechCount);
			Assert.Equal((ushort)1, game.PlayerFutureTech);
			Assert.Equal(0, playa.Science);
		}

		[Fact]
		public void ProcessScienceIncrementsOnlyPlayerCounterForAiFutureTech()
		{
			var aiPlayer = Game.Instance.Players.First(player => player != playa && !(player.Civilization is CivOne.Civilizations.Barbarian));
			foreach (var advance in Common.Advances.ToArray())
			{
				if (!aiPlayer.HasAdvance(advance))
				{
					aiPlayer.AddAdvance(advance, false);
				}
			}

			aiPlayer.CurrentResearch = null;
			aiPlayer.Science = aiPlayer.ScienceCost;
			aiPlayer.FutureTechCount = 0;

			var game = Game.Instance;
			game.SetCurrentPlayerForTesting(game.PlayerNumber(aiPlayer));
			Assert.Equal((ushort)0, game.PlayerFutureTech);

			new ProcessScience(aiPlayer).Run();

			Assert.Equal((ushort)1, aiPlayer.FutureTechCount);
			Assert.Equal((ushort)0, game.PlayerFutureTech);
			Assert.Equal(0, aiPlayer.Science);
		}
	}
}
