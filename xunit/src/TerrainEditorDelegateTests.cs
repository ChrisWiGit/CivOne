using CivOne.Screens.GamePlayPanels;
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
	}
}