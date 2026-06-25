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
using System.Threading.Tasks;
using CivOne.Persistence.Factories;
using CivOne.Persistence.Model;
using CivOne.UnitTests;
using Xunit;

namespace CivOne.src
{
    /// <summary>
    /// All tests need to derive from this class. A fresh runtime, map, game, and player
    /// are built up and torn down for each test.
    /// </summary>
    public abstract class TestsBase : IDisposable, IAsyncLifetime
    {
        private RuntimeSettings rs;
        private MockRuntime runtime;
        private bool _disposed;

        // Use the unchecked cast sanitizer for all tests deriving from this base, to preserve legacy behavior in integration-like scenarios where clamping would hide or alter values under test.
        private readonly IDisposable _checkedSanitizerScope;
        internal Player playa;

        /// <summary>
        /// A hard-coded Game, using Earth and the player as Babylonian.
        /// </summary>
        protected TestsBase()
        {
            _checkedSanitizerScope = ValueSanitizerFactory.UseCheckedValueSanitizer(new UncheckedCastValueSanitizer());

            rs = new RuntimeSettings();
            rs.InitialSeed = 23905;
            runtime = new MockRuntime(rs);

            // Load Earth map from bundled earth.yml (no MAP.PIC required)
            Map.Reset(new MapGenerationFromYaml());
            Map.Instance.LoadEarthMapInThread();

            // Start with Babylonians at King level
            Game.CreateGame(3, 2, Common.Civilizations.First(x => x.Name == "Babylonian"));
            playa = Game.Instance.HumanPlayer;
        }

        /// <summary>
        /// Initializes per-test state after the full test instance is constructed.
        /// </summary>
        /// <returns>
        /// A completed task.
        /// </returns>
        public virtual Task InitializeAsync()
        {
            BeforeEach();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes per-test state using xUnit async lifecycle.
        /// </summary>
        /// <returns>
        /// A completed task.
        /// </returns>
        public virtual Task DisposeAsync()
        {
            Dispose();
            return Task.CompletedTask;
        }

        protected virtual void BeforeEach()
        {
            // Override in derived classes to set up before each test
        }
        
        protected virtual void AfterEach()
        {
            // Override in derived classes to tear down after each test
        }

        /// <summary>
        /// Releases all resources for the test instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        /// True when called from <see cref="Dispose()"/>.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                AfterEach();
                _checkedSanitizerScope.Dispose();
                // Tear everything down
                Map.Reset();
                Game.Wipe();
                runtime?.Dispose();
                RuntimeHandler.Wipe();
            }

            _disposed = true;
            GC.Collect();
        }
    }
}
