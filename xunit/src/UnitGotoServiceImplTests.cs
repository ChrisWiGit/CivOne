using System.Drawing;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Services.Pathfinding;
using CivOne.Tiles;
using CivOne.Units;
using Xunit;

namespace CivOne.UnitTests
{
	public class UnitGotoServiceImplTests
	{
		public static TheoryData<bool> ImplementationModes => new()
		{
			false,
			true
		};

		[Theory]
		[MemberData(nameof(ImplementationModes))]
		public void GotoStep_WhenAlreadyAtGoal_ReturnsNull(bool useNewImpl)
		{
			// Arrange
			var (map, _) = MakeLandMap(10, 10);
			var unit = MakeUnit(5, 5, 5, 5);
			var testee = CreateTestee(map, useNewImpl);

			// Act
			ITile actual = testee.GotoStep(unit);

			// Assert
			Assert.Null(actual);
		}

		[Theory]
		[MemberData(nameof(ImplementationModes))]
		public void GotoStep_WhenGoalIsDirectNeighbour_ReturnsTileAtGoal(bool useNewImpl)
		{
			// Arrange
			var (map, _) = MakeLandMap(10, 10);
			var unit = MakeUnit(5, 5, 6, 5);
			var testee = CreateTestee(map, useNewImpl);

			// Act
			ITile actual = testee.GotoStep(unit);

			// Assert
			Assert.NotNull(actual);
			Assert.Equal(6, actual.X);
			Assert.Equal(5, actual.Y);
		}

		[Theory]
		[MemberData(nameof(ImplementationModes))]
		public void GotoStep_WhenMultipleStepsAway_ReturnsFirstStepNotGoal(bool useNewImpl)
		{
			// Arrange
			// 10-wide map, unit at (0,5) wants to reach (4,5).
			// First step must be X=1 (one step right); Y may vary since A* uses diagonal movement.
			var (map, _) = MakeLandMap(10, 10);
			var unit = MakeUnit(0, 5, 4, 5);
			var testee = CreateTestee(map, useNewImpl);

			// Act
			ITile actual = testee.GotoStep(unit);

			// Assert
			Assert.NotNull(actual);
			Assert.Equal(1, actual.X); // moved exactly one step toward goal
		}

		[Theory]
		[MemberData(nameof(ImplementationModes))]
		public void GotoStep_LandUnit_DoesNotEnterOceanTile(bool useNewImpl)
		{
			// Arrange
			// Horizontal ocean wall: all x at y=1..8 are ocean.
			// Unit at (2,0), goal at (2,9). Y-axis has no wrap, so the wall is impassable.
			var (map, tiles) = MakeLandMap(5, 10);
			for (int x = 0; x < 5; x++)
				for (int y = 1; y <= 8; y++)
				{
					tiles[x, y].IsOcean = true;
					tiles[x, y].Type = Terrain.Ocean;
				}
			var unit = MakeUnit(2, 0, 2, 9, UnitClass.Land);
			var testee = CreateTestee(map, useNewImpl);

			// Act
			ITile actual = testee.GotoStep(unit);

			// Assert
			Assert.Null(actual);
		}

		[Theory]
		[MemberData(nameof(ImplementationModes))]
		public void GotoStep_LandUnit_DoesNotEnterArcticTile(bool useNewImpl)
		{
			// Arrange
			// Horizontal arctic wall: all x at y=1..3 are arctic.
			// Unit at (2,0), goal at (2,4). Y-axis has no wrap, so the wall is impassable.
			var (map, tiles) = MakeLandMap(5, 5);
			for (int x = 0; x < 5; x++)
				for (int y = 1; y <= 3; y++)
					tiles[x, y].Type = Terrain.Arctic;
			var unit = MakeUnit(2, 0, 2, 4, UnitClass.Land);
			var testee = CreateTestee(map, useNewImpl);

			// Act
			ITile actual = testee.GotoStep(unit);

			// Assert
			Assert.Null(actual);
		}

		[Theory]
		[MemberData(nameof(ImplementationModes))]
		public void GotoStep_WaterUnit_DoesNotEnterLandTile(bool useNewImpl)
		{
			// Arrange
			// All-ocean 10×10 map. Tile (6,5) is land, blocking the direct path.
			// A* must route around via (6,4) or (6,6) — the land tile must not be the first step.
			var (map, tiles) = MakeLandMap(10, 10);
			for (int x = 0; x < 10; x++)
				for (int y = 0; y < 10; y++)
				{
					tiles[x, y].IsOcean = true;
					tiles[x, y].Type = Terrain.Ocean;
				}
			tiles[6, 5].IsOcean = false;
			tiles[6, 5].Type = Terrain.Grassland1;
			var unit = MakeUnit(5, 5, 7, 5, UnitClass.Water);
			var testee = CreateTestee(map, useNewImpl);

			// Act
			ITile actual = testee.GotoStep(unit);

			// Assert: path exists, but first step is NOT the land tile
			Assert.NotNull(actual);
			Assert.False(actual.X == 6 && actual.Y == 5, "water unit must not step onto land tile");
		}

		[Theory]
		[MemberData(nameof(ImplementationModes))]
		public void GotoStep_WaterUnit_CanLandOnGoalTileEvenIfNotOcean(bool useNewImpl)
		{
			// Arrange
			// All-ocean map. Goal tile at (4,5) is a land tile (isGoal bypass).
			var (map, tiles) = MakeLandMap(10, 10);
			for (int x = 0; x < 10; x++)
				for (int y = 0; y < 10; y++)
				{
					tiles[x, y].IsOcean = true;
					tiles[x, y].Type = Terrain.Ocean;
				}
			// Goal tile is land (non-ocean)
			tiles[4, 5].IsOcean = false;
			tiles[4, 5].Type = Terrain.Grassland1;

			var unit = MakeUnit(0, 5, 4, 5, UnitClass.Water);
			var testee = CreateTestee(map, useNewImpl);

			// Act
			ITile actual = testee.GotoStep(unit);

			// Assert – a path must be found (the goal bypass is in effect)
			Assert.NotNull(actual);
		}

		[Theory]
		[MemberData(nameof(ImplementationModes))]
		public void GotoStep_WhenFullyBlocked_ReturnsNull(bool useNewImpl)
		{
			// Arrange
			// 3×3 map. Unit at (0,0), goal at (2,2).
			// All tiles except start are ocean → land unit can't move.
			var (map, tiles) = MakeLandMap(3, 3);
			for (int x = 0; x < 3; x++)
				for (int y = 0; y < 3; y++)
					if (x != 0 || y != 0)
					{
						tiles[x, y].IsOcean = true;
						tiles[x, y].Type = Terrain.Ocean;
					}
			var unit = MakeUnit(0, 0, 2, 2, UnitClass.Land);
			var testee = CreateTestee(map, useNewImpl);

			// Act
			ITile actual = testee.GotoStep(unit);

			// Assert
			Assert.Null(actual);
		}

		[Theory]
		[MemberData(nameof(ImplementationModes))]
		public void GotoStep_PrefersRailroadOverRoadOverTerrain(bool useNewImpl)
		{
			// Arrange
			// 5-wide map (y=2 only usable row). Two paths from (0,2) to (4,2):
			//   Upper path y=1: plain terrain (cost = movement*9)
			//   Direct path y=2: railroad (cost = 1 per step)
			// The railroad path should be preferred.
			var (map, tiles) = MakeLandMap(5, 5);
			// Mark y=2 row as railroad
			for (int x = 0; x < 5; x++)
				tiles[x, 2].RailRoad = true;

			var unit = MakeUnit(0, 2, 4, 2, UnitClass.Land);
			var testee = CreateTestee(map, useNewImpl);

			// Act
			ITile actual = testee.GotoStep(unit);

			// Assert: first step stays on y=2 (railroad row), not y=1
			Assert.NotNull(actual);
			Assert.Equal(2, actual.Y);
		}

		[Theory]
		[MemberData(nameof(ImplementationModes))]
		public void GotoStep_HorizontalWrap_FindsPathAroundMapEdge(bool useNewImpl)
		{
			// Arrange
			// 10-wide, 5-tall map. Unit at x=1, goal at x=9.
			// Block all tiles with x=2..8 (land unit can't cross) → force wrap via x=0.
			// Note: wrapping goes 1→0→9.
			var (map, tiles) = MakeLandMap(10, 5);
			for (int x = 2; x <= 8; x++)
				for (int y = 0; y < 5; y++)
				{
					tiles[x, y].IsOcean = true;
					tiles[x, y].Type = Terrain.Ocean;
				}

			var unit = MakeUnit(1, 2, 9, 2, UnitClass.Land);
			var testee = CreateTestee(map, useNewImpl);

			// Act
			ITile actual = testee.GotoStep(unit);

			// Assert: first step must wrap left to x=0, not try to cross ocean
			Assert.NotNull(actual);
			Assert.Equal(0, actual.X);
		}


		[Fact]
		public void GotoStep_WithRiverFastMovement_PrefersRiverOverPlainTerrain()
		{
			// Arrange
			// 10-wide, 5-tall map. Unit at (2,2), goal at (7,2).
			// y=2 row: River tiles  -> cost 3/step with riverFastMovement=true
			// y=1 row: plain terrain -> cost 18/step always
			// River path (5 steps x 3 = 15) is clearly cheaper than plain detour.
			var (map, tiles) = MakeLandMap(10, 5);
			for (int x = 0; x < 10; x++)
				tiles[x, 2].Type = Terrain.River;

			var unit = MakeUnit(2, 2, 7, 2, UnitClass.Land);
			var testee = CreateTestee(map, useNewImpl: true, riverFastMovement: true);

			// Act
			ITile actual = testee.GotoStep(unit);

			// Assert: first step stays on y=2 (river row)
			Assert.NotNull(actual);
			Assert.Equal(2, actual.Y);
		}

		[Theory]
		[MemberData(nameof(ImplementationModes))]
		public void GotoStep_WithoutRiverFastMovement_PrefersRoadOverRiver(bool useNewImpl)
		{
			// Arrange
			// 10-wide, 5-tall map. Unit at (2,2), goal at (7,2).
			// y=2 row: River tiles → cost 18/step without riverFastMovement
			// y=1 row: Road tiles  → cost 3/step always
			// Road detour (~5 steps × 3 + 18 goal = 33) beats river row (5 × 18 = 90).
			var (map, tiles) = MakeLandMap(10, 5);
			for (int x = 0; x < 10; x++)
			{
				tiles[x, 2].Type = Terrain.River;
				tiles[x, 1].Road = true;
			}

			var unit = MakeUnit(2, 2, 7, 2, UnitClass.Land);
			var testee = CreateTestee(map, useNewImpl, riverFastMovement: false);

			// Act
			ITile actual = testee.GotoStep(unit);

			// Assert: first step moves to the road row (y=1)
			Assert.NotNull(actual);
			Assert.Equal(1, actual.Y);
		}


		// ── helpers ──────────────────────────────────────────────────────────────

		/// <summary>
		/// Minimal IMapTiles backed by a flat 2-D array of StubTile.
		/// Width and Height are exposed so tests control them.
		/// </summary>
		private sealed class StubMapTiles : IMapTiles
		{
			private readonly StubTile[,] _tiles;

			public int Width { get; }
			public int Height { get; }
			public ITile this[int x, int y] => _tiles[x, y];

			public StubMapTiles(StubTile[,] tiles)
			{
				_tiles = tiles;
				Width = tiles.GetLength(0);
				Height = tiles.GetLength(1);
			}
		}

		/// <summary>
		/// Minimal ITile stub with overridable properties.
		/// All passability-relevant defaults: land, no road, no railroad, movement=2.
		/// </summary>
		private sealed class StubTile : ITile
		{
			public int X { get; }
			public int Y { get; }
			public Terrain Type { get; set; } = Terrain.Grassland1;
			public bool IsOcean { get; set; } = false;
			public bool Road { get; set; } = false;
			public bool RailRoad { get; set; } = false;
			public byte Movement { get; set; } = 2;

			// unused ITile members – minimal stubs
			public bool Special => false;
			public byte ContinentId { get; set; }
			public byte LandValue { get; set; }
			public byte LandScore => 0;
			public byte Defense => 1;
			public sbyte Food => 0;
			public sbyte Shield => 0;
			public sbyte Trade => 0;
			public sbyte IrrigationFoodBonus => 0;
			public byte IrrigationCost => 0;
			public sbyte MiningShieldBonus => 0;
			public byte MiningCost => 0;
			public byte Borders => 0;
			public byte RoadCost => 0;
			public byte RailRoadCost => 0;
			public bool Irrigation { get; set; }
			public bool Pollution { get; set; }
			public byte PollutionCost => 0;
			public bool Fortress { get; set; }
			public byte FortressCost { get; set; }
			public bool Mine { get; set; }
			public bool Hut { get; set; }
			public byte Visited => 0;
			public void Visit(byte owner) { }

			public Picture DrawPage(byte pageNumber)
			{
				throw new System.NotImplementedException();
			}

			public City City => null;
			public bool HasCity => false;
			public IUnit[] Units => [];
			public ITile this[int relativeX, int relativeY] => null;
			public string Name => "StubTile";

			IBitmap ICivilopedia.Icon => throw new System.NotImplementedException();

			byte ICivilopedia.PageCount => throw new System.NotImplementedException();

			public StubTile(int x, int y)
			{
				X = x;
				Y = y;
			}
		}

		/// <summary>Builds a w×h all-land map of StubTiles.</summary>
		private static (StubMapTiles map, StubTile[,] tiles) MakeLandMap(int w, int h)
		{
			var tiles = new StubTile[w, h];
			for (int x = 0; x < w; x++)
				for (int y = 0; y < h; y++)
					tiles[x, y] = new StubTile(x, y);
			return (new StubMapTiles(tiles), tiles);
		}

		private static MockedIUnit MakeUnit(int sx, int sy, int gx, int gy,
			UnitClass unitClass = UnitClass.Land)
		{
			return new MockedIUnit
			{
				X = sx,
				Y = sy,
				Goto = new Point(gx, gy),
				Class = unitClass,
			};
		}



		private static IUnitGotoService CreateTestee(IMapTiles map, bool useNewImpl, bool riverFastMovement = false)
			=> useNewImpl ? new UnitGotoServiceImpl2(map, riverFastMovement) :
							new UnitGotoServiceImpl(map);
	}
}
