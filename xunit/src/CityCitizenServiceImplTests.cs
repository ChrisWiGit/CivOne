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
using CivOne.Graphics.Sprites;
using CivOne.Governments;
using CivOne.UserInterface;

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
        MockedICity mockedCity;

        MockedIMap mockedIMap;

        MockedGrassland mockedGrassland = new();
        public override void BeforeEach()
        {
            // var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
            // city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);

            mockedSpecialists = [];

            mockedCity = new MockedICity()
            {
                Size = 1,
                Tile = mockedGrassland
            };

            mockedIGame = new MockedIGame()
            {
                Difficulty = 4,
                MaxDifficulty = 5,
                GameTurn = 1
            };
            mockedIMap = new MockedIMap();

            testee = new CityCitizenServiceImpl(
                mockedCity,
                mockedCity,
                mockedIGame,//Game.Instance,
                mockedSpecialists,
                mockedIMap
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
            mockedCity.Size = 6;
            mockedSpecialists.Clear();

            mockedSpecialists.AddRange([.. Enumerable.Repeat(Citizen.HappyMale, mockedCity.Size - contentCount - unhappyCount)]);

            var target = new Citizen[mockedCity.Size];
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
            mockedCity.Size = (byte)(initialContent + initialUnhappy);

            var ct = new CitizenTypes
            {
                Citizens = new Citizen[mockedCity.Size]
            };
            ct = testee.StageBasic(ct, initialContent, initialUnhappy);

            Assert.Equal(initialContent, ct.content);
            Assert.Equal(initialUnhappy, ct.unhappy);

            var (happy, content, unhappy, redShirt) = testee.CountCitizenTypes(ct.Citizens);

            Assert.Equal(ct.happy, happy);
            Assert.Equal(ct.content, content);
            Assert.Equal(ct.unhappy, unhappy);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        public void HasBachsCathedralTests(
            int cityContinentId,
            bool isCathedralPresent
        )
        {
            mockedCity.ContinentId = cityContinentId;

            mockedIMap.ReturnContinentCitiesValues(
                [
                    new MockedICity()
					{
                        Owner = mockedCity.Owner,
                        ContinentId = 1
                    }
                    .ReturnHasWonderValues(false, false, false, false),
                    new MockedICity()
					{
                        Owner = mockedCity.Owner,
                        ContinentId = 2
                    }
                    .ReturnHasWonderValues(true, true, true, true),
                    new MockedICity()
					{
                        Owner = 255,
                        ContinentId = 3
                    }
                    .ReturnHasWonderValues(true, true, true, true)
                ]
            );

            Assert.Equal(isCathedralPresent, testee.HasBachsCathedral());
        }

        [Theory]
        [InlineData(false, false, true, 0)]
        [InlineData(true, true, false, 4)]
        [InlineData(true, false, false, 4)]
        [InlineData(true, false, true, 6)]
        public void CathedralDeltaTest(
            bool hasCathedral,
            bool obsoleteMichelangelosChapel,
            bool hasMichelangelosChapel,
            int expectedDelta
        )
        {
            mockedIGame.OnWonderObsoleteByType = 
                (type) => obsoleteMichelangelosChapel;

            mockedCity.ReturnHasBuildingValues(
                hasCathedral);

            mockedIGame.OnGetPlayer = (owner) =>
            {
                var player = new MockPlayer();
                player
                    .withCitiesInterface([
                        new MockedICity()
                        .ReturnHasWonderValues(
                                hasMichelangelosChapel)
                        .WithContinentId(mockedCity.ContinentId),
                        new MockedICity()
                        .ReturnHasWonderValues(
                                true)
                        .WithContinentId(mockedCity.ContinentId+1)
                    ]);
                return player;
            };

            Assert.Equal(expectedDelta, testee.CathedralDelta());
            // internal int CathedralDelta()
            // {
            // 	if (!_cityBuildings.HasBuilding<Cathedral>()) return 0;

            // 	int unhappyDelta = 0;

            // 	// CW: Michelangelo's Chapel gives +6 happiness if on same continent as city with wonder, else +4
            // 	// https://civilization.fandom.com/wiki/Michelangelo%27s_Chapel_(Civ1)
            // 	bool hasChapel = !_game.WonderObsolete<MichelangelosChapel>()
            // 			&& _game.GetPlayer(_city.Owner)
            //          .Cities.Any(c => c.HasWonder<MichelangelosChapel>()
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

        [Theory]
        [InlineData(typeof(Democracy), 0, 5)] // not despot
        [InlineData(typeof(Anarchy), 1, 4)]
        [InlineData(typeof(Anarchy), 2, 3)]
        [InlineData(typeof(Anarchy), 3, 2)]
        [InlineData(typeof(Anarchy), 4, 2)] // max 3 units affect martial law
        [InlineData(typeof(Anarchy), 5, 2)]
        [InlineData(typeof(Despotism), 5, 2)]
        public void ApplyMartialLawTests(
            Type government,
            int unitsInCityCount,
            int expectedUnhappy)
        {
            mockedCity.Size = 5;
            var player = new MockPlayer().withGovernmentType(government);
            mockedCity.MockPlayer = player;

            var units = new List<IUnit>();
            for (int i = 0; i < unitsInCityCount; i++)
            {
                var unit = new MockedUnit(mockedCity.Location.X, mockedCity.Location.Y)
                .WithHome(mockedCity);
                units.Add(unit);
            }
            mockedCity.MockUnits = [.. units];
            mockedCity.Tile = mockedGrassland;
            mockedGrassland.WithUnits([.. units]);

            // mockedIGame.OnGetUnits = (x, y) =>
            // {
            //     Assert.Equal(mockedCityBasic.Location.X, x);
            //     Assert.Equal(mockedCityBasic.Location.Y, y);
            //     return mockedCityBasic.MockUnits;
            // };

            var ct = new CitizenTypes
            {
                Citizens = new Citizen[mockedCity.Size],
                MarshallLawUnits = []
            };

            ct.unhappy = 5;
            ct.content = 0;
            ct.happy = 0;
            ct.redShirt = 0;
            testee.InitCitizens(ct.Citizens, 0, mockedCity.Size);


            testee.ApplyMartialLaw(ct);

            var (happy, content, unhappy, redShirt) = testee.CountCitizenTypes(ct.Citizens);

            Assert.Equal(expectedUnhappy, unhappy);
        }

        [Theory]
        [InlineData(typeof(Anarchy), 0, 0)] // not democratic
        [InlineData(typeof(Republic), 0, 0)] // no units not in city
        [InlineData(typeof(Democracy), 0, 0)] // no units not in city
        [InlineData(typeof(Anarchy), 1, 0)] // not democratic
        [InlineData(typeof(Republic), 1, 1)] // 1 unit
        [InlineData(typeof(Republic), 2, 2)] // 2 units
        [InlineData(typeof(Democracy), 1, 2)] // 2 units
        [InlineData(typeof(Democracy), 2, 4)] // 2 units
        [InlineData(typeof(Democracy), 3, 5)] // 3 units with max city size 5

        public void ApplyDemocracyEffectsTests(
            Type government,
            int unitsNotInCityCount,
            int expectedUnhappy
        )
        {
            mockedCity.Size = 5;
            var player = new MockPlayer().withGovernmentType(government);
            mockedCity.MockPlayer = player;
            mockedIGame.OnGetPlayer = (playerId) =>
            {
                return player;
            };
            mockedIGame.OnGetUnits = (_, __) =>
            {
                var units = new List<IUnit>();
                for (int i = 0; i < unitsNotInCityCount; i++)
                {
                    var unit = new MockedUnit()
                    .WithHome(mockedCity);

                    units.Add(unit);
                }
                return [.. units];
            };

            var ct = new CitizenTypes
            {
                Citizens = new Citizen[mockedCity.Size],
                MarshallLawUnits = []
            };
            int initialContent = mockedCity.Size;

            testee.InitCitizens(ct.Citizens, initialContent, 0);

            testee.ApplyDemocracyEffects(ct, initialContent);

            var (happy, content, unhappy, redShirt) = testee.CountCitizenTypes(ct.Citizens);

            Assert.Equal(expectedUnhappy, unhappy);
        }

        [Fact]
        public void CreateCitizenTypesTests()
        {
            var actual = testee.CreateCitizenTypes();

            Assert.Equal(0, actual.happy);
            Assert.Equal(0, actual.content);
            Assert.Equal(0, actual.unhappy);
            Assert.Equal(0, actual.redShirt);
            Assert.Equal(0, actual.elvis);
            Assert.Equal(0, actual.einstein);
            Assert.Equal(0, actual.taxman);
            Assert.NotNull(actual.Citizens);
            Assert.Equal(mockedCity.Size, actual.Citizens.Length);
            Assert.NotNull(actual.Buildings);
            Assert.Empty(actual.Buildings);
            Assert.NotNull(actual.Wonders);
            Assert.Empty(actual.Wonders);
            Assert.NotNull(actual.MarshallLawUnits);
            Assert.Empty(actual.MarshallLawUnits);
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
        [InlineData(5, 36 + 12 + 1, 5, 0, 2)] // second redshirt at 48 cities >= 48
        [InlineData(5, 36 + 12 + 12 + 1, 5, 0, 3)] // third redshirt at 61 cities
        [InlineData(5, 36 + 12 + 12 + 12 + 1, 5, 0, 4)] // fourth redshirt at 73 cities
        public void ApplyEmperorEffectsTests(
            int gameDifficulty,
            int totalCities,
            int citySize,
            int expectedBornContent,
            int expectedReadShirts
        )
        {
            // Also tests:
            //  NumberOfRedShirts(totalCities)
            mockedIGame.OnGetPlayer = (playerId) =>
            {
                return new MockPlayer().withCitiesCount(totalCities);
            };
            mockedIGame.Difficulty = gameDifficulty;
            mockedCity.Size = (byte)citySize;

            CitizenTypes ct = new()
            {
                Citizens = new Citizen[citySize]
            };
            testee.InitCitizens(ct.Citizens, citySize, 0);
            testee.ApplyEmperorEffects(ct);

            var (happy, content, unhappy, redShirt) = testee.CountCitizenTypes(ct.Citizens);

            Assert.Equal(expectedBornContent, content);
            Assert.Equal(expectedReadShirts, redShirt);
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
            public Func<int, int, IUnit[]> OnGetUnits { get; set; }
            public Func<Type, bool> OnWonderObsoleteByType { get; set; }
            public Func<IWonder, bool> OnWonderObsolete { get; set; }

            public Player GetPlayer(byte playerId)
                => OnGetPlayer?.Invoke(playerId)
                    ?? throw new NotImplementedException("GetPlayer not implemented by delegate.");

            public IUnit[] GetUnits()
                => OnGetUnits?.Invoke(int.MinValue, int.MinValue)
                    ?? throw new NotImplementedException("GetUnits not implemented by delegate.");
            public IUnit[] GetUnits(int x, int y)
                => OnGetUnits?.Invoke(x, y)
                    ?? throw new NotImplementedException("GetUnits not implemented by delegate.");


            public bool WonderObsolete<T>() where T : IWonder, new()
                => OnWonderObsoleteByType?.Invoke(typeof(T))
                    ?? throw new NotImplementedException("WonderObsolete<T> not implemented by delegate.");

            public bool WonderObsolete(IWonder wonder)
                => OnWonderObsolete?.Invoke(wonder)
                    ?? throw new NotImplementedException("WonderObsolete(IWonder) not implemented by delegate.");
        }

        public class MockedICity : 
            // ICityBasic, ICityBuildings, ICityOnContinent
            ICity
        {
            public Point Location => new Point(0, 0);
            public byte Size { get; set; } = 5;
            public short Luxuries { get; set; } = 0;
            public byte Owner { get; set; } = 0;


            public ITile Tile { get; set; } = null;
            
            public int ContinentId { get; set; } = 0;
            public Player Player => _player;
            private Player _player = null;
            public Player MockPlayer
            {
                get => _player;
                set => _player = value;
            }

            private IUnit[] _units = Array.Empty<IUnit>();
            public IUnit[] MockUnits
            {
                get => _units;
                set => _units = value;
            }

            public int Entertainers { get; set; } = 0;
            public int Scientists { get; set; } = 0;
            public int Taxmen { get; set; } = 0;

            private readonly SupplyMockedValues<bool> _hasBuilding;
            private readonly SupplyMockedValues<bool> _hasWonder;

            public MockedICity()
            {
                _hasBuilding = new SupplyMockedValues<bool>();
                _hasWonder = new SupplyMockedValues<bool>();
            }

            public MockedICity ReturnHasBuildingValues(params bool[] values)
            {
                _hasBuilding.Reset(values);
                return this;
            }

            public MockedICity ReturnHasWonderValues(params bool[] values)
            {
                _hasWonder.Reset(values);
                return this;
            }

            public MockedICity WithContinentId(int continentId)
            {
                ContinentId = continentId;
                return this;
            }

            public bool HasBuilding<T>() where T : IBuilding => _hasBuilding.Next();

            public bool HasWonder<T>() where T : IWonder => _hasWonder.Next();

			public void NewTurn()
			{
				throw new NotImplementedException();
			}
		}

        partial class MockPlayer : Player
        {
            private int citiesCount;
            private City[] _cities;
            private ICity[] _citiesInterface;

            public MockPlayer() : base()
            {
                this.citiesCount = 0;
            }

            public override City[] Cities => _cities;
            public override ICity[] CitiesInterface => _citiesInterface;
            public MockPlayer withCities(City[] cities)
            {
                this._cities = cities;
                this.citiesCount = cities.Length;
                return this;
            }
            public MockPlayer withCitiesInterface(ICity[] cities)
            {
                this._citiesInterface = cities;
                this.citiesCount = cities.Length;
                return this;
            }

            public MockPlayer withCitiesCount(int count)
            {
                this.citiesCount = count;
                this._cities = new City[count];
                return this;
            }
            public MockPlayer withGovernment(IGovernment government)
            {
                this.Government = government;
                return this;
            }
            public MockPlayer withGovernmentType(Type government)
            {
                IGovernment gov = government switch
                {
                    var t when t == typeof(Republic) => new Republic(),
                    var t when t == typeof(Democracy) => new Democracy(),
                    var t when t == typeof(Anarchy) => new Anarchy(),
                    var t when t == typeof(Despotism) => new Despotism(),
                    var t when t == typeof(Monarchy) => new Monarchy(),
                    _ => throw new NotImplementedException($"Government type {government} not implemented in MockPlayer"),
                };
                this.Government = gov;
                return this;
            }
        }
        class MockedUnit : BaseUnit, IUnit
        {
            public override IEnumerable<MenuItem<int>> MenuItems => throw new NotImplementedException();

            public MockedUnit(int x = 1, int y = 1, byte attack = 1)
            {
                X = x;
                Y = y;
                Attack = attack;
            }

            private ICityBasic _city;

            public MockedUnit WithHome(ICityBasic city)
            {
                _city = city;
                return this;
            }

            public bool IsHome(ICityBasic city)
            {
                return _city == city;
            }

            protected override bool ValidMoveTarget(ITile tile)
            {
                throw new NotImplementedException();
            }
        }

        class MockedGrassland : Grassland, ITile
        {
            private IUnit[] _units = Array.Empty<IUnit>();


            public MockedGrassland()
            {
            }

            public MockedGrassland WithUnits(params IUnit[] units)
            {
                _units = units;
                return this;
            }

            public override IUnit[] Units => _units;
        }

        class MockedIMap : IMap
		{
            private readonly List<ICityOnContinent> _continentCities = new();
	    	public IEnumerable<ICityOnContinent> ContinentCities(int continentId)
			{
				return [.._continentCities.Where(city => city.ContinentId == continentId)];
			}

            public MockedIMap ReturnContinentCitiesValues(params ICityOnContinent[] values)
            {
                _continentCities.RemoveAll(_ => true);
                _continentCities.AddRange(values);

                return this;
            }
        }


    }
}
