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
				RandomService = new StubRandomService(), // always returns the minimum of a range: (0, 2)
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
				RandomService = new StubRandomService(), // would pick (0, 2) for every candidate without batch tracking
				IsFirstGameTurn = false
			};

			IReadOnlyList<StartPositionResult> results = new LegacyStartPositionService().FindStartPositions(candidates, context);

			Assert.True(results[0].Success);
			Assert.True(results[1].Success);
			Assert.NotEqual((results[0].Position.X, results[0].Position.Y), (results[1].Position.X, results[1].Position.Y));
		}

		[Fact]
		public void ChieftainDifficultyCanRollASecondSettlerAtTheSamePosition()
		{
			MockedMapEditor map = BuildAllLandMap(10, 10, continentId: 1, landValue: 15);
			var candidate = new StartPositionCandidate { Civilization = new MockedICivilization(1) };
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService { HitResult = true },
				IsFirstGameTurn = false,
				Difficulty = (int)DifficultyLevel.Chieftain
			};

			StartPositionResult result = new LegacyStartPositionService().FindStartPositions([candidate], context)[0];

			Assert.True(result.Success);
			Assert.True(result.PlaceSecondSettlerAtSamePosition);
		}

		[Fact]
		public void NonChieftainDifficultyNeverRollsASecondSettler()
		{
			MockedMapEditor map = BuildAllLandMap(10, 10, continentId: 1, landValue: 15);
			var candidate = new StartPositionCandidate { Civilization = new MockedICivilization(1) };
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService { HitResult = true },
				IsFirstGameTurn = false,
				Difficulty = (int)DifficultyLevel.Prince
			};

			StartPositionResult result = new LegacyStartPositionService().FindStartPositions([candidate], context)[0];

			Assert.True(result.Success);
			Assert.False(result.PlaceSecondSettlerAtSamePosition);
		}
	}
}
