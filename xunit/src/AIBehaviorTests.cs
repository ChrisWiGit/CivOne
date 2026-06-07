using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.src;
using CivOne.Units;
using Xunit;

namespace CivOne.UnitTests
{
	public class AIBehaviorTests : TestsBase2
	{
		private static Player[] GetAiPlayersExcludingBarbarians(Game game, int count)
		{
			var players = Enumerable.Range(0, game.Players.Count())
				.Select(index => game.GetPlayer((byte)index))
				.Where(player => player != null && !player.IsHuman && player.Civilization is not Barbarian)
				.Take(count)
				.ToArray();

			Assert.True(players.Length >= count, $"Expected at least {count} non-human, non-barbarian players in test setup.");
			return players;
		}

		[Fact]
		public void CityProductionWhenTradeAndLessThanThreeRoutesChoosesCaravanAfterUnitThreshold()
		{
			// Arrange
			var game = Game.Instance;
			var player = GetAiPlayersExcludingBarbarians(game, 1).Single();
			City city = game.AddCity(player, 0, 40, 30);
			Assert.NotNull(city);

			player.AddAdvance(new Trade());
			city.AddBuilding(new Barracks());
			city.AddBuilding(new Granary());
			city.AddBuilding(new Temple());
			city.AddBuilding(new CityWalls());
			city.Size = 1;

			for (int i = 0; i < 4; i++)
			{
				var unit = game.CreateUnit(UnitType.Militia, city.X, city.Y, game.PlayerNumber(player));
				((BaseUnit)unit).SetHome(city);
			}

			// Act
			AI.Instance(player).CityProduction(city);

			// Assert
			Assert.IsType<Caravan>(city.CurrentProduction);
		}

	}
}