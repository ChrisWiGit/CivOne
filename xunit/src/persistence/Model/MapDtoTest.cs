using System;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Persistence.Yaml;
using CivOne.Tiles;
using CivOne.Units;
using Xunit;

namespace CivOne.Persistence.Model
{
	public class MapDtoTest
	{
		private readonly MapDtoMapper _testee;
		private readonly MockedIMapFactory _mockedMapFactory;
		private readonly MockedITileDtoMapper _mockedTileDtoMapper;

		private TileDto[,] _tileDtos;
		private readonly int expectedWidth = 5;
		private readonly int expectedHeight = 6;

		public MapDtoTest()
		{
			_mockedMapFactory = new MockedIMapFactory();
			_mockedTileDtoMapper = new MockedITileDtoMapper(() => _mockedMapFactory.CurrentMapTiles);
			_testee = new MapDtoMapper(_mockedMapFactory, _mockedTileDtoMapper, _terrainSeed: 12345);

			_tileDtos = new TileDto[expectedWidth, expectedHeight];
			for (int x = 0; x < expectedWidth; x++)
			{
				for (int y = 0; y < expectedHeight; y++)
				{
					_tileDtos[x, y] = new TileDto
					{
						Terrain = Terrain.Plains,
						Road = x % 2 == 0,
						RailRoad = y % 2 == 0,
						Irrigation = false,
						Pollution = false,
						Fortress = false,
						Mine = false,
						Hut = false,
						LandValue = (byte)((x + y) * 10)
					};
				}
			}
		}

		[Fact]
		public void TestWriteYamlWithFlowStyleArrays()
		{
			var actualMapDto = new MapDto
			{
				TerrainSeed = 12345,
				Tiles = new Map2d<TileDto>(_tileDtos)
			};

			Assert.NotNull(actualMapDto);

			// Write with flow style converter - produces true YAML arrays
			// Output will be: LandValues: [[0, 10, 20, 30, 40], [10, 20, 30, 40, 50], ...]
			// Instead of string-based format
			YamlWriter.Of(actualMapDto)
				.WithStandard()
				.WithTypeConverter(new MapDtoTileDtoYamlConverter())
				.WithTypeConverter(new ByteArrayArrayFlowStyleYamlTypeConverter())
				.ToFile("MapDtoTest.TestWriteYamlWithFlowStyleArrays.yaml");
		}

		[Fact]
		public void TestTilesToDtoAndBack()
		{
			var mapDto = new MapDto
			{
				TerrainSeed = 12345,
				Tiles = new Map2d<TileDto>(_tileDtos)
			};

			var map = _testee.FromDto(mapDto);
			Assert.NotNull(map);
			Assert.Equal(expectedWidth, map.Width);
			Assert.Equal(expectedHeight, map.Height);
			Assert.NotNull(map[0, 0]);
			Assert.NotNull(map[map.Width - 1, map.Height - 1]);

			var mapDto2 = _testee.ToDto(map);

			Assert.Equal(mapDto.TerrainSeed, mapDto2.TerrainSeed);
			Assert.Equal(expectedWidth, mapDto2.Tiles.Width());
			Assert.Equal(expectedHeight, mapDto2.Tiles.Height());

			for (int x = 0; x < expectedWidth; x++)
			{
				for (int y = 0; y < expectedHeight; y++)
				{
					var originalTile = mapDto.Tiles[x, y];
					var roundTrippedTile = mapDto2.Tiles[x, y];

					Assert.NotNull(originalTile);
					Assert.NotNull(roundTrippedTile);

					Assert.Equal(originalTile.Terrain, roundTrippedTile.Terrain);
					Assert.Equal(originalTile.Road, roundTrippedTile.Road);
					Assert.Equal(originalTile.RailRoad, roundTrippedTile.RailRoad);
					Assert.Equal(originalTile.Irrigation, roundTrippedTile.Irrigation);
					Assert.Equal(originalTile.Pollution, roundTrippedTile.Pollution);
					Assert.Equal(originalTile.Fortress, roundTrippedTile.Fortress);
					Assert.Equal(originalTile.Mine, roundTrippedTile.Mine);
					Assert.Equal(originalTile.Hut, roundTrippedTile.Hut);
					Assert.Equal(originalTile.LandValue, roundTrippedTile.LandValue);
				}
			}
		}


		public class MockedIMapFactory : IMapFactory
		{
			private IMapTilesCommand _currentMapTiles;

			internal IMapTilesCommand CurrentMapTiles => _currentMapTiles;

			public IMapTiles CreateMap(int width, int height, uint terrainSeed)
			{
				// For testing, create a new mock map with the correct dimensions
				_currentMapTiles = new MockedIMapTiles(width, height);
				return _currentMapTiles;
			}
		}


		public interface IMapTilesCommand : IMapTiles
		{
			void ReplaceTile(ITile tile, int x, int y);
		}

		/// <summary>
		/// Simple mock implementation of IMapTiles for testing.
		/// </summary>
		public class MockedIMapTiles(int width, int height) : IMapTilesCommand
		{
			private readonly ITile[,] _tiles = new ITile[width, height];
			public int Width { get; } = width;
			public int Height { get; } = height;

			public ITile this[int x, int y] => _tiles[x, y];

			public void ReplaceTile(ITile tile, int x, int y)
			{
				_tiles[x, y] = tile;
			}
		}

		/// <summary>
		/// Mock implementation of ITileDtoMapper for testing purposes.
		/// </summary>
		public class MockedITileDtoMapper(Func<MapDtoTest.IMapTilesCommand> getMapTiles = null) : ITileDtoMapper
		{
			private readonly Func<IMapTilesCommand> _getMapTiles = getMapTiles;

			public void SetTileFromDto(TileDto dto, int x, int y)
			{
				var mapTiles = _getMapTiles?.Invoke();
				mapTiles?.ReplaceTile(new MockedITile(dto, x, y), x, y);
			}

			public TileDto ToDto(ITile domain)
			{
				return new TileDto
				{
					Terrain = domain.Type,
					Road = domain.Road,
					RailRoad = domain.RailRoad,
					Irrigation = domain.Irrigation,
					Pollution = domain.Pollution,
					Fortress = domain.Fortress,
					Mine = domain.Mine,
					Hut = domain.Hut,
					LandValue = domain.LandValue
				};
			}

			public ITile FromDto(TileDto dto)
			{
				throw new NotImplementedException("Use SetTileFromDto instead");
			}
		}

		public class MockedITile(TileDto dto, int x, int y) : ITile
		{
			public ITile this[int relativeX, int relativeY] => throw new NotImplementedException();

			public Terrain Type { get; } = dto.Terrain;
			public bool Road { get; } = dto.Road;
			public bool RailRoad { get; } = dto.RailRoad;
			public bool Irrigation { get; } = dto.Irrigation;
			public bool Pollution { get; } = dto.Pollution;
			public bool Fortress { get; } = dto.Fortress;
			public bool Mine { get; } = dto.Mine;
			public bool Hut { get; } = dto.Hut;
			public byte LandValue { get; } = dto.LandValue;

			public int X { get; } = x;
			public int Y { get; } = y;

			public bool Special => throw new NotImplementedException();

			public byte ContinentId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

			public byte LandScore => throw new NotImplementedException();

			public byte Movement => throw new NotImplementedException();

			public byte Defense => throw new NotImplementedException();

			public sbyte Food => throw new NotImplementedException();

			public sbyte Shield => throw new NotImplementedException();

			public sbyte Trade => throw new NotImplementedException();

			public sbyte IrrigationFoodBonus => throw new NotImplementedException();

			public byte IrrigationCost => throw new NotImplementedException();

			public sbyte MiningShieldBonus => throw new NotImplementedException();

			public byte MiningCost => throw new NotImplementedException();

			public byte Borders => throw new NotImplementedException();

			public byte RoadCost => throw new NotImplementedException();

			public byte RailRoadCost => throw new NotImplementedException();

			public byte PollutionCost => throw new NotImplementedException();

			public byte FortressCost { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

			public byte Visited => throw new NotImplementedException();

			public bool IsOcean => throw new NotImplementedException();

			public City City => throw new NotImplementedException();

			public bool HasCity => throw new NotImplementedException();

			public IUnit[] Units => throw new NotImplementedException();

			public string Name => throw new NotImplementedException();

			public IBitmap Icon => throw new NotImplementedException();

			public byte PageCount => throw new NotImplementedException();

			byte ITile.LandValue { get => LandValue; set => throw new NotImplementedException(); }
			bool ITile.Road { get => Road; set => throw new NotImplementedException(); }
			bool ITile.RailRoad { get => RailRoad; set => throw new NotImplementedException(); }
			bool ITile.Irrigation { get => Irrigation; set => throw new NotImplementedException(); }
			bool ITile.Pollution { get => Pollution; set => throw new NotImplementedException(); }
			bool ITile.Fortress { get => Fortress; set => throw new NotImplementedException(); }
			bool ITile.Mine { get => Mine; set => throw new NotImplementedException(); }
			bool ITile.Hut { get => Hut; set => throw new NotImplementedException(); }

			public Picture DrawPage(byte pageNumber)
			{
				throw new NotImplementedException();
			}

			public void Visit(byte owner)
			{
				throw new NotImplementedException();
			}
		}
	}
}

