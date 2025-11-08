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
using CivOne.Screens.Services;
using CivOne.Enums;
using System.Collections.Generic;
using CivOne.Wonders;

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
    public class CityCitizenServiceImplTests : TestsBase
    {
        CityCitizenServiceImpl testee;
        City city;
        List<Citizen> mockedSpecialists;
        MockedICityBuildings mockedCityBuildings = new();
        public override void BeforeEach()
        {
            var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);

            mockedSpecialists = [];

            testee = new CityCitizenServiceImpl(
                city,
                mockedCityBuildings,
                Game.Instance,
                mockedSpecialists,
                Map.Instance
            );
        }

        public override void AfterEach()
        {
            testee = null;
            city = null;
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

            Assert.Fail("failed to make entertainer");
        }

        [Fact]
        public void IsHappy()
        {
            Assert.True(testee.IsHappy(Citizen.HappyFemale));
            Assert.True(testee.IsHappy(Citizen.HappyMale));
        }

        // unhappy
        [Fact]
        public void IsUnhappy()
        {
            Assert.True(testee.IsUnhappy(Citizen.UnhappyFemale));
            Assert.True(testee.IsUnhappy(Citizen.UnhappyMale));
            Assert.True(testee.IsUnhappy(Citizen.RedShirtMale));
            Assert.True(testee.IsUnhappy(Citizen.RedShirtFemale));
        }

        [Fact]
        public void DowngradeCitizenTests()
        {
            Assert.Equal(Citizen.ContentMale, testee.DowngradeCitizen(Citizen.HappyMale));
            Assert.Equal(Citizen.ContentFemale, testee.DowngradeCitizen(Citizen.HappyFemale));
            Assert.Equal(Citizen.UnhappyMale, testee.DowngradeCitizen(Citizen.ContentMale));
            Assert.Equal(Citizen.UnhappyFemale, testee.DowngradeCitizen(Citizen.ContentFemale));
            Assert.Equal(Citizen.RedShirtMale, testee.DowngradeCitizen(Citizen.RedShirtMale));
            Assert.Equal(Citizen.RedShirtFemale, testee.DowngradeCitizen(Citizen.RedShirtFemale));
        }

        [Fact]
        public void UpgradeCitizenTests()
        {
            Assert.Equal(Citizen.ContentMale, testee.UpgradeCitizen(Citizen.UnhappyMale));
            Assert.Equal(Citizen.ContentFemale, testee.UpgradeCitizen(Citizen.UnhappyFemale));
            Assert.Equal(Citizen.HappyMale, testee.UpgradeCitizen(Citizen.ContentMale));
            Assert.Equal(Citizen.HappyFemale, testee.UpgradeCitizen(Citizen.ContentFemale));
            Assert.Equal(Citizen.UnhappyFemale, testee.UpgradeCitizen(Citizen.RedShirtMale));
            Assert.Equal(Citizen.UnhappyMale, testee.UpgradeCitizen(Citizen.RedShirtFemale));
        }

        [Fact]
        public void CitizenByIndexTests()
        {
            // even index   
            Assert.Equal(Citizen.HappyMale, testee.CitizenByIndex(0, Citizen.HappyMale));
            Assert.Equal(Citizen.HappyFemale, testee.CitizenByIndex(1, Citizen.HappyFemale));
            Assert.Equal(Citizen.UnhappyMale, testee.CitizenByIndex(2, Citizen.UnhappyMale));
            Assert.Equal(Citizen.UnhappyFemale, testee.CitizenByIndex(3, Citizen.UnhappyFemale));
            Assert.Equal(Citizen.Taxman, testee.CitizenByIndex(4, Citizen.Taxman));
            Assert.Equal(Citizen.Scientist, testee.CitizenByIndex(5, Citizen.Scientist));
            Assert.Equal(Citizen.Entertainer, testee.CitizenByIndex(6, Citizen.Entertainer));
            Assert.Equal(Citizen.RedShirtFemale, testee.CitizenByIndex(7, Citizen.RedShirtMale));
            Assert.Equal(Citizen.RedShirtMale, testee.CitizenByIndex(8, Citizen.RedShirtFemale));
        }

        [Fact]
        public void WearRedShirtTests()
        {
            var target = new Citizen[4];
            testee.WearRedShirt(target, 3);
            target[3] = Citizen.ContentMale;

            Assert.Equal(Citizen.RedShirtMale, target[0]);
            Assert.Equal(Citizen.RedShirtFemale, target[1]);
            Assert.Equal(Citizen.RedShirtMale, target[2]);
            Assert.NotEqual(Citizen.RedShirtFemale, target[3]);
        }

        [Fact]
        public void DowngradeCitizensTests()
        {
            var target = new Citizen[7];
            target[0] = Citizen.HappyMale;
            target[1] = Citizen.HappyFemale;
            target[2] = Citizen.ContentMale;
            target[3] = Citizen.ContentFemale;
            target[4] = Citizen.RedShirtMale;
            target[5] = Citizen.HappyFemale;
            target[6] = Citizen.HappyFemale;

            testee.DowngradeCitizens(target, 5);

            Assert.Equal(Citizen.ContentMale, target[0]);
            Assert.Equal(Citizen.ContentFemale, target[1]);
            Assert.Equal(Citizen.UnhappyMale, target[2]);
            Assert.Equal(Citizen.UnhappyFemale, target[3]);
            Assert.Equal(Citizen.RedShirtMale, target[4]);
            Assert.Equal(Citizen.ContentFemale, target[5]);
            // count exceeded, so no change
            Assert.Equal(Citizen.HappyFemale, target[6]);
        }

        [Fact]
        public void UnhappyToContentTests_ZeroCount()
        {
            var target = new Citizen[5];
            target[0] = Citizen.UnhappyMale;
            target[1] = Citizen.UnhappyFemale;
            target[2] = Citizen.RedShirtMale;
            target[3] = Citizen.ContentMale;
            target[4] = Citizen.HappyFemale;

            testee.UnhappyToContent(target, 0);

            Assert.Equal(Citizen.UnhappyMale, target[0]);
            Assert.Equal(Citizen.UnhappyFemale, target[1]);
            Assert.Equal(Citizen.RedShirtMale, target[2]);
            Assert.Equal(Citizen.ContentMale, target[3]);
            Assert.Equal(Citizen.HappyFemale, target[4]);
        }

        [Theory]
        [InlineData(1, 3)]
        [InlineData(2, 4)]
        [InlineData(3, 4)]
        [InlineData(4, 5)]
        [InlineData(5, 5)]
        [InlineData(6, 6)]
        [InlineData(7, 6)]
        [InlineData(8, 6)]
        [InlineData(9, 6)]
        [InlineData(10, 6)]
        [InlineData(11, 6)]
        public void UnhappyToContentTests(
            int conversionCount, int expectedContentCount)
        {
            mockedSpecialists.Clear();
            var target = new Citizen[8];
            target[0] = Citizen.UnhappyMale; // count necessary: 1, content: 2
            target[1] = Citizen.UnhappyFemale; // count necessary: 2, content: 4
            target[2] = Citizen.RedShirtMale; // count necessary: 4, content: 5
            target[3] = Citizen.RedShirtFemale; // count necessary: 6, content: 6
            target[4] = Citizen.ContentMale; // not changed
            target[5] = Citizen.ContentFemale; // not changed
            target[6] = Citizen.HappyMale; // not changed
            target[7] = Citizen.HappyFemale; // not changed

            int actualContentCount = target.Count(c => c == Citizen.ContentMale || c == Citizen.ContentFemale);
            testee.UnhappyToContent(target, conversionCount);

            int newContentCount = target.Count(c => c == Citizen.ContentMale || c == Citizen.ContentFemale);

            Assert.Equal(expectedContentCount, newContentCount);
        }

        [Theory]
        [InlineData(0, 2)]
        [InlineData(1, 3)]
        [InlineData(2, 4)]
        [InlineData(3, 4)]
        [InlineData(4, 4)]
        [InlineData(5, 4)]
        [InlineData(6, 4)]
        [InlineData(7, 4)]
        [InlineData(8, 4)]
        public void ContentToHappyTests(
            int conversionCount, int expectedHappyCount)
        {
            mockedSpecialists.Clear();
            var target = new Citizen[8];
            target[0] = Citizen.ContentMale; // count: 1, happy: 1
            target[1] = Citizen.ContentFemale; // count: 2, happy: 2
            target[2] = Citizen.UnhappyMale; // count: 3, happy: 3
            target[3] = Citizen.UnhappyFemale; // count: 4, happy: 4
            target[4] = Citizen.HappyMale; // not changed
            target[5] = Citizen.HappyFemale; // not changed
            target[6] = Citizen.RedShirtFemale; // not changed
            target[7] = Citizen.RedShirtMale; // not changed

            int actualHappyCount = target.Count(c => c == Citizen.HappyMale || c == Citizen.HappyFemale);
            testee.ContentToHappy(target, conversionCount);

            int newHappyCount = target.Count(c => c == Citizen.HappyMale || c == Citizen.HappyFemale);

            Assert.Equal(expectedHappyCount, newHappyCount);
        }

        [Fact]
        public void InitSpecialistsTest()
        {
            var specialists = new List<Citizen>
            {
                Citizen.HappyMale,
                Citizen.HappyFemale
            };
            var target = new Citizen[8];
            testee.InitSpecialists(specialists, target);

            Assert.Equal(Citizen.HappyMale, target[6]);
            Assert.Equal(Citizen.HappyFemale, target[7]);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(2, 1)]
        [InlineData(3, 1)]
        public void InitCitizensTests(
            int contentCount, int unhappyCount)
        {
            mockedSpecialists.Clear();

            var target = new Citizen[6];
            testee.InitCitizens(target, contentCount, unhappyCount);

            int actualUnhappyCount = target.Count(c => c == Citizen.UnhappyMale | c == Citizen.UnhappyFemale);
            Assert.Equal(unhappyCount, actualUnhappyCount);

            int actualContentCount = target.Count(c => c == Citizen.ContentMale | c == Citizen.ContentFemale);
            Assert.Equal(contentCount, actualContentCount);
        }

        [Theory]
        [InlineData(0, 2, 2)] // no upgrades
        [InlineData(1, 2, 2)] // not enough for redshirt
        [InlineData(2, 3, 2)] // 1st. redshirt to content
        [InlineData(3, 2, 3)] // 1st. content to happy
        [InlineData(6, 2, 4)] // 2nd. redshirt to content to happy
        [InlineData(8, 2, 5)] // 1st.unhappy to happy
        [InlineData(10, 2, 6)] // 2nd.unhappy to happy
        [InlineData(12, 0, 8)] // 2x content to happy
        [InlineData(14, 0, 8)] // no more upgrades
        public void UpgradeCitizensTests(
            int upgradeCount,
            int expectedContentCount,
            int expectedHappyCount)
        {
            mockedSpecialists.Clear();

            mockedSpecialists.Clear();
            var target = new Citizen[8];

            target[0] = Citizen.RedShirtMale;
            target[1] = Citizen.RedShirtFemale; 
            target[2] = Citizen.UnhappyMale; 
            target[3] = Citizen.UnhappyFemale; 
            target[4] = Citizen.ContentMale;
            target[5] = Citizen.ContentFemale;
            target[6] = Citizen.HappyMale;
            target[7] = Citizen.HappyFemale;

            testee.UpgradeCitizens(target, upgradeCount);

            int actualContentCount = target.Count(c => c == Citizen.ContentMale | c == Citizen.ContentFemale);
            Assert.Equal(expectedContentCount, actualContentCount);

            int actualHappyCount = target.Count(c => c == Citizen.HappyMale | c == Citizen.HappyFemale);
            Assert.Equal(expectedHappyCount, actualHappyCount);
        }

        public class MockedICityBuildings : ICityBuildings
        {
            private readonly SupplyMockedValues<bool> _hasBuilding;
            private readonly SupplyMockedValues<bool> _hasWonder;

            public MockedICityBuildings()
            {
                _hasBuilding = new SupplyMockedValues<bool>();
                _hasWonder = new SupplyMockedValues<bool>();
            }

            public MockedICityBuildings ReturnHasBuildingValues(params bool[] values)
            {
                _hasBuilding.Reset(values);
                return this;
            }

            public MockedICityBuildings ReturnHasWonderValues(params bool[] values)
            {
                _hasWonder.Reset(values);
                return this;
            }

            public bool HasBuilding<T>() where T : IBuilding => _hasBuilding.Next();

            public bool HasWonder<T>() where T : IWonder => _hasWonder.Next();
        }

        // /// <summary>
        // /// City size 2, with 1 entertainer: results are 1 happy, 1 entertainer
        // /// </summary>
        // [Fact]
        // public void CityHappy2With1Entertainer()
        // {
        //     var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
        //     City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
        //     acity.Size = 2;

        //     MakeOneEntertainer(acity);

        //     using (var foo = acity.Residents.GetEnumerator())
        //     {
        //         foo.MoveNext();
        //         var citizenTypes = foo.Current;
        //         Assert.Equal(1, citizenTypes.content);
        //         Assert.Equal(1, citizenTypes.elvis);

        //         foo.MoveNext();
        //         citizenTypes = foo.Current;
        //         Assert.Equal(1, citizenTypes.happy);
        //         Assert.Equal(1, citizenTypes.elvis);

        //     }
        // }

        // /// <summary>
        // /// First tricky one. City size 6 at King level: starts with 3 content, 3 unhappy.
        // /// Switch 3 people to entertainers: now 3 unhappy, 3 entertainers. Entertainers
        // /// make content people happy, then unhappy people content, but sequentially.
        // /// Specifically: a) 1 unhappy-> 1 content; b) the 1 content-> 1 happy; c) 1 unhappy
        // /// to 1 content. Final: 1 happy, 1 content, 1 unhappy, 3 entertainers.
        // ///
        // /// The "parallel" approach would be to make all 3 unhappy people content, but
        // /// that is not how Microprose did it.
        // /// </summary>
        // [Fact]
        // public void City6With3EntertainersAndTemple()
        // {
        //     var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
        //     City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
        //     acity.Size = 6;
        //     acity.ResetResourceTiles(); // setting city size doesn't allocate all resources

        //     MakeOneEntertainer(acity);
        //     MakeOneEntertainer(acity);
        //     MakeOneEntertainer(acity);
        //     acity.AddBuilding(Reflect.GetBuildings().First(b => b is Temple));

        //     using (var foo = acity.Residents.GetEnumerator())
        //     {
        //         foo.MoveNext();
        //         var citizenTypes = foo.Current;
        //         Assert.Equal(0, citizenTypes.content);
        //         Assert.Equal(3, citizenTypes.unhappy);
        //         Assert.Equal(3, citizenTypes.elvis);

        //         foo.MoveNext();
        //         citizenTypes = foo.Current;
        //         Assert.Equal(1, citizenTypes.happy);
        //         Assert.Equal(1, citizenTypes.content);
        //         Assert.Equal(1, citizenTypes.unhappy);
        //         Assert.Equal(3, citizenTypes.elvis);

        //         foo.MoveNext();
        //         citizenTypes = foo.Current;
        //         // temple effect
        //         Assert.Equal(1, citizenTypes.happy);
        //         Assert.Equal(2, citizenTypes.content);
        //         Assert.Equal(0, citizenTypes.unhappy);
        //         Assert.Equal(3, citizenTypes.elvis);
        //     }

        // }

        // [Fact]
        // public void City5With1EntAndTemple()
        // {
        //     var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
        //     City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
        //     acity.Size = 5;
        //     acity.ResetResourceTiles(); // setting city size doesn't allocate all resources

        //     MakeOneEntertainer(acity);
        //     acity.AddBuilding(Reflect.GetBuildings().First(b => b is Temple));

        //     using (var foo = acity.Residents.GetEnumerator())
        //     {
        //         foo.MoveNext();
        //         var citizenTypes = foo.Current;
        //         // initial state
        //         Assert.Equal(0, citizenTypes.happy);
        //         Assert.Equal(2, citizenTypes.content);
        //         Assert.Equal(2, citizenTypes.unhappy);
        //         Assert.Equal(1, citizenTypes.elvis);

        //         foo.MoveNext();
        //         citizenTypes = foo.Current;
        //         // luxury
        //         Assert.Equal(1, citizenTypes.happy);
        //         Assert.Equal(1, citizenTypes.content);
        //         Assert.Equal(2, citizenTypes.unhappy);
        //         Assert.Equal(1, citizenTypes.elvis);

        //         foo.MoveNext();
        //         citizenTypes = foo.Current;
        //         // temple
        //         Assert.Equal(1, citizenTypes.happy);
        //         Assert.Equal(2, citizenTypes.content);
        //         Assert.Equal(1, citizenTypes.unhappy);
        //         Assert.Equal(1, citizenTypes.elvis);
        //     }

        // }

        // [Fact]
        // public void City5Colosseum()
        // {
        //     var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
        //     City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
        //     acity.Size = 5;
        //     acity.ResetResourceTiles(); // setting city size doesn't allocate all resources

        //     acity.AddBuilding(Reflect.GetBuildings().First(b => b is Colosseum));

        //     using (var foo = acity.Residents.GetEnumerator())
        //     {
        //         foo.MoveNext();
        //         var citizenTypes = foo.Current;
        //         // initial state
        //         Assert.Equal(0, citizenTypes.happy);
        //         Assert.Equal(3, citizenTypes.content);
        //         Assert.Equal(2, citizenTypes.unhappy);
        //         Assert.Equal(0, citizenTypes.elvis);

        //         foo.MoveNext();
        //         citizenTypes = foo.Current;
        //         // luxury
        //         Assert.Equal(0, citizenTypes.happy);
        //         Assert.Equal(3, citizenTypes.content);
        //         Assert.Equal(2, citizenTypes.unhappy);
        //         Assert.Equal(0, citizenTypes.elvis);

        //         foo.MoveNext();
        //         citizenTypes = foo.Current;
        //         // Colosseum
        //         Assert.Equal(0, citizenTypes.happy);
        //         Assert.Equal(5, citizenTypes.content);
        //         Assert.Equal(0, citizenTypes.unhappy);
        //         Assert.Equal(0, citizenTypes.elvis);
        //     }

        // }
    }
}
