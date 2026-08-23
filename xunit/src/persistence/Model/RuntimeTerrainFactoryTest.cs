using System;
using System.Linq;
using CivOne.Enums;
using CivOne.Persistence.Factories;
using CivOne.Tiles;
using Xunit;

namespace CivOne.Persistence.Factories.Tests
{
	public class RuntimeTerrainFactoryTest
	{
		private readonly RuntimeTerrainFactory _testee;

		public RuntimeTerrainFactoryTest()
		{
			_testee = new RuntimeTerrainFactory();
		}

		[Theory]
		[InlineData(Terrain.Forest,     typeof(Forest))]
		[InlineData(Terrain.Swamp,      typeof(Swamp))]
		[InlineData(Terrain.Plains,     typeof(Plains))]
		[InlineData(Terrain.Tundra,     typeof(Tundra))]
		[InlineData(Terrain.River,      typeof(River))]
		[InlineData(Terrain.Grassland1, typeof(Grassland))]
		[InlineData(Terrain.Grassland2, typeof(Grassland))]
		[InlineData(Terrain.Jungle,     typeof(Jungle))]
		[InlineData(Terrain.Hills,      typeof(Hills))]
		[InlineData(Terrain.Mountains,  typeof(Mountains))]
		[InlineData(Terrain.Desert,     typeof(Desert))]
		[InlineData(Terrain.Arctic,     typeof(Arctic))]
		[InlineData(Terrain.Ocean,      typeof(Ocean))]
		public void CreateTileKnownTerrainReturnsCorrectTileType(Terrain terrain, Type expectedType)
		{
			// Act
			ITile tile = _testee.CreateTile(terrain, 0, 0, false);

			// Assert
			Assert.NotNull(tile);
			Assert.IsType(expectedType, tile);
		}

		[Fact]
		public void CreateTileAllDefinedTerrainValuesAreHandledWithoutException()
		{
			// Arrange
			var terrains = Enum.GetValues<Terrain>()
				.Where(t => t != Terrain.None)
				.ToList();

			// Act & Assert – if a new Terrain value is added to the enum but not
			// to RuntimeTerrainFactory the factory will throw, failing this test.
			foreach (Terrain terrain in terrains)
			{
				var actualException = Record.Exception(() => _testee.CreateTile(terrain, 0, 0, false));
				Assert.Null(actualException);
			}
		}

		[Fact]
		public void CreateTileNoneTerrainThrowsArgumentOutOfRangeException()
		{
			// Act & Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => _testee.CreateTile(Terrain.None, 0, 0, false));
		}

		[Fact]
		public void CreateTileUnknownTerrainThrowsArgumentOutOfRangeException()
		{
			// Act & Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => _testee.CreateTile((Terrain)999, 0, 0, false));
		}
	}
}
