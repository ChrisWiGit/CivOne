using CivOne.Buildings;
using CivOne.src;
using CivOne.Units;
using System.Linq;
using Xunit;

namespace CivOne.UnitTests
{
	public class CityBuyGuardsTests : TestsBase
	{
		private City CreateCityForHuman()
		{
			Game.Instance.SetCurrentPlayerForTesting(Game.Instance.PlayerNumber(playa));
			var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
			City? result = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);

			Assert.NotNull(result);
			return result;
		}

		[Fact]
		public void BuyWhenBuyPriceIsNonPositiveReturnsFalseAndKeepsGold()
		{
			// Arrange
			City city = CreateCityForHuman();
			
			var production = new Barracks();
			city.SetProduction(production);
			city.Shields = (int)production.Price * 10 + 1;
			short expectedGold = 500;
			playa.Gold = expectedGold;

			// Act
			bool actual = city.Buy();

			// Assert
			Assert.False(actual);
			Assert.Equal(expectedGold, playa.Gold);
		}

		[Fact]
		public void BuyWhenCityInRiotAndProductionIsBuildingReturnsFalseAndKeepsGold()
		{
			// Arrange
			City city = CreateCityForHuman();
			city.SetProduction(new Barracks());
			city.IsRiot = true;
			short expectedGold = 500;
			playa.Gold = expectedGold;

			// Act
			bool actual = city.Buy();

			// Assert
			Assert.False(actual);
			Assert.Equal(expectedGold, playa.Gold);
		}

		[Fact]
		public void BuyWhenCityInRiotAndProductionIsUnitAllowsBuying()
		{
			// Arrange
			City city = CreateCityForHuman();
			city.SetProduction(new Militia());
			city.IsRiot = true;
			playa.Gold = 500;

			// Act
			bool actual = city.Buy();

			// Assert
			Assert.True(actual);
			Assert.True(playa.Gold < 500);
		}
	}
}