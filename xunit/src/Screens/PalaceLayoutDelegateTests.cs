using System.Collections.Generic;
using System.Linq;
using CivOne.Enums;
using CivOne.Screens.PalaceAssets;
using Xunit;

namespace CivOne.UnitTests.Screens
{
	public class PalaceLayoutDelegateTests
	{
		private static readonly int[] StartXs = [88, 113, 135, 185, 207];
		private static readonly PalacePieceKind[] StartKinds =
		[
			PalacePieceKind.Wall, PalacePieceKind.LeftTower, PalacePieceKind.Center, PalacePieceKind.WallShadow, PalacePieceKind.RightTower,
		];
		private static readonly int[] FullWallXs = [41, 65, 89, 113, 185, 209, 233, 257];

		private readonly PalaceLayoutDelegate _testee = new();

		[Fact]
		public void StartPalaceIsPlacedLikeTheOriginal()
		{
			IReadOnlyList<PalacePiece> pieces = _testee.Layout(new PalaceData());

			Assert.Equal(StartXs, pieces.Select(p => p.X).Order());
			Assert.Equal(StartKinds, pieces.Select(p => p.Kind));
			Assert.All(pieces, piece => Assert.Equal(37, piece.Y));
			Assert.All(pieces, piece => Assert.Equal(0, piece.Level));
		}

		[Fact]
		public void FullPalaceHasEndTowersWallsAndCenterAtTheOriginalPositions()
		{
			IReadOnlyList<PalacePiece> pieces = _testee.Layout(Palace(PalaceStyle.Medieval, 1, 1, 1, 1, 1, 1, 1));

			Assert.Equal(16, Single(pieces, PalacePieceKind.LeftTower, y: 39).X);
			Assert.Equal(279, Single(pieces, PalacePieceKind.RightTower, y: 39).X);
			Assert.Equal(135, Single(pieces, PalacePieceKind.Center).X);
			Assert.Equal(FullWallXs, pieces.Where(p => p.Kind is PalacePieceKind.Wall or PalacePieceKind.WallShadow).Select(p => p.X).Order());
		}

		[Fact]
		public void PerspectiveTowersSitBehindTheInnerNeighbourSevenPixelsInAndTwoPixelsHigher()
		{
			IReadOnlyList<PalacePiece> pieces = _testee.Layout(Palace(PalaceStyle.Medieval, 1, 1, 1, 1, 1, 1, 1));

			PalacePiece left = Single(pieces, PalacePieceKind.LeftTower, y: 37);
			PalacePiece right = Single(pieces, PalacePieceKind.RightTower, y: 37);
			Assert.Equal(48, left.X);
			Assert.Equal(240, right.X);

			// Drawn before the walls of the inner section, so the walls cover them.
			int leftIndex = IndexOf(pieces, left);
			int wallIndex = IndexOf(pieces, pieces.First(p => p.X == 41));
			Assert.True(leftIndex < wallIndex);
		}

		[Fact]
		public void PerspectiveTowerNeedsTheSameStyleAsTheEnd()
		{
			PalaceData palace = Palace(PalaceStyle.Medieval, 3, 3, 1, 1, 1, 3, 3);
			palace.SetPalace(0, (byte)PalaceStyle.Classical, 3);
			palace.SetPalace(5, (byte)PalaceStyle.Islamic, 3);
			palace.SetPalace(6, (byte)PalaceStyle.Islamic, 3);

			IReadOnlyList<PalacePiece> pieces = _testee.Layout(palace);

			Assert.Single(pieces, p => p.Kind == PalacePieceKind.LeftTower);
			Assert.Equal(2, pieces.Count(p => p.Kind == PalacePieceKind.RightTower));
		}

		[Fact]
		public void HigherLevelsSitLower()
		{
			PalacePiece low = Single(_testee.Layout(Palace(PalaceStyle.Medieval, 0, 0, 1, 1, 1, 0, 0)), PalacePieceKind.Center);
			PalacePiece high = Single(_testee.Layout(Palace(PalaceStyle.Medieval, 0, 0, 1, 4, 1, 0, 0)), PalacePieceKind.Center);

			Assert.Equal(38, low.Y);
			Assert.Equal(41, high.Y);
		}

		[Fact]
		public void ClassicalCenterShiftsTheRowByOnePixel()
		{
			PalaceData palace = Palace(PalaceStyle.Medieval, 0, 0, 1, 1, 1, 0, 0);
			palace.SetPalace(3, (byte)PalaceStyle.Classical, 1);

			IReadOnlyList<PalacePiece> pieces = _testee.Layout(palace);

			// The row starts one pixel further right and the classical center takes one pixel less on each side.
			Assert.Equal(135, Single(pieces, PalacePieceKind.Center).X);
			Assert.Equal(184, pieces.Where(p => p.Kind == PalacePieceKind.WallShadow).Max(p => p.X));
		}

		[Fact]
		public void ClassicalLeftEndTowerIsDrawnLast()
		{
			PalaceData palace = Palace(PalaceStyle.Classical, 4, 1, 1, 1, 1, 0, 0);

			IReadOnlyList<PalacePiece> pieces = _testee.Layout(palace);

			PalacePiece last = pieces[^1];
			Assert.Equal(PalacePieceKind.LeftTower, last.Kind);
			Assert.Equal(12, last.X);
		}

		[Theory]
		[InlineData(1, 2)] // no wings: "1" moves inwards to the first base section
		[InlineData(7, 4)]
		[InlineData(4, 3)]
		public void SectionNumberMovesInwardsUntilASectionCanBeBuilt(int number, int expected)
		{
			Assert.Equal(expected, _testee.ResolveSection(new PalaceData(), number));
		}

		[Fact]
		public void SectionNumberStopsAtAnOuterSectionNextToABuiltOne()
		{
			PalaceData palace = Palace(PalaceStyle.Medieval, 0, 0, 1, 0, 0, 0, 0);

			Assert.Equal(1, _testee.ResolveSection(palace, 2));
		}

		[Fact]
		public void FullSectionIsRefused()
		{
			PalaceData palace = Palace(PalaceStyle.Medieval, 0, 0, 4, 0, 0, 0, 0);

			Assert.Null(_testee.ResolveSection(palace, 3));
		}

		[Theory]
		[InlineData(0, 100, '1')]
		[InlineData(40, 100, '2')]
		[InlineData(319, 100, '7')]
		[InlineData(0, 154, 'A')]
		[InlineData(200, 160, 'B')]
		[InlineData(319, 199, 'C')]
		public void MouseChoosesLikeTheOriginal(int x, int y, char expected)
		{
			Assert.Equal(expected, _testee.ChoiceAt(x, y));
		}

		[Fact]
		public void PreviewOfALeftEndShowsTowerAndWall()
		{
			PalaceData palace = Palace(PalaceStyle.Medieval, 0, 0, 1, 1, 1, 0, 0);

			IReadOnlyList<PalacePiece> pieces = _testee.Preview(palace, 1, PalaceStyle.Islamic, 1, 36);

			Assert.Equal(2, pieces.Count);
			Assert.All(pieces, piece => Assert.Equal(PalaceStyle.Islamic, piece.Style));
			Assert.Contains(pieces, p => p.Kind == PalacePieceKind.LeftTower && p.X == 36);
		}

		private static PalaceData Palace(PalaceStyle style, params byte[] levels)
		{
			PalaceData palace = new();
			for (int k = 0; k < levels.Length; k++)
			{
				palace.SetPalace(k, (byte)style, levels[k]);
			}
			return palace;
		}

		private static PalacePiece Single(IReadOnlyList<PalacePiece> pieces, PalacePieceKind kind, int? y = null)
			=> pieces.Single(p => p.Kind == kind && (y == null || p.Y == y));

		private static int IndexOf(IReadOnlyList<PalacePiece> pieces, PalacePiece piece)
			=> pieces.ToList().IndexOf(piece);
	}
}
