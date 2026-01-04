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
using CivOne.UnitTests;

namespace CivOne.src
{
    /// <summary>
    /// All tests need to derive from this class. A fresh runtime, map, game, and player
    /// are built up and torn down for each test.
    /// </summary>
    public abstract class TestsBase : IDisposable
    {
        private RuntimeSettings rs;
        private MockRuntime runtime;
        internal Player playa;

        /// <summary>
        /// A hard-coded Game, using Earth and the player as Babylonian.
        /// </summary>
        protected TestsBase()
        {
            rs = new RuntimeSettings();
            rs.InitialSeed = 23905;
            runtime = new MockRuntime(rs);

            // Load Earth map
            Map.Reset(new MapGenerationWithoutThread());
            Map.Instance.LoadEarthMapInThread();

            // Start with Babylonians at King level
            Game.CreateGame(3, 2, Common.Civilizations.First(x => x.Name == "Babylonian"));
            playa = Game.Instance.HumanPlayer;

            BeforeEach();
        }

        public virtual void BeforeEach()
        {
            // Override in derived classes to set up before each test
        }
        
        public virtual void AfterEach()
        {
            // Override in derived classes to tear down after each test
        }

        public virtual void Dispose()
        {
            AfterEach();
            // Tear everything down
            Map.Reset();
            Game.Wipe();
            runtime?.Dispose();
            RuntimeHandler.Wipe();
            GC.Collect();
        }
    }
}
