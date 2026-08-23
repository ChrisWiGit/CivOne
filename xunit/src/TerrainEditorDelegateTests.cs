using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Persistence.Model;
using CivOne.Screens.GamePlayPanels;
using CivOne.Tiles;
using Xunit;

namespace CivOne.UnitTests
{
	public class TerrainEditorDelegateTests
	{
		[Fact]
		public void BrushSizeSequenceIncludesTwoAndMatchesRequirement()
		{
			// Arrange
			TerrainEditorDelegate testee = new();
			int[] expected = [1, 2, 3, 5, 7, 9, 11, 13, 15];

			// Act + Assert
			Assert.Equal(expected.Length, testee.BrushSizeCount);
			for (int i = 0; i < expected.Length; i++)
			{
				Assert.Equal(expected[i], testee.GetBrushSize(i));
			}
		}

		[Fact]
		public void SetStartPositionReturnsFalseForBarbarians()
		{
			MockedMapEditor mapEditor = new();
			TerrainEditorDelegate testee = new(mapEditor);

			bool result = testee.SetStartPosition(5, 5, Civilization.Barbarians);

			Assert.False(result);
			Assert.Empty(mapEditor.SetStartPositionCalls);
		}

		[Fact]
		public void SetStartPositionReturnsFalseWhenTileIsMissing()
		{
			MockedMapEditor mapEditor = new();
			TerrainEditorDelegate testee = new(mapEditor);

			bool result = testee.SetStartPosition(5, 5, Civilization.Romans);

			Assert.False(result);
			Assert.Empty(mapEditor.SetStartPositionCalls);
		}

		[Fact]
		public void SetStartPositionReturnsFalseWhenTileIsOcean()
		{
			MockedMapEditor mapEditor = new();
			mapEditor.SetTile(5, 5, new Ocean(5, 5, false));
			TerrainEditorDelegate testee = new(mapEditor);

			bool result = testee.SetStartPosition(5, 5, Civilization.Romans);

			Assert.False(result);
			Assert.Empty(mapEditor.SetStartPositionCalls);
		}

		[Fact]
		public void SetStartPositionSetsPositionForValidLandTile()
		{
			MockedMapEditor mapEditor = new();
			mapEditor.SetTile(5, 5, new MockedGrassland(5, 5));
			TerrainEditorDelegate testee = new(mapEditor);

			bool result = testee.SetStartPosition(5, 5, Civilization.Romans);

			Assert.True(result);
			(Civilization Civilization, MapLocation Location) call = Assert.Single(mapEditor.SetStartPositionCalls);
			Assert.Equal(Civilization.Romans, call.Civilization);
			Assert.Equal(5u, call.Location.X);
			Assert.Equal(5u, call.Location.Y);
		}

		[Fact]
		public void RemoveStartPositionAtRemovesMatchingCivilizationAndReturnsTrue()
		{
			MockedMapEditor mapEditor = new();
			mapEditor.SeedStartPosition(Civilization.Romans, new MapLocation(5, 5));
			ICivilization[] civilizations = [.. MockedICivilization.Mock(1)]; // Id 1 -> Civilization.Romans
			TerrainEditorDelegate testee = new(mapEditor, civilizations);

			bool result = testee.RemoveStartPositionAt(5, 5);

			Assert.True(result);
			Civilization removed = Assert.Single(mapEditor.RemoveStartPositionCalls);
			Assert.Equal(Civilization.Romans, removed);
		}

		[Fact]
		public void RemoveStartPositionAtReturnsFalseWhenNoCivilizationAtLocation()
		{
			MockedMapEditor mapEditor = new();
			mapEditor.SeedStartPosition(Civilization.Romans, new MapLocation(1, 1));
			ICivilization[] civilizations = [.. MockedICivilization.Mock(1)];
			TerrainEditorDelegate testee = new(mapEditor, civilizations);

			bool result = testee.RemoveStartPositionAt(5, 5);

			Assert.False(result);
			Assert.Empty(mapEditor.RemoveStartPositionCalls);
		}
	}
}
