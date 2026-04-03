namespace CivOne.Persistence.Model
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using CivOne;
	using CivOne.Buildings;
	using CivOne.Enums;
	using CivOne.Persistence.Yaml;
	using CivOne.Tiles;
	using CivOne.UnitTests;
	using CivOne.Wonders;
	using Xunit;

	public class CityDtoMapperTest
	{
		private readonly CityDtoMapper _testee;
		private readonly CityDto _originalDto;
		private readonly List<ITile> resourceTiles;

		private readonly MockedCityTile _cityTile;

		public CityDtoMapperTest()
		{
			var production = new TestProduction(price: 1, buyPrice: 50, productionId: 1);
			var productionDto = new ProductionDto { Price = production.Price, BuyPrice = (uint)production.BuyPrice, ProductionId = production.ProductionId };
			_testee = new CityDtoMapper(
				new ProductionDtoMapper(new TestReflect([production])),
				new TestCityDefinitionResolver(),
				new YamlReadValueSanitizer(new NoOpLogger()));
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
			var cityId = Guid.Parse("00000000-0000-0000-0000-000000000001");
			var tradeCityId = Guid.Parse("00000000-0000-0000-0000-000000000002");

			_originalDto = new CityDto
			{
				Id = cityId,
				Location = new MapLocation(10, 12),
				Owner = 1,
				Name = "Rome",
				Size = 7,
				VisibleSizes = [7, 5],
				CurrentProduction = productionDto,
				ResourceTiles = new Bool2dMap(5, 5),
				Specialists = [Citizen.Entertainer, Citizen.Taxman],
				Buildings = [Building.Barracks],
				Wonders = [Wonder.Pyramids],
				Status = [CityStatusEnum.Riot, CityStatusEnum.AutoBuild],
				WasInDisorder = true,
				TradingCities = [tradeCityId],
				ContinentId = 3
			};

			var tile = _cityTile.Tile;
			Assert.Equal(2, tile.X);
			Assert.Equal(2, tile.Y);
		}

		[Fact]
		public void TestCityDtoMapper_ContractCheck()
		{
			var dtoProperties = GetWritablePropertyNames<CityDto>();
			var expectedProperties = GetCityDtoRoundTripAssertionMap(_originalDto, _originalDto).Keys.ToHashSet();

			Assert.Equal([], dtoProperties.Except(expectedProperties).OrderBy(x => x));
		}

		[Fact]
		public void TestCityDtoMapper_RoundTrip()
		{
			var city = _testee.FromDto(_originalDto);
			var roundTripDto = _testee.ToDto(city);

			Assert.NotNull(roundTripDto);

			YamlWriter.Of(roundTripDto)
				.WithStandard()
				.WithTypeConverter(new MapDtoTileDtoYamlConverter())
				.ToFile("CityDtoMapperTest.TestCityDtoMapper_RoundTrip.yaml");

			var assertions = GetCityDtoRoundTripAssertionMap(_originalDto, roundTripDto);
			foreach (var assertion in assertions.Values)
			{
				assertion();
			}
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

		private static Dictionary<string, Action> GetCityDtoRoundTripAssertionMap(CityDto expected, CityDto actual)
			=> new()
			{
				[nameof(CityDto.Id)] = () => Assert.Equal(expected.Id, actual.Id),
				[nameof(CityDto.Location)] = () => Assert.Equal(expected.Location, actual.Location),
				[nameof(CityDto.Owner)] = () => Assert.Equal(expected.Owner, actual.Owner),
				[nameof(CityDto.Name)] = () => Assert.Equal(expected.Name, actual.Name),
				[nameof(CityDto.Size)] = () => Assert.Equal(expected.Size, actual.Size),
				[nameof(CityDto.VisibleSizes)] = () => Assert.Equal(expected.VisibleSizes, actual.VisibleSizes),
				[nameof(CityDto.CurrentProduction)] = () =>
				{
					Assert.NotNull(actual.CurrentProduction);
					Assert.Equal(expected.CurrentProduction.ProductionId, actual.CurrentProduction.ProductionId);
				},
				[nameof(CityDto.ResourceTiles)] = () => Assert.Equal(expected.ResourceTiles.ToArray(), actual.ResourceTiles.ToArray()),
				[nameof(CityDto.Specialists)] = () => Assert.Equal(expected.Specialists, actual.Specialists),
				[nameof(CityDto.Buildings)] = () => Assert.Equal(expected.Buildings, actual.Buildings),
				[nameof(CityDto.Wonders)] = () => Assert.Equal(expected.Wonders, actual.Wonders),
				[nameof(CityDto.Status)] = () => Assert.Equal(expected.Status.OrderBy(x => x), actual.Status.OrderBy(x => x)),
				[nameof(CityDto.WasInDisorder)] = () => Assert.Equal(expected.WasInDisorder, actual.WasInDisorder),
				[nameof(CityDto.TradingCities)] = () => Assert.Empty(actual.TradingCities),
				[nameof(CityDto.ContinentId)] = () => Assert.Equal(expected.ContinentId, actual.ContinentId)
			};

		private static HashSet<string> GetWritablePropertyNames<T>() => typeof(T).GetProperties()
			.Where(p => p.CanRead && p.CanWrite)
			.Select(p => p.Name)
			.ToHashSet();

		private sealed class TestReflect(IEnumerable<IProduction> productions) : IReflect
		{
			public IEnumerable<IProduction> GetProduction() => productions;
		}

		private sealed class TestProduction(byte price, short buyPrice, byte productionId) : IProduction
		{
			public byte Price { get; } = price;
			public short BuyPrice { get; } = buyPrice;
			public byte ProductionId { get; } = productionId;
		}

		private sealed class TestCityDefinitionResolver : ICityDefinitionResolver
		{
			public IBuilding[] ResolveBuildings(IEnumerable<Building> buildingTypes)
				=> [.. (buildingTypes ?? []).Select(type => new MockedIBuilding { Type = type })];

			public IWonder[] ResolveWonders(IEnumerable<Wonder> wonderTypes)
				=> [.. (wonderTypes ?? []).Select(type => new MockedIWonder { Type = type })];
		}
	}
}