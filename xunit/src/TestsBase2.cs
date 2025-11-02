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
using System.Threading;
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
        internal Player playa;

        /// <summary>
        /// A hard-coded Game, using Earth and the player as Chinese.
        /// </summary>
        protected TestsBase2()
        {
            rs = new RuntimeSettings();
            rs.InitialSeed = 7595;
            runtime = new MockRuntime(rs);

            // Load Earth map
            Map.Reset(new MapGenerationWithoutThread());
            Map.Instance.LoadEarthMapInThread();

            // Start with Chinese, 7 players, at King level
            Game.CreateGame(3, 7, Common.Civilizations.First(x => x.Name=="Chinese"));
            playa = Game.Instance.HumanPlayer;

            City.Game = Game.Instance; // CW: Dependency Injection for now. Ensure static Game reference is set
        }

        public void Dispose()
        {
            // Tear everything down
            Map.Reset();
            Game.Wipe();
            runtime?.Dispose();
            RuntimeHandler.Wipe();
            GC.Collect();
        }
    }
}