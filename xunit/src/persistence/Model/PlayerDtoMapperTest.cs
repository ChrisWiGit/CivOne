namespace CivOne.Persistence.Model
{
	using System;
	using System.Linq;
	using CivOne.Civilizations;
	using CivOne.UnitTests;
	using Xunit;
	using AdvanceId = System.UInt32;

	public class PlayerDtoMapperTest
	{
		private readonly PlayerDtoMapper _testee;
		private readonly MockedIPlayer _player;

		public PlayerDtoMapperTest()
		{
			_testee = new PlayerDtoMapper(
				null, // IPlayerGame – only used in FromDto, not needed for ToDto
				new CivilizationMapper([new MockedICivilization()]),
				new PalaceDtoMapper(),
				new CityDtoMapper(new ProductionDtoMapper(new MockedReflect())));
			_player = new MockedIPlayer()
		}

		[Fact]
		public void ToDto_MapsTribeNames()
		{
			var player = new MockedIPlayer();
			var dto = _testee.ToDto(player);

			Assert.Equal(player.TribeName, dto.TribeName);
			Assert.Equal(player.TribeNamePlural, dto.TribeNamePlural);
		}

		[Fact]
		public void ToDto_MapsCivilization()
		{
			var player = AsInterface;
			var dto = _testee.ToDto(player);

			Assert.NotNull(dto.Civilization);
			Assert.Equal(player.Civilization.Leader.GetType().Name, dto.Civilization.LeaderClassName);
		}

		[Fact]
		public void ToDto_MapsAdvances()
		{
			var player = AsInterface;
			var dto = _testee.ToDto(player);

			Assert.Equal(player.Advances.Count, dto.Advances.Count);
			for (int i = 0; i < player.Advances.Count; i++)
				Assert.Equal((AdvanceId)player.Advances[i], dto.Advances[i]);
		}

		[Fact]
		public void ToDto_MapsEmbassies_WhenEmpty()
		{
			var dto = _testee.ToDto(AsInterface);

			Assert.Empty(dto.Embassies);
		}

		[Fact]
		public void ToDto_MapsGold()
		{
			var player = AsInterface;
			var dto = _testee.ToDto(player);

			Assert.Equal(player.Gold, dto.Gold);
		}

		[Fact]
		public void ToDto_MapsAnarchy()
		{
			var player = AsInterface;
			var dto = _testee.ToDto(player);

			Assert.Equal(player.Anarchy, dto.Anarchy);
		}

		[Fact]
		public void ToDto_MapsGovernment()
		{
			var player = AsInterface;
			var dto = _testee.ToDto(player);

			Assert.Equal((byte)player.Government.Id, dto.Government);
		}

		[Fact]
		public void ToDto_MapsRates()
		{
			var player = AsInterface;
			var dto = _testee.ToDto(player);

			Assert.Equal(player.LuxuriesRate, dto.LuxuriesRate);
			Assert.Equal(player.TaxesRate, dto.TaxesRate);
			Assert.Equal(player.ScienceRate, dto.ScienceRate);
		}

		[Fact]
		public void ToDto_MapsScience()
		{
			var player = AsInterface;
			var dto = _testee.ToDto(player);

			Assert.Equal(player.Science, dto.Science);
		}

		[Fact]
		public void ToDto_MapsCityNamesSkipped()
		{
			var player = AsInterface;
			var dto = _testee.ToDto(player);

			Assert.Equal(player.CityNamesSkipped, dto.CityNamesSkipped);
		}

		[Fact]
		public void ToDto_MapsCities()
		{
			var player = AsInterface;
			var dto = _testee.ToDto(player);

			Assert.Equal(player.Cities.Count, dto.Cities.Count);
		}

		[Fact]
		public void ToDto_MapsCurrentResearch_WhenSet()
		{
			var player = AsInterface;
			var dto = _testee.ToDto(player);

			Assert.Equal((AdvanceId)player.CurrentResearch.Id, dto.CurrentResearch);
		}

		[Fact]
		public void ToDto_ThrowsWhenCurrentResearchIsNull()
		{
			playa.CurrentResearch = null;

			Assert.Throws<InvalidOperationException>(() => _testee.ToDto(AsInterface));
		}

		[Fact]
		public void ToDto_MapsExploredMap()
		{
			var player = AsInterface;
			var dto = _testee.ToDto(player);

			Assert.NotNull(dto.Explored);
			Assert.Equal(player.Explored.GetLength(0), dto.Explored.Width());
			Assert.Equal(player.Explored.GetLength(1), dto.Explored.Height());
		}

		[Fact]
		public void ToDto_MapsVisibleMap()
		{
			var player = AsInterface;
			var dto = _testee.ToDto(player);

			Assert.NotNull(dto.Visible);
			Assert.Equal(player.Visible.GetLength(0), dto.Visible.Width());
			Assert.Equal(player.Visible.GetLength(1), dto.Visible.Height());
		}

		[Fact]
		public void ToDto_MapsPalace()
		{
			var player = AsInterface;
			var dto = _testee.ToDto(player);

			Assert.NotNull(dto.Palace);
		}


		// Alles Vorlagen wie Tests aussehen
		// [Fact]
		// public void TestMapResourceTiles()
		// {
		// 	resourceTiles.RemoveAt(2 * 5 + 2); // center tile is not a resource tile
		// 	var map = _testee.MapResourceTiles([.. resourceTiles]);

		// 	Assert.False(map[2, 2]);
		// 	Assert.Equal(resourceTiles.Count, map.ToArray().Cast<bool>().Count(b => b));
		// }

		// [Fact]
		// public void TestMapResourceTiles_OutOfBounds()
		// {
		// 	resourceTiles[0] = new Grassland(-3, -3);

		// 	Assert.Throws<System.ArgumentException>(
		// 		() => _testee.MapResourceTiles([.. resourceTiles]));
		// }

		// [Fact]
		// public void TestMapMapToTiles()
		// {
		// 	bool[][] data = [
		// 		[true, false, true, false, true],
		// 		[false, true, false, true, false],
		// 		[true, false, false, false, true],
		// 		[false, true, false, true, false],
		// 		[true, true, true, true, true]
		// 	];
		// 	int dataTrueCount = data.SelectMany(row => row).Count(b => b);

		// 	Bool2dMap map = new(data);

		// 	var tiles = _testee.MapMapToTiles(_cityTile, map);

		// 	Assert.Equal(dataTrueCount, tiles.Count);
		// 	foreach (var tile in tiles)
		// 	{
		// 		int dx = tile.X;
		// 		int dy = tile.Y;
		// 		Assert.False(dx == 2 && dy == 2);
		// 		Assert.Equal(data[dx][dy], tile.X == dx && tile.Y == dy);
		// 	}
		// }

		// [Fact]
		// public void TestMapMapToTiles_Empty()
		// {
		// 	Bool2dMap map = new(5, 5);
		// 	var tiles = _testee.MapMapToTiles(_cityTile, map);
		// 	Assert.Empty(tiles);
		// }

		// [Fact]
		// public void TestMapResourceTiles_MapMapToTiles()
		// {
		// 	var map = _testee.MapResourceTiles([.. resourceTiles]);
		// 	var tiles = _testee.MapMapToTiles(_cityTile, map);

		// 	Assert.Equal(resourceTiles.Count, tiles.Count);
		// 	foreach (var tile in resourceTiles)
		// 	{
		// 		Assert.Contains(tiles, t => t.X == tile.X && t.Y == tile.Y);
		// 	}

		// 	var map2 = _testee.MapResourceTiles(tiles.ToArray());
		// 	Assert.Equal(map.ToArray(), map2.ToArray());
		// }

		// [Theory]
		// [InlineData(true, false, true, false, true, false, true, false)]
		// [InlineData(false, true, false, true, false, true, false, true)]
		// [InlineData(true, true, true, true, true, true, true, true)]
		// [InlineData(false, false, false, false, false, false, false, false)]
		// public void TestMapStatusFlags(
		// 	bool isRiot,
		// 	bool isCoastal,
		// 	bool celebrationCancelled,
		// 	bool hydroAvailable,
		// 	bool autoBuild,
		// 	bool techStolen,
		// 	bool celebrationOrRapture,
		// 	bool buildingSold)
		// {
		// 	var status = new MockedCityStatus();
		// 	status.IsRiot = isRiot;
		// 	status.IsCoastal = isCoastal;
		// 	status.CelebrationCancelled = celebrationCancelled;
		// 	status.HydroAvailable = hydroAvailable;
		// 	status.AutoBuild = autoBuild;
		// 	status.TechStolen = techStolen;
		// 	status.CelebrationOrRapture = celebrationOrRapture;
		// 	status.BuildingSold = buildingSold;

		// 	var flags = _testee.MapStatusFlags(status);

		// 	Assert.Equal(isRiot, flags.Contains(CityStatusEnum.Riot));
		// 	Assert.Equal(isCoastal, flags.Contains(CityStatusEnum.Coastal));
		// 	Assert.Equal(celebrationCancelled, flags.Contains(CityStatusEnum.CelebrationCancelled));
		// 	Assert.Equal(hydroAvailable, flags.Contains(CityStatusEnum.HydroAvailable));
		// 	Assert.Equal(autoBuild, flags.Contains(CityStatusEnum.AutoBuild));
		// 	Assert.Equal(techStolen, flags.Contains(CityStatusEnum.TechStolen));
		// 	Assert.Equal(celebrationOrRapture, flags.Contains(CityStatusEnum.CelebrationRapture));
		// 	Assert.Equal(buildingSold, flags.Contains(CityStatusEnum.ImprovementSold));

		// 	var status2 = new MockedCityStatus();
		// 	_testee.MapStatusFlags(status2, flags);

		// 	Assert.Equal(isRiot, status2.IsRiot);
		// 	Assert.Equal(isCoastal, status2.IsCoastal);
		// 	Assert.Equal(celebrationCancelled, status2.CelebrationCancelled);
		// 	Assert.Equal(hydroAvailable, status2.HydroAvailable);
		// 	Assert.Equal(autoBuild, status2.AutoBuild);
		// 	Assert.Equal(techStolen, status2.TechStolen);
		// 	Assert.Equal(celebrationOrRapture, status2.CelebrationOrRapture);
		// 	Assert.Equal(buildingSold, status2.BuildingSold);
		// }

		// public class MockedCityStatus : ICityStatus
		// {
		// 	public bool IsRiot { get; set; }
		// 	public bool IsCoastal { get; set; }
		// 	public bool CelebrationCancelled { get; set; }
		// 	public bool HydroAvailable { get; set; }
		// 	public bool AutoBuild { get; set; }
		// 	public bool TechStolen { get; set; }
		// 	public bool CelebrationOrRapture { get; set; }
		// 	public bool BuildingSold { get; set; }
		// }

	}
}


// using System;
// using System.Collections.Generic;
// using System.Linq;
// using CivOne.Civilizations;
// using CivOne.Enums;

// namespace CivOne.Persistence.Model
// {
// 	using PlayerId = System.UInt32;
//     using AdvanceId = System.UInt32;
//     using GovernmentId = System.Byte;
// 	using CityId = System.UInt32;

//     public class PlayerDtoMapper(
// 		IPlayerGame gameInstance,
// 		DtoMapper<CivilizationDto, ICivilization> civilizationMapper,
// 		PalaceDtoMapper palaceMapper,
// 		CityDtoMapper cityMapper
// 		) : DtoMapper<PlayerDto, IPlayer>
// 	{
// 		private readonly DtoMapper<CivilizationDto, ICivilization> 
// 			_civilizationMapper = civilizationMapper;
// 		private readonly PalaceDtoMapper _palaceMapper = palaceMapper;
// 		private readonly CityDtoMapper _cityMapper = cityMapper;

// 		public IPlayer FromDto(PlayerDto dto)
// 		{
// 			if (Player.Game == null) {
// 				// TODO: remove if we can inject the game instance into the player by constructor
// 				Player.Game = gameInstance;
// 			}
// 			return new Player(
// 				civilization: _civilizationMapper.FromDto(dto.Civilization)
// 				// TODO: many more properties to be mapped here
// 			);
// 		}
//         public PlayerDto ToDto(IPlayer player)
//         {
//             return new PlayerDto
//             {
//                 Civilization = _civilizationMapper.ToDto(player.Civilization),

// 				Explored = player.Explored,
// 				Visible = player.Visible,

// 				TribeName = player.TribeName,
// 				TribeNamePlural = player.TribeNamePlural,

// 				Advances = [.. player.Advances.Select(a => (AdvanceId)a)],
// 				Embassies = [.. player.Embassies],

// 				Anarchy = player.Anarchy,
// 				Gold = player.Gold,
// 				CurrentResearch = (AdvanceId)player.CurrentResearch?.Id,
// 				CityNamesSkipped = player.CityNamesSkipped,

// 				Government = (GovernmentId)player.Government?.Id,
// 				LuxuriesRate = player.LuxuriesRate,
// 				TaxesRate = player.TaxesRate,
// 				ScienceRate = player.ScienceRate,
// 				Science = player.Science,
// 				Palace = _palaceMapper.ToDto(player.Palace),

// 				Cities = [.. player.Cities
// 					.Select(_cityMapper.ToDto)]
// 			};
//         }
// 	}
// }

