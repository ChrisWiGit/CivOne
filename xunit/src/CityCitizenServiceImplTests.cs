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
using System.Drawing;
using CivOne.Tiles;
using CivOne.Units;
using System;

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

        MockedIGame mockedIGame;
        MockedICityBuildings mockedCityBuildings;
        MockedICityBasic mockedCityBasic;
        public override void BeforeEach()
        {
            // var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            // city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);

            mockedSpecialists = [];

            mockedCityBuildings = new MockedICityBuildings();
            mockedCityBasic = new MockedICityBasic()
            {
                Size = 1
            };

            mockedIGame = new MockedIGame()
            {
                Difficulty = 4,
                MaxDifficulty = 5,
                GameTurn = 1
            };

            testee = new CityCitizenServiceImpl(
                mockedCityBasic,
                mockedCityBuildings,
                mockedIGame,//Game.Instance,
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

            // content citizens are at start of array
            for (int i = 0; i < contentCount; i++)
            {
                Assert.True(target[i] == Citizen.ContentMale || target[i] == Citizen.ContentFemale);
            }
            // unhappy citizens follow content citizens
            for (int i = contentCount; i < contentCount + unhappyCount; i++)
            {
                Assert.True(target[i] == Citizen.UnhappyMale || target[i] == Citizen.UnhappyFemale);
            }
        }

        [Theory]
        [InlineData(0, 2, 2)] // no upgrades
        [InlineData(1, 2, 2)] // not enough for redshirt
        [InlineData(2, 3, 2)] // 1st. redshirt to content
        [InlineData(3, 2, 3)] // 1st. content to happy
        [InlineData(6, 2, 4)] // 2nd. redshirt to content to happy
        [InlineData(8, 2, 5)] // 1st.unhappy to happy (takes 2 upgrades)
        [InlineData(10, 2, 6)] // 2nd.unhappy to happy (takes 2 upgrades)
        [InlineData(12, 0, 8)] // 2x content to happy
        [InlineData(14, 0, 8)] // no more upgrades
        public void UpgradeCitizensTests(
            int upgradeCount,
            int expectedContentCount,
            int expectedHappyCount)
        {
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

        [Fact]
        public void CountCitizenTypesTests()
        {
            var target = new Citizen[9];
            target[0] = Citizen.HappyMale;
            target[1] = Citizen.HappyFemale;
            target[2] = Citizen.ContentMale;
            target[3] = Citizen.ContentFemale;
            target[4] = Citizen.UnhappyMale;
            target[5] = Citizen.UnhappyFemale;
            target[6] = Citizen.Taxman;
            target[7] = Citizen.Scientist;
            target[8] = Citizen.Entertainer;

            var (happy, content, unhappy, redShirt) = testee.CountCitizenTypes(target);

            Assert.Equal(2, happy);
            Assert.Equal(2, content);
            Assert.Equal(2, unhappy);
        }

        [Theory]
        [InlineData(2, 2)]
        [InlineData(3, 2)]
        [InlineData(4, 1)]
        [InlineData(5, 1)]
        [InlineData(6, 0)]
        public void StageBasicTests(
            int initialContent,
            int initialUnhappy)
        {
            mockedCityBasic.Size = (byte)(initialContent + initialUnhappy);

            var ct = new CitizenTypes
            {
                Citizens = new Citizen[mockedCityBasic.Size]
            };
            ct = testee.StageBasic(ct, initialContent, initialUnhappy);

            Assert.Equal(initialContent, ct.content);
            Assert.Equal(initialUnhappy, ct.unhappy);

            var (happy, content, unhappy, redShirt) = testee.CountCitizenTypes(ct.Citizens);

            Assert.Equal(ct.happy, happy);
            Assert.Equal(ct.content, content);
            Assert.Equal(ct.unhappy, unhappy);
        }

        [Fact]
        public void HasBachsCathedralTests()
        {
            Assert.Fail("Not implemented");
            //     		protected internal bool HasBachsCathedral()
            // {
            // 	return _city.Tile != null
            // 				&& _map.ContinentCities(_city.Tile.ContinentId)
            // 					.Any(x => x.Size > 0 && x.Owner == _city.Owner && x.HasWonder<JSBachsCathedral>());
            // }
        }

        [Fact]
        public void CathedralDeltaTest()
        {
            Assert.Fail("Not implemented");
            // internal int CathedralDelta()
            // {
            // 	if (!_cityBuildings.HasBuilding<Cathedral>()) return 0;

            // 	int unhappyDelta = 0;

            // 	// CW: Michelangelo's Chapel gives +6 happiness if on same continent as city with wonder, else +4
            // 	// https://civilization.fandom.com/wiki/Michelangelo%27s_Chapel_(Civ1)
            // 	bool hasChapel = !_game.WonderObsolete<MichelangelosChapel>()
            // 			&& _game.GetPlayer(_city.Owner).Cities.Any(c => c.HasWonder<MichelangelosChapel>()
            // 			&& c.ContinentId == _city.ContinentId);
            // 	int chapelBonus = hasChapel ? 6 : 4;

            // 	unhappyDelta += chapelBonus;

            // 	return unhappyDelta;
            // }
        }

        [Fact]
        public void ApplyBuildingEffectsTests()
        {
            Assert.Fail("Not implemented");
            // protected internal void ApplyBuildingEffects(CitizenTypes ct)
            // {
            // 	if (_cityBuildings.HasWonder<ShakespearesTheatre>() && !_game.WonderObsolete<ShakespearesTheatre>())
            // 	{
            // 		// All unhappy become content, but only in this city.
            // 		UnhappyToContent(ct.Citizens, ct.unhappy);

            // 		ct.Wonders.Add(new ShakespearesTheatre());

            // 		// Continuing would not make sense, as all unhappy are already content
            // 		return;
            // 	}

            // 	int unhappyToContent = 0;

            // 	if (_cityBuildings.HasBuilding<Temple>())
            // 	{
            // 		unhappyToContent++;
            // 		if (_city.Player.HasAdvance<Mysticism>()) unhappyToContent <<= 1;
            // 		if (_city.Player.HasWonderEffect<Oracle>())
            // 		{
            // 			unhappyToContent <<= 1;
            // 			// CW: showing this wonder while processing it in this stage
            // 			// would be confusing for the player to see
            // 			// ct.Wonders.Add(new Oracle()); 
            // 		}

            // 		ct.Buildings.Add(new Temple());
            // 	}

            // 	if (HasBachsCathedral())
            // 	{
            // 		unhappyToContent += 2;
            // 		// CW: Same as above, don't show wonder here
            // 		// ct.Wonders.Add(new JSBachsCathedral());
            // 	}

            // 	if (_cityBuildings.HasBuilding<Colosseum>())
            // 	{
            // 		unhappyToContent += 3;
            // 		ct.Buildings.Add(new Colosseum());
            // 	}

            // 	unhappyToContent += CathedralDelta();

            // 	if (unhappyToContent <= 0)
            // 	{
            // 		return;
            // 	}

            // 	UnhappyToContent(ct.Citizens, unhappyToContent);
            // }
        }

        [Fact]
        public void ApplyMartialLawTests()
        {
            Assert.Fail("Not implemented");
            // 			protected internal void ApplyMartialLaw(CitizenTypes ct)
            // {
            // 	if (!_city.Player.AnarchyDespotism && !_city.Player.MonarchyCommunist)
            // 	{
            // 		return;
            // 	}

            // 	var attackUnitsInCity = _city.Tile.Units.Where(u => u.Attack > 0);

            // 	ct.MarshallLawUnits.AddRange(attackUnitsInCity);

            // 	const int MAX_MARTIAL_LAW_UNITS = 3;

            // 	int martialLawUnits = Math.Min(MAX_MARTIAL_LAW_UNITS, attackUnitsInCity.Count());
            // 	int unhappyToContent = Math.Min(martialLawUnits, ct.unhappy);

            // 	UnhappyToContent(ct.Citizens, unhappyToContent);
            // }
        }

        [Fact]
        public void ApplyDemocracyEffectsTests()
        {
            Assert.Fail("Not implemented");
            // 		protected internal void ApplyWonderEffects(CitizenTypes ct)
            // {
            // 	int happy = 0;
            // 	if (_city.Player.HasWonderEffect<HangingGardens>() && !_game.WonderObsolete<HangingGardens>())
            // 	{
            // 		happy += 1;
            // 		ct.Wonders.Add(new HangingGardens());
            // 	}
            // 	if (_city.Player.HasWonderEffect<CureForCancer>() && !_game.WonderObsolete<CureForCancer>())
            // 	{
            // 		happy += 1;
            // 		ct.Wonders.Add(new CureForCancer());
            // 	}

            // 	int happyToContent = Math.Min(happy, ct.content);
            // 	ContentToHappy(ct.Citizens, happyToContent);
            // }
        }

        [Fact]
        public void CreateCitizenTypesTests()
        {
            Assert.Fail("Not implemented");
            // 		protected internal CitizenTypes CreateCitizenTypes()
            // {
            // 	return new()
            // 	{
            // 		happy = 0,
            // 		content = 0,
            // 		unhappy = 0,
            // 		redshirt = 0,
            // 		elvis = _city.Entertainers,
            // 		einstein = _city.Scientists,
            // 		taxman = _city.Taxmen,
            // 		Citizens = new Citizen[_city.Size],
            // 		Buildings = [],
            // 		Wonders = [],
            // 		MarshallLawUnits = []
            // 	};
            // }
        }

        [Theory]
        [InlineData(35, 0)]
        [InlineData(36, 0)]
        [InlineData(37, 1)]
        [InlineData(48, 2)]
        [InlineData(61, 3)]
        [InlineData(62, 3)]
        public void NumberOfRedShirtsTests(int totalCities, int expectedRedShirts)
        {
            int result = testee.NumberOfRedShirts(totalCities);
            Assert.Equal(expectedRedShirts, result);
        }

        [Theory]
        [InlineData(3, 10, 5, 5, 0)] // difficulty too low
        [InlineData(4, 10, 5, 5, 0)] // total cities too low < 12
        [InlineData(4, 12, 5, 5, 0)] // total cities at limit = 12 
        [InlineData(4, 13, 5, 1, 0)] // total cities just above limit > 13
        [InlineData(4, 24, 5, 1, 0)] // total cities at limit = 24
        [InlineData(4, 25, 5, 0, 0)] // total cities just above limit > 24
        [InlineData(4, 36, 5, 0, 0)] // total cities well above limit > 24
        [InlineData(5, 37, 5, 0, 1)] // first redshirt at 37 cities > 36
        [InlineData(5, 48, 5, 0, 2)] // second redshirt at 48 cities >= 48
        [InlineData(5, 61, 5, 0, 3)] // third redshirt at 61 cities
        [InlineData(5, 62, 5, 0, 3)] // third redshirt at 61 cities
        public void ApplyEmperorEffectsTests(
            int gameDifficulty,
            int totalCities,
            int citySize,
            int expectedBornContent,
            int expectedReadShirts
        )
        {
            mockedIGame.OnGetPlayer = (playerId) =>
            {
                return new MockPlayer(totalCities);
            };
            mockedIGame.Difficulty = gameDifficulty;
            mockedCityBasic.Size = (byte)citySize;

            CitizenTypes ct = new()
			{
                Citizens = new Citizen[citySize]
            };
            testee.InitCitizens(ct.Citizens, citySize, 0);
            testee.ApplyEmperorEffects(ct);

            var (happy, content, unhappy, redShirt) = testee.CountCitizenTypes(ct.Citizens);

            Assert.Equal(expectedBornContent, content);
            Assert.Equal(expectedReadShirts, redShirt);


            //    		protected internal void
            //  ApplyEmperorEffects(CitizenTypes ct)
            // {
            // 	if (_game.Difficulty < 4)
            // 	{
            // 		return;
            // 	}

            // 	int totalCities = _game.GetPlayer(_city.Owner).Cities.Count();

            // 	if (totalCities <= 12)
            // 	{
            // 		return;
            // 	}

            // 	// >= 24 cities = 1 born-content
            // 	// >= 36 cities = 0 born-content

            // 	int downgradeCount = _city.Size; // case >= 36

            // 	if (totalCities <= 24)
            // 	{
            // 		downgradeCount = 1;
            // 	}
            // 	DowngradeCitizens(ct.Citizens, downgradeCount);

            // 	WearRedShirt(ct.Citizens, NumberOfRedShirts(totalCities));
            // }
            // rotected internal int NumberOfRedShirts(int totalCities)
		// {
		// 	if (totalCities <= 36)
		// 	{
		// 		return 0;
		// 	}
		// 	return 1 + (totalCities - 36) / 12;
			// 1+ (37-36) /12 = 1 + 1/12 = 1
			// 1+ (48-36) /12 = 1 + 12/12 = 2
			// 1+ (61-36) /12 = 1 + 25/12 = 3
        }
        partial class MockPlayer(int citiesCount) : Player()
        {
            private readonly int citiesCount = citiesCount;
            public override City[] Cities => new City[citiesCount];
        }

        [Theory]
        [InlineData(2, 2)]
        public void CalculateCityStatsTests(int initialUnhappyCount, int initialContent)
        {
            Assert.Fail("Not implemented");
            // protected internal (int initialUnhappyCount, int initialContent)
            // 	CalculateCityStats(CitizenTypes ct, IGame _game)
            // {
            // 	// max difficulty = 4|5, easiest = 0
            // 	// int difficulty = 4; //Debug only
            // 	int difficulty = _game.Difficulty;
            // 	int specialists = ct.elvis + ct.einstein + ct.taxman;
            // 	int workersAvailable = _city.Size - specialists;

            // 	// https://civfanatics.com/civ1/difficulty/
            // 	// diff 0 = 6 content, all else unhappy
            // 	// diff 1 = 5 content, all else unhappy
            // 	// diff 2 = 4 content, all else unhappy
            // 	// diff 3 = 3 content, all else unhappy
            // 	// diff 4 = 2 content, all else unhappy
            // 	// diff 5 = 1 content, all else unhappy
            // 	int contentLimit = 6 - difficulty - specialists;

            // 	// size 4, 0 ent → specialists=0, available=4 → contentLimit=3 → 3c + 1u
            // 	// size 4, 1 ent → specialists=1, available=3 → 2c + 1u + 1ent
            // 	// size 4, 2 ent → specialists=2, available=2 → 1c + 1u + 2ent
            // 	// size 4, 3 ent → specialists=3, available=1 → 0c + 1u + 3ent
            // 	// Anzahl der zufriedenen Bürger (content), aber niemals größer als die verfügbare Anzahl
            // 	int initialContent = Math.Min(workersAvailable, contentLimit);

            // 	int initialUnhappyCount = Math.Max(0, workersAvailable - initialContent);

            // 	return (initialUnhappyCount, initialContent);
            // }
        }

        [Fact]
        public void GetCitizenTypesTests()
        {
            Assert.Fail("Not implemented");
            //   public CitizenTypes GetCitizenTypes()
            // {
            // 	DebugService.Assert(_specialists.Count <= _city.Size);
            // 	CitizenTypes ct = CreateCitizenTypes();

            // 	(int initialUnhappyCount, int initialContent) = CalculateCityStats(ct, _game);

            // 	// Stage 1: basic content/unhappy
            // 	ct = StageBasic(ct, initialContent, initialUnhappyCount);

            // 	ApplyEmperorEffects(ct);

            // 	// Stage 2: impact of luxuries: content->happy; unhappy->content and then content->happy
            // 	int happyUpgrades = (int)Math.Floor((double)_city.Luxuries / 2);
            // 	UpgradeCitizens(ct.Citizens, happyUpgrades);


            // 	// Stage 3: Building effects
            // 	ApplyBuildingEffects(ct);

            // 	// Stage 4: martial law
            // 	ApplyMartialLaw(ct);
            // 	ApplyDemocracyEffects(ct, initialContent);

            // 	//Stage 5: wonder effects
            // 	ApplyWonderEffects(ct);
            // 	(ct.happy, ct.content, ct.unhappy) = CountCitizenTypes(ct.Citizens);

            // 	DebugService.Assert(ct.Sum() == _city.Size);
            // 	DebugService.Assert(ct.Valid());

            // 	return ct;
            // }
        }

        [Fact]
        public void EnumerateCitizensTests()
        {
            Assert.Fail("Not implemented");
            //   public IEnumerable<CitizenTypes> EnumerateCitizens()
            // {
            // 	DebugService.Assert(_specialists.Count <= _city.Size);
            // 	CitizenTypes ct = CreateCitizenTypes();

            // 	(int initialUnhappyCount, int initialContent) = CalculateCityStats(ct, _game);

            // 	// Stage 1: basic content/unhappy
            // 	ct = StageBasic(ct, initialContent, initialUnhappyCount);

            // 	(ct.happy, ct.content, ct.unhappy) = CountCitizenTypes(ct.Citizens);

            // 	DebugService.Assert(ct.Sum() == _city.Size);
            // 	DebugService.Assert(ct.Valid());

            // 	yield return ct;

            // 	// Stage 2: impact of luxuries: content->happy; unhappy->content and then content->happy
            // 	// entertainers produce these luxury effects, but also marketplace, bank and luxury trade settings.
            // 	int happyUpgrades = (int)Math.Floor((double)_city.Luxuries / 2);
            // 	UpgradeCitizens(ct.Citizens, happyUpgrades);

            // 	(ct.happy, ct.content, ct.unhappy) = CountCitizenTypes(ct.Citizens);

            // 	DebugService.Assert(ct.Sum() == _city.Size);
            // 	DebugService.Assert(ct.Valid());

            // 	yield return ct;

            // 	// Stage 3: Building effects
            // 	ApplyBuildingEffects(ct);
            // 	(ct.happy, ct.content, ct.unhappy) = CountCitizenTypes(ct.Citizens);

            // 	DebugService.Assert(ct.Sum() == _city.Size);
            // 	DebugService.Assert(ct.Valid());
            // 	yield return ct;

            // 	// Stage 4: martial law
            // 	ApplyMartialLaw(ct);
            // 	ApplyDemocracyEffects(ct, initialContent);
            // 	(ct.happy, ct.content, ct.unhappy) = CountCitizenTypes(ct.Citizens);

            // 	DebugService.Assert(ct.Sum() == _city.Size);
            // 	DebugService.Assert(ct.Valid());
            // 	yield return ct;

            // 	//Stage 5: wonder effects
            // 	ApplyWonderEffects(ct);
            // 	(ct.happy, ct.content, ct.unhappy) = CountCitizenTypes(ct.Citizens);

            // 	DebugService.Assert(ct.Sum() == _city.Size);
            // 	DebugService.Assert(ct.Valid());
            // 	yield return ct;
            // }
        }

        public class MockedIGame : IGameCitizenDependency
        {
            public ushort GameTurn { get; set; }
            public int Difficulty { get; set; }
            public int MaxDifficulty { get; set; }

            public Func<byte, Player> OnGetPlayer { get; set; }
            public Func<IUnit[]> OnGetUnits { get; set; }
            public Func<Type, bool> OnWonderObsoleteByType { get; set; }
            public Func<IWonder, bool> OnWonderObsolete { get; set; }

            public Player GetPlayer(byte playerId)
                => OnGetPlayer?.Invoke(playerId)
                    ?? throw new NotImplementedException("GetPlayer not implemented by delegate.");

            public IUnit[] GetUnits()
                => OnGetUnits?.Invoke()
                    ?? throw new NotImplementedException("GetUnits not implemented by delegate.");

            public bool WonderObsolete<T>() where T : IWonder, new()
                => OnWonderObsoleteByType?.Invoke(typeof(T))
                    ?? throw new NotImplementedException("WonderObsolete<T> not implemented by delegate.");

            public bool WonderObsolete(IWonder wonder)
                => OnWonderObsolete?.Invoke(wonder)
                    ?? throw new NotImplementedException("WonderObsolete(IWonder) not implemented by delegate.");
        }

        public class MockedICityBasic : ICityBasic
        {
            public Point Location => new Point(0, 0);
            public byte Size { get; set; } = 5;
            public short Luxuries { get; set; } = 0;
            public byte Owner => 0;


            public ITile Tile => _tile;
            private ITile _tile = null;
            public ITile MockTile
            {
                get => _tile;
                set => _tile = value;
            }
            public int ContinentId => 0;
            public Player Player => null;
            private Player _player = null;
            public Player MockPlayer
            {
                get => _player;
                set => _player = value;
            }
            public int Entertainers { get; set; } = 0;
            public int Scientists { get; set; } = 0;
            public int Taxmen { get; set; } = 0;
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
    }
}
