using System.Linq;
using CivOne.Tiles;
using Xunit;

namespace CivOne.UnitTests
{
    public class IrrigateTest : TestsBase2
    {
        [Fact]
        public void AllowIrrigate()
        {
            // Test bugfix: AllowIrrigation() would return true for 
            // a tile immediately west of a city.

            // Using seed of 7595, Earth, Chinese, the initial city is
            // like so:
            //  P P O  - P: plains, O: ocean, M: Mtns, C: city
            //  P C O
            //  M P P

            var unit = Game.Instance.GetUnits().First(x => playa == x.Owner);
            City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);

            ITile tile = Map.Instance[unit.X, unit.Y];
            Assert.True(tile.HasCity);
            Assert.True(tile is Grassland);

            // Before 20190810, this would incorrectly return true
            Assert.False(Map.Instance[unit.X-1, unit.Y].AllowIrrigation());

            Assert.False(Map.Instance[unit.X-1, unit.Y-1].AllowIrrigation());
            Assert.False(Map.Instance[unit.X-1, unit.Y+1].AllowIrrigation());
            Assert.False(Map.Instance[unit.X-0, unit.Y+1].AllowIrrigation());

            Assert.True(Map.Instance[unit.X-0, unit.Y-1].AllowIrrigation());
            Assert.True(Map.Instance[unit.X+1, unit.Y+1].AllowIrrigation());
        }
    }
}
