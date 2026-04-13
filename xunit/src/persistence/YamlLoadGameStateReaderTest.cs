using System;
using System.Collections.Generic;
using CivOne.Enums;
using CivOne.Persistence;
using CivOne.Persistence.Model;
using CivOne.Persistence.Yaml;
using CivOne.src;
using Xunit;

namespace CivOne.UnitTests.Persistence
{
	public class YamlLoadGameStateReaderTest
	{
		[Fact]
		public void GameStateDtoYamlRead_MapsCoreSections()
		{
			// Arrange
			const string yaml =
				"Difficulty: King\n" +
				"GameTurn: 42\n" +
				"HumanPlayer: 0\n" +
				"CurrentPlayer: 1\n" +
				"Players:\n" +
				"- Civilization:\n" +
				"    LeaderClassName: Hammurabi\n" +
				"  Advances: [1]\n" +
				"  Embassies: []\n" +
				"  Cities: []\n" +
				"  Units: []\n" +
				"- Civilization:\n" +
				"    LeaderClassName: Caesar\n" +
				"  Advances: [1]\n" +
				"  Embassies: []\n" +
				"  Cities: []\n" +
				"  Units:\n" +
				"  - ClassName: Militia\n" +
				"    PlayerId: 1\n" +
				"Map:\n" +
				"  TerrainSeed: 4242\n" +
				"  Tiles:\n" +
				"  - ARIE\n" +
				"  - AiQD\n" +
				"  LandValues:\n" +
				"  - 01,03\n" +
				"  - 02,04\n" +
				"GameOptions: [EndOfTurn, Sound]\n";

			// Act
			var actual = YamlReader.OfString(yaml)
				.WithStandard()
				.WithTypeConverter(new MapDtoTileDtoYamlConverter())
				.As<GameStateDto>();

			// Assert
			Assert.NotNull(actual);
			Assert.NotNull(actual.Players);
			Assert.Equal(2, actual.Players.Count);
			Assert.NotNull(actual.Players[1].Units);
			Assert.True(actual.Players[1].Units.Count > 0);
			Assert.NotNull(actual.Map);
			Assert.NotNull(actual.Map.Tiles);
			Assert.True(actual.Map.MapSeed > 0);
			Assert.Equal((ushort)0, actual.HumanPlayer);
			Assert.Equal((ushort)1, actual.CurrentPlayer);
		}

		[Fact]
		public void GameStateDtoYamlRead_PreservesNegativeAdvanceSentinel()
		{
			const string yaml =
				"Players:\n" +
				"- Civilization:\n" +
				"    LeaderClassName: Hammurabi\n" +
				"  Advances: [-1]\n" +
				"  Embassies: []\n" +
				"  Cities: []\n" +
				"  Units: []\n";

			var actual = YamlReader.OfString(yaml)
				.WithStandard()
				.As<GameStateDto>();

			Assert.NotNull(actual);
			Assert.Single(actual.Players);
			Assert.Equal([-1L], actual.Players[0].Advances);
		}
	}

	public class YamlLoadGameStateMapperTest : TestsBase
	{
		private readonly IValueSanitizer _sanitizer = new ValueSanitizer(new TestLogger());

		[Fact]
		public void FromDto_MapsPlayersUnitsCitiesAndMapTiles()
		{
			// Arrange
			var dto = YamlLoadGameStateReaderTestData.BuildSampleGameStateDto();
			Player.Game = null;
			var deps = YamlLoadMapperDependenciesFactory.Create(_sanitizer);
			var mapper = new GameStateDtoMapper(deps.PlayerMapper, deps.UnitMapper, deps.MapMapper, deps.GlobalWarmingMapper, deps.Sanitizer);

			// Act
			var actual = mapper.FromDto(dto);

			// Assert
			Assert.NotNull(actual);
			Assert.NotNull(actual.Players);
			Assert.Equal(2, actual.Players.Length);
			Assert.NotNull(actual.Units);
			Assert.NotEmpty(actual.Units);
			Assert.NotNull(actual.Cities);
			Assert.Single(actual.Players[0].Cities);
			Assert.NotNull(actual.MapTiles);
			Assert.NotNull(actual.MapTiles[0, 0]);
			Assert.NotNull(actual.MapTiles[4, 3]);
			Assert.NotNull(actual.HumanPlayer);
			Assert.NotNull(actual.CurrentPlayer);
		}

		[Fact]
		public void FromDto_TransfersHutAndLandValue()
		{
			// Arrange
			var dto = YamlLoadGameStateReaderTestData.BuildSampleGameStateDto();
			Player.Game = null;
			var deps = YamlLoadMapperDependenciesFactory.Create(_sanitizer);
			var mapper = new GameStateDtoMapper(deps.PlayerMapper, deps.UnitMapper, deps.MapMapper, deps.GlobalWarmingMapper, deps.Sanitizer);

			// Act
			var actual = mapper.FromDto(dto);

			// Assert
			var tile = actual.MapTiles[4, 3];
			Assert.True(tile.Hut);
			Assert.Equal((byte)77, tile.LandValue);
		}

		[Fact]
		public void FromDto_PreservesCityStatusFlags()
		{
			// Arrange
			var dto = YamlLoadGameStateReaderTestData.BuildSampleGameStateDto();
			dto.Players[0].Cities[0].Status =
			[
				CityStatusEnum.Riot,
				CityStatusEnum.CelebrationCancelled,
				CityStatusEnum.AutoBuild,
				CityStatusEnum.ImprovementSold
			];

			Player.Game = null;
			var deps = YamlLoadMapperDependenciesFactory.Create(_sanitizer);
			var mapper = new GameStateDtoMapper(deps.PlayerMapper, deps.UnitMapper, deps.MapMapper, deps.GlobalWarmingMapper, deps.Sanitizer);

			// Act
			var actual = mapper.FromDto(dto);
			var roundTrip = mapper.ToDto(actual);

			// Assert
			var city = Assert.Single(actual.Cities);
			Assert.True(city.IsRiot);
			Assert.True(city.CelebrationCancelled);
			Assert.True(city.AutoBuild);
			Assert.True(city.BuildingSold);

			var roundTripStatus = roundTrip.Players[0].Cities[0].Status;
			Assert.Contains(CityStatusEnum.Riot, roundTripStatus);
			Assert.Contains(CityStatusEnum.CelebrationCancelled, roundTripStatus);
			Assert.Contains(CityStatusEnum.AutoBuild, roundTripStatus);
			Assert.Contains(CityStatusEnum.ImprovementSold, roundTripStatus);
		}

		private sealed class TestLogger : ILogger
		{
			public void Log(string text, params object[] parameters)	{ }
		}
	}

	internal static class YamlLoadGameStateReaderTestData
	{
		internal static GameStateDto BuildSampleGameStateDto()
		{
			const uint advanceId = 1;
			const string leader0 = "Hammurabi";
			const string leader1 = "Caesar";
			var cityId = Guid.Parse("00000000-0000-0000-0000-000000000111");

			var map = new MapDto
			{
				MapSeed = 4242,
				Tiles = CreateTiles(80, 50)
			};

			var city = new CityDto
			{
				Id = cityId,
				Owner = 0,
				Name = "Babylon",
				Size = 2,
				Food = 8,
				Shields = 5,
				Location = new MapLocation(2, 2),
				ResourceTiles = new Bool2dMap(5, 5),
				Specialists = [],
				Buildings = [],
				Wonders = [],
				Status = [],
				VisibleSizes = new uint[] { 2, 0 },
				TradingCities = Array.Empty<Guid>(),
				ContinentId = 1
			};

			var player0 = new PlayerDto
			{
				Id = 0,
				Civilization = new CivilizationDto { LeaderClassName = leader0 },
				TribeName = "Babylonian",
				TribeNamePlural = "Babylonians",
				Explored = new Bool2dMap(80, 50),
				Visible = new Bool2dMap(80, 50),
				Advances = [advanceId],
				Embassies = [],
				Anarchy = 0,
				Gold = 50,
				CurrentResearch = advanceId,
				CityNamesSkipped = 0,
				Government = 1,
				LuxuriesRate = 0,
				TaxesRate = 5,
				ScienceRate = 5,
				Science = 10,
				Palace = null,
				Cities = [city],
				Units =
				[
					new UnitDto
					{
						ClassName = "Settlers",
						PlayerId = 0,
						Location = new MapLocation(2, 2),
						Goto = new MapLocation(2, 2),
						HomeCityGuid = cityId,
						Busy = false,
						Veteran = false,
						Sentry = false,
						FortifyActive = false,
						Fortify = false,
						FuelOrProgress = 0,
						Fuel = 0,
						WorkProgress = 0,
						Order = Order.None,
						MovesSkip = 0,
						MovesLeft = 1,
						PartMoves = 0
					}
				]
			};

			var player1 = new PlayerDto
			{
				Id = 1,
				Civilization = new CivilizationDto { LeaderClassName = leader1 },
				TribeName = "Roman",
				TribeNamePlural = "Romans",
				Explored = new Bool2dMap(80, 50),
				Visible = new Bool2dMap(80, 50),
				Advances = [advanceId],
				Embassies = [],
				Anarchy = 0,
				Gold = 100,
				CurrentResearch = advanceId,
				CityNamesSkipped = 0,
				Government = 1,
				LuxuriesRate = 1,
				TaxesRate = 4,
				ScienceRate = 5,
				Science = 12,
				Palace = null,
				Cities = [],
				Units =
				[
					new UnitDto
					{
						ClassName = "Militia",
						PlayerId = 1,
						Location = new MapLocation(6, 6),
						Goto = new MapLocation(6, 6),
						HomeCityGuid = null,
						Busy = false,
						Veteran = true,
						Sentry = false,
						FortifyActive = false,
						Fortify = false,
						FuelOrProgress = 0,
						Fuel = 0,
						WorkProgress = 0,
						Order = Order.None,
						MovesSkip = 0,
						MovesLeft = 1,
						PartMoves = 0
					}
				]
			};

			return new GameStateDto
			{
				Difficulty = DifficultyLevel.King,
				GameTurn = 42,
				HumanPlayer = 0,
				CurrentPlayer = 1,
				Players = [player0, player1],
				GameRandomSeed = 1234,
				AnthologyTurn = 75,
				Map = map,
				GameOptions = [GameOptionEnum.EndOfTurn, GameOptionEnum.Sound]
			};
		}

		private static Map2d<TileDto> CreateTiles(int width, int height)
		{
			var result = new Map2d<TileDto>(width, height);

			for (var x = 0; x < width; x++)
			for (var y = 0; y < height; y++)
			{
				result[x, y] = new TileDto
				{
					Terrain = Terrain.Plains,
					LandValue = 0
				};
			}

			result[4, 3] = new TileDto
			{
				Terrain = Terrain.Forest,
				Hut = true,
				LandValue = 77
			};

			return result;
		}
	}
}
