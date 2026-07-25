// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.UnitTests;
using Xunit;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Tests for <see cref="AreaBasedStartPositionService"/>, isolated from the real <c>Map</c> singleton via
	/// <see cref="UnitTests.MockedMapEditor"/>.
	/// </summary>
	public class AreaBasedStartPositionServiceTests
	{
		private static MockedMapEditor BuildAllLandMap(int width, int height, byte landValue = 15)
		{
			var map = new MockedMapEditor { Width = width, Height = height };
			for (int y = 2; y < height - 2; y++)
			{
				for (int x = 0; x < width; x++)
				{
					map.SetTile(x, y, new Grassland(x, y) { ContinentId = 1, LandValue = landValue });
				}
			}
			return map;
		}

		[Fact]
		public void PlacesEveryCandidateOnADistinctTile()
		{
			MockedMapEditor map = BuildAllLandMap(20, 10);
			List<MockedICivilization> civilizations = MockedICivilization.Mock(3);
			StartPositionCandidate[] candidates = [.. civilizations.Select(c => new StartPositionCandidate { Civilization = c })];
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = false
			};

			IReadOnlyList<StartPositionResult> results = new AreaBasedStartPositionService().FindStartPositions(candidates, context);

			Assert.All(results, r => Assert.True(r.Success));
			var distinctPositions = results.Select(r => (r.Position.X, r.Position.Y)).Distinct().Count();
			Assert.Equal(candidates.Length, distinctPositions);
		}

		[Fact]
		public void NeverPlacesTheChieftainSecondSettlerRule()
		{
			MockedMapEditor map = BuildAllLandMap(10, 10);
			var candidate = new StartPositionCandidate { Civilization = new MockedICivilization(1) };
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService { HitResult = true },
				IsFirstGameTurn = false,
				Difficulty = 0 // Chieftain
			};

			StartPositionResult result = new AreaBasedStartPositionService().FindStartPositions([candidate], context)[0];

			Assert.True(result.Success);
			Assert.False(result.PlaceSecondSettlerAtSamePosition);
		}

		[Fact]
		public void FindsAPositionForEveryRequestedAdditionalUnit()
		{
			MockedMapEditor map = BuildAllLandMap(20, 10);
			var candidate = new StartPositionCandidate
			{
				Civilization = new MockedICivilization(1),
				AdditionalUnitTypes = [UnitType.Militia, UnitType.Phalanx]
			};
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = false
			};

			StartPositionResult result = new AreaBasedStartPositionService().FindStartPositions([candidate], context)[0];

			Assert.True(result.Success);
			Assert.Equal(2, result.AdditionalUnitPositions.Count);
		}

		[Fact]
		public void FallsBackToTheWholeMapWhenACandidatesAreaIsEntirelyOcean()
		{
			// 2 candidates over a 4-wide map split the map into a land half and an all-ocean half.
			var map = new MockedMapEditor { Width = 4, Height = 6 };
			for (int y = 2; y < 4; y++)
			{
				map.SetTile(0, y, new Grassland(0, y) { ContinentId = 1, LandValue = 15 });
				map.SetTile(1, y, new Grassland(1, y) { ContinentId = 1, LandValue = 15 });
				map.SetTile(2, y, new Ocean(2, y, false));
				map.SetTile(3, y, new Ocean(3, y, false));
			}

			List<MockedICivilization> civilizations = MockedICivilization.Mock(2);
			StartPositionCandidate[] candidates = [.. civilizations.Select(c => new StartPositionCandidate { Civilization = c })];
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = false
			};

			IReadOnlyList<StartPositionResult> results = new AreaBasedStartPositionService().FindStartPositions(candidates, context);

			Assert.All(results, r => Assert.True(r.Success));
			Assert.All(results, r => Assert.IsType<Grassland>(map[(int)r.Position.X, (int)r.Position.Y]));
		}
	}
}
