// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

// Author: Kevin Routley : July, 2019

using CivOne.src;
using System.Linq;
using CivOne.Buildings;
using Xunit;

namespace CivOne.UnitTests
{

    /// <summary>
    /// Tests to exercise City citizen happiness. Citizen happiness
    /// is displayed in the 'Happy' pane of the City manager view
    /// as a five-step sequence:
    /// 1. initial state - determined by game difficulty
    /// 2. impact of luxuries - from entertainers or settings
    /// 3. impact of buildings - temple, etc
    /// 4. impact of units - presence or absence of units depending on government
    /// 5. impact of wonders
    /// The final results are displayed in the 'header' of the City manager
    /// view and dictate if a city goes into disorder or celebration.
    /// </summary>
    public class CityHappy : TestsBase
    {
        /// <summary>
        /// With no buildings, luxuries, wonders or martial law,
        /// the count/types of citizens will remain unchanged
        /// </summary>
        [Fact]
        public void CityHappyBasic()
        {
            var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);

            foreach (var citizenTypes in acity.Residents)
            {
                Assert.Equal(1, citizenTypes.content);
                Assert.Equal(0, citizenTypes.elvis);
            }

            acity.Size = 2;

            foreach (var citizenTypes in acity.Residents)
            {
                Assert.Equal(2, citizenTypes.content);
                Assert.Equal(0, citizenTypes.elvis);
            }

            acity.Size = 3;

            foreach (var citizenTypes in acity.Residents)
            {
                Assert.Equal(3, citizenTypes.content);
                Assert.Equal(0, citizenTypes.elvis);
            }

            acity.Size = 4; // at King level, 3 content and 1 unhappy

            foreach (var citizenTypes in acity.Residents)
            {
                Assert.Equal(3, citizenTypes.content);
                Assert.Equal(1, citizenTypes.unhappy);
                Assert.Equal(0, citizenTypes.elvis);
            }

            acity.Size = 5; // at King level, 3 content and 2 unhappy

            foreach (var citizenTypes in acity.Residents)
            {
                Assert.Equal(3, citizenTypes.content);
                Assert.Equal(2, citizenTypes.unhappy);
                Assert.Equal(0, citizenTypes.elvis);
            }

        }

        /// <summary>
        /// City size 1: change to an entertainer
        /// </summary>
        [Fact]
        public void CityHappy1Entertainer()
        {
            var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);

            MakeOneEntertainer(acity);

            foreach (var citizenTypes in acity.Residents)
            {
                Assert.Equal(0, citizenTypes.content);
                Assert.Equal(1, citizenTypes.elvis);
            }

        }

        /// <summary>
        /// Turn one citizen into an entertainer. This is done
        /// by using SetResourceTile() to toggle the first resource
        /// generating tile [like clicking on a resource tile in
        /// the City Manager map].
        /// </summary>
        /// <param name="acity"></param>
        private void MakeOneEntertainer(City acity)
        {
            var tiles = acity.ResourceTiles.ToArray();
            foreach (var tile in tiles)
            {
                if (tile.X != acity.X || tile.Y != acity.Y)
                {
                    acity.SetResourceTile(tile);
                    acity.Citizens.ToArray(); // TODO fire-eggs used to force side effect of updating specialists counts
                    return;
                }
            }

            Assert.True(false, "failed to make entertainer");
        }

        /// <summary>
        /// City size 2, with 1 entertainer: results are 1 happy, 1 entertainer
        /// </summary>
        [Fact]
        public void CityHappy2With1Entertainer()
        {
            var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
            acity.Size = 2;

            MakeOneEntertainer(acity);

            using (var foo = acity.Residents.GetEnumerator())
            {
                foo.MoveNext();
                var citizenTypes = foo.Current;
                Assert.Equal(1, citizenTypes.content);
                Assert.Equal(1, citizenTypes.elvis);

                foo.MoveNext();
                citizenTypes = foo.Current;
                Assert.Equal(1, citizenTypes.happy);
                Assert.Equal(1, citizenTypes.elvis);
            }
        }

        /// <summary>
        /// City size 2, with 2 entertainer: results are always 2 entertainer
        /// </summary>
        [Fact]
        public void CityHappy2With2Entertainers()
        {
            var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
            acity.Size = 2;

            MakeOneEntertainer(acity);
            MakeOneEntertainer(acity);

            using (var foo = acity.Residents.GetEnumerator())
            {
                foo.MoveNext();
                var citizenTypes = foo.Current;
                Assert.Equal(0, citizenTypes.content);
                Assert.Equal(2, citizenTypes.elvis);

                foo.MoveNext();
                citizenTypes = foo.Current;
                Assert.Equal(0, citizenTypes.happy);
                Assert.Equal(2, citizenTypes.elvis);

            }
        }

        /// <summary>
        /// King level: city size 4 starts w/ 3 content, 1 unhappy. Add an
        /// entertainer, 1 content is made happy
        /// </summary>
        [Fact]
        public void CityHappy4With1Entertainers()
        {
            var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
            acity.Size = 4;
            acity.ResetResourceTiles();
            MakeOneEntertainer(acity);

            using (var foo = acity.Residents.GetEnumerator())
            {
                foo.MoveNext();
                var citizenTypes = foo.Current;
                Assert.Equal(2, citizenTypes.content);
                Assert.Equal(1, citizenTypes.unhappy);
                Assert.Equal(1, citizenTypes.elvis);

                foo.MoveNext();
                citizenTypes = foo.Current;
                Assert.Equal(1, citizenTypes.happy);
                Assert.Equal(1, citizenTypes.content);
                Assert.Equal(1, citizenTypes.unhappy);
                Assert.Equal(1, citizenTypes.elvis);
            }

        }

        /// <summary>
        /// King level: city size 4 starts w/ 3 content, 1 unhappy. Add 2
        /// entertainer, 1 content is made happy, 1 unhappy made content
        /// </summary>
        [Fact]
        public void CityHappy4With2Entertainers()
        {
            var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
            acity.Size = 4;
            acity.ResetResourceTiles(); // setting city size doesn't allocate all resources

            MakeOneEntertainer(acity);
            MakeOneEntertainer(acity);

            using (var foo = acity.Residents.GetEnumerator())
            {
                foo.MoveNext();
                var citizenTypes = foo.Current;
                Assert.Equal(1, citizenTypes.content);
                Assert.Equal(1, citizenTypes.unhappy);
                Assert.Equal(2, citizenTypes.elvis);

                foo.MoveNext();
                citizenTypes = foo.Current;
                // CW: 2 Entertainer = 2 * 3 Luxury = 6
                // 6 Luxury = upgrade 3 times 
                // 1 content -> happy, 1 unhappy -> content -> happy
                Assert.Equal(2, citizenTypes.happy);
                Assert.Equal(0, citizenTypes.content);
                Assert.Equal(0, citizenTypes.unhappy);
                Assert.Equal(2, citizenTypes.elvis);
            }

        }

        /// <summary>
        /// King level: city size 4 starts w/ 3 content, 1 unhappy. Add 3
        /// entertainer, 1 person remaining is happy
        /// </summary>
        [Fact]
        public void CityHappy4With3Entertainers()
        {
            var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
            acity.Size = 4;
            acity.ResetResourceTiles(); // setting city size doesn't allocate all resources

            MakeOneEntertainer(acity);
            MakeOneEntertainer(acity);
            MakeOneEntertainer(acity);

            using (var foo = acity.Residents.GetEnumerator())
            {
                foo.MoveNext();
                var citizenTypes = foo.Current;
                Assert.Equal(0, citizenTypes.content);
                Assert.Equal(1, citizenTypes.unhappy);
                Assert.Equal(3, citizenTypes.elvis);

                foo.MoveNext();
                citizenTypes = foo.Current;
                Assert.Equal(1, citizenTypes.happy);
                Assert.Equal(0, citizenTypes.content);
                Assert.Equal(0, citizenTypes.unhappy);
                Assert.Equal(3, citizenTypes.elvis);
            }

        }

        /// <summary>
        /// City size 4, King: temple and no other changes means 4 content
        /// </summary>
        [Fact]
        public void City4Temple()
        {
            var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
            acity.Size = 4;
            acity.ResetResourceTiles(); // setting city size doesn't allocate all resources

            acity.AddBuilding(Reflect.GetBuildings().First(b => b is Temple));

            using (var foo = acity.Residents.GetEnumerator())
            {
                foo.MoveNext();
                var citizenTypes = foo.Current;
                // initial state
                Assert.Equal(0, citizenTypes.happy);
                Assert.Equal(3, citizenTypes.content);
                Assert.Equal(1, citizenTypes.unhappy);

                foo.MoveNext();
                citizenTypes = foo.Current;
                // no luxury effect
                Assert.Equal(0, citizenTypes.happy);
                Assert.Equal(3, citizenTypes.content);
                Assert.Equal(1, citizenTypes.unhappy);
                Assert.Equal(0, citizenTypes.elvis);

                foo.MoveNext();
                citizenTypes = foo.Current;
                // temple effect
                Assert.Equal(0, citizenTypes.happy);
                Assert.Equal(4, citizenTypes.content);
                Assert.Equal(0, citizenTypes.unhappy);
                Assert.Equal(0, citizenTypes.elvis);
            }

        }

        /// <summary>
        /// First tricky one. City size 6 at King level: starts with 3 content, 3 unhappy.
        /// Switch 3 people to entertainers: now 3 unhappy, 3 entertainers. Entertainers
        /// make content people happy, then unhappy people content, but sequentially.
        /// Specifically: a) 1 unhappy-> 1 content; b) the 1 content-> 1 happy; c) 1 unhappy
        /// to 1 content. Final: 1 happy, 1 content, 1 unhappy, 3 entertainers.
        ///
        /// The "parallel" approach would be to make all 3 unhappy people content, but
        /// that is not how Microprose did it.
        /// </summary>
        [Fact]
        public void City6With3EntertainersAndTemple()
        {
            var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
            acity.Size = 6;
            acity.ResetResourceTiles(); // setting city size doesn't allocate all resources

            MakeOneEntertainer(acity);
            MakeOneEntertainer(acity);
            MakeOneEntertainer(acity);
            acity.AddBuilding(Reflect.GetBuildings().First(b => b is Temple));

            using (var foo = acity.Residents.GetEnumerator())
            {
                foo.MoveNext();
                var citizenTypes = foo.Current;
                Assert.Equal(0, citizenTypes.content);
                Assert.Equal(3, citizenTypes.unhappy);
                Assert.Equal(3, citizenTypes.elvis);

                foo.MoveNext();
                citizenTypes = foo.Current;
                // CW: 3 Entertainer = 3 * 3 Luxury = 9
                // 9 Luxury = upgrade 4 times
                // unhappy-> 2 content -> 2 happy
                Assert.Equal(2, citizenTypes.happy);
                Assert.Equal(0, citizenTypes.content);
                Assert.Equal(1, citizenTypes.unhappy);
                Assert.Equal(3, citizenTypes.elvis);

                foo.MoveNext();
                citizenTypes = foo.Current;
                // temple effect
                Assert.Equal(2, citizenTypes.happy);
                Assert.Equal(1, citizenTypes.content);
                Assert.Equal(0, citizenTypes.unhappy);
                Assert.Equal(3, citizenTypes.elvis);
            }

        }

        [Fact]
        public void City5With1EntAndTemple()
        {
            var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
            acity.Size = 5;
            acity.ResetResourceTiles(); // setting city size doesn't allocate all resources

            MakeOneEntertainer(acity);
            acity.AddBuilding(Reflect.GetBuildings().First(b => b is Temple));

            using (var foo = acity.Residents.GetEnumerator())
            {
                foo.MoveNext();
                var citizenTypes = foo.Current;
                // initial state
                // difficulty 3: 3 content, 2 unhappy
                Assert.Equal(0, citizenTypes.happy);
                Assert.Equal(2, citizenTypes.content);
                Assert.Equal(2, citizenTypes.unhappy);
                Assert.Equal(1, citizenTypes.elvis);

                foo.MoveNext();
                citizenTypes = foo.Current;
                // luxury
                Assert.Equal(1, citizenTypes.happy);
                Assert.Equal(1, citizenTypes.content);
                Assert.Equal(2, citizenTypes.unhappy);
                Assert.Equal(1, citizenTypes.elvis);

                foo.MoveNext();
                citizenTypes = foo.Current;
                // temple
                Assert.Equal(1, citizenTypes.happy);
                Assert.Equal(2, citizenTypes.content);
                Assert.Equal(1, citizenTypes.unhappy);
                Assert.Equal(1, citizenTypes.elvis);
            }

        }

        [Fact]
        public void City5Colosseum()
        {
            var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
            acity.Size = 5;
            acity.ResetResourceTiles(); // setting city size doesn't allocate all resources

            acity.AddBuilding(Reflect.GetBuildings().First(b => b is Colosseum));

            using (var foo = acity.Residents.GetEnumerator())
            {
                foo.MoveNext();
                var citizenTypes = foo.Current;
                // initial state
                Assert.Equal(0, citizenTypes.happy);
                Assert.Equal(3, citizenTypes.content);
                Assert.Equal(2, citizenTypes.unhappy);
                Assert.Equal(0, citizenTypes.elvis);

                foo.MoveNext();
                citizenTypes = foo.Current;
                // luxury
                Assert.Equal(0, citizenTypes.happy);
                Assert.Equal(3, citizenTypes.content);
                Assert.Equal(2, citizenTypes.unhappy);
                Assert.Equal(0, citizenTypes.elvis);

                foo.MoveNext();
                citizenTypes = foo.Current;
                // Colosseum
                Assert.Equal(0, citizenTypes.happy);
                Assert.Equal(5, citizenTypes.content);
                Assert.Equal(0, citizenTypes.unhappy);
                Assert.Equal(0, citizenTypes.elvis);
            }

        }
    }
}
