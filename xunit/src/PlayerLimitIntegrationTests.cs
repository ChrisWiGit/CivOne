using System;
using System.Linq;
using CivOne.Persistence.Factories;
using CivOne.Persistence.Model;
using CivOne.Services;
using CivOne.src;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Integration tests exercising a full Game instance with more than the original 8-player limit,
	/// to confirm that raising the limit did not break new-game creation or respawn.
	///
	/// Competition is kept at 12 (not higher) because the current start-position placement service
	/// (AreaBasedStartPositionService, a separate, in-progress piece of work) can fail to find a valid
	/// tile for some players once player count exceeds roughly the number of high-land-value tiles on
	/// the bundled Earth map — a pre-existing limitation unrelated to the player-count/civilization-reuse
	/// changes covered by this file. See CivilizationAssignmentTests for reuse/determinism coverage that
	/// does not depend on real map placement.
	/// </summary>
	public class PlayerLimitIntegrationTests : IDisposable
	{
		private const int Competition = 12;

		private readonly RuntimeSettings _runtimeSettings;
		private readonly MockRuntime _runtime;

		public PlayerLimitIntegrationTests()
		{
			TranslationServiceFactory.ResetForTests();

			_runtimeSettings = new RuntimeSettings { InitialSeed = 24601 };
			_runtime = new MockRuntime(_runtimeSettings);

			Map.Reset(new MapGenerationFromYaml());
			Map.Instance.LoadEarthMapInThread();

			Game.CreateGame(3, Competition, Common.Civilizations.First(c => c.Name == "Chinese"));
		}

		public void Dispose()
		{
			Map.Reset();
			Game.Wipe();
			_runtime.Dispose();
			RuntimeHandler.Wipe();
			GC.Collect();
			GC.SuppressFinalize(this);
		}

		[Fact]
		public void CreatesAllPlayersWithDistinctStartsAndAtLeastOneUnitEach()
		{
			Assert.Equal(Competition + 1, Game.Instance.Players.Count());
			Assert.NotNull(Game.Instance.HumanPlayer);

			var unitsByOwner = Game.Instance.GetUnits().ToLookup(u => u.Owner);
			for (byte i = 1; i <= Competition; i++)
			{
				Assert.True(unitsByOwner[i].Any(), $"Player {i} has no starting units.");
			}

			var startPositions = Enumerable.Range(1, Competition)
				.Select(i => Game.Instance.GetUnits().First(u => u.Owner == i))
				.Select(u => (u.X, u.Y))
				.ToList();
			Assert.Equal(startPositions.Count, startPositions.Distinct().Count());
		}

		[Fact]
		public void DisbandingTheLastUnitOfAPlayerBeyondPlayerSevenRespawnsAtTheSameSlot()
		{
			// Player indices 8+ are only reachable once the 8-player cap is lifted; this is the scenario
			// where the old code (Game.cs: _players[newPlayer.Civilization.PreferredPlayerNumber] = newPlayer)
			// would have written the replacement into the wrong slot (1-7) instead of here.
			byte targetIndex = (byte)Enumerable.Range(8, Competition - 7)
				.First(i => !Game.Instance.GetPlayer((byte)i)!.IsHuman);

			Player originalPlayer = Game.Instance.GetPlayer(targetIndex)!;

			// HandleExtinction (and thus the Destroyed event and respawn) is triggered automatically as
			// soon as a player's last unit is disbanded, so no explicit destroy call is needed here.
			foreach (var unit in Game.Instance.GetUnits().Where(u => u.Owner == targetIndex).ToArray())
			{
				Game.Instance.DisbandUnit(unit);
			}

			Player replacement = Game.Instance.GetPlayer(targetIndex)!;
			Assert.NotSame(originalPlayer, replacement);
			Assert.False(replacement.IsHuman);
			Assert.True(Game.Instance.GetUnits().Any(u => u.Owner == targetIndex));
		}
	}
}
