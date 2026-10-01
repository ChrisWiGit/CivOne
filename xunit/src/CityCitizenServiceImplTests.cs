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
    /// </summary>
    public partial class CityCitizenServiceImplTests : TestsBase
    {
        CityCitizenServiceImplShim testee = null!;
        List<Citizen>? mockedSpecialists;

        MockedGame mockedIGame = null!;
        MockedCity mockedCity = null!;

        MockedMap mockedIMap = null!;

        MockedGrassland mockedGrassland = null!;
        protected override void BeforeEach()
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

            testee = new CityCitizenServiceImplShim(
                mockedCity,
                mockedCity,
                mockedIGame,
                mockedSpecialists,
                mockedIMap
            );
        }

        protected override void AfterEach()
        {
            testee = null!;
        }

        [Fact]
        public void GetCitizenTypesTests()
        {
            // no emperor effects
            mockedIGame.Difficulty = 3;
            mockedIGame.MaxDifficulty = 5;

            mockedCity.Size = 15;
            mockedCity.Entertainers = 1;
            mockedCity.Scientists = 1;
            mockedCity.Taxmen = 1;
            mockedCity.Luxuries = 5; // 2 + 3 from entertainer

            mockedSpecialists!.AddRange([Citizen.Entertainer, Citizen.Scientist, Citizen.Taxman]);

            mockedCity.MockPlayer = new MockedPlayer()
                .WithGovernmentType(typeof(Anarchy))
                .WithWonderEffect<HangingGardens>(true);

            mockedCity.Tile = mockedGrassland;
            mockedGrassland.WithUnits(
                [new MockedUnit().WithHome(mockedCity)]);

            mockedCity.ReturnHasWonderValues(false);
            // No market place, no bank (both queried by Luxuries() in stage 2), then a temple and no colosseum.
            mockedCity.ReturnHasBuildingValues(false, false, true, false);
            mockedIGame.OnWonderObsoleteByType = (type) => false;
            mockedIGame.OnGetPlayer = (_) => new MockedPlayer().withCitiesCount(1);


            var actual = testee.GetCitizenTypes();

            // Same five-stage walk as EnumerateCitizensTests: the luxury happy citizen is taken back by the
            // seat normalisation in stage 2, the temple has no effect without Mysticism or Ceremonial Burial,
            // and Hanging Gardens turns one content citizen happy again in stage 5.
            AssertCitizenTypes(actual,
                expectedHappy: 1,
                expectedContent: 1,
                expectedUnhappy: 10,
                expectedRedShirt: 0,
                expectedElvis: 1,
                expectedEinstein: 1,
                expectedTaxman: 1);
        }

        [Fact]
        public void EnumerateCitizensTests()
        {
            mockedIGame.Difficulty = 3;

            const int citySize = 15;
            const int specialists = 3;

            mockedCity.Size = citySize;
            mockedCity.Entertainers = 1;
            mockedCity.Scientists = 1;
            mockedCity.Taxmen = 1;
            mockedCity.Luxuries = 4 + 3; // 4 lux + entertainer effect

            mockedSpecialists!.AddRange([Citizen.Entertainer, Citizen.Scientist, Citizen.Taxman]);

            mockedCity.MockPlayer = new MockedPlayer()
                .WithGovernmentType(typeof(Anarchy))
                .WithWonderEffect<HangingGardens>(true);

            mockedCity.Tile = mockedGrassland;
            mockedGrassland.WithUnits([new MockedUnit()
                .WithHome(mockedCity)]);

            mockedCity.ReturnHasWonderValues(false, true); //hanging gardens
            // No market place, no bank (both queried by Luxuries() in stage 2), then a temple and no colosseum.
            mockedCity.ReturnHasBuildingValues(false, false, true, false);
            mockedIGame.OnWonderObsoleteByType = (type) => false;
            mockedIGame.OnGetPlayer = (_) => new MockedPlayer().withCitiesCount(1);


            var enumeration = testee.EnumerateCitizens().GetEnumerator();

            Assert.True(enumeration.MoveNext());
            var stage1 = enumeration.Current;

            // 3 content max from difficulty 3 
            // all content are going to be specialists, so 0. 
            // 15 - 3specs - 3 content = 9 unhappy
            AssertCitizenTypes(stage1,
                expectedHappy: 0,
                expectedContent: 0,
                expectedUnhappy: citySize - specialists,
                expectedRedShirt: 0,
                expectedElvis: 1,
                expectedEinstein: 1,
                expectedTaxman: 1);


            Assert.True(enumeration.MoveNext());
            var stage2 = enumeration.Current;

            // Luxuries() sets happy from the entertainer alone (trade is 0 in this mock), which overshoots
            // the seats by one together with the 12 unhappy citizens; the normalisation takes the overshoot
            // back from happy rather than from unhappy, so the luxury turns one unhappy into content instead
            // of into happy.
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

            // The temple has no effect without Mysticism or Ceremonial Burial, which this player has
            // neither of, so the building stage leaves the counts from stage 2 unchanged.
            AssertCitizenTypes(stage3,
                expectedHappy: 0,
                expectedContent: 1,
                expectedUnhappy: 11,
                expectedRedShirt: 0,
                expectedElvis: 1,
                expectedEinstein: 1,
                expectedTaxman: 1);
            Assert.Single(stage3.Buildings);
            Assert.IsType<Temple>(stage3.Buildings[0]);

            Assert.True(enumeration.MoveNext());
            var stage4 = enumeration.Current;

            Assert.Single(stage4.MarshallLawUnits);

            // an unhappy to content from unit present
            AssertCitizenTypes(stage4,
                expectedHappy: 0,
                expectedContent: 2,
                expectedUnhappy: 10,
                expectedRedShirt: 0,
                expectedElvis: 1,
                expectedEinstein: 1,
                expectedTaxman: 1);

            Assert.True(enumeration.MoveNext());
            var stage5 = enumeration.Current;

            // 1 content to happy from wonder hanging gardens
            AssertCitizenTypes(stage5,
                expectedHappy: 1,
                expectedContent: 1,
                expectedUnhappy: 10,
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
            Assert.Equal(expectedHappy, ct.happy);
            Assert.Equal(expectedContent, ct.content);
            Assert.Equal(expectedUnhappy, ct.unhappy);
            Assert.Equal(expectedRedShirt, ct.redShirt);
            Assert.Equal(expectedElvis, ct.elvis);
            Assert.Equal(expectedEinstein, ct.einstein);
            Assert.Equal(expectedTaxman, ct.taxman);
            
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
        public void UnhappyToContentTestsZeroCount()
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
            mockedSpecialists!.Clear();
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
            mockedSpecialists!.Clear();
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
            mockedSpecialists!.Clear();

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
                        CityOwnerPlayerIndex = mockedCity.CityOwnerPlayerIndex,
                        ContinentId = 1
                    }
                    .ReturnHasWonderValues(false, false, false, false),
                    new MockedCity()
                    {
                        CityOwnerPlayerIndex = mockedCity.CityOwnerPlayerIndex,
                        ContinentId = 2
                    }
                    .ReturnHasWonderValues(true, true, true, true),
                    new MockedCity()
                    {
                        CityOwnerPlayerIndex = 255,
                        ContinentId = 3
                    }
                    .ReturnHasWonderValues(true, true, true, true)
                ]
            );

            Assert.Equal(isCathedralPresent, testee.HasBachsCathedral());
        }

        [Fact]
        public void ApplyBuildingEffectsTestsNoEffects()
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

        [Fact]
        public void ApplyWonderEffectsNoWondersEffectsTests()
        {
            mockedCity.Size = 5;
            mockedCity.MockPlayer = new MockedPlayer()
                .WithWonderEffect<HangingGardens>(true)
                .WithWonderEffect<CureForCancer>(false);

            mockedIGame.OnWonderObsoleteByType = (type) => true;

            var ct = new CitizenTypes
            {
                Citizens = new Citizen[5],
                Wonders = [],
                content = 2,
                unhappy = 3
            };
            testee.InitCitizens(ct.Citizens, 2, 3); // 2 content, 3 unhappy

            testee.ApplyWonderEffects(ct);

            Assert.Empty(ct.Wonders);

            var (happy, content, unhappy, redShirt) = testee.CountCitizenTypes(ct.Citizens);
            Assert.Equal(0, happy); // no effect
            Assert.Equal(2, content);
            Assert.Equal(3, unhappy);
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
            var player = new MockedPlayer().WithGovernmentType(government);
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

        [Fact]
        public void AdaptCitizensTests()
        {
            var ct = new CitizenTypes
            {
                Citizens =
				[
					Citizen.ContentFemale,
                    Citizen.ContentMale,
                    Citizen.UnhappyFemale,
                    Citizen.UnhappyMale,
                    Citizen.RedShirtFemale,
                    Citizen.RedShirtMale,
                    Citizen.HappyFemale,
                    Citizen.HappyMale
                ]
            };

            ct = testee.AdaptCitizens(ct);

            Assert.Equal(Citizen.ContentMale, ct.Citizens[0]);
            Assert.Equal(Citizen.ContentFemale, ct.Citizens[1]);
            Assert.Equal(Citizen.UnhappyMale, ct.Citizens[2]);
            Assert.Equal(Citizen.UnhappyFemale, ct.Citizens[3]);
            Assert.Equal(Citizen.RedShirtMale, ct.Citizens[4]);
            Assert.Equal(Citizen.RedShirtFemale, ct.Citizens[5]);
            Assert.Equal(Citizen.HappyMale, ct.Citizens[6]);
            Assert.Equal(Citizen.HappyFemale, ct.Citizens[7]);
        }
       
        [Fact]
        public void AdaptCitizensNoChangesTests()
        {
            var ct = new CitizenTypes
            {
                Citizens =
                [
                    Citizen.ContentMale,
                    Citizen.ContentFemale,
                    Citizen.UnhappyMale,
                    Citizen.UnhappyFemale,
                    Citizen.RedShirtMale,
                    Citizen.RedShirtFemale,
                    Citizen.HappyMale,
                    Citizen.HappyFemale
                ]
            };

            ct = testee.AdaptCitizens(ct);

            Assert.Equal(Citizen.ContentMale, ct.Citizens[0]);
            Assert.Equal(Citizen.ContentFemale, ct.Citizens[1]);
            Assert.Equal(Citizen.UnhappyMale, ct.Citizens[2]);
            Assert.Equal(Citizen.UnhappyFemale, ct.Citizens[3]);
            Assert.Equal(Citizen.RedShirtMale, ct.Citizens[4]);
            Assert.Equal(Citizen.RedShirtFemale, ct.Citizens[5]);
            Assert.Equal(Citizen.HappyMale, ct.Citizens[6]);
            Assert.Equal(Citizen.HappyFemale, ct.Citizens[7]);
        }
    }
}
