// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.src;
using System.Linq;
using CivOne.Buildings;
using Xunit;
using CivOne.Screens.Services;
using CivOne.Enums;
using System.Collections.Generic;
using CivOne.Wonders;
using CivOne.Units;
using System;
using CivOne.Graphics.Sprites;
using CivOne.Governments;
using CivOne.Advances;

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
    public partial class CityCitizenServiceImplTests : TestsBase
    {
        CityCitizenServiceImplShims testee;
        List<Citizen> mockedSpecialists;

        MockedGame mockedIGame;
        MockedCity mockedCity;

        MockedMap mockedIMap;

        MockedGrassland mockedGrassland;
        public override void BeforeEach()
        {
            mockedSpecialists = [];
            mockedGrassland = new MockedGrassland();

            mockedCity = new MockedCity()
            {
                Size = 1,
                Tile = mockedGrassland
            };

            mockedIGame = new MockedGame()
            {
                Difficulty = 4,
                MaxDifficulty = 5,
                GameTurn = 1
            };
            mockedIMap = new MockedMap();

            testee = new CityCitizenServiceImplShims(
                mockedCity,
                mockedCity,
                mockedIGame,
                mockedSpecialists,
                mockedIMap
            );
        }

        public override void AfterEach()
        {
            testee = null;
        }

        [Fact]
        public void GetCitizenTypesTests()
        {
            // no emperor effects
            mockedIGame.Difficulty = 3;

            mockedCity.Size = 15;
            mockedCity.Entertainers = 1;
            mockedCity.Scientists = 1;
            mockedCity.Taxmen = 1;
            mockedCity.Luxuries = 2;

            mockedSpecialists.AddRange([Citizen.Entertainer, Citizen.Scientist, Citizen.Taxman]);

            mockedCity.MockPlayer = new MockedPlayer()
                .withGovernmentType(typeof(Anarchy))
                .withWonderEffect<HangingGardens>(true);

            mockedCity.Tile = mockedGrassland;
            mockedGrassland.WithUnits(
                [new MockedUnit().WithHome(mockedCity)]);

            mockedCity.ReturnHasWonderValues(false);
            mockedCity.ReturnHasBuildingValues(true, false); // Temple to test building effects
            mockedIGame.OnWonderObsoleteByType = (type) => false;


            var actual = testee.GetCitizenTypes();

            AssertCitizenTypes(actual,
                expectedHappy: 1,
                expectedContent: 2,
                expectedUnhappy: 9,
                expectedRedShirt: 0,
                expectedElvis: 1,
                expectedEinstein: 1,
                expectedTaxman: 1);
        }

        [Fact]
        public void EnumerateCitizensTests()
        {
            mockedIGame.Difficulty = 3;

            mockedCity.Size = 15;
            mockedCity.Entertainers = 1;
            mockedCity.Scientists = 1;
            mockedCity.Taxmen = 1;
            mockedCity.Luxuries = 2;

            mockedSpecialists.AddRange([Citizen.Entertainer, Citizen.Scientist, Citizen.Taxman]);

            mockedCity.MockPlayer = new MockedPlayer()
                .withGovernmentType(typeof(Anarchy))
                .withWonderEffect<HangingGardens>(true);

            mockedCity.Tile = mockedGrassland;
            mockedGrassland.WithUnits([new MockedUnit()
                .WithHome(mockedCity)]);

            mockedCity.ReturnHasWonderValues(false);
            mockedCity.ReturnHasBuildingValues(true, false); // Temple to test building effects
            mockedIGame.OnWonderObsoleteByType = (type) => false;


            var enumeration = testee.EnumerateCitizens().GetEnumerator();

            Assert.True(enumeration.MoveNext());
            var stage1 = enumeration.Current;

            AssertCitizenTypes(stage1,
                expectedHappy: 0,
                expectedContent: 0,
                expectedUnhappy: 12,
                expectedRedShirt: 0,
                expectedElvis: 1,
                expectedEinstein: 1,
                expectedTaxman: 1);


            Assert.True(enumeration.MoveNext());
            var stage2 = enumeration.Current;

            AssertCitizenTypes(stage2,
                expectedHappy: 0,
                expectedContent: 1,
                expectedUnhappy: 11,
                expectedRedShirt: 0,
                expectedElvis: 1,
                expectedEinstein: 1,
                expectedTaxman: 1);

            Assert.True(enumeration.MoveNext());
            var stage3 = enumeration.Current;

            AssertCitizenTypes(stage3,
                expectedHappy: 0,
                expectedContent: 2,
                expectedUnhappy: 10,
                expectedRedShirt: 0,
                expectedElvis: 1,
                expectedEinstein: 1,
                expectedTaxman: 1);
            Assert.Single(stage3.Buildings);
            Assert.IsType<Temple>(stage3.Buildings[0]);

            Assert.True(enumeration.MoveNext());
            var stage4 = enumeration.Current;

            Assert.Single(stage4.MarshallLawUnits);
            AssertCitizenTypes(stage4,
                expectedHappy: 0,
                expectedContent: 3,
                expectedUnhappy: 9,
                expectedRedShirt: 0,
                expectedElvis: 1,
                expectedEinstein: 1,
                expectedTaxman: 1);

            Assert.True(enumeration.MoveNext());
            var stage5 = enumeration.Current;

            AssertCitizenTypes(stage5,
                expectedHappy: 1,
                expectedContent: 2,
                expectedUnhappy: 9,
                expectedRedShirt: 0,
                expectedElvis: 1,
                expectedEinstein: 1,
                expectedTaxman: 1);
        }

        private void AssertCitizenTypes(
            CitizenTypes ct,
            int expectedHappy,
            int expectedContent,
            int expectedUnhappy,
            int expectedRedShirt,
            int expectedElvis,
            int expectedEinstein,
            int expectedTaxman)
        {
            Assert.Equal(ct.happy, expectedHappy);
            Assert.Equal(ct.content, expectedContent);
            Assert.Equal(ct.unhappy, expectedUnhappy);
            Assert.Equal(ct.redShirt, expectedRedShirt);
            Assert.Equal(ct.elvis, expectedElvis);
            Assert.Equal(ct.einstein, expectedEinstein);
            Assert.Equal(ct.taxman, expectedTaxman);
            
            var (happy, content, unhappy, redShirt) = testee.CountCitizenTypes(ct. Citizens);

            Assert.Equal(expectedHappy, happy);
            Assert.Equal(expectedContent, content);
            Assert.Equal(expectedUnhappy, unhappy);
            Assert.Equal(expectedRedShirt, redShirt);
            Assert.Equal(expectedElvis, ct.elvis);
            Assert.Equal(expectedEinstein, ct.einstein);
            Assert.Equal(expectedTaxman, ct.taxman);
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
                    new MockedCity()
                    {
                        Owner = mockedCity.Owner,
                        ContinentId = 1
                    }
                    .ReturnHasWonderValues(false, false, false, false),
                    new MockedCity()
                    {
                        Owner = mockedCity.Owner,
                        ContinentId = 2
                    }
                    .ReturnHasWonderValues(true, true, true, true),
                    new MockedCity()
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
                var player = new MockedPlayer();
                player
                    .withCitiesInterface([
                        new MockedCity()
                        .ReturnHasWonderValues(
                                hasMichelangelosChapel)
                        .WithContinentId(mockedCity.ContinentId),
                        new MockedCity()
                        .ReturnHasWonderValues(
                                true)
                        .WithContinentId(mockedCity.ContinentId+1)
                    ]);
                return player;
            };

            Assert.Equal(expectedDelta, testee.CathedralDelta());
        }

        [Fact]
        public void ApplyBuildingEffectsTestsWithShakespearesTheatre()
        {
            mockedCity.ReturnHasWonderValues(true);
            mockedIGame.OnWonderObsoleteByType = (type) => false;

            var ct = new CitizenTypes
            {
                Citizens = new Citizen[5],
                Wonders = []
            };
            testee.InitCitizens(ct.Citizens, 5, 0);

            testee.ApplyBuildingEffects(ct);

            var (happy, content, unhappy, redShirt) = testee.CountCitizenTypes(ct.Citizens);

            Assert.Equal(0, unhappy);
            Assert.Equal(5, content);

            Assert.Single(ct.Wonders);
            Assert.IsType<ShakespearesTheatre>(ct.Wonders[0]);
        }

        [Theory]
        [InlineData(false, false, 1)]
        [InlineData(true, false, 2)]
        [InlineData(true, true, 4)]
        public void ApplyBuildingEffectsTestsWithTemple(
            bool hasMysticism,
            bool hasOracle,
            int expectedContent)
        {
            mockedCity.Size = 5;
            mockedCity.ReturnHasWonderValues(false);
            mockedCity.ReturnHasBuildingValues(true, false); // Temple

            mockedCity.MockPlayer = new MockedPlayer()
                .withAdvance<Mysticism>(hasMysticism)
                .withWonderEffect<Oracle>(hasOracle);

            var ct = new CitizenTypes
            {
                Citizens = new Citizen[5],
                Buildings = [],
                Wonders = []
            };

            testee.InitCitizens(ct.Citizens, 1, 4); // 1 content, 4 unhappy

            testee.ApplyBuildingEffects(ct);

            Assert.Single(ct.Buildings);
            Assert.IsType<Temple>(ct.Buildings[0]);

            var (happy, content, unhappy, redShirt) = testee.CountCitizenTypes(ct.Citizens);

            Assert.Equal(0, happy);
            Assert.Equal(0, redShirt);
            Assert.Equal(mockedCity.Size - content, unhappy);
            Assert.Equal(expectedContent + 1, content);
        }

        [Fact]
        public void ApplyBuildingEffectsTests_Cathedrals()
        {
            mockedCity.Size = 5;
            mockedCity.ReturnHasWonderValues(false);
            mockedCity.ReturnHasBuildingValues(false, false); // Temple and Colosseum
            testee.BachsCathedral = true;
            testee.CathedralDeltaValue = 2;

            var ct = new CitizenTypes
            {
                Citizens = new Citizen[5],
                Buildings = [],
                Wonders = []
            };
            testee.InitCitizens(ct.Citizens, 1, 4); // 1 content, 4 unhappy

            testee.ApplyBuildingEffects(ct);

            Assert.Empty(ct.Buildings);

            var (happy, content, unhappy, redShirt) = testee.CountCitizenTypes(ct.Citizens);
            Assert.Equal(0, happy);
            Assert.Equal(0, redShirt);
            Assert.Equal(0, unhappy);
            Assert.Equal(1 + 4, content); // 1 initial + Bach (2) + Cathedral delta (2)
        }

        [Fact]
        public void ApplyBuildingEffectsTests_Colosseum()
        {
            mockedCity.Size = 5;
            mockedCity.ReturnHasWonderValues(false);
            mockedCity.ReturnHasBuildingValues(false, true); // Colosseum
            testee.BachsCathedral = false;
            testee.CathedralDeltaValue = 0;

            var ct = new CitizenTypes
            {
                Citizens = new Citizen[5],
                Buildings = [],
                Wonders = []
            };
            testee.InitCitizens(ct.Citizens, 1, 4);

            testee.ApplyBuildingEffects(ct);

            Assert.Single(ct.Buildings);
            Assert.IsType<Colosseum>(ct.Buildings[0]);

            var (happy, content, unhappy, redShirt) = testee.CountCitizenTypes(ct.Citizens);

            Assert.Equal(0, happy);
            Assert.Equal(0, redShirt);
            Assert.Equal(1, unhappy);
            Assert.Equal(4, content); // 1 initial + 3 from Colosseum
        }

        [Fact]
        public void ApplyBuildingEffectsTests_NoEffects()
        {
            mockedCity.Size = 5;
            mockedCity.ReturnHasWonderValues(false);
            mockedCity.ReturnHasBuildingValues(false, false); // no Temple or Colosseum
            testee.BachsCathedral = false;
            testee.CathedralDeltaValue = 0;

            var ct = new CitizenTypes
            {
                Citizens = new Citizen[5],
                Buildings = [],
                Wonders = []
            };
            testee.InitCitizens(ct.Citizens, 1, 4);

            testee.ApplyBuildingEffects(ct);

            Assert.Empty(ct.Buildings);
            Assert.Empty(ct.Wonders);
            var (happy, content, unhappy, redShirt) = testee.CountCitizenTypes(ct.Citizens);

            Assert.Equal(0, happy);
            Assert.Equal(0, redShirt);
            Assert.Equal(4, unhappy);
            Assert.Equal(1, content); // no change
        }

        [Theory]
        [InlineData(typeof(CivOne.Governments.Democracy), 0, 5)] // not despot
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
            var player = new MockedPlayer().withGovernmentType(government);
            mockedCity.MockPlayer = player;

            var units = new List<IUnit>();
            for (int i = 0; i < unitsInCityCount; i++)
            {
                var unit = new MockedUnit(mockedCity.Location.X, mockedCity.Location.Y)
                .WithHome(mockedCity);
                units.Add(unit);
            }
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
        [InlineData(typeof(CivOne.Governments.Democracy), 0, 0)] // no units not in city
        [InlineData(typeof(Anarchy), 1, 0)] // not democratic
        [InlineData(typeof(Republic), 1, 1)] // 1 unit
        [InlineData(typeof(Republic), 2, 2)] // 2 units
        [InlineData(typeof(CivOne.Governments.Democracy), 1, 2)] // 2 units
        [InlineData(typeof(CivOne.Governments.Democracy), 2, 4)] // 2 units
        [InlineData(typeof(CivOne.Governments.Democracy), 3, 5)] // 3 units with max city size 5

        public void ApplyDemocracyEffectsTests(
            Type government,
            int unitsNotInCityCount,
            int expectedUnhappy
        )
        {
            mockedCity.Size = 5;
            var player = new MockedPlayer().withGovernmentType(government);
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
                return new MockedPlayer().withCitiesCount(totalCities);
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
        //
        // difficulty, size, specialists, expectedContent, expectedUnhappy
        //
        [InlineData(0, 5, 0, 5, 0)]  // maximaler Content bei niedrigster Difficulty
        [InlineData(0, 5, 3, 2, 0)]  // nur 1 Worker, hoher ContentLimit
        [InlineData(3, 5, 1, 2, 2)]  // mittlere Difficulty, gemischter Case
        [InlineData(5, 5, 3, 0, 2)]  // negativer contentLimit -> content=0
        [InlineData(2, 3, 0, 3, 0)]  // contentLimit<workersAvailable
        [InlineData(4, 3, 3, 0, 0)]  // alle Spezialisten -> keine Workers, alles 0
        public void CalculateCityStats_AllCases(
            byte difficulty,
            byte citySize,
            int specialists,
            int expectedContent,
            int expectedUnhappy)
        {
            // Arrange
            mockedIGame.Difficulty = difficulty;
            mockedCity.Size = citySize;

            var ct = new CitizenTypes
            {
                Citizens = new Citizen[citySize],
                // Spezialisten direkt setzen
                elvis = specialists >= 1 ? 1 : 0,
                einstein = specialists >= 2 ? 1 : 0,
                taxman = specialists >= 3 ? 1 : 0
            };

            // This test only supports up to 3 specialists.
            Assert.InRange(specialists, 0, 3);

            // Act
            var (initialUnhappyCount, initialContent) = testee.CalculateCityStats(ct);

            // Assert
            Assert.Equal(expectedContent, initialContent);
            Assert.Equal(expectedUnhappy, initialUnhappyCount);
        }

        class CityCitizenServiceImplShims : CityCitizenServiceImpl
        {
            public CityCitizenServiceImplShims(ICityBasic city,
                    ICityBuildings cityBuildings,
                    IGameCitizenDependency game,
                    List<Citizen> specialists,
                    IMap map) : base(city, cityBuildings, game, specialists, map)
            {
            }

            public bool? BachsCathedral { get; set; } = null;

            protected override internal bool HasBachsCathedral()
            {
                if (BachsCathedral.HasValue)
                {
                    return BachsCathedral.Value;
                }
                return base.HasBachsCathedral();
            }

            public int? CathedralDeltaValue { get; set; } = null;
            internal override int CathedralDelta()
            {
                if (CathedralDeltaValue.HasValue)
                {
                    return CathedralDeltaValue.Value;
                }
                return base.CathedralDelta();
            }
        }
    }
}
