using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Persistence.Yaml;
using CivOne.Tiles;
using CivOne.Units;
using Xunit;
using YamlDotNet.Serialization;

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

		private readonly uint _terrainSeed = 12345;

		public MapDtoTest()
		{
			_mockedMapFactory = new MockedIMapFactory();
			_mockedTileDtoMapper = new MockedITileDtoMapper(() => _mockedMapFactory.CurrentMapTiles);
			_testee = new MapDtoMapper(_mockedMapFactory, _mockedTileDtoMapper, _terrainSeed: _terrainSeed);

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
		public void TestMapDtoMapper_ContractCheck()
		{
			var dtoProperties = GetWritablePropertyNames<MapDto>();
			var expectedProperties = GetMapDtoRoundTripAssertionMap(
				new MapDto { TerrainSeed = _terrainSeed, Tiles = new Map2d<TileDto>(_tileDtos) },
				new MapDto { TerrainSeed = _terrainSeed, Tiles = new Map2d<TileDto>(_tileDtos) }).Keys.ToHashSet();

			Assert.Equal([], dtoProperties.Except(expectedProperties).OrderBy(x => x));
		}

		[Fact]
		public void TestWriteYamlWithFlowStyleArrays()
		{
			var actualMapDto = new MapDto
			{
				TerrainSeed = _terrainSeed,
				Tiles = new Map2d<TileDto>(_tileDtos)
			};

			Assert.NotNull(actualMapDto);

			// Write map YAML with compact row encoding for LandValues
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

		[Fact]
		public void TestByteArrayArrayFlowStyleYamlTypeConverterRoundTrip()
		{
			// Test data: 3x2 byte array
			byte[][] testData =
			[
				[0, 10, 20, 30, 40],
				[50, 60, 70, 80, 90],
				[100, 110, 120, 130, 140]
			];

			// Serialize to YAML
			string yaml = YamlWriter.Of(testData)
				.WithTypeConverter(new ByteArrayArrayFlowStyleYamlTypeConverter())
				.AsString();

			Assert.NotNull(yaml);
			Assert.NotEmpty(yaml);

			// ByteArrayArrayFlowStyleYamlTypeConverter creates flow style arrays
			Assert.Contains("- [", yaml);
			Assert.Contains("0, 10, 20, 30, 40", yaml);

			// Deserialize back
			var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
				.WithTypeConverter(new ByteArrayArrayFlowStyleYamlTypeConverter())
				.Build();

			byte[][] roundTripped = deserializer.Deserialize<byte[][]>(yaml);

			// Verify round trip
			Assert.NotNull(roundTripped);
			Assert.Equal(testData.Length, roundTripped.Length);

			for (int i = 0; i < testData.Length; i++)
			{
				Assert.Equal(testData[i].Length, roundTripped[i].Length);
				for (int j = 0; j < testData[i].Length; j++)
				{
					Assert.Equal(testData[i][j], roundTripped[i][j]);
				}
			}
		}

		[Fact]
		public void TestMapDtoTileDtoYamlConverterEncoding()
		{
			var mapDto = new MapDto
			{
				TerrainSeed = 99999,
				Tiles = new Map2d<TileDto>(_tileDtos)
			};

			string yaml = YamlWriter.Of(mapDto)
				.WithTypeConverter(new MapDtoTileDtoYamlConverter())
				.AsString();

			Assert.NotNull(yaml);
			Assert.NotEmpty(yaml);

			// Verify that tiles are encoded as Base64 strings (2 chars per tile)
			// For a 5-wide map, each row should have at least 10 characters of Base64-encoded data
			Assert.Equal(
				"TerrainSeed: 99999\n" +
				"Tiles:\n" +
				"- AxAhAxAhAx\n" +
				"- ARABARABAR\n" +
				"- AxAhAxAhAx\n" +
				"- ARABARABAR\n" +
				"- AxAhAxAhAx\n" +
				"- ARABARABAR\n" +
				"LandValues:\n" +
				"- 00,64,C8,2C,90\n" +
				"- 64,C8,2C,90,F4\n" +
				"- C8,2C,90,F4,58\n" +
				"- 2C,90,F4,58,BC\n" +
				"- 90,F4,58,BC,20\n" +
				"- F4,58,BC,20,84\n",
				yaml.Replace("\r\n", "\n")); 
		}

		[Fact]
		public void TestMapDtoTileDtoYamlConverterDecoding()
		{
			const string yaml =
				"TerrainSeed: 4242\n" +
				"Tiles:\n" +
				"- ARIE\n" +
				"- AiQD\n" +
				"LandValues:\n" +
				"- 01,03\n" +
				"- 02,04\n";

			var actual = YamlReader.OfString(yaml)
				.WithTypeConverter(new MapDtoTileDtoYamlConverter())
				.As<MapDto>();

			Assert.NotNull(actual);
			Assert.Equal((uint)4242, actual.TerrainSeed);
			Assert.NotNull(actual.Tiles);
			Assert.Equal(2, actual.Tiles.Width());
			Assert.Equal(2, actual.Tiles.Height());

			Assert.Equal(Terrain.Plains, actual.Tiles[0, 0].Terrain);
			Assert.True(actual.Tiles[0, 0].Road);
			Assert.Equal((byte)1, actual.Tiles[0, 0].LandValue);

			Assert.Equal(Terrain.Hills, actual.Tiles[1, 0].Terrain);
			Assert.True(actual.Tiles[1, 0].Mine);
			Assert.Equal((byte)3, actual.Tiles[1, 0].LandValue);

			Assert.Equal(Terrain.Grassland1, actual.Tiles[0, 1].Terrain);
			Assert.True(actual.Tiles[0, 1].RailRoad);
			Assert.Equal((byte)2, actual.Tiles[0, 1].LandValue);

			Assert.Equal(Terrain.Forest, actual.Tiles[1, 1].Terrain);
			Assert.True(actual.Tiles[1, 1].Hut);
			Assert.Equal((byte)4, actual.Tiles[1, 1].LandValue);
		}

		[Fact]
		public void TestByteArrayArrayFlowStyleYamlTypeConverterEmptyArray()
		{
			// Test with empty array
			byte[][] testData = [];

			string yaml = YamlWriter.Of(testData)
				.WithTypeConverter(new ByteArrayArrayFlowStyleYamlTypeConverter())
				.AsString();

			Assert.NotNull(yaml);

			var roundTripped = YamlReader.OfString(yaml)
				.WithTypeConverter(new ByteArrayArrayFlowStyleYamlTypeConverter())
				.As<byte[][]>();	

			Assert.Empty(roundTripped);
		}

		[Fact]
		public void TestByteArrayArrayFlowStyleYamlTypeConverterSingleRow()
		{
			byte[][] testData = [[1, 2, 3]];

			string yaml = YamlWriter.Of(testData)
				.WithTypeConverter(new ByteArrayArrayFlowStyleYamlTypeConverter())
				.AsString();

			var roundTripped = YamlReader.OfString(yaml)
				.WithTypeConverter(new ByteArrayArrayFlowStyleYamlTypeConverter())
				.As<byte[][]>();

			Assert.Single(roundTripped);
			Assert.Equal(new byte[] { 1, 2, 3 }, roundTripped[0]);
		}

		[Fact]
		public void TestByteArrayArrayFlowStyleYamlTypeConverterHexadecimalFormat()
		{
			byte[][] testData = [[0, 10, 255]];

			string yaml = YamlWriter.Of(testData)
				.WithTypeConverter(new ByteArrayArrayFlowStyleYamlTypeConverter(ByteArrayValueFormat.Hexadecimal))
				.AsString();

			Assert.Contains("- [00, 0A, FF]", yaml);

			var roundTripped = YamlReader.OfString(yaml)
				.WithTypeConverter(new ByteArrayArrayFlowStyleYamlTypeConverter())
				.As<byte[][]>();

			Assert.Single(roundTripped);
			Assert.Equal(new byte[] { 0, 10, 255 }, roundTripped[0]);
		}

		[Fact]
		public void TestByteArrayArrayFlowStyleYamlTypeConverterBinaryFormat()
		{
			byte[][] testData = [[1, 2, 3]];

			string yaml = YamlWriter.Of(testData)
				.WithTypeConverter(new ByteArrayArrayFlowStyleYamlTypeConverter(ByteArrayValueFormat.Binary))
				.AsString();

			Assert.Contains("- [00000001, 00000010, 00000011]", yaml);

			var roundTripped = YamlReader.OfString(yaml)
				.WithTypeConverter(new ByteArrayArrayFlowStyleYamlTypeConverter())
				.As<byte[][]>();

			Assert.Single(roundTripped);
			Assert.Equal(new byte[] { 1, 2, 3 }, roundTripped[0]);
		}

		private static Dictionary<string, Action> GetMapDtoRoundTripAssertionMap(MapDto expected, MapDto actual)
			=> new()
			{
				[nameof(MapDto.TerrainSeed)] = () => Assert.Equal(expected.TerrainSeed, actual.TerrainSeed),
				[nameof(MapDto.Tiles)] = () =>
				{
					Assert.NotNull(expected.Tiles);
					Assert.NotNull(actual.Tiles);
					Assert.Equal(expected.Tiles.Width(), actual.Tiles.Width());
					Assert.Equal(expected.Tiles.Height(), actual.Tiles.Height());
				}
			};

		private static HashSet<string> GetWritablePropertyNames<T>() => typeof(T).GetProperties()
			.Where(p => p.CanRead && p.CanWrite && !(p.GetMethod?.IsStatic ?? false))
			.Select(p => p.Name)
			.ToHashSet();


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
		public class MockedITileDtoMapper(Func<IMapTilesCommand> getMapTiles = null) : ITileDtoMapper
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

