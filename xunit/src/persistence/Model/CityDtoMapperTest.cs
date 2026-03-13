namespace CivOne.Persistence.Model
{
	using System.Collections.Generic;
	using System.Linq;
	using CivOne.Tiles;
	using CivOne.Units;
	using CivOne.UnitTests;
	using Xunit;
	using CityId = System.UInt32;

	public class CityDtoMapperTest : TestsBase2
	{
		private readonly CityDtoMapper _testee;
		private readonly List<ITile> resourceTiles;

		private readonly MockedCityTile _cityTile;

		public CityDtoMapperTest()
		{
			_testee = new CityDtoMapper(
				new ProductionDtoMapper(new MockedReflect()));
			List<ITile> tiles = new();

			for (int x = 0; x < 5; x++)
			{
				for (int y = 0; y < 5; y++)
				{
					tiles.Add(new Grassland(x, y));
				}
			}
			tiles.RemoveAt(2 * 5 + 2); // center tile
			resourceTiles = tiles;
			_cityTile = new MockedCityTile();

			var tile = _cityTile.Tile;
			Assert.Equal(2, tile.X);
			Assert.Equal(2, tile.Y);
		}
		[Fact]
		public void TestMapResourceTiles()
		{
			resourceTiles.RemoveAt(2 * 5 + 2); // center tile is not a resource tile
			var map = _testee.MapResourceTiles([.. resourceTiles]);

			Assert.False(map[2, 2]);
			Assert.Equal(resourceTiles.Count, map.ToArray().Cast<bool>().Count(b => b));
		}

		[Fact]
		public void TestMapResourceTiles_OutOfBounds()
		{
			resourceTiles[0] = new Grassland(-3, -3);

			Assert.Throws<System.ArgumentException>(
				() => _testee.MapResourceTiles([.. resourceTiles]));
		}

		[Fact]
		public void TestMapMapToTiles()
		{
			bool[][] data = [
				[true, false, true, false, true],
				[false, true, false, true, false],
				[true, false, false, false, true],
				[false, true, false, true, false],
				[true, true, true, true, true]
			];
			int dataTrueCount = data.SelectMany(row => row).Count(b => b);

			Bool2dMap map = new(data);

			var tiles = _testee.MapMapToTiles(_cityTile, map);

			Assert.Equal(dataTrueCount, tiles.Count);
			foreach (var tile in tiles)
			{
				int dx = tile.X;
				int dy = tile.Y;
				Assert.False(dx == 2 && dy == 2);
				Assert.Equal(data[dx][dy], tile.X == dx && tile.Y == dy);
			}
		}

		[Fact]
		public void TestMapMapToTiles_Empty()
		{
			Bool2dMap map = new(5, 5);
			var tiles = _testee.MapMapToTiles(_cityTile, map);
			Assert.Empty(tiles);
		}

		[Fact]
		public void TestMapResourceTiles_MapMapToTiles()
		{
			var map = _testee.MapResourceTiles([.. resourceTiles]);
			var tiles = _testee.MapMapToTiles(_cityTile, map);

			Assert.Equal(resourceTiles.Count, tiles.Count);
			foreach (var tile in resourceTiles)
			{
				Assert.Contains(tiles, t => t.X == tile.X && t.Y == tile.Y);
			}

			var map2 = _testee.MapResourceTiles(tiles.ToArray());
			Assert.Equal(map.ToArray(), map2.ToArray());
		}

		[Theory]
		[InlineData(true, false, true, false, true, false, true, false)]
		[InlineData(false, true, false, true, false, true, false, true)]
		[InlineData(true, true, true, true, true, true, true, true)]
		[InlineData(false, false, false, false, false, false, false, false)]
		public void TestMapStatusFlags(
			bool isRiot,
			bool isCoastal,
			bool celebrationCancelled,
			bool hydroAvailable,
			bool autoBuild,
			bool techStolen,
			bool celebrationOrRapture,
			bool buildingSold)
		{
			var status = new MockedCityStatus();
			status.IsRiot = isRiot;
			status.IsCoastal = isCoastal;
			status.CelebrationCancelled = celebrationCancelled;
			status.HydroAvailable = hydroAvailable;
			status.AutoBuild = autoBuild;
			status.TechStolen = techStolen;
			status.CelebrationOrRapture = celebrationOrRapture;
			status.BuildingSold = buildingSold;

			var flags = _testee.MapStatusFlags(status);

			Assert.Equal(isRiot, flags.Contains(CityStatusEnum.Riot));
			Assert.Equal(isCoastal, flags.Contains(CityStatusEnum.Coastal));
			Assert.Equal(celebrationCancelled, flags.Contains(CityStatusEnum.CelebrationCancelled));
			Assert.Equal(hydroAvailable, flags.Contains(CityStatusEnum.HydroAvailable));
			Assert.Equal(autoBuild, flags.Contains(CityStatusEnum.AutoBuild));
			Assert.Equal(techStolen, flags.Contains(CityStatusEnum.TechStolen));
			Assert.Equal(celebrationOrRapture, flags.Contains(CityStatusEnum.CelebrationRapture));
			Assert.Equal(buildingSold, flags.Contains(CityStatusEnum.ImprovementSold));

			var status2 = new MockedCityStatus();
			_testee.MapStatusFlags(status2, flags);

			Assert.Equal(isRiot, status2.IsRiot);
			Assert.Equal(isCoastal, status2.IsCoastal);
			Assert.Equal(celebrationCancelled, status2.CelebrationCancelled);
			Assert.Equal(hydroAvailable, status2.HydroAvailable);
			Assert.Equal(autoBuild, status2.AutoBuild);
			Assert.Equal(techStolen, status2.TechStolen);
			Assert.Equal(celebrationOrRapture, status2.CelebrationOrRapture);
			Assert.Equal(buildingSold, status2.BuildingSold);
		}

		// 	namespace CivOne.Tiles
		// {
		// 	public interface ITile : ICivilopedia
		// 	{
		// 		int X { get; }
		// 		int Y { get; }
		// 		Terrain Type { get; }
		// 		bool Special { get; }
		// 		byte ContinentId { get; set; }
		// 		byte LandValue { get; set; }
		// 		byte LandScore { get; }
		// 		byte Movement { get; }
		// 		byte Defense { get; }
		// 		sbyte Food { get; }
		// 		sbyte Shield { get; }
		// 		sbyte Trade { get; }	
		// 		sbyte IrrigationFoodBonus { get; }
		// 		/// <summary>
		// 		/// Number of turns required to irrigate this terrain
		// 		/// </summary>
		// 		byte IrrigationCost { get; }
		// 		sbyte MiningShieldBonus { get; }
		// 		/// <summary>
		// 		/// Number of turns required to build mines on this terrain
		// 		/// </summary>
		// 		byte MiningCost { get; }
		// 		byte Borders { get; }
		// 		bool Road { get; set; }
		// 		/// <summary>
		// 		/// Number of turns required to build a road on this terrain
		// 		/// </summary>
		// 		byte RoadCost { get; }
		// 		bool RailRoad { get; set; }
		// 		/// <summary>
		// 		/// Number of turns required to build a railroad on terrain with road
		// 		/// </summary>
		// 		byte RailRoadCost { get; }
		// 		bool Irrigation { get; set; }
		// 		bool Pollution { get; set; }
		// 		byte PollutionCost { get; }
		// 		bool Fortress { get; set; }
		// 		byte FortressCost { get; set; }
		// 		bool Mine { get; set; }
		// 		bool Hut { get; set; }
		// 		byte Visited { get; }
		// 		void Visit(byte owner);
		// 		bool IsOcean { get; }
		// 		City City { get; }
		//         bool HasCity { get; }

		//         IUnit[] Units { get; }
		// 		ITile this[int relativeX, int relativeY] { get; }

		// 		bool SameLocationAs(ITile other)
		// 		{
		// 			return this.X == other.X && this.Y == other.Y;
		// 		}
		// 	}
		// }

		// public List<ITile> MapMapToTiles(ICityTile city, Bool2dMap activatedResourceTileMap)
		// 	{
		//         List<ITile> tiles = [];

		//         for (int x = 0; x < activatedResourceTileMap.Width(); x++)
		//         for (int y = 0; y < activatedResourceTileMap.Height(); y++)
		//         {
		//             if (activatedResourceTileMap[x, y])
		//             {
		//                 int dx = x - 2;
		//                 int dy = y - 2;

		//                 if (dx == 0 && dy == 0)
		//                 {
		//                     continue;
		//                 }

		//                 tiles.Add(city.Tile[dx, dy]);
		//             }
		//         } 

		//         return tiles;
		//     }
	}
}


// public List<CityStatusEnum> MapStatusFlags(ICityStatus status)
//         {
//             List<CityStatusEnum> flags = [];

//             if (status.IsRiot) flags.Add(CityStatusEnum.Riot);
//             if (status.IsCoastal) flags.Add(CityStatusEnum.Coastal);
//             if (status.CelebrationCancelled) flags.Add(CityStatusEnum.CelebrationCancelled);
//             if (status.HydroAvailable) flags.Add(CityStatusEnum.HydroAvailable);
//             if (status.AutoBuild) flags.Add(CityStatusEnum.AutoBuild);
//             if (status.TechStolen) flags.Add(CityStatusEnum.TechStolen);
//             if (status.CelebrationOrRapture) flags.Add(CityStatusEnum.CelebrationRapture);
//             if (status.BuildingSold) flags.Add(CityStatusEnum.ImprovementSold);

//             return flags;
//         }

//         public void MapStatusFlags(ICityStatus status, List<CityStatusEnum> flags)
//         {
//             status.IsRiot = flags.Contains(CityStatusEnum.Riot);
//             status.IsCoastal = flags.Contains(CityStatusEnum.Coastal);
//             status.CelebrationCancelled = flags.Contains(CityStatusEnum.CelebrationCancelled);
//             status.HydroAvailable = flags.Contains(CityStatusEnum.HydroAvailable);
//             status.AutoBuild = flags.Contains(CityStatusEnum.AutoBuild);
//             status.TechStolen = flags.Contains(CityStatusEnum.TechStolen);
//             status.CelebrationOrRapture = flags.Contains(CityStatusEnum.CelebrationRapture);
//             status.BuildingSold = flags.Contains(CityStatusEnum.ImprovementSold);
//         }