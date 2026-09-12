using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.src;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Governments;
using CivOne.Screens.Services;
using CivOne.Wonders;
using Xunit;

namespace CivOne.UnitTests
{
    /// <summary>
    /// Tests for the corrected happiness model.
    ///
    /// The cases are grouped the way the implementation plan groups them: the base term and the empire size
    /// penalty, the pending unhappiness, the stage order, the single modifiers, and the two entry points of
    /// the service.
    /// </summary>
    public class OriginalCityCitizenServiceTests : TestsBase
    {
        private MockedGame _game = null!;
        private MockedCity _city = null!;
        private MockedMap _map = null!;
        private List<Citizen> _specialists = null!;

        protected override void BeforeEach()
        {
            _specialists = [];
            _map = new MockedMap();
            _city = new MockedCity()
            {
                Size = 6,
                Tile = new MockedGrassland()
            };
            _city.MockPlayer = new MockedPlayer().WithGovernmentType(typeof(Despotism));
            _city.ReturnHasWonderValues(false);
            _city.ReturnHasBuildingValues(false);
            _game = new MockedGame() { Difficulty = 4, MaxDifficulty = 5, GameTurn = 1 };
            _game.OnWonderObsoleteByType = _ => false;
            _game.OnGetPlayer = _ => new MockedPlayer().withCitiesCount(1);
            _game.OnGetCityIndex = _ => 0;
            _game.OnGetUnits = (_, _) => [];
        }

        private OriginalCityCitizenService Create(PendingUnhappinessRefill refill, int? luxuryRate = null)
            => new(_city, _city, _game, _specialists, _map, refill, luxuryRate);

        private OriginalCityCitizenService Testee() => Create(new EqualisingPendingUnhappinessDelegate().Refill);

        private void WithEmpire(int cityCount, int cityIndex = 0)
        {
            _game.OnGetPlayer = _ => new MockedPlayer().withCitiesCount(cityCount);
            _game.OnGetCityIndex = _ => cityIndex;
        }

        private void WithGovernment(Type government)
        {
            MockedPlayer player = (MockedPlayer)_city.MockPlayer!;
            player.WithGovernmentType(government);
        }

        // ── The base term and the empire size penalty ──────────────────────────────────────────────────

        [Theory]
        [InlineData(0, typeof(Despotism), 14)]
        [InlineData(0, typeof(CivOne.Governments.Monarchy), 21)]
        [InlineData(0, typeof(Republic), 28)]
        [InlineData(1, typeof(Despotism), 12)]
        [InlineData(2, typeof(Despotism), 10)]
        [InlineData(3, typeof(Despotism), 8)]
        [InlineData(3, typeof(Anarchy), 8)]
        [InlineData(4, typeof(Despotism), 6)]
        [InlineData(4, typeof(CivOne.Governments.Monarchy), 9)]
        [InlineData(4, typeof(Republic), 12)]
        [InlineData(4, typeof(CivOne.Governments.Democracy), 12)]
        [InlineData(5, typeof(Despotism), 6)]
        [InlineData(5, typeof(Republic), 12)]
        public void EmpireSizeBasePerDifficultyAndGovernment(int difficulty, Type government, int expected)
        {
            _game.Difficulty = difficulty;
            WithGovernment(government);

            Assert.Equal(expected, Testee().EmpireSizeBase());
        }

        [Fact]
        public void CommunismSharesTheMonarchyEmpireSize()
        {
            _game.Difficulty = 4;
            ((MockedPlayer)_city.MockPlayer!).withGovernment(new CivOne.Governments.Communism());

            Assert.Equal(9, Testee().EmpireSizeBase());
        }

        [Theory]
        [InlineData(0, 4)]
        [InlineData(1, 5)]
        [InlineData(2, 6)]
        [InlineData(3, 7)]
        [InlineData(4, 8)]
        [InlineData(5, 9)]
        public void BaseUnhappyGrowsWithDifficulty(int difficulty, int expected)
        {
            _city.Size = 10;
            _game.Difficulty = difficulty;

            Assert.Equal(expected, Testee().BaseUnhappy());
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(5, 40)]
        public void ComputerPlayersCarryTheirOwnBaseTerm(int difficulty, int cityCount)
        {
            _city.Size = 10;
            _game.Difficulty = difficulty;
            _game.OnIsHumanPlayer = _ => false;
            WithEmpire(cityCount);

            // Neither the difficulty nor the empire size reaches a computer player.
            Assert.Equal(7, Testee().BaseUnhappy());
        }

        [Theory]
        [InlineData(0, typeof(Despotism), 14)]
        [InlineData(3, typeof(Republic), 16)]
        [InlineData(4, typeof(Despotism), 6)]
        public void NoPenaltyWhileTheEmpireStaysWithinItsBase(int difficulty, Type government, int sizeBase)
        {
            _game.Difficulty = difficulty;
            WithGovernment(government);

            for (int slot = 0; slot < sizeBase; slot++)
            {
                WithEmpire(sizeBase, slot);
                Assert.Equal(0, Testee().EmpireSizePenalty());
            }
        }

        [Theory]
        [InlineData(0, typeof(Despotism), 14)]
        [InlineData(3, typeof(Republic), 16)]
        [InlineData(4, typeof(Despotism), 6)]
        public void TheLastSlotIsPenalisedFirst(int difficulty, Type government, int sizeBase)
        {
            _game.Difficulty = difficulty;
            WithGovernment(government);

            int penalised = 0;
            int penalisedSlot = -1;

            for (int slot = 0; slot < sizeBase; slot++)
            {
                WithEmpire(sizeBase + 1, slot);
                if (Testee().EmpireSizePenalty() > 0)
                {
                    penalised++;
                    penalisedSlot = slot;
                }
            }

            Assert.Equal(1, penalised);
            Assert.Equal(sizeBase - 1, penalisedSlot);
        }

        [Theory]
        [InlineData(6, 0)]    // the empire is exactly at its base
        [InlineData(7, 1)]    // one more city, one penalised slot
        [InlineData(9, 3)]
        [InlineData(11, 5)]
        [InlineData(12, 6)]   // at twice the base every slot carries the penalty
        public void ThePenaltyReachesOneSlotPerExtraCity(int cityCount, int expectedPenalisedSlots)
        {
            _game.Difficulty = 4;
            const int SizeBase = 6;

            int penalised = 0;
            for (int slot = 0; slot < SizeBase; slot++)
            {
                WithEmpire(cityCount, slot);
                if (Testee().EmpireSizePenalty() > 0)
                {
                    penalised++;
                }
            }

            Assert.Equal(expectedPenalisedSlots, penalised);
        }

        [Fact]
        public void ThePenaltyIsAddedBeforeTheLowerClamp()
        {
            _city.Size = 1;
            _game.Difficulty = 0;

            // Base 14 under a despotism, 45 cities, slot 0: the penalty is 2.
            WithEmpire(45);

            OriginalCityCitizenService testee = Testee();

            Assert.Equal(2, testee.EmpireSizePenalty());

            // 1 + 0 - 6 + 2. Clamping the size term first and adding the penalty afterwards would leave 2
            // unhappy citizens in a city of one.
            Assert.Equal(-3, testee.BaseUnhappy());
            Assert.Equal(0, testee.GetCitizenTypes().unhappy);
        }

        [Fact]
        public void ASmallCityTurnsUnhappyOnceThePenaltyOutgrowsItsSize()
        {
            _city.Size = 1;
            _game.Difficulty = 0;

            // Base 14 under a despotism, 98 cities, slot 0: the penalty is 6.
            WithEmpire(98);

            OriginalCityCitizenService testee = Testee();

            Assert.Equal(6, testee.EmpireSizePenalty());
            Assert.Equal(1, testee.BaseUnhappy());
            Assert.Equal(1, testee.GetCitizenTypes().unhappy);
        }

        [Fact]
        public void DeityUsesTheEmperorEmpireSize()
        {
            _game.Difficulty = 4;
            int emperor = Testee().EmpireSizeBase();

            _game.Difficulty = 5;
            int deity = Testee().EmpireSizeBase();

            Assert.Equal(6, emperor);
            Assert.Equal(emperor, deity);
        }

        // ── The pending unhappiness ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// A city of four at deity level in an empire of forty: the base term asks for eight unhappy
        /// citizens, four fit into the city and four stay pending.
        /// </summary>
        private void WithPendingUnhappiness()
        {
            _city.Size = 4;
            _game.Difficulty = 5;
            WithEmpire(40);
        }

        [Fact]
        public void WhatDoesNotFitIntoTheCityStaysPending()
        {
            WithPendingUnhappiness();

            Assert.Equal(8, Testee().BaseUnhappy());
            Assert.Equal(4, Testee().GetCitizenTypes().unhappy + Testee().GetCitizenTypes().redShirt);
        }

        [Fact]
        public void AnImprovementIsPartlyAbsorbedByThePendingUnhappiness()
        {
            WithPendingUnhappiness();
            _city.WithBuilding<Colosseum>();

            CitizenTypes ct = Testee().GetCitizenTypes();

            // The colosseum makes three citizens content, two of them turn unhappy again.
            Assert.Equal(3, ct.unhappy + ct.redShirt);
            Assert.Equal(2, ct.redShirt);
        }

        [Fact]
        public void ShakespeareIsNotAbsoluteWhileUnhappinessIsPending()
        {
            WithPendingUnhappiness();
            _city.WithWonder<ShakespearesTheatre>();

            CitizenTypes ct = Testee().GetCitizenTypes();

            // Shakespeare removes all four visible unhappy citizens, then the pending four are balanced
            // against the empty visible side, which brings two of them back.
            Assert.Equal(2, ct.unhappy + ct.redShirt);
        }

        [Fact]
        public void ShakespeareIsAbsoluteWithoutPendingUnhappiness()
        {
            _city.WithWonder<ShakespearesTheatre>();

            CitizenTypes ct = Testee().GetCitizenTypes();

            Assert.Equal(0, ct.unhappy + ct.redShirt);
            Assert.Equal(6, ct.content);
        }

        [Fact]
        public void ShakespeareWipesOutTheWarWeariness()
        {
            _game.Difficulty = 0;
            WithGovernment(typeof(CivOne.Governments.Democracy));
            _city.WithWonder<ShakespearesTheatre>();

            // Two units abroad under a democracy are four points of war weariness in stage 4.
            // The theatre runs in stage 5, after them, so the city ends up with nobody unhappy.
            // Applying it with the buildings in stage 3 would let the weariness undo it.
            _game.OnGetUnits = (_, _) =>
            [
                new MockedUnit(5, 5).WithHome(_city),
                new MockedUnit(6, 6).WithHome(_city)
            ];

            Assert.Equal(0, Testee().GetCitizenTypes().unhappy);
        }

        [Fact]
        public void BachsCathedralRunsAfterTheWarWeariness()
        {
            _game.Difficulty = 0;
            WithGovernment(typeof(CivOne.Governments.Democracy));
            _city.ContinentId = 1;
            _map.ReturnContinentCitiesValues(new MockedCity
            {
                ContinentId = 1,
                CityOwnerPlayerIndex = _city.CityOwnerPlayerIndex
            }.WithWonder<JSBachsCathedral>());

            // Size 6 on difficulty 0 has no unhappy citizens of its own.
            // One unit abroad under a democracy adds two in stage 4, and the cathedral takes exactly those
            // two away again in stage 5.
            _game.OnGetUnits = (_, _) => [new MockedUnit(5, 5).WithHome(_city)];

            Assert.Equal(0, Testee().GetCitizenTypes().unhappy);
        }

        // ── The stage order ───────────────────────────────────────────────────────────────────────────

        [Fact]
        public void AModifierLosesItsSurplusAtItsOwnStage()
        {
            _game.Difficulty = 3;
            WithGovernment(typeof(Republic));
            _city.WithBuilding<Colosseum>();
            _city.WithBuilding<Temple>();
            _game.OnGetUnits = (_, _) => [new MockedUnit(5, 5).WithHome(_city)];

            // Three unhappy citizens meet four points of building effect. The spare point belongs to stage
            // three and must not pay for the war weariness of stage four.
            CitizenTypes ct = Testee().GetCitizenTypes();

            Assert.Equal(1, ct.unhappy + ct.redShirt);
        }

        // ── The single modifiers ──────────────────────────────────────────────────────────────────────

        [Fact]
        public void HangingGardensMakeOneCitizenHappy()
        {
            ((MockedPlayer)_city.MockPlayer!).WithWonderEffect<HangingGardens>();

            CitizenTypes ct = Testee().GetCitizenTypes();

            Assert.Equal(1, ct.happy);
            Assert.Equal(4, ct.unhappy);
        }

        [Fact]
        public void CureForCancerMakesOneCitizenHappy()
        {
            ((MockedPlayer)_city.MockPlayer!).WithWonderEffect<CureForCancer>();

            Assert.Equal(1, Testee().GetCitizenTypes().happy);
        }

        [Fact]
        public void TheTwoHappinessWondersAddUp()
        {
            ((MockedPlayer)_city.MockPlayer!)
                .WithWonderEffect<HangingGardens>()
                .WithWonderEffect<CureForCancer>();

            Assert.Equal(2, Testee().GetCitizenTypes().happy);
        }

        [Fact]
        public void BachsCathedralNeedsACityOnTheSameContinent()
        {
            MockedCity wonderCity = new MockedCity() { CityOwnerPlayerIndex = _city.CityOwnerPlayerIndex };
            wonderCity.ReturnHasWonderValues(false);
            wonderCity.WithWonder<JSBachsCathedral>();
            wonderCity.ContinentId = _city.ContinentId;
            _map.ReturnContinentCitiesValues(wonderCity);

            Assert.Equal(2, Testee().GetCitizenTypes().unhappy);
        }

        [Fact]
        public void BachsCathedralDoesNotReachAnotherContinent()
        {
            MockedCity wonderCity = new MockedCity() { CityOwnerPlayerIndex = _city.CityOwnerPlayerIndex };
            wonderCity.ReturnHasWonderValues(false);
            wonderCity.WithWonder<JSBachsCathedral>();
            wonderCity.ContinentId = _city.ContinentId + 1;
            _map.ReturnContinentCitiesValues(wonderCity);

            Assert.Equal(4, Testee().GetCitizenTypes().unhappy);
        }

        [Theory]
        // ceremonial burial, mysticism, oracle, expected unhappy out of the four the city starts with
        [InlineData(false, false, false, 4)]   // without an advance the temple does nothing at all
        [InlineData(true, false, false, 3)]    // Ceremonial Burial makes it worth one
        [InlineData(false, true, false, 2)]    // Mysticism makes it worth two, Ceremonial Burial is not needed
        [InlineData(true, true, false, 2)]     // and it does not stack with Ceremonial Burial
        [InlineData(false, false, true, 3)]    // the oracle needs a temple, not an advance
        [InlineData(true, false, true, 2)]     // one for the temple, one for the oracle
        [InlineData(false, true, true, 0)]     // two for each once Mysticism is known
        [InlineData(true, true, true, 0)]
        public void TempleAndOracle(
            bool hasCeremonialBurial,
            bool hasMysticism,
            bool hasOracle,
            int expectedUnhappy)
        {
            MockedPlayer player = (MockedPlayer)_city.MockPlayer!;
            player.withAdvance<CeremonialBurial>(hasCeremonialBurial);
            player.withAdvance<Mysticism>(hasMysticism);
            player.WithWonderEffect<Oracle>(hasOracle);
            _city.WithBuilding<Temple>();

            Assert.Equal(expectedUnhappy, Testee().GetCitizenTypes().unhappy);
        }

        [Fact]
        public void TheOracleNeedsATempleToWork()
        {
            MockedPlayer player = (MockedPlayer)_city.MockPlayer!;
            player.withAdvance<Mysticism>();
            player.WithWonderEffect<Oracle>();

            // No temple in this city, so the oracle has nothing to attach to.
            Assert.Equal(4, Testee().GetCitizenTypes().unhappy);
        }

        [Fact]
        public void ColosseumMakesThreeCitizensContent()
        {
            _city.WithBuilding<Colosseum>();

            Assert.Equal(1, Testee().GetCitizenTypes().unhappy);
        }

        [Theory]
        [InlineData(typeof(Republic), false, 1)]
        [InlineData(typeof(Republic), true, 0)]
        [InlineData(typeof(CivOne.Governments.Democracy), false, 2)]
        [InlineData(typeof(CivOne.Governments.Democracy), true, 1)]
        public void WomensSuffrageNeverAddsWarWeariness(
            Type government,
            bool hasWomensSuffrage,
            int expectedUnhappy)
        {
            _game.Difficulty = 0;
            WithGovernment(government);
            ((MockedPlayer)_city.MockPlayer!).WithWonderEffect<WomensSuffrage>(hasWomensSuffrage);

            // The city stands at 0,0, so a unit at 5,5 is away from home.
            _game.OnGetUnits = (_, _) => [new MockedUnit(5, 5).WithHome(_city)];

            Assert.Equal(expectedUnhappy, Testee().GetCitizenTypes().unhappy);
        }

        [Theory]
        [InlineData(UnitClass.Air, 1)]      // an air unit is weary even at home
        [InlineData(UnitClass.Water, 0)]    // a ship in its home port is not
        [InlineData(UnitClass.Land, 0)]     // nor is a land unit standing in the city
        public void OnlyAirUnitsAreWearyWhileAtHome(UnitClass unitClass, int expectedUnhappy)
        {
            _game.Difficulty = 0;
            WithGovernment(typeof(Republic));

            // The unit stands on the city tile itself, so only the air class makes it count as away.
            _game.OnGetUnits = (_, _) => [new MockedUnit(_city.Location.X, _city.Location.Y)
                .WithHome(_city)
                .WithCategory(unitClass)];

            Assert.Equal(expectedUnhappy, Testee().GetCitizenTypes().unhappy);
        }

        [Fact]
        public void WarWearinessRaisesTheUnhappyCountAndDoesNotDowngradeHappyCitizens()
        {
            _game.Difficulty = 0;
            WithGovernment(typeof(Republic));

            // Size 4 on difficulty 0 has four content citizens; the luxuries then make two of them happy.
            // One unit abroad under a republic is one point of weariness.
            //
            // The original adds that point to the unhappy counter, so the city ends up with one unhappy
            // citizen. Spending the point on downgrading the leading citizen instead would turn a happy one
            // back into a content one and leave nobody unhappy, which is what this test guards against.
            _city.Size = 4;
            _city.TradeTotalGross = 20;
            _city.LuxuryCorruption = 0;
            _game.OnGetUnits = (_, _) => [new MockedUnit(5, 5).WithHome(_city)];

            CitizenTypes citizens = Create(new EqualisingPendingUnhappinessDelegate().Refill, luxuryRate: 2)
                .GetCitizenTypes();

            Assert.Equal(2, citizens.happy);
            Assert.Equal(1, citizens.unhappy);
        }

        [Fact]
        public void OnlyTheSurplusBeyondTheCitySizeIsParked()
        {
            // Regression: the parked unhappiness used to be measured against the seats
            // (size minus specialists) rather than against the city size. A city whose base unhappiness fit
            // into the city but not into its seats then parked the difference, and that surplus shielded the
            // happy citizens from the very first normalisation — every crowded city showed one happy citizen
            // too many.
            //
            // Taken from a real save: size 18, Warlord, despotism, a marketplace and six entertainers.
            // Base unhappiness is 18 + 1 - 6 = 13, which fits into the city of 18 but not into the 12 seats,
            // so nothing may be parked. The luxuries give 6 x 2 = 12, raised to 18 by the marketplace, so
            // nine raw happy citizens meet twelve unhappy ones against twelve seats. The normalisation lowers
            // both together until the sum is 11, which leaves four happy and seven unhappy.
            _game.Difficulty = 1;
            WithGovernment(typeof(Despotism));
            _city.Size = 18;
            _city.Entertainers = 6;
            _city.WithBuilding<MarketPlace>();
            _specialists.AddRange(Enumerable.Repeat(Citizen.Entertainer, 6));

            CitizenTypes ct = Create(new EqualisingPendingUnhappinessDelegate().Refill, luxuryRate: 0)
                .GetCitizenTypes();

            Assert.Equal(4, ct.happy);
            Assert.Equal(1, ct.content);
            Assert.Equal(7, ct.unhappy);
            Assert.Equal(0, ct.redShirt);
        }

        // ── Modifiers in combination ───────────────────────────────────────────────────────────────────
        //
        // The single modifiers are pinned above. These cases pin what they do *together*, because the
        // consolidation of Phase F moves them between classes and stages. A combination that is only ever
        // tested one effect at a time can silently change its total without any test noticing.

        private MockedCity WithBachsCathedralOnTheSameContinent()
        {
            MockedCity wonderCity = new MockedCity() { CityOwnerPlayerIndex = _city.CityOwnerPlayerIndex };
            wonderCity.ReturnHasWonderValues(false);
            wonderCity.WithWonder<JSBachsCathedral>();
            wonderCity.ContinentId = _city.ContinentId;
            _map.ReturnContinentCitiesValues(wonderCity);
            return wonderCity;
        }

        [Fact]
        public void TheCathedralAndBachsCathedralAddUpAcrossTheirStages()
        {
            // The cathedral works in stage 3 and J.S. Bach's Cathedral in stage 5, with a normalisation
            // between them. Their totals still add up: eight unhappy citizens lose four to the cathedral and
            // two more to the wonder.
            _city.Size = 10;
            _game.Difficulty = 4;
            ((MockedPlayer)_city.MockPlayer!).withAdvance<Religion>();
            _city.WithBuilding<Cathedral>();
            WithBachsCathedralOnTheSameContinent();

            CitizenTypes ct = Testee().GetCitizenTypes();

            Assert.Equal(2, ct.unhappy);
            Assert.Equal(8, ct.content);
            Assert.Equal(0, ct.happy);
        }

        [Fact]
        public void TheBuildingsOfStageThreeAddUp()
        {
            // Temple, colosseum and cathedral are summed into one conversion inside stage 3.
            // 10 + 4 - 6 = 8 unhappy, minus 1 temple, minus 3 colosseum, minus 4 cathedral, leaves none.
            _city.Size = 10;
            _game.Difficulty = 4;
            ((MockedPlayer)_city.MockPlayer!)
                .withAdvance<Religion>()
                .withAdvance<CeremonialBurial>();
            _city.WithBuilding<Temple>();
            _city.WithBuilding<Colosseum>();
            _city.WithBuilding<Cathedral>();

            CitizenTypes ct = Testee().GetCitizenTypes();

            Assert.Equal(0, ct.unhappy);
            Assert.Equal(10, ct.content);
        }

        [Fact]
        public void ShakespeareLeavesNothingForBachsCathedral()
        {
            // Both run in stage 5, the theatre first. It clears the unhappiness of its own city, so the
            // wonder that follows finds nothing to convert and the result is the same as the theatre alone.
            _city.Size = 10;
            _game.Difficulty = 4;
            _city.WithWonder<ShakespearesTheatre>();
            WithBachsCathedralOnTheSameContinent();

            CitizenTypes ct = Testee().GetCitizenTypes();

            Assert.Equal(0, ct.unhappy);
            Assert.Equal(10, ct.content);
        }

        [Fact]
        public void TheHappinessWondersAndTheBuildingsDoNotGetInEachOthersWay()
        {
            // The colosseum converts unhappy citizens in stage 3, the two wonders raise content ones to happy
            // in stage 5. 10 + 4 - 6 = 8 unhappy, minus 3 leaves 5, and two of the five content citizens
            // become happy.
            _city.Size = 10;
            _game.Difficulty = 4;
            _city.WithBuilding<Colosseum>();
            ((MockedPlayer)_city.MockPlayer!)
                .WithWonderEffect<HangingGardens>()
                .WithWonderEffect<CureForCancer>();

            CitizenTypes ct = Testee().GetCitizenTypes();

            Assert.Equal(2, ct.happy);
            Assert.Equal(3, ct.content);
            Assert.Equal(5, ct.unhappy);
        }

        // ── The invariants, across the whole parameter space ───────────────────────────────────────────

        [Fact]
        public void EveryConstellationKeepsTheCitizenInvariants()
        {
            Type[] governments = [typeof(Despotism), typeof(CivOne.Governments.Monarchy), typeof(Republic)];
            int checkedCases = 0;

            foreach (Type government in governments)
            {
                foreach (int difficulty in Enumerable.Range(0, 6))
                {
                    foreach (int size in Enumerable.Range(1, 20))
                    {
                        foreach (int specialists in Enumerable.Range(0, size + 1))
                        {
                            BeforeEach();
                            WithGovernment(government);
                            _game.Difficulty = difficulty;
                            _city.Size = (byte)size;
                            _city.Entertainers = specialists;
                            _specialists.AddRange(Enumerable.Repeat(Citizen.Entertainer, specialists));
                            WithEmpire(40);

                            int seats = Math.Max(size - specialists, 0);

                            foreach (CitizenTypes stage in Testee().EnumerateCitizens())
                            {
                                Assert.True(stage.Valid(),
                                    $"negative count at size {size}, difficulty {difficulty}, "
                                    + $"{specialists} specialists, {government.Name}");
                                Assert.Equal(size, stage.Sum());
                                Assert.True(stage.happy + stage.unhappy + stage.redShirt <= seats,
                                    $"more citizens than seats at size {size}, difficulty {difficulty}, "
                                    + $"{specialists} specialists, {government.Name}");
                            }

                            checkedCases++;
                        }
                    }
                }
            }

            // 3 governments x 6 difficulties x sum over sizes 1..20 of (size + 1) specialist counts.
            Assert.Equal(3 * 6 * 230, checkedCases);
        }

        [Fact]
        public void MartialLawAndWarWearinessAreExclusive()
        {
            _game.Difficulty = 0;

            // A despotism keeps order with troops, so the units at home count and the ones away do not.
            _city.Size = 8;
            _city.Tile = new MockedGrassland().WithUnits(new MockedUnit(), new MockedUnit());
            _game.OnGetUnits = (_, _) => [new MockedUnit(5, 5).WithHome(_city)];

            // 8 + 0 - 6 gives two unhappy citizens, two units at home make both content.
            Assert.Equal(0, Testee().GetCitizenTypes().unhappy);
        }

        [Theory]
        [InlineData(false, false, 0)]   // without Religion the cathedral does nothing
        [InlineData(true, false, 4)]    // with Religion it makes four citizens content
        [InlineData(true, true, 6)]     // Michelangelo raises it, wherever the chapel stands
        public void CathedralNeedsReligionAndIsRaisedByMichelangelo(
            bool hasReligion,
            bool hasChapel,
            int expectedDelta)
        {
            MockedPlayer player = (MockedPlayer)_city.MockPlayer!;
            player.withAdvance<Religion>(hasReligion);
            player.WithWonderEffect<MichelangelosChapel>(hasChapel);
            _city.WithBuilding<Cathedral>();

            // The chapel stands on another continent than the city, which must not matter.
            _city.ContinentId = 1;

            Assert.Equal(expectedDelta, Testee().CathedralDelta());
        }

        [Fact]
        public void LuxuriesFollowTheOriginalOrder()
        {
            _city.TradeTotalGross = 20;
            _city.LuxuryCorruption = 4;
            _city.Entertainers = 2;
            _city.WithBuilding<MarketPlace>();
            _city.WithBuilding<Bank>();

            // (5 x 16 + 5) / 10 = 8, plus 2 entertainers x 2 = 12, market place 18, bank 27.
            Assert.Equal(27, Create(new EqualisingPendingUnhappinessDelegate().Refill, luxuryRate: 5).Luxuries());
        }

        [Fact]
        public void LuxuriesNeverExceedTheTradeTheCityHas()
        {
            _city.TradeTotalGross = 6;
            _city.LuxuryCorruption = 0;

            // The full rate on six trade would round up to seven without the upper bound.
            Assert.Equal(6, Create(new EqualisingPendingUnhappinessDelegate().Refill, luxuryRate: 10).Luxuries());
        }

        [Fact]
        public void TheLuxuryRateOfTheCallerWins()
        {
            _city.TradeTotalGross = 20;
            _city.LuxuryCorruption = 0;

            Assert.Equal(20, Create(new EqualisingPendingUnhappinessDelegate().Refill, luxuryRate: 10).Luxuries());
            Assert.Equal(0, Create(new EqualisingPendingUnhappinessDelegate().Refill, luxuryRate: 0).Luxuries());
        }

        [Fact]
        public void WithoutARateTheCityUsesWhatTheSlidersLeaveOver()
        {
            _city.TradeTotalGross = 20;
            _city.LuxuryCorruption = 0;

            // The mocked player keeps the default of five tenths tax and five tenths science.
            Assert.Equal(0, Testee().LuxuryRate);
        }

        // ── The two entry points ──────────────────────────────────────────────────────────────────────

        [Fact]
        public void BothEntryPointsAgreeOnTheSameInstance()
        {
            WithPendingUnhappiness();
            _city.WithBuilding<Colosseum>();

            OriginalCityCitizenService testee = Testee();

            CitizenTypes first = testee.GetCitizenTypes();
            CitizenTypes second = testee.GetCitizenTypes();
            CitizenTypes lastStage = testee.EnumerateCitizens().Last();
            CitizenTypes afterEnumerating = testee.GetCitizenTypes();

            Assert.Equal(first.happy, second.happy);
            Assert.Equal(first.unhappy, second.unhappy);
            Assert.Equal(first.redShirt, second.redShirt);

            Assert.Equal(first.happy, lastStage.happy);
            Assert.Equal(first.unhappy, lastStage.unhappy);
            Assert.Equal(first.redShirt, lastStage.redShirt);

            Assert.Equal(first.unhappy, afterEnumerating.unhappy);
            Assert.Equal(first.redShirt, afterEnumerating.redShirt);
        }

        [Fact]
        public void EveryStageKeepsTheCitizenCount()
        {
            WithPendingUnhappiness();
            _city.WithBuilding<Colosseum>();

            foreach (CitizenTypes stage in Testee().EnumerateCitizens())
            {
                Assert.Equal(4, stage.Sum());
                Assert.True(stage.Valid());
            }
        }

        // ── The city slot ─────────────────────────────────────────────────────────────────────────────

        [Fact]
        public void TheCitySlotDecidesWhichCityIsPenalised()
        {
            _game.Difficulty = 4;

            // Base 6 under a despotism. With seven cities only slot 5 is penalised, with eight cities
            // slots 4 and 5 are. This pins the mapping, so a change to the slot source fails loudly.
            int[] penalisedAtSeven = PenalisedSlots(7, 6);
            int[] penalisedAtEight = PenalisedSlots(8, 6);

            Assert.Equal(ExpectedAtSeven, penalisedAtSeven);
            Assert.Equal(ExpectedAtEight, penalisedAtEight);
        }

        private static readonly int[] ExpectedAtSeven = [5];
        private static readonly int[] ExpectedAtEight = [4, 5];

        private int[] PenalisedSlots(int cityCount, int sizeBase)
        {
            List<int> slots = [];
            for (int slot = 0; slot < sizeBase; slot++)
            {
                WithEmpire(cityCount, slot);
                if (Testee().EmpireSizePenalty() > 0)
                {
                    slots.Add(slot);
                }
            }

            return [.. slots];
        }

        [Fact]
        public void ACityOutsideTheGameIsTreatedAsTheFirstSlot()
        {
            _game.Difficulty = 4;
            _game.OnGetPlayer = _ => new MockedPlayer().withCitiesCount(7);
            _game.OnGetCityIndex = _ => -1;

            Assert.Equal(0, Testee().EmpireSizePenalty());
        }
    }
}
