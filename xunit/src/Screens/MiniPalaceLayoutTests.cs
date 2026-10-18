using System.Linq;
using CivOne.Enums;
using CivOne.Screens.PalaceAssets;
using Xunit;

namespace CivOne.UnitTests.Screens
{
	public class MiniPalaceLayoutTests
	{
		private static readonly PalaceStyle[] Medieval = [.. Enumerable.Repeat(PalaceStyle.Medieval, 9)];
		private static readonly int[] StartPalaceLevels = [-1, -1, -1, 0, 0, 0, -1, -1, -1];
		private static readonly int[] ShiftedLevels = [-1, 2, 3, 0, 4, 0, -1, -1, -1];

		[Fact]
		public void FromPalaceMapsTheStartPalaceToTheOriginalBase()
		{
			MiniPalaceLayout layout = MiniPalaceLayout.FromPalace(new PalaceData());

			int[] levels = [.. Enumerable.Range(0, 9).Select(layout.Level)];
			Assert.Equal(StartPalaceLevels, levels);
			Assert.False(layout.HasVisibleSlot);
		}

		[Fact]
		public void FromPalaceShiftsSectionsByOneSlot()
		{
			PalaceData palace = new();
			palace.SetPalace(0, style: (byte)PalaceStyle.Islamic, level: 2);
			palace.SetPalace(1, style: (byte)PalaceStyle.Islamic, level: 3);
			palace.SetPalace(3, style: (byte)PalaceStyle.Classical, level: 4);

			MiniPalaceLayout layout = MiniPalaceLayout.FromPalace(palace);

			int[] levels = [.. Enumerable.Range(0, 9).Select(layout.Level)];
			Assert.Equal(ShiftedLevels, levels);
			Assert.Equal(PalaceStyle.Islamic, layout.Style(1));
			Assert.Equal(PalaceStyle.Classical, layout.Style(4));
		}

		[Theory]
		[InlineData(1, PreviewPalacePart.Left)]
		[InlineData(2, PreviewPalacePart.WallLeft)]
		[InlineData(3, PreviewPalacePart.WallLeft)]
		[InlineData(4, PreviewPalacePart.Center)]
		[InlineData(5, PreviewPalacePart.WallRight)]
		[InlineData(6, PreviewPalacePart.WallRight)]
		[InlineData(7, PreviewPalacePart.Right)]
		public void FullPalaceUsesEdgesOnSlotsOneAndSeven(int slot, PreviewPalacePart expected)
		{
			MiniPalaceLayout layout = new([-1, 1, 1, 1, 1, 1, 1, 1, -1], Medieval);

			Assert.Equal(expected, layout.PartAt(slot));
		}

		[Theory]
		[InlineData(3, PreviewPalacePart.Left)]
		[InlineData(5, PreviewPalacePart.Right)]
		public void SlotNextToAMissingSectionIsAnEdge(int slot, PreviewPalacePart expected)
		{
			MiniPalaceLayout layout = new([-1, -1, -1, 1, 1, 1, -1, -1, -1], Medieval);

			Assert.Equal(expected, layout.PartAt(slot));
		}

		[Theory]
		[InlineData(3, PreviewPalacePart.WallLeft)]
		[InlineData(5, PreviewPalacePart.WallRight)]
		public void SlotNextToABaseSectionIsAnInnerWall(int slot, PreviewPalacePart expected)
		{
			MiniPalaceLayout layout = new([-1, -1, 0, 1, 1, 1, 0, -1, -1], Medieval);

			Assert.Equal(expected, layout.PartAt(slot));
		}

		[Fact]
		public void OuterSlotsAreInnerWalls()
		{
			MiniPalaceLayout layout = new([1, 1, 1, 1, 1, 1, 1, 1, 1], Medieval);

			Assert.Equal(PreviewPalacePart.WallLeft, layout.PartAt(0));
			Assert.Equal(PreviewPalacePart.WallRight, layout.PartAt(8));
		}

		[Fact]
		public void LevelsAboveFourAreCapped()
		{
			MiniPalaceLayout layout = new([-1, -1, -1, 0, 7, 0, -1, -1, -1], Medieval);

			Assert.Equal(4, layout.Level(4));
		}

		[Fact]
		public void EmptyLayoutHasNoSection()
		{
			Assert.All(Enumerable.Range(0, 9), slot => Assert.Equal(-1, MiniPalaceLayout.Empty.Level(slot)));
		}
	}
}
