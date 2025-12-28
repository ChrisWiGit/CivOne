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
using System.Diagnostics;
using Xunit.Abstractions;

namespace CivOne.UnitTests
{

    /// <summary>
    /// CW: Use this test to measure performance 
    /// of CityCitizenServiceImpl.
    /// Usually this implementation will take longer than
    /// the original one, due to added flexibility and features
    /// that were not present in the original code in City.cs.
    /// This test will allow to optimize performance without preoptimizing too much.
    /// </summary>
    public partial class CityCitizenServiceImplPerformanceTests : TestsBase
    {
        private readonly ITestOutputHelper output;

        public CityCitizenServiceImplPerformanceTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        CityCitizenServiceImplShim testee;
        List<Citizen> mockedSpecialists;

        MockedGame mockedIGame;
        MockedCity mockedCity;

        MockedMap mockedIMap;

        MockedGrassland mockedGrassland;
        public override void BeforeEach()
        {
            mockedSpecialists = [];
            mockedGrassland = new MockedGrassland();
            mockedGrassland.WithUnits(
                [new MockedUnit().WithHome(mockedCity)]);

            mockedCity = new MockedCity()
            {
                Size = 10,
                Tile = mockedGrassland,
                Entertainers = 2,
                Luxuries = 2 + 2 * 3, // 2 base + 2 luxuries * 3 (entertainer luxury value)
                ContinentId = 1,
            };

            mockedCity.MockPlayer = new MockedPlayer()
                .WithGovernmentType(typeof(Anarchy))
                .withAdvances([new Mysticism()])
                .WithWonderEffect<HangingGardens>(true)
                .WithWonderEffect<Oracle>(true)
                .WithWonderEffect<CureForCancer>(true)
                .withCitiesInterface([mockedCity])
                .withCitiesCount(CityCitizenServiceImpl.MinRedShirtCityCount);

            mockedIGame = new MockedGame()
            {
                Difficulty = 4,
                MaxDifficulty = 5,
                GameTurn = 1,
                OnGetPlayer = (playerId) => mockedCity.MockPlayer
            };
            mockedIMap = new MockedMap();
            mockedIMap.ReturnContinentCitiesValues(
                [mockedCity]);

            mockedSpecialists.AddRange([Citizen.Entertainer,
                Citizen.Entertainer]);

            mockedCity
                // .WithWonder<ShakespearesTheatre>() //This would shortcut ApplyBuildingEffects()
                .WithWonder<MichelangelosChapel>()
                .WithWonder<JSBachsCathedral>();
            mockedIGame.OnWonderObsoleteByType =
                (type) =>
                    type != typeof(MichelangelosChapel) &&
                    type != typeof(HangingGardens) &&
                    type != typeof(CureForCancer) &&
                    type != typeof(ShakespearesTheatre);

            mockedCity
                .WithBuilding<Colosseum>()
                .WithBuilding<Cathedral>()
                .WithBuilding<Temple>();


            testee = new CityCitizenServiceImplShim(
                mockedCity,
                mockedCity,
                mockedIGame,
                mockedSpecialists,
                mockedIMap
            );
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
        }

        public override void AfterEach()
        {
            testee = null;
        }

        [Fact]
        public void Measure()
        {
            /*
            CW:
            7 civs with 128 cities max each = 896 cities
            i.e. we expect at least 500 iterations per second on a decent machine
            In this way we can detect performance regressions in the future
            AND improve performance over time.
            */
            var watch = System.Diagnostics.Stopwatch.StartNew();

            int iterations = 0;
            long minElapsedMs = long.MaxValue;
            long maxElapsedMs = long.MinValue;

            while (watch.ElapsedMilliseconds < 1_000)
            {
                long innerWatchStart = watch.ElapsedMilliseconds;

                testee.GetCitizenTypes();

                long diff = watch.ElapsedMilliseconds - innerWatchStart;

                minElapsedMs = diff != 0 ? Math.Min(minElapsedMs, diff) : minElapsedMs;
                maxElapsedMs = Math.Max(maxElapsedMs, diff);

                iterations++;
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine($"Elapsed time for {iterations} iterations: {elapsedMs} ms");

            Assert.True(iterations >= 500, $"Performance test failed: only {iterations} iterations in {elapsedMs} ms");

            Assert.True(elapsedMs / iterations < 30, $"Performance test failed: single call took {elapsedMs} ms");

            output.WriteLine($"[CityCitizenServiceImplPerformanceTests.cs] Performance test passed: {iterations} iterations in {elapsedMs} ms");
            output.WriteLine($"Min elapsed time: {minElapsedMs} ms");
            output.WriteLine($"Max elapsed time: {maxElapsedMs} ms");
            output.WriteLine($"Average elapsed time: {(double)elapsedMs / iterations} ms");
        }
    }
}