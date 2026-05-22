using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Governments;
using CivOne.src;
using CivOne.Tiles;
using CivOne.Wonders;
using Xunit;

namespace CivOne.UnitTests
{
    public class CityEconomyServiceImplTests
    {
        // The public helper methods are pure calculations (value-in, value-out),
        // so no City/IGame setup is needed; null is safe here.
        private readonly CityEconomyServiceImpl _service = new(null, null);

        [Theory]
        [InlineData(1, 5, 0)]
        [InlineData(1, 6, 0)]
        [InlineData(1, 9, 0)]
        [InlineData(19, 5, 9)]
        [InlineData(19, 9, 17)]
        public void CalculateTradeTaxes_TruncatesTowardsZero(int totalTrade, int taxesRate, short expectedTaxes)
        {
            short taxes = _service.CalculateTradeTaxes(totalTrade, taxesRate);

            Assert.Equal(expectedTaxes, taxes);
        }

        [Fact]
        public void CalculateTradeLuxuries_ReturnsZero_WhenTaxesRateIsMax()
        {
            short luxuries = _service.CalculateTradeLuxuries(20, 10, 10, 5);
            Assert.Equal(0, luxuries);
        }

        [Fact]
        public void CalculateTradeScience_DoesNotGoNegative()
        {
            short science = _service.CalculateTradeScience(3, 2, 3);
            Assert.Equal(0, science);
        }

        [Fact]
        public void CalculateLuxuries_AppliesBuildingAndEntertainerBonuses()
        {
            short luxuries = _service.CalculateLuxuries(4, hasMarketPlace: true, hasBank: true, entertainerLuxuries: 3);
            Assert.Equal(12, luxuries);
        }

        [Fact]
        public void CalculateTaxes_AppliesBuildingAndTaxmanBonuses()
        {
            short taxes = _service.CalculateTaxes(4, hasMarketPlace: true, hasBank: true, taxmen: 2);
            Assert.Equal(13, taxes);
        }

        [Fact]
        public void CalculateScience_AppliesScienceMultipliersInExpectedOrder()
        {
            short science = _service.CalculateScience(
                tradeScience: 10,
                hasLibrary: true,
                hasUniversity: true,
                hasSeti: false,
                hasNewton: true,
                hasCopernicus: true,
                scientists: 1);

            Assert.Equal(59, science);
        }

        [Fact]
        public void CalculateScience_CapsAtShortMaxValue()
        {
            short science = _service.CalculateScience(
                tradeScience: 30000,
                hasLibrary: true,
                hasUniversity: true,
                hasSeti: true,
                hasNewton: true,
                hasCopernicus: true,
                scientists: 1000);

            Assert.Equal(short.MaxValue, science);
        }
    }

    public class CityEconomyServiceImplCalculateBreakdownTests : TestsBase
    {
        [Fact]
        public void FoodTotal_Recomputes_WhenWorkedTileFoodChangesWithinSameTurn()
        {
            var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            City city = Game.Instance.AddCity(playa, 0, unit.X, unit.Y);
            city.Size = 4;
            city.ResetResourceTiles();
            playa.Government = new Monarchy();

            // Find a worked tile where toggling pollution actually changes effective food on the current map/government setup.
            ITile tile = city.ResourceTiles.First(t =>
            {
                bool originalPollution = t.Pollution;
                int originalFood = city.FoodValue(t);
                t.Pollution = true;
                int pollutedFood = city.FoodValue(t);
                t.Pollution = originalPollution;
                return pollutedFood < originalFood;
            });

            int initial = city.FoodTotal;
            tile.Pollution = true;
            int expected = city.ResourceTiles.Sum(t => city.FoodValue(t));

            Assert.Equal(expected, city.FoodTotal);
            Assert.True(city.FoodTotal < initial);

            tile.Pollution = false;
            int restoredExpected = city.ResourceTiles.Sum(t => city.FoodValue(t));
            Assert.Equal(restoredExpected, city.FoodTotal);
            Assert.Equal(initial, city.FoodTotal);
        }

        [Fact]
        public void ShieldTotal_Recomputes_WhenWorkedTileShieldChangesWithinSameTurn()
        {
            var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            City city = Game.Instance.AddCity(playa, 0, unit.X, unit.Y);
            city.Size = 4;
            city.ResetResourceTiles();
            playa.Government = new Monarchy();

            // Prime cache and capture state hash.
            int initialTotal = city.ShieldTotal;
            ulong initialHash = GetCachedShieldRawStateHash(city);

            ITile tile = city.ResourceTiles.First();
            bool originalPollution = tile.Pollution;

            tile.Pollution = !originalPollution;
            int mutatedExpected = city.ResourceTiles.Sum(t => city.ShieldValue(t));
            int mutatedActual = city.ShieldTotal;
            ulong mutatedHash = GetCachedShieldRawStateHash(city);

            Assert.Equal(mutatedExpected, mutatedActual);
            Assert.NotEqual(initialHash, mutatedHash);

            tile.Pollution = originalPollution;
            int restoredExpected = city.ResourceTiles.Sum(t => city.ShieldValue(t));
            int restoredActual = city.ShieldTotal;
            ulong restoredHash = GetCachedShieldRawStateHash(city);

            Assert.Equal(restoredExpected, restoredActual);
            Assert.Equal(initialTotal, restoredActual);
            Assert.Equal(initialHash, restoredHash);
        }

        private static ulong GetCachedShieldRawStateHash(City city)
        {
            var field = typeof(City).GetField("_cachedShieldRawStateHash", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);

            object rawValue = field.GetValue(city);
            Assert.NotNull(rawValue);

            return (ulong)rawValue;
        }

        [Fact]
        public void CalculateBreakdown_IncludesTradingRoutesAndWonderRules()
        {
            Player player = playa;
            City city = Game.Instance.AddCity(player, 0, 40, 30);
            City tradingCity = Game.Instance.AddCity(player, 1, 42, 30);

            city.AddTradingCity(tradingCity);
            city.AddBuilding(new MarketPlace());
            city.AddBuilding(new Bank());
            city.AddBuilding(new Library());
            city.AddBuilding(new University());
            city.AddWonder(new IsaacNewtonsCollege());
            city.AddWonder(new SETIProgram());
            city.AddWonder(new CopernicusObservatory());
            city.SetupSpecialists = new List<Citizen> { Citizen.Entertainer, Citizen.Taxman, Citizen.Scientist };
            city.InvalidateCityBreakdownCache();

            player.TaxesRate = 4;
            player.LuxuriesRate = 3;

            CityEconomyServiceImpl service = new(city, Game.Instance);
            CityEconomyBreakdown breakdown = service.CalculateBreakdown();

            int expectedTotalTrade = city.RawTradeTotal + city.TradingCitiesSumValue;
            short expectedTradeTaxes = service.CalculateTradeTaxes(expectedTotalTrade, player.TaxesRate);
            short expectedTradeLuxuries = service.CalculateTradeLuxuries(expectedTotalTrade, expectedTradeTaxes, player.TaxesRate, player.LuxuriesRate);
            short expectedTradeScience = service.CalculateTradeScience(expectedTotalTrade, expectedTradeLuxuries, expectedTradeTaxes);
            short expectedLuxuries = service.CalculateLuxuries(expectedTradeLuxuries, hasMarketPlace: true, hasBank: true, city.EntertainerLuxuries);
            short expectedTaxes = service.CalculateTaxes(expectedTradeTaxes, hasMarketPlace: true, hasBank: true, city.Taxmen);

            bool hasSeti = city.Player.HasWonder<SETIProgram>();
            bool hasNewton = !Game.Instance.WonderObsolete<IsaacNewtonsCollege>() && city.Player.HasWonder<IsaacNewtonsCollege>() && !hasSeti;
            bool hasCopernicus = !Game.Instance.WonderObsolete<CopernicusObservatory>() && city.HasWonder<CopernicusObservatory>();

            short expectedScience = service.CalculateScience(
                expectedTradeScience,
                hasLibrary: true,
                hasUniversity: true,
                hasSeti,
                hasNewton,
                hasCopernicus,
                city.Scientists);

            Assert.Equal(city.RawTradeTotal, breakdown.TradeTotal);
            Assert.Equal(expectedTotalTrade, breakdown.TotalTrade);
            Assert.Equal(expectedTradeScience, breakdown.TradeScience);
            Assert.Equal(expectedTradeLuxuries, breakdown.TradeLuxuries);
            Assert.Equal(expectedTradeTaxes, breakdown.TradeTaxes);
            Assert.Equal(expectedLuxuries, breakdown.Luxuries);
            Assert.Equal(expectedTaxes, breakdown.Taxes);
            Assert.Equal(expectedScience, breakdown.Science);
            Assert.True(city.TradingCitiesSumValue >= 0);
            Assert.True(hasSeti);
            Assert.False(hasNewton);
            Assert.True(hasCopernicus);
        }
    }
}
