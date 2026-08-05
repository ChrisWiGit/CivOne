using System;
using System.Linq;
using CivOne.Persistence.Factories;
using CivOne.Persistence.Model;
using CivOne.Services;
using CivOne.src;
using CivOne.Units;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Integration tests exercising a full Game instance with more than the original 8-player limit,
	/// to confirm that raising the limit did not break new-game creation or respawn.
	///
	/// Competition is 20, above the 16 entries the per-player structures used to be sized for, so player
	/// slots in the 16-31 range are actually exercised. The New Game menu offers 16, 20, 24 and 31
	/// civilizations, so this is a reachable configuration.
	/// See CivilizationAssignmentTests for reuse/determinism coverage that does not depend on real map
	/// placement.
	/// </summary>
	public class PlayerLimitIntegrationTests : IDisposable
	{
		private const int Competition = 20;

		/// <summary>
		/// A player slot at or above 16, where the fixed 16-entry <see cref="City.VisibleSizes"/> array used
		/// to throw.
		/// </summary>
		private const byte HighPlayerIndex = 17;

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

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				Map.Reset();
				Game.Wipe();
				_runtime.Dispose();
				RuntimeHandler.Wipe();
				GC.Collect();
			}
		}

		public void Dispose()
		{
			Dispose(true);
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
			Assert.Contains(Game.Instance.GetUnits(), u => u.Owner == targetIndex);
		}

		[Fact]
		public void CityOwnedByAPlayerBeyondIndexFifteenExposesItsVisibleSizes()
		{
			Player owner = Game.Instance.GetPlayer(HighPlayerIndex)!;
			IUnit startUnit = Game.Instance.GetUnits().First(u => u.Owner == HighPlayerIndex);

			City? city = Game.Instance.AddCity(owner, 1, startUnit.X, startUnit.Y);
			Assert.NotNull(city);

			Assert.Equal(Game.MaxPlayers, city.VisibleSizes.Length);
			Assert.Equal(city.Size, city.VisibleSizes[HighPlayerIndex]);
		}

		[Fact]
		public void SeeingAnEnemyCityStoresTheSizeForAHighPlayerIndex()
		{
			Player owner = Game.Instance.GetPlayer(1)!;
			IUnit startUnit = Game.Instance.GetUnits().First(u => u.Owner == 1);

			City? city = Game.Instance.AddCity(owner, 1, startUnit.X, startUnit.Y);
			Assert.NotNull(city);

			// What Player.UpdateVisibleCitySizes does once the city becomes visible to that player.
			city.VisibleSizes[HighPlayerIndex] = city.Size;

			Assert.Equal(city.Size, city.VisibleSizes[HighPlayerIndex]);
		}
	}
}
