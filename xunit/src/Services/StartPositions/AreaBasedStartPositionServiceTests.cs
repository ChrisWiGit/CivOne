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
using CivOne.Persistence.Model;
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

		private static MockedMapEditor BuildOceanMap(int width, int height)
		{
			var map = new MockedMapEditor { Width = width, Height = height };
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					map.SetTile(x, y, new Ocean(x, y, false));
				}
			}
			return map;
		}

		private static StartPositionCandidate[] Candidates(int count)
			=> [.. Enumerable.Range(1, count).Select(i => new StartPositionCandidate { Civilization = new MockedICivilization(1, (byte)i) })];

		private static bool Contains(AreaBasedStartPositionService.MapArea area, MapLocation location)
			=> location.X >= (uint)area.X0 && location.X < (uint)area.X1
				&& location.Y >= (uint)area.Y0 && location.Y < (uint)area.Y1;

		[Fact]
		public void PlacesEveryCandidateOnADistinctTile()
		{
			MockedMapEditor map = BuildAllLandMap(20, 10);
			StartPositionCandidate[] candidates = Candidates(3);
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = false
			};

			IReadOnlyList<StartPositionResult> results = new AreaBasedStartPositionService().FindStartPositions(candidates, context);

			Assert.All(results, r => Assert.True(r.Success));
			int distinctPositions = results.Select(r => (r.Position.X, r.Position.Y)).Distinct().Count();
			Assert.Equal(candidates.Length, distinctPositions);
		}

		[Fact]
		public void PlacesEveryCandidateInsideItsOwnArea()
		{
			// The point of the algorithm: no two civs share an area, so every civ gets its own part of the map.
			const int width = 60, height = 40;
			MockedMapEditor map = BuildAllLandMap(width, height);
			StartPositionCandidate[] candidates = Candidates(4);
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new SeededRandomService(1234),
				IsFirstGameTurn = false
			};

			IReadOnlyList<StartPositionResult> results = new AreaBasedStartPositionService().FindStartPositions(candidates, context);

			AreaBasedStartPositionService.MapArea[] areas = AreaBasedStartPositionService.BuildAreas(
				candidates.Length * AreaBasedStartPositionService.AreaOversampleFactor, width, height);

			Assert.All(results, r => Assert.True(r.Success));
			int[] usedAreas = [.. results.Select(r => areas.ToList().FindIndex(a => Contains(a, r.Position)))];
			Assert.DoesNotContain(-1, usedAreas);
			Assert.Equal(usedAreas.Length, usedAreas.Distinct().Count());
		}

		[Fact]
		public void KeepsCandidatesApartAcrossAreaBorders()
		{
			// Areas alone don't prevent two civs from starting right next to each other at a shared border.
			const int width = 60, height = 40;
			MockedMapEditor map = BuildAllLandMap(width, height);
			StartPositionCandidate[] candidates = Candidates(4);
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new SeededRandomService(4321),
				IsFirstGameTurn = false
			};

			IReadOnlyList<StartPositionResult> results = new AreaBasedStartPositionService().FindStartPositions(candidates, context);

			Assert.All(results, r => Assert.True(r.Success));
			foreach (StartPositionResult first in results)
			{
				foreach (StartPositionResult second in results.Where(r => r != first))
				{
					int distance = Common.DistanceToTile((int)first.Position.X, (int)first.Position.Y, (int)second.Position.X, (int)second.Position.Y);
					Assert.True(distance >= 10, $"Start positions are only {distance} tiles apart.");
				}
			}
		}

		[Fact]
		public void KeepsDistanceToSettlersThatAreAlreadyOnTheMap()
		{
			const int width = 60, height = 40;
			MockedMapEditor map = BuildAllLandMap(width, height);
			var existingSettler = new MapLocation(30, 20);
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new SeededRandomService(99),
				IsFirstGameTurn = false,
				SettlerLocations = [existingSettler],
				OccupiedTiles = [existingSettler]
			};

			StartPositionResult result = new AreaBasedStartPositionService().FindStartPositions(Candidates(1), context)[0];

			Assert.True(result.Success);
			int distance = Common.DistanceToTile((int)result.Position.X, (int)result.Position.Y, (int)existingSettler.X, (int)existingSettler.Y);
			Assert.True(distance >= 10, $"Start position is only {distance} tiles away from an existing settler.");
		}

		[Fact]
		public void FallsBackToAnotherAreaWhenTheAssignedAreaIsEntirelyOcean()
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

			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = false
			};

			IReadOnlyList<StartPositionResult> results = new AreaBasedStartPositionService().FindStartPositions(Candidates(2), context);

			Assert.All(results, r => Assert.True(r.Success));
			Assert.All(results, r => Assert.IsType<Grassland>(map[(int)r.Position.X, (int)r.Position.Y]));
		}

		[Fact]
		public void PlacesOnPoorLandRatherThanFailing()
		{
			// A single land tile with a land value far below the regular threshold: the last-resort scan must still find it.
			var map = new MockedMapEditor { Width = 8, Height = 8 };
			for (int y = 0; y < 8; y++)
			{
				for (int x = 0; x < 8; x++)
				{
					map.SetTile(x, y, new Ocean(x, y, false));
				}
			}
			map.SetTile(5, 5, new Grassland(5, 5) { ContinentId = 1, LandValue = 0 });

			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = false
			};

			StartPositionResult result = new AreaBasedStartPositionService().FindStartPositions(Candidates(1), context)[0];

			Assert.True(result.Success);
			Assert.Equal(5u, result.Position.X);
			Assert.Equal(5u, result.Position.Y);
		}

		[Fact]
		public void ReportsFailureWhenTheMapHasNoLandAtAll()
		{
			MockedMapEditor map = BuildOceanMap(10, 10);
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = false
			};

			StartPositionResult result = new AreaBasedStartPositionService().FindStartPositions(Candidates(1), context)[0];

			Assert.False(result.Success);
		}

		[Fact]
		public void UsesTheCandidatesFixedMapStartPositionOnTheFirstTurn()
		{
			MockedMapEditor map = BuildAllLandMap(20, 10);
			var candidate = new StartPositionCandidate
			{
				Civilization = new MockedICivilization(1),
				MapStartPosition = new MapLocation(7, 4)
			};
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = true,
				AnyFixedMapStartPosition = true
			};

			StartPositionResult result = new AreaBasedStartPositionService().FindStartPositions([candidate], context)[0];

			Assert.True(result.Success);
			Assert.Equal(7u, result.Position.X);
			Assert.Equal(4u, result.Position.Y);
		}

		[Fact]
		public void UsesTheCivilizationDefaultPositionWhenTheMapHasFixedStartPositions()
		{
			MockedMapEditor map = BuildAllLandMap(20, 10);
			map.FixedStartPositions = true;
			var civilization = new MockedICivilization(1);
			var context = new StartPositionContext
			{
				Map = map,
				RandomService = new StubRandomService(),
				IsFirstGameTurn = true,
				AnyFixedMapStartPosition = false
			};

			StartPositionResult result = new AreaBasedStartPositionService()
				.FindStartPositions([new StartPositionCandidate { Civilization = civilization }], context)[0];

			Assert.True(result.Success);
			Assert.Equal((uint)civilization.StartX, result.Position.X);
			Assert.Equal((uint)civilization.StartY, result.Position.Y);
		}

		[Theory]
		[InlineData(2)]
		[InlineData(8)]
		[InlineData(10)]
		[InlineData(14)]
		public void BuildAreasCoversEveryTileExactlyOnce(int count)
		{
			const int width = 80, height = 50;
			const int poleMargin = 2;

			AreaBasedStartPositionService.MapArea[] areas = AreaBasedStartPositionService.BuildAreas(count, width, height);

			Assert.True(areas.Length >= count);
			var coverage = new int[width, height];
			foreach (AreaBasedStartPositionService.MapArea area in areas)
			{
				for (int y = area.Y0; y < area.Y1; y++)
				{
					for (int x = area.X0; x < area.X1; x++)
					{
						coverage[x, y]++;
					}
				}
			}

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					int expected = (y < poleMargin || y >= height - poleMargin) ? 0 : 1;
					Assert.Equal(expected, coverage[x, y]);
				}
			}
		}

		[Fact]
		public void BuildAreasKeepsThePoleMargin()
		{
			AreaBasedStartPositionService.MapArea[] areas = AreaBasedStartPositionService.BuildAreas(8, 80, 50);

			Assert.Equal(2, areas.Min(a => a.Y0));
			Assert.Equal(48, areas.Max(a => a.Y1));
			Assert.Equal(0, areas.Min(a => a.X0));
			Assert.Equal(80, areas.Max(a => a.X1));
		}
	}
}
