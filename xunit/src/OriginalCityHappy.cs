using System.Collections.Generic;
using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Governments;
using CivOne.Screens.Services;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.src;
using Xunit;

namespace CivOne.UnitTests
{
    /// <summary>
    /// The integration cases of <see cref="CityHappy"/>, re-derived by hand against the corrected happiness
    /// model. Same game setup, same cities, same buildings, expectations recomputed from the formula rather
    /// than adjusted until they passed.
    ///
    /// The setup is a Babylonian despotism at king level with a single city, so the empire size penalty is
    /// zero and the base unhappiness is the city size minus three. The tax and science sliders sit at five
    /// tenths each, which leaves nothing for luxuries, so the only luxuries in these cases come from
    /// entertainers, at two points each.
    ///
    /// Two cases differ from the model CivOne shipped with, and both are marked below. The reason is the
    /// same in both: the original sets the happy count from the luxuries and then trims happy and unhappy
    /// together until they fit into the seats the city has, instead of upgrading citizens one at a time.
    /// </summary>
    /// <seealso cref="CityHappy"/>
    /// <seealso cref="OriginalCityCitizenServiceTests"/>
    public class OriginalCityHappy : TestsBase
    {
        private City AddCity(int size = 1)
        {
            IUnit unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            City? city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);

            Assert.NotNull(city);

            if (size > 1)
            {
                city.Size = (byte)size;
                city.ResetResourceTiles();
            }

            return city;
        }

        /// <summary>
        /// Adds a temple to the city, together with the advance that a real game would have required to build
        /// it. Ceremonial Burial is the temple's prerequisite, so a city that owns one always has an owner who
        /// knows it — except for captured cities, which the unit tests cover separately.
        /// </summary>
        /// <param name="city">The city to build the temple in.</param>
        private static void AddTempleAndItsAdvance(City city)
        {
            city.CityOwnerPlayer.AddAdvance(new CeremonialBurial(), setOrigin: false);
            city.AddBuilding(Reflect.GetBuildings().First(b => b is Temple));
        }

        private static List<CitizenTypes> Stages(City city)
        {
            city.UpdateSpecialists();

            OriginalCityCitizenService service = new(
                city,
                city,
                (IGameCitizenDependency)Game.Instance,
                [.. city.Specialists],
                Map.Instance,
                new HalvingPendingUnhappinessDelegate().Refill);

            return [.. service.EnumerateCitizens()];
        }

        /// <summary>
        /// Turns one citizen into an entertainer by releasing a resource tile, the same way the city
        /// manager does when a tile is clicked.
        /// </summary>
        /// <param name="city">The city to change.</param>
        private static void MakeOneEntertainer(City city)
        {
            foreach (ITile tile in city.ResourceTiles.ToArray())
            {
                if (tile.X != city.X || tile.Y != city.Y)
                {
                    city.SetResourceTile(tile);
                    return;
                }
            }

            Assert.Fail("failed to make entertainer");
        }

        /// <summary>
        /// Pins the assumptions every case below rests on.
        /// If one of these changes, the expectations in this file have to be recomputed rather than adjusted.
        /// </summary>
        [Fact]
        public void TheSetupTheseCasesAssume()
        {
            City city = AddCity();

            Assert.Equal(3, Game.Instance.Difficulty);
            Assert.IsType<Despotism>(playa.Government);
            Assert.Single(playa.Cities);
            Assert.True(((IGameCitizenDependency)Game.Instance).IsHumanPlayer(city.CityOwnerPlayerIndex));

            // Five tenths tax and five tenths science leave nothing for luxuries.
            Assert.Equal(0, playa.LuxuriesRate);
        }

        [Theory]
        [InlineData(1, 1, 0)]   // 1 - 3 is below zero, so nobody is unhappy
        [InlineData(2, 2, 0)]
        [InlineData(3, 3, 0)]
        [InlineData(4, 3, 1)]
        [InlineData(5, 3, 2)]
        public void ACityWithoutAnythingFollowsTheSizeTerm(int size, int expectedContent, int expectedUnhappy)
        {
            City city = AddCity(size);

            foreach (CitizenTypes stage in Stages(city))
            {
                Assert.Equal(expectedContent, stage.content);
                Assert.Equal(expectedUnhappy, stage.unhappy);
                Assert.Equal(0, stage.elvis);
            }
        }

        [Fact]
        public void ACityOfOneWithAnEntertainerHasNoWorkerLeft()
        {
            City city = AddCity();
            MakeOneEntertainer(city);

            CitizenTypes first = Stages(city)[0];

            Assert.Equal(0, first.content);
            Assert.Equal(1, first.elvis);
        }

        [Fact]
        public void ACityOfTwoWithOneEntertainer()
        {
            City city = AddCity(2);
            MakeOneEntertainer(city);

            List<CitizenTypes> stages = Stages(city);

            Assert.Equal(1, stages[0].content);
            Assert.Equal(1, stages[0].elvis);

            // Two luxuries make one citizen happy, and there is exactly one seat for them.
            Assert.Equal(1, stages[1].happy);
            Assert.Equal(1, stages[1].elvis);
        }

        [Fact]
        public void ACityOfTwoWithTwoEntertainers()
        {
            City city = AddCity(2);
            MakeOneEntertainer(city);
            MakeOneEntertainer(city);

            List<CitizenTypes> stages = Stages(city);

            Assert.Equal(0, stages[0].content);
            Assert.Equal(2, stages[0].elvis);

            // Four luxuries would make two citizens happy, but no seat is left for them.
            Assert.Equal(0, stages[1].happy);
            Assert.Equal(2, stages[1].elvis);
        }

        [Fact]
        public void ACityOfFourWithOneEntertainer()
        {
            City city = AddCity(4);
            MakeOneEntertainer(city);

            List<CitizenTypes> stages = Stages(city);

            Assert.Equal(2, stages[0].content);
            Assert.Equal(1, stages[0].unhappy);
            Assert.Equal(1, stages[0].elvis);

            // Two luxuries, one happy citizen, three seats: everything fits.
            Assert.Equal(1, stages[1].happy);
            Assert.Equal(1, stages[1].content);
            Assert.Equal(1, stages[1].unhappy);
            Assert.Equal(1, stages[1].elvis);
        }

        /// <summary>
        /// Differs from the model CivOne shipped with, which ends at two happy citizens and no content one.
        /// Four luxuries ask for two happy citizens, but with one unhappy citizen and two seats the city
        /// cannot show both, so one happy and one unhappy citizen are trimmed away together.
        /// </summary>
        [Fact]
        public void ACityOfFourWithTwoEntertainers()
        {
            City city = AddCity(4);
            MakeOneEntertainer(city);
            MakeOneEntertainer(city);

            List<CitizenTypes> stages = Stages(city);

            Assert.Equal(1, stages[0].content);
            Assert.Equal(1, stages[0].unhappy);
            Assert.Equal(2, stages[0].elvis);

            Assert.Equal(1, stages[1].happy);
            Assert.Equal(1, stages[1].content);
            Assert.Equal(0, stages[1].unhappy);
            Assert.Equal(2, stages[1].elvis);
        }

        [Fact]
        public void ACityOfFourWithThreeEntertainers()
        {
            City city = AddCity(4);
            MakeOneEntertainer(city);
            MakeOneEntertainer(city);
            MakeOneEntertainer(city);

            List<CitizenTypes> stages = Stages(city);

            Assert.Equal(0, stages[0].content);
            Assert.Equal(1, stages[0].unhappy);
            Assert.Equal(3, stages[0].elvis);

            // Six luxuries, one seat: one happy citizen is all the city can show.
            Assert.Equal(1, stages[1].happy);
            Assert.Equal(0, stages[1].content);
            Assert.Equal(0, stages[1].unhappy);
            Assert.Equal(3, stages[1].elvis);
        }

        [Fact]
        public void ACityOfFourWithATemple()
        {
            City city = AddCity(4);
            AddTempleAndItsAdvance(city);

            List<CitizenTypes> stages = Stages(city);

            Assert.Equal(3, stages[0].content);
            Assert.Equal(1, stages[0].unhappy);

            // No luxuries without entertainers.
            Assert.Equal(0, stages[1].happy);
            Assert.Equal(1, stages[1].unhappy);

            // The temple makes the last unhappy citizen content.
            Assert.Equal(4, stages[2].content);
            Assert.Equal(0, stages[2].unhappy);
        }

        /// <summary>
        /// Differs from the model CivOne shipped with, which ends at two happy and one content citizen.
        /// Six luxuries ask for three happy citizens against three unhappy ones in three seats, so two
        /// happy and two unhappy citizens are trimmed away together before the temple runs.
        /// </summary>
        [Fact]
        public void ACityOfSixWithThreeEntertainersAndATemple()
        {
            City city = AddCity(6);
            MakeOneEntertainer(city);
            MakeOneEntertainer(city);
            MakeOneEntertainer(city);
            AddTempleAndItsAdvance(city);

            List<CitizenTypes> stages = Stages(city);

            Assert.Equal(0, stages[0].content);
            Assert.Equal(3, stages[0].unhappy);
            Assert.Equal(3, stages[0].elvis);

            Assert.Equal(1, stages[1].happy);
            Assert.Equal(1, stages[1].content);
            Assert.Equal(1, stages[1].unhappy);

            Assert.Equal(1, stages[2].happy);
            Assert.Equal(2, stages[2].content);
            Assert.Equal(0, stages[2].unhappy);
            Assert.Equal(3, stages[2].elvis);
        }

        [Fact]
        public void ACityOfFiveWithOneEntertainerAndATemple()
        {
            City city = AddCity(5);
            MakeOneEntertainer(city);
            AddTempleAndItsAdvance(city);

            List<CitizenTypes> stages = Stages(city);

            Assert.Equal(0, stages[0].happy);
            Assert.Equal(2, stages[0].content);
            Assert.Equal(2, stages[0].unhappy);
            Assert.Equal(1, stages[0].elvis);

            Assert.Equal(1, stages[1].happy);
            Assert.Equal(1, stages[1].content);
            Assert.Equal(2, stages[1].unhappy);

            Assert.Equal(1, stages[2].happy);
            Assert.Equal(2, stages[2].content);
            Assert.Equal(1, stages[2].unhappy);
            Assert.Equal(1, stages[2].elvis);
        }

        [Fact]
        public void ACityOfFiveWithAColosseum()
        {
            City city = AddCity(5);
            city.AddBuilding(Reflect.GetBuildings().First(b => b is Colosseum));

            List<CitizenTypes> stages = Stages(city);

            Assert.Equal(3, stages[0].content);
            Assert.Equal(2, stages[0].unhappy);

            Assert.Equal(0, stages[1].happy);
            Assert.Equal(2, stages[1].unhappy);

            // Three points of colosseum against two unhappy citizens, the spare point is lost here.
            Assert.Equal(5, stages[2].content);
            Assert.Equal(0, stages[2].unhappy);
        }
    }
}
