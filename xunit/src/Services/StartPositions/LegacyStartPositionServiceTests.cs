// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using CivOne.Persistence.Model;
using CivOne.Tiles;
using CivOne.UnitTests;
using Xunit;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Tests for <see cref="LegacyStartPositionService"/>, isolated from the real <c>Map</c> singleton via
	/// <see cref="UnitTests.MockedMapEditor"/>, so map size and land layout can be controlled per test.
	/// </summary>
	public class LegacyStartPositionServiceTests
	{
		private static MockedMapEditor BuildAllLandMap(int width, int height, int continentId, byte landValue)
		{
			var map = new MockedMapEditor { Width = width, Height = height };
			for (int y = 2; y < height - 2; y++)
			{
				for (int x = 0; x < width; x++)
				{
					map.SetTile(x, y, new Grassland(x, y) { ContinentId = continentId, LandValue = landValue });
				}
			}
			return map;
		}

		[Fact]
		public void FixedMapStartPositionIsUsedDirectlyOnFirstTurn()
		{
			MockedMapEditor map = BuildAllLandMap(10, 10, continentId: 1, landValue: 15);
			var candidate = new StartPositionCandidate
			{
				Civilization = new MockedICivilization(1),
				MapStartPosition = new MapLocation(3, 2)
			};
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = true
			};

			IReadOnlyList<StartPositionResult> results = new LegacyStartPositionService().FindStartPositions([candidate], context);

			Assert.True(results[0].Success);
			Assert.Equal(3u, results[0].Position.X);
			Assert.Equal(2u, results[0].Position.Y);
		}

		[Fact]
		public void RandomSearchSucceedsOnALargeEnoughContinent()
		{
			// The continent needs at least 32 buildable tiles for the strict search to accept a tile at turn 0.
			MockedMapEditor map = BuildAllLandMap(10, 10, continentId: 1, landValue: 15);
			var candidate = new StartPositionCandidate { Civilization = new MockedICivilization(1) };
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = false
			};

			StartPositionResult result = new LegacyStartPositionService().FindStartPositions([candidate], context)[0];

			Assert.True(result.Success);
			Assert.Equal(0u, result.Position.X);
			Assert.Equal(2u, result.Position.Y);
		}

		[Fact]
		public void FallsBackToExhaustiveScanWhenContinentIsTooSmallForStrictSearch()
		{
			// Only 8 buildable tiles on the continent: the strict search's "32 buildable tiles" constraint can never pass.
			MockedMapEditor map = BuildAllLandMap(4, 6, continentId: 1, landValue: 15);
			var candidate = new StartPositionCandidate { Civilization = new MockedICivilization(1) };
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = false
			};

			StartPositionResult result = new LegacyStartPositionService().FindStartPositions([candidate], context)[0];

			Assert.True(result.Success);
			Assert.Equal(0u, result.Position.X);
			Assert.Equal(2u, result.Position.Y);
		}

		[Fact]
		public void SecondCandidateAvoidsTileClaimedByFirstCandidateInTheSameBatch()
		{
			MockedMapEditor map = BuildAllLandMap(10, 10, continentId: 1, landValue: 15);
			StartPositionCandidate[] candidates =
			[
				new() { Civilization = new MockedICivilization(1) },
				new() { Civilization = new MockedICivilization(2) }
			];
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = false
			};

			IReadOnlyList<StartPositionResult> results = new LegacyStartPositionService().FindStartPositions(candidates, context);

			Assert.True(results[0].Success);
			Assert.True(results[1].Success);
			Assert.NotEqual((results[0].Position.X, results[0].Position.Y), (results[1].Position.X, results[1].Position.Y));
		}

		[Fact]
		public void UsesTheCivilizationDefaultPositionWhenTheMapHasFixedStartPositions()
		{
			MockedMapEditor map = BuildAllLandMap(20, 10, continentId: 1, landValue: 15);
			map.FixedStartPositions = true;
			var civilization = new MockedICivilization(1);
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = true,
				AnyFixedMapStartPosition = false
			};

			StartPositionResult result = new LegacyStartPositionService()
				.FindStartPositions([new StartPositionCandidate { Civilization = civilization }], context)[0];

			Assert.True(result.Success);
			Assert.Equal((uint)civilization.StartX, result.Position.X);
			Assert.Equal((uint)civilization.StartY, result.Position.Y);
		}

		[Fact]
		public void IgnoresTheCivilizationDefaultPositionWhenAnotherPlayerHasACustomPosition()
		{
			MockedMapEditor map = BuildAllLandMap(20, 10, continentId: 1, landValue: 15);
			map.FixedStartPositions = true;
			var civilization = new MockedICivilization(1);
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = true,
				AnyFixedMapStartPosition = true
			};

			StartPositionResult result = new LegacyStartPositionService()
				.FindStartPositions([new StartPositionCandidate { Civilization = civilization }], context)[0];

			Assert.True(result.Success);
			Assert.Equal(0u, result.Position.X);
			Assert.Equal(2u, result.Position.Y);
		}

		[Fact]
		public void FallsBackToTheSearchWhenTheFixedPositionIsOcean()
		{
			MockedMapEditor map = BuildAllLandMap(20, 10, continentId: 1, landValue: 15);
			map.SetTile(6, 4, new Ocean(6, 4, false));
			var candidate = new StartPositionCandidate
			{
				Civilization = new MockedICivilization(1),
				MapStartPosition = new MapLocation(6, 4)
			};
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = true,
				AnyFixedMapStartPosition = true
			};

			StartPositionResult result = new LegacyStartPositionService().FindStartPositions([candidate], context)[0];

			Assert.True(result.Success);
			Assert.Equal(0u, result.Position.X);
			Assert.Equal(2u, result.Position.Y);
		}

		[Fact]
		public void FallsBackToTheSearchWhenTheFixedPositionIsOccupied()
		{
			MockedMapEditor map = BuildAllLandMap(20, 10, continentId: 1, landValue: 15);
			var candidate = new StartPositionCandidate
			{
				Civilization = new MockedICivilization(1),
				MapStartPosition = new MapLocation(6, 4)
			};
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = true,
				AnyFixedMapStartPosition = true,
				OccupiedTiles = [new MapLocation(6, 4)]
			};

			StartPositionResult result = new LegacyStartPositionService().FindStartPositions([candidate], context)[0];

			Assert.True(result.Success);
			Assert.NotEqual((6u, 4u), (result.Position.X, result.Position.Y));
		}

		[Fact]
		public void SkipsTilesWithAHut()
		{
			MockedMapEditor map = BuildAllLandMap(10, 10, continentId: 1, landValue: 15);
			map.SetTile(0, 2, new Grassland(0, 2) { ContinentId = 1, LandValue = 15, Hut = true });
			var candidate = new StartPositionCandidate { Civilization = new MockedICivilization(1) };
			var context = new StartPositionContext
			{
				Map = map,
				// The search proposes the hut tile (0,2) first, then (1,2).
				RandomService = new StubRandomService(0, 0, 1, 0),
				IsFirstGameTurn = false
			};

			StartPositionResult result = new LegacyStartPositionService().FindStartPositions([candidate], context)[0];

			Assert.True(result.Success);
			Assert.Equal(1u, result.Position.X);
			Assert.Equal(2u, result.Position.Y);
		}

		[Fact]
		public void KeepsDistanceToExistingCities()
		{
			MockedMapEditor map = BuildAllLandMap(20, 10, continentId: 1, landValue: 15);
			var candidate = new StartPositionCandidate { Civilization = new MockedICivilization(1) };
			var context = new StartPositionContext
			{
				Map = map,
				// The search proposes (5,2) next to the city first, then (12,2) far enough away.
				RandomService = new StubRandomService(5, 0, 12, 0),
				IsFirstGameTurn = false,
				CityLocations = [new MapLocation(0, 2)]
			};

			StartPositionResult result = new LegacyStartPositionService().FindStartPositions([candidate], context)[0];

			Assert.True(result.Success);
			Assert.Equal(12u, result.Position.X);
			Assert.Equal(2u, result.Position.Y);
		}

		[Fact]
		public void KeepsDistanceToExistingSettlers()
		{
			MockedMapEditor map = BuildAllLandMap(20, 10, continentId: 1, landValue: 15);
			var candidate = new StartPositionCandidate { Civilization = new MockedICivilization(1) };
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(5, 0, 12, 0),
				IsFirstGameTurn = false,
				SettlerLocations = [new MapLocation(0, 2)]
			};

			StartPositionResult result = new LegacyStartPositionService().FindStartPositions([candidate], context)[0];

			Assert.True(result.Success);
			Assert.Equal(12u, result.Position.X);
			Assert.Equal(2u, result.Position.Y);
		}

		[Fact]
		public void ReportsFailureWhenTheMapHasNoLandAtAll()
		{
			var map = new MockedMapEditor { Width = 10, Height = 10 };
			for (int y = 0; y < 10; y++)
			{
				for (int x = 0; x < 10; x++)
				{
					map.SetTile(x, y, new Ocean(x, y, false));
				}
			}

			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = false
			};

			StartPositionResult result = new LegacyStartPositionService()
				.FindStartPositions([new StartPositionCandidate { Civilization = new MockedICivilization(1) }], context)[0];

			Assert.False(result.Success);
		}

		[Fact]
		public void ClampsANegativeGameTurnInsteadOfWrappingAround()
		{
			MockedMapEditor map = BuildAllLandMap(10, 10, continentId: 1, landValue: 15);
			var logger = new MockedLogger();
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = false,
				GameTurn = -1,
				Logger = logger
			};

			StartPositionResult result = new LegacyStartPositionService()
				.FindStartPositions([new StartPositionCandidate { Civilization = new MockedICivilization(1) }], context)[0];

			Assert.True(result.Success);
			Assert.Contains(logger.Messages, m => m.Contains("clamped", System.StringComparison.OrdinalIgnoreCase));
		}
	}
}
