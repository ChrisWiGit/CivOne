// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

// Author: Kevin Routley : July, 2019

using System;
using System.Linq;
using CivOne.Persistence.Factories;
using CivOne.Persistence.Model;
using CivOne.Services;
using CivOne.src;

namespace CivOne.UnitTests
{
    /// <summary>
    /// All tests need to derive from this class. A fresh runtime, map, game, and player
    /// are built up and torn down for each test.
    /// </summary>
    public abstract class TestsBase2 : IDisposable
    {
        private RuntimeSettings rs;
        private MockRuntime runtime;

        // Use the unchecked cast sanitizer for all tests deriving from this base, to preserve legacy behavior in integration-like scenarios where clamping would hide or alter values under test.
        private readonly IDisposable _checkedSanitizerScope;
        internal Player playa;

        /// <summary>
        /// A hard-coded Game, using Earth and the player as Chinese.
        /// </summary>
        protected TestsBase2()
        {
            TranslationServiceFactory.ResetForTests();
            _checkedSanitizerScope = ValueSanitizerFactory.UseCheckedValueSanitizer(new UncheckedCastValueSanitizer());

            rs = new RuntimeSettings();
            rs.InitialSeed = 7595;
            runtime = new MockRuntime(rs);

            // Load Earth map from bundled earth.yml (no MAP.PIC required)
            Map.Reset(new MapGenerationFromYaml());
            Map.Instance.LoadEarthMapInThread();

            // Start with Chinese, 7 players, at King level
            Game.CreateGame(3, 7, Common.Civilizations.First(x => x.Name=="Chinese"));
            playa = Game.Instance.HumanPlayer;
        }

        public void Dispose()
        {
            _checkedSanitizerScope.Dispose();
            // Tear everything down
            Map.Reset();
            Game.Wipe();
            runtime?.Dispose();
            RuntimeHandler.Wipe();
            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
}