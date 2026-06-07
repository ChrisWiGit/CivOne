namespace CivOne.Persistence.Model
{
	using System;
	using System.Collections.Generic;
	using Xunit;
	using CivOne.UnitTests;
	using CivOne.Persistence.Game;
	using CivOne.Persistence.Resolver;

	/// <summary>
	/// Tests for UnitsDestroyedByResolver class.
	/// Verifies correct resolution and application of destruction counts from GUID mappings.
	/// </summary>
	public class UnitsDestroyedByResolverTest
	{
		private readonly UnitsDestroyedByResolver _testee;
		private readonly ValueSanitizer _sanitizer;

		public UnitsDestroyedByResolverTest()
		{
			_sanitizer = new ValueSanitizer(new NoOpLogger());
			_testee = new UnitsDestroyedByResolver(_sanitizer);
		}

		/// <summary>
		/// Tests that null players array is handled gracefully without throwing.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverNullPlayersDoesNotThrow()
		{
			var playerDtos = new List<PlayerDto>
			{
				new() { UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long> { [Guid.NewGuid()] = 10 } }
			};

			// Should not throw
			_testee.ResolveAndApply(null, playerDtos);
		}

		/// <summary>
		/// Tests that empty players array is handled gracefully.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverEmptyPlayersDoesNotThrow()
		{
			var playerDtos = new List<PlayerDto>
			{
				new() { UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long> { [Guid.NewGuid()] = 10 } }
			};

			var players = Array.Empty<IPlayer>();
			// Should not throw
			_testee.ResolveAndApply(players, playerDtos);
		}

		/// <summary>
		/// Tests that null player DTOs list is handled gracefully.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverNullPlayerDtosDoesNotThrow()
		{
			var players = new[] { CreateMockPlayer(Guid.NewGuid()) };

			// Should not throw
			_testee.ResolveAndApply(players, null);
		}

		/// <summary>
		/// Tests that empty player DTOs list is handled gracefully.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverEmptyPlayerDtosDoesNotThrow()
		{
			var players = new[] { CreateMockPlayer(Guid.NewGuid()) };

			var playerDtos = new List<PlayerDto>();
			// Should not throw
			_testee.ResolveAndApply(players, playerDtos);
		}

		/// <summary>
		/// Tests basic resolution of a single destruction count mapping.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverSingleMappingResolvesCorrectly()
		{
			var player0Guid = Guid.NewGuid();
			var player1Guid = Guid.NewGuid();
			var player2Guid = Guid.NewGuid();

			var players = new IPlayer[]
			{
				CreateMockPlayer(player0Guid),
				CreateMockPlayer(player1Guid),
				CreateMockPlayer(player2Guid)
			};

			var playerDtos = new List<PlayerDto>
			{
				new()
				{
					UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long> { [player2Guid] = 42 }
				}
			};

			_testee.ResolveAndApply(players, playerDtos);

			var restorable = (IPlayerRestorable)players[0];
			Assert.Equal(42, restorable.UnitsDestroyedBy[2]);
		}

		/// <summary>
		/// Tests resolution of multiple destruction count mappings for a single player.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverMultipleMappingsResolvesAll()
		{
			var player0Guid = Guid.NewGuid();
			var player1Guid = Guid.NewGuid();
			var player2Guid = Guid.NewGuid();

			var players = new IPlayer[]
			{
				CreateMockPlayer(player0Guid),
				CreateMockPlayer(player1Guid),
				CreateMockPlayer(player2Guid)
			};

			var playerDtos = new List<PlayerDto>
			{
				new() {
					UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long>
					{
						[player1Guid] = 10,
						[player2Guid] = 20
					}
				}
			};

			_testee.ResolveAndApply(players, playerDtos);

			var restorable = (IPlayerRestorable)players[0];
			Assert.Equal(10, restorable.UnitsDestroyedBy[1]);
			Assert.Equal(20, restorable.UnitsDestroyedBy[2]);
		}

		/// <summary>
		/// Tests that multiple players' destruction counts are resolved independently.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverMultiplePlayersResolvesIndependently()
		{
			var player0Guid = Guid.NewGuid();
			var player1Guid = Guid.NewGuid();
			var player2Guid = Guid.NewGuid();

			var players = new IPlayer[]
			{
				CreateMockPlayer(player0Guid),
				CreateMockPlayer(player1Guid),
				CreateMockPlayer(player2Guid)
			};

			var playerDtos = new List<PlayerDto>
			{
				new() {
					UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long> { [player1Guid] = 100 }
				},
				new() {
					UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long>
					{
						[player0Guid] = 200,
						[player2Guid] = 300
					}
				}
			};

			_testee.ResolveAndApply(players, playerDtos);

			var restorable0 = (IPlayerRestorable)players[0];
			var restorable1 = (IPlayerRestorable)players[1];

			// Player 0 is destroyed by player 1
			Assert.Equal(100, restorable0.UnitsDestroyedBy[1]);
			// Player 1 is destroyed by player 0
			Assert.Equal(200, restorable1.UnitsDestroyedBy[0]);
			// Player 1 is destroyed by player 2
			Assert.Equal(300, restorable1.UnitsDestroyedBy[2]);
		}

		/// <summary>
		/// Tests that empty GUID in mapping is skipped safely.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverEmptyGuidIsSkipped()
		{
			var player0Guid = Guid.NewGuid();
			var player1Guid = Guid.NewGuid();

			var players = new IPlayer[]
			{
				CreateMockPlayer(player0Guid),
				CreateMockPlayer(player1Guid)
			};

			var playerDtos = new List<PlayerDto>
			{
				new() {
					UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long>
					{
						[Guid.Empty] = 99,
						[player1Guid] = 10
					}
				}
			};

			_testee.ResolveAndApply(players, playerDtos);

			var restorable = (IPlayerRestorable)players[0];
			// Empty GUID should be skipped, so index 0 should not have value 99
			Assert.NotEqual(99, restorable.UnitsDestroyedBy[0]);
			// But player1 mapping should work
			Assert.Equal(10, restorable.UnitsDestroyedBy[1]);
		}

		/// <summary>
		/// Tests that GUIDs not found in players array are skipped.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverUnknownGuidIsSkipped()
		{
			var player0Guid = Guid.NewGuid();
			var unknownGuid = Guid.NewGuid();

			var players = new IPlayer[]
			{
				CreateMockPlayer(player0Guid)
			};

			var playerDtos = new List<PlayerDto>
			{
				new() {
					UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long>
					{
						[unknownGuid] = 99
					}
				}
			};

			// Should not throw
			_testee.ResolveAndApply(players, playerDtos);

			var restorable = (IPlayerRestorable)players[0];
			// Unknown GUID should be skipped
			Assert.Equal(0, restorable.UnitsDestroyedBy[0]);
		}

		/// <summary>
		/// Tests that values exceeding ushort.MaxValue are clamped correctly.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverValueExceedsMaxIsClamped()
		{
			var player0Guid = Guid.NewGuid();
			var player1Guid = Guid.NewGuid();

			var players = new IPlayer[]
			{
				CreateMockPlayer(player0Guid),
				CreateMockPlayer(player1Guid)
			};

			var playerDtos = new List<PlayerDto>
			{
				new() {
					UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long>
					{
						[player1Guid] = long.MaxValue
					}
				}
			};

			_testee.ResolveAndApply(players, playerDtos);

			var restorable = (IPlayerRestorable)players[0];
			Assert.Equal(ushort.MaxValue, restorable.UnitsDestroyedBy[1]);
		}

		/// <summary>
		/// Tests that negative values are clamped to zero.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverNegativeValueIsClampedToZero()
		{
			var player0Guid = Guid.NewGuid();
			var player1Guid = Guid.NewGuid();

			var players = new IPlayer[]
			{
				CreateMockPlayer(player0Guid),
				CreateMockPlayer(player1Guid)
			};

			var playerDtos = new List<PlayerDto>
			{
				new() {
					UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long>
					{
						[player1Guid] = -100
					}
				}
			};

			_testee.ResolveAndApply(players, playerDtos);

			var restorable = (IPlayerRestorable)players[0];
			Assert.Equal(0, restorable.UnitsDestroyedBy[1]);
		}

		/// <summary>
		/// Tests that existing destruction counts are preserved when not overwritten.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverPreservesExistingCounts()
		{
			var player0Guid = Guid.NewGuid();
			var player1Guid = Guid.NewGuid();
			var player2Guid = Guid.NewGuid();

			var players = new IPlayer[]
			{
				CreateMockPlayer(player0Guid),
				CreateMockPlayer(player1Guid),
				CreateMockPlayer(player2Guid)
			};

			// Initialize with existing destruction counts
			var restorable0 = (IPlayerRestorable)players[0];
			restorable0.UnitsDestroyedBy = [100, 200, 300, 0, 0, 0, 0, 0];

			var playerDtos = new List<PlayerDto>
			{
				new() {
					UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long>
					{
						[player1Guid] = 500  // Override only player 1
					}
				}
			};

			_testee.ResolveAndApply(players, playerDtos);

			// Verify player0's count (index 0) is preserved
			Assert.Equal(100, restorable0.UnitsDestroyedBy[0]);
			// Verify player1's count (index 1) is overridden
			Assert.Equal(500, restorable0.UnitsDestroyedBy[1]);
			// Verify player2's count (index 2) is preserved
			Assert.Equal(300, restorable0.UnitsDestroyedBy[2]);
		}

		/// <summary>
		/// Tests that non-restorable players are skipped without error.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverNonRestorablePlayerIsSkipped()
		{
			var player0Guid = Guid.NewGuid();

			// Create a non-restorable mock player
			var nonRestorablePlayer = new MockedIPlayer { PlayerGuid = player0Guid };
			var players = new IPlayer[] { nonRestorablePlayer };

			var playerDtos = new List<PlayerDto>
			{
				new() {
					UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long>
					{
						[player0Guid] = 100
					}
				}
			};

			// Should not throw
			_testee.ResolveAndApply(players, playerDtos);
		}

		/// <summary>
		/// Tests that null UnitsDestroyedByByPlayerGuid in DTO is handled.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverNullGuidMappingIsSkipped()
		{
			var player0Guid = Guid.NewGuid();

			var players = new IPlayer[]
			{
				CreateMockPlayer(player0Guid)
			};

			var playerDtos = new List<PlayerDto>
			{
				new() {
					UnitsDestroyedByByPlayerGuid = null  // Null mapping
				}
			};

			// Should not throw
			_testee.ResolveAndApply(players, playerDtos);
		}

		/// <summary>
		/// Tests that empty UnitsDestroyedByByPlayerGuid in DTO is handled.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverEmptyGuidMappingIsSkipped()
		{
			var player0Guid = Guid.NewGuid();

			var players = new IPlayer[]
			{
				CreateMockPlayer(player0Guid)
			};

			var playerDtos = new List<PlayerDto>
			{
				new() {
					UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long>()  // Empty dictionary
				}
			};

			// Should not throw
			_testee.ResolveAndApply(players, playerDtos);
		}

		/// <summary>
		/// Tests that resolved array size is at least 8 (standard player slots).
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverMinimumArraySizeIsEight()
		{
			var player0Guid = Guid.NewGuid();

			// Create a scenario with fewer than 8 players
			var players = new IPlayer[]
			{
				CreateMockPlayer(player0Guid)
			};

			var playerDtos = new List<PlayerDto>
			{
				new() {
					UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long>()
				}
			};

			_testee.ResolveAndApply(players, playerDtos);

			var restorable = (IPlayerRestorable)players[0];
			// Array should have at least 8 elements
			Assert.True(restorable.UnitsDestroyedBy.Length >= 8);
		}

		/// <summary>
		/// WARNING: GENERATED BY AI! (Claude Haiku 4.5 - 2026-04-12)
		/// Tests that resolved array size equals player count when exceeding 8.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverExceedingMinimumUsesPlayerCount()
		{
			var playerGuids = new Guid[16];
			for (var i = 0; i < 16; i++)
			{
				playerGuids[i] = Guid.NewGuid();
			}

			var players = new IPlayer[16];
			for (var i = 0; i < 16; i++)
			{
				// Create mock with empty array to test resizing
				players[i] = new MockedIPlayer 
				{ 
					PlayerGuid = playerGuids[i],
					UnitsDestroyedBy = []  // Empty array
				};
			}

			// Must have at least one mapping for IsResolvablePlayer to return true
			var playerDtos = new List<PlayerDto>
			{
				new PlayerDto
				{
					UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long>
					{
						[playerGuids[1]] = 100  // Add one mapping to make it resolvable
					}
				}
			};

			_testee.ResolveAndApply(players, playerDtos);

			var restorable = (IPlayerRestorable)players[0];
			// Array should use Math.Max(8, playerCount) = 16
			Assert.Equal(16, restorable.UnitsDestroyedBy.Length);
		}

		/// <summary>
		/// Tests mixed scenarios with valid and invalid mappings.
		/// </summary>
		[Fact]
		public void TestUnitsDestroyedByResolverMixedScenarioHandlesCorrectly()
		{
			var player0Guid = Guid.NewGuid();
			var player1Guid = Guid.NewGuid();
			var player2Guid = Guid.NewGuid();
			var unknownGuid = Guid.NewGuid();

			var players = new IPlayer[]
			{
				CreateMockPlayer(player0Guid),
				CreateMockPlayer(player1Guid),
				CreateMockPlayer(player2Guid)
			};

			var restorable0 = (IPlayerRestorable)players[0];
			restorable0.UnitsDestroyedBy = [50, 75, 0, 0, 0, 0, 0, 0];

			var playerDtos = new List<PlayerDto>
			{
				new() {
					UnitsDestroyedByByPlayerGuid = new Dictionary<Guid, long>
					{
						[Guid.Empty] = 999,        // Should be skipped
						[unknownGuid] = 888,       // Should be skipped
						[player1Guid] = 100,       // Should override 75 to 100
						[player2Guid] = 150        // Should set to 150
					}
				}
			};

			_testee.ResolveAndApply(players, playerDtos);

			// player0 count preserved
			Assert.Equal(50, restorable0.UnitsDestroyedBy[0]);
			// player1 count overridden
			Assert.Equal(100, restorable0.UnitsDestroyedBy[1]);
			// player2 count set
			Assert.Equal(150, restorable0.UnitsDestroyedBy[2]);
		}

		/// <summary>
		/// Creates a mock player for testing purposes.
		/// </summary>
		private static IPlayer CreateMockPlayer(Guid playerGuid)
		{
			return new MockedIPlayer
			{
				PlayerGuid = playerGuid,
				UnitsDestroyedBy = [0, 0, 0, 0, 0, 0, 0, 0]
			};
		}
	}
}
