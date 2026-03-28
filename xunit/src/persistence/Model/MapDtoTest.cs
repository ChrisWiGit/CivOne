using System;
using CivOne.Enums;
using CivOne.Persistence.Yaml;
using CivOne.Tiles;
using Xunit;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Test für MapDto YAML Serialization
	/// 
	/// Zwei verschiedene Converter-Implementierungen werden getestet:
	/// 
	/// 1. ByteArrayArrayYamlTypeConverter (default, compact):
	///    - Konvertiert byte[][] zu string[] mit Array-Darstellung
	///    - LandValues: ['[0, 10, 20]', '[30, 40, 50]', ...]
	///    - Vorteil: Sehr kompakt, eine Zeile pro Row
	///    - Output: MapDtoTest.TestWriteYamlToFileForManualChecks.yaml
	/// 
	/// 2. ByteArrayArrayFlowStyleYamlTypeConverter (alternative):
	///    - Konvertiert byte[][] zu List<List<byte>>
	///    - Produziert echte YAML-Arrays als nested sequences
	///    - LandValues: - [0, 10, 20, 30, 40]
	///                  - [10, 20, 30, 40, 50]
	///    - Vorteil: Echte YAML-Struktur, besser für weitere Bearbeitung
	///    - Output: MapDtoTest.TestWriteYamlWithFlowStyleArrays.yaml
	/// </summary>
	public class MapDtoTest		
	{
		private readonly MapDtoMapper _testee;
		private readonly MockedIMapFactory _mockedMapFactory;
		private readonly MockedITileDtoMapper _mockedTileDtoMapper;

		public MapDtoTest()
		{
			_mockedMapFactory = new MockedIMapFactory();
			_mockedTileDtoMapper = new MockedITileDtoMapper();
			_testee = new MapDtoMapper(_mockedMapFactory, _mockedTileDtoMapper, _terrainSeed: 12345);
		}

		[Fact]
		public void TestWriteYamlToFileForManualChecks()
		{
			// Create a small 5x6 map with test data
			int expectedWidth = 5;
			int expectedHeight = 6;

			// Create sample TileDtos with encoded format
			// The TileCodec encodes each tile into exactly 2 Base64 characters,
			// so a row of 5 tiles becomes 10 characters.
			var tileDtos = new TileDto[expectedWidth, expectedHeight];
			for (int x = 0; x < expectedWidth; x++)
			{
				for (int y = 0; y < expectedHeight; y++)
				{
					tileDtos[x, y] = new TileDto
					{
						Terrain = Terrain.Plains,
						Road = x % 2 == 0,  // Road on even x coordinates
						RailRoad = y % 2 == 0,  // Railroad on even y coordinates
						Irrigation = false,
						Pollution = false,
						Fortress = false,
						Mine = false,
						Hut = false,
						LandValue = (byte)((x + y) * 10)
					};
				}
			}

			var actualMapDto = new MapDto
			{
				TerrainSeed = 12345,
				Tiles = new Map2d<TileDto>(tileDtos)
			};

			Assert.NotNull(actualMapDto);
			Assert.Equal(expectedWidth, actualMapDto.Tiles.Width());
			Assert.Equal(expectedHeight, actualMapDto.Tiles.Height());

			// Write to YAML file for manual inspection
			// The output will show:
			// - TerrainSeed: uint value for map generation
			// - Tiles: array of strings, each string contains Base64-encoded tile data
			//   where 2 characters = 1 tile's properties (terrain + flags)
		// - LandValues: 2D array of land values in compact flow style [[...], [...]]
		YamlWriter.Of(actualMapDto)
			.WithStandard()
			.WithTypeConverter(new MapDtoTileDtoYamlConverter())
			.WithTypeConverter(new ByteArrayArrayYamlTypeConverter())
				.ToFile("MapDtoTest.TestWriteYamlToFileForManualChecks.yaml");
		}
	[Fact]
	public void TestWriteYamlWithFlowStyleArrays()
	{
		// Create a small 5x6 map with test data
		int expectedWidth = 5;
		int expectedHeight = 6;

		var tileDtos = new TileDto[expectedWidth, expectedHeight];
		for (int x = 0; x < expectedWidth; x++)
		{
			for (int y = 0; y < expectedHeight; y++)
			{
				tileDtos[x, y] = new TileDto
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

		var actualMapDto = new MapDto
		{
			TerrainSeed = 12345,
			Tiles = new Map2d<TileDto>(tileDtos)
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
	}	}

	/// <summary>
	/// Mock implementation of IMapFactory for testing purposes.
	/// </summary>
	public class MockedIMapFactory : IMapFactory
	{
		public IMapTiles CreateMap(int width, int height, uint terrainSeed)
		{
			// For testing, return a simple mock map
			return new MockedIMapTiles(width, height);
		}
	}

	/// <summary>
	/// Simple mock implementation of IMapTiles for testing.
	/// </summary>
	public class MockedIMapTiles : IMapTiles
	{
		private readonly ITile[,] _tiles;
		public int Width { get; }
		public int Height { get; }

		public MockedIMapTiles(int width, int height)
		{
			Width = width;
			Height = height;
			_tiles = new ITile[width, height];
		}

		public ITile this[int x, int y] => _tiles[x, y];
	}

	/// <summary>
	/// Mock implementation of ITileDtoMapper for testing purposes.
	/// </summary>
	public class MockedITileDtoMapper : ITileDtoMapper
	{
		private readonly ITile[,] _tiles = new ITile[5, 6]; // Default small map size

		public void SetTileFromDto(TileDto dto, int x, int y)
		{
			// Mock: just store the DTO properties somewhere if needed
			// For testing, we don't need to do anything
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
}

