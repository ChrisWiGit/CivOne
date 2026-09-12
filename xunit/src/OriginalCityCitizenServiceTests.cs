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
    /// penalty, the pending unhappiness in both refill readings, the stage order, the single modifiers, and
    /// the two entry points of the service.
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

        private OriginalCityCitizenService Halving() => Create(new HalvingPendingUnhappinessDelegate().Refill);

        private OriginalCityCitizenService FullDrain() => Create(new FullDrainPendingUnhappinessDelegate().Refill);

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

            Assert.Equal(expected, Halving().EmpireSizeBase());
        }

        [Fact]
        public void CommunismSharesTheMonarchyEmpireSize()
        {
            _game.Difficulty = 4;
            ((MockedPlayer)_city.MockPlayer!).withGovernment(new CivOne.Governments.Communism());

            Assert.Equal(9, Halving().EmpireSizeBase());
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

            Assert.Equal(expected, Halving().BaseUnhappy());
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
            Assert.Equal(7, Halving().BaseUnhappy());
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
                Assert.Equal(0, Halving().EmpireSizePenalty());
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
                if (Halving().EmpireSizePenalty() > 0)
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
                if (Halving().EmpireSizePenalty() > 0)
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

            OriginalCityCitizenService testee = Halving();

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

            OriginalCityCitizenService testee = Halving();

            Assert.Equal(6, testee.EmpireSizePenalty());
            Assert.Equal(1, testee.BaseUnhappy());
            Assert.Equal(1, testee.GetCitizenTypes().unhappy);
        }

        [Fact]
        public void DeityUsesTheEmperorEmpireSize()
        {
            _game.Difficulty = 4;
            int emperor = Halving().EmpireSizeBase();

            _game.Difficulty = 5;
            int deity = Halving().EmpireSizeBase();

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

            Assert.Equal(8, Halving().BaseUnhappy());
            Assert.Equal(4, Halving().GetCitizenTypes().unhappy + Halving().GetCitizenTypes().redShirt);
            Assert.Equal(4, FullDrain().GetCitizenTypes().unhappy + FullDrain().GetCitizenTypes().redShirt);
        }

        [Fact]
        public void HalvingGivesBackPartOfAnImprovement()
        {
            WithPendingUnhappiness();
            _city.WithBuilding<Colosseum>();

            CitizenTypes ct = Halving().GetCitizenTypes();

            // The colosseum makes three citizens content, two of them turn unhappy again.
            Assert.Equal(3, ct.unhappy + ct.redShirt);
            Assert.Equal(2, ct.redShirt);
        }

        [Fact]
        public void FullDrainGivesBackAllOfAnImprovement()
        {
            WithPendingUnhappiness();
            _city.WithBuilding<Colosseum>();

            CitizenTypes ct = FullDrain().GetCitizenTypes();

            // The colosseum is absorbed completely while anything is still pending.
            Assert.Equal(4, ct.unhappy + ct.redShirt);
        }

        [Theory]
        [InlineData(true, 2)]    // halving
        [InlineData(false, 4)]   // full drain
        public void ShakespeareIsNotAbsoluteWhileUnhappinessIsPending(bool halving, int expectedUnhappy)
        {
            WithPendingUnhappiness();
            _city.WithWonder<ShakespearesTheatre>();

            CitizenTypes ct = halving ? Halving().GetCitizenTypes() : FullDrain().GetCitizenTypes();

            Assert.Equal(expectedUnhappy, ct.unhappy + ct.redShirt);
        }

        [Fact]
        public void ShakespeareIsAbsoluteWithoutPendingUnhappiness()
        {
            _city.WithWonder<ShakespearesTheatre>();

            CitizenTypes ct = Halving().GetCitizenTypes();

            Assert.Equal(0, ct.unhappy + ct.redShirt);
            Assert.Equal(6, ct.content);
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
            CitizenTypes ct = Halving().GetCitizenTypes();

            Assert.Equal(1, ct.unhappy + ct.redShirt);
        }

        // ── The single modifiers ──────────────────────────────────────────────────────────────────────

        [Fact]
        public void HangingGardensMakeOneCitizenHappy()
        {
            ((MockedPlayer)_city.MockPlayer!).WithWonderEffect<HangingGardens>();

            CitizenTypes ct = Halving().GetCitizenTypes();

            Assert.Equal(1, ct.happy);
            Assert.Equal(4, ct.unhappy);
        }

        [Fact]
        public void CureForCancerMakesOneCitizenHappy()
        {
            ((MockedPlayer)_city.MockPlayer!).WithWonderEffect<CureForCancer>();

            Assert.Equal(1, Halving().GetCitizenTypes().happy);
        }

        [Fact]
        public void TheTwoHappinessWondersAddUp()
        {
            ((MockedPlayer)_city.MockPlayer!)
                .WithWonderEffect<HangingGardens>()
                .WithWonderEffect<CureForCancer>();

            Assert.Equal(2, Halving().GetCitizenTypes().happy);
        }

        [Fact]
        public void BachsCathedralNeedsACityOnTheSameContinent()
        {
            MockedCity wonderCity = new MockedCity() { CityOwnerPlayerIndex = _city.CityOwnerPlayerIndex };
            wonderCity.ReturnHasWonderValues(false);
            wonderCity.WithWonder<JSBachsCathedral>();
            wonderCity.ContinentId = _city.ContinentId;
            _map.ReturnContinentCitiesValues(wonderCity);

            Assert.Equal(2, Halving().GetCitizenTypes().unhappy);
        }

        [Fact]
        public void BachsCathedralDoesNotReachAnotherContinent()
        {
            MockedCity wonderCity = new MockedCity() { CityOwnerPlayerIndex = _city.CityOwnerPlayerIndex };
            wonderCity.ReturnHasWonderValues(false);
            wonderCity.WithWonder<JSBachsCathedral>();
            wonderCity.ContinentId = _city.ContinentId + 1;
            _map.ReturnContinentCitiesValues(wonderCity);

            Assert.Equal(4, Halving().GetCitizenTypes().unhappy);
        }

        [Theory]
        [InlineData(false, false, 3)]   // the temple alone
        [InlineData(true, false, 2)]    // Mysticism doubles it
        [InlineData(false, true, 2)]    // the Oracle doubles it
        [InlineData(true, true, 0)]     // both together
        public void TempleAndOracle(bool hasMysticism, bool hasOracle, int expectedUnhappy)
        {
            MockedPlayer player = (MockedPlayer)_city.MockPlayer!;
            player.withAdvance<Mysticism>(hasMysticism);
            player.WithWonderEffect<Oracle>(hasOracle);
            _city.WithBuilding<Temple>();

            Assert.Equal(expectedUnhappy, Halving().GetCitizenTypes().unhappy);
        }

        [Fact]
        public void ColosseumMakesThreeCitizensContent()
        {
            _city.WithBuilding<Colosseum>();

            Assert.Equal(1, Halving().GetCitizenTypes().unhappy);
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

            Assert.Equal(expectedUnhappy, Halving().GetCitizenTypes().unhappy);
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
            Assert.Equal(0, Halving().GetCitizenTypes().unhappy);
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

            Assert.Equal(expectedDelta, Halving().CathedralDelta());
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
            Assert.Equal(27, Create(new HalvingPendingUnhappinessDelegate().Refill, luxuryRate: 5).Luxuries());
        }

        [Fact]
        public void LuxuriesNeverExceedTheTradeTheCityHas()
        {
            _city.TradeTotalGross = 6;
            _city.LuxuryCorruption = 0;

            // The full rate on six trade would round up to seven without the upper bound.
            Assert.Equal(6, Create(new HalvingPendingUnhappinessDelegate().Refill, luxuryRate: 10).Luxuries());
        }

        [Fact]
        public void TheLuxuryRateOfTheCallerWins()
        {
            _city.TradeTotalGross = 20;
            _city.LuxuryCorruption = 0;

            Assert.Equal(20, Create(new HalvingPendingUnhappinessDelegate().Refill, luxuryRate: 10).Luxuries());
            Assert.Equal(0, Create(new HalvingPendingUnhappinessDelegate().Refill, luxuryRate: 0).Luxuries());
        }

        [Fact]
        public void WithoutARateTheCityUsesWhatTheSlidersLeaveOver()
        {
            _city.TradeTotalGross = 20;
            _city.LuxuryCorruption = 0;

            // The mocked player keeps the default of five tenths tax and five tenths science.
            Assert.Equal(0, Halving().LuxuryRate);
        }

        // ── The two entry points ──────────────────────────────────────────────────────────────────────

        [Fact]
        public void BothEntryPointsAgreeOnTheSameInstance()
        {
            WithPendingUnhappiness();
            _city.WithBuilding<Colosseum>();

            OriginalCityCitizenService testee = Halving();

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

            foreach (CitizenTypes stage in Halving().EnumerateCitizens())
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
                if (Halving().EmpireSizePenalty() > 0)
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

            Assert.Equal(0, Halving().EmpireSizePenalty());
        }
    }
}
