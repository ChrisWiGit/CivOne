// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using CivOne.Enums;

namespace CivOne.Graphics
{
	public readonly record struct PixelRange(int StartInclusive, int EndInclusive)
	{
		public int Length => EndInclusive - StartInclusive + 1;
	}

	public readonly record struct PalacePartSourceRect(int X, int Y, int Width, int Height);

	public readonly record struct PalacePartSourceSelection(
		int OffsetX,
		int OffsetY,
		int MiddleShadowX,
		int MiddleNormalX,
		int WallFirstHalfX,
		int RightTowerWallX);

	public sealed record PalaceQuadrant(
		string Name,
		PixelRange X,
		PixelRange Y);

	public sealed record PalaceMainSlices(
		PixelRange Center,
		PixelRange Separator,
		PixelRange MiddleShadow,
		PixelRange MiddleNormal,
		PixelRange LeftTower,
		PixelRange RightTower);

	public sealed record PalaceTowerEndSlices(
	PixelRange LeftEnd,
	PixelRange RightEnd,
	int DrawOffsetY);

	public sealed record PalacePictureLayout(
		int Level,
		PalaceQuadrant Q1,
		PalaceQuadrant Q2,
		PalaceQuadrant Q3,
		PalaceQuadrant Q4,
		PalaceMainSlices Q1Slices,
		PalaceMainSlices Q2Slices,
		PalaceMainSlices Q3Slices,
		PalaceTowerEndSlices Q4Slices,
		bool ClassicalMainContainsTowerSegments);

	/// <summary>
	/// Sprite layout for palace/castle graphics.
	/// The layout consists of 4 quadrants (each 160x100 pixels):
	///	 X: 0-159              X: 160-319
	///   ┌──────────────────┬──────────────────┐
	///   │ Q1-Medieval      │ Q3-ClassicalMain │  Y: 0-99
	///   │ (Medieval)       │ (Classical)      │
	///   ├──────────────────┼──────────────────┤
	///   │ Q2-Oriental      │ Q4-ClassicalEnds │  Y: 100-199
	///   │ (Oriental)       │ (Classical Ends) │
	///   └──────────────────┴──────────────────┘
	/// Q1/Q2/Q3 share the same public X partitioning (MainSlices):
	/// - Center (0-51), Separator (52), MiddleShadow (53-76), MiddleNormal (78-101),
	///   LeftTower (103-130), RightTower
	public sealed class PalaceSpriteLayout : IPalaceSpriteLayout
	{
		private static readonly PalaceQuadrant Quadrant1 = new("Q1-Medieval", new PixelRange(0, 159), new PixelRange(0, 99));
		private static readonly PalaceQuadrant Quadrant2 = new("Q2-Oriental", new PixelRange(0, 159), new PixelRange(100, 199));
		private static readonly PalaceQuadrant Quadrant3 = new("Q3-ClassicalMain", new PixelRange(160, 319), new PixelRange(0, 99));
		private static readonly PalaceQuadrant Quadrant4 = new("Q4-ClassicalEnds", new PixelRange(160, 319), new PixelRange(100, 199));

		// Q1/Q2/Q3 share the same public x partitioning.
		private static readonly PalaceMainSlices MainSlices = new(
			Center: new PixelRange(0, 51),
			Separator: new PixelRange(52, 52),
			MiddleShadow: new PixelRange(53, 76),
			MiddleNormal: new PixelRange(78, 101),
			LeftTower: new PixelRange(103, 130),
			RightTower: new PixelRange(132, 158));

		// Q4 is special and contains only outer classical end pieces.
		private static readonly PalaceTowerEndSlices ClassicalTowerEndSlices = new(
			LeftEnd: new PixelRange(160, 194),
			RightEnd: new PixelRange(196, 230),
			DrawOffsetY: 0);

		public static IReadOnlyDictionary<int, PalacePictureLayout> Castle1To4 { get; } =
		new Dictionary<int, PalacePictureLayout>
		{
			[1] = CreateLayout(level: 1, classicalMainContainsTowerSegments: false),
			[2] = CreateLayout(level: 2, classicalMainContainsTowerSegments: true),
			[3] = CreateLayout(level: 3, classicalMainContainsTowerSegments: false),
			[4] = CreateLayout(level: 4, classicalMainContainsTowerSegments: false)
		};

		private static PalacePictureLayout CreateLayout(int level, bool classicalMainContainsTowerSegments)
			=> new(
				Level: level,
				Q1: Quadrant1,
				Q2: Quadrant2,
				Q3: Quadrant3,
				Q4: Quadrant4,
				Q1Slices: MainSlices,
				Q2Slices: MainSlices,
				Q3Slices: MainSlices,
				Q4Slices: ClassicalTowerEndSlices,
				ClassicalMainContainsTowerSegments: classicalMainContainsTowerSegments);

		public PalacePictureLayout GetLayout(int level)
		{
			if (Castle1To4.TryGetValue(level, out PalacePictureLayout layout))
			{
				return layout;
			}

			// Castle0 uses the legacy/base composition and falls back to a default split definition.
			return CreateLayout(level: 0, classicalMainContainsTowerSegments: false);
		}

		/// <summary>
		/// Returns the source rectangle for a given palace part, style and layout.
		/// These numbers come from the original Civ1 sprites and are hardcoded here based on the observed layout of the source images.
		/// The source rect is the area within the source castle image (CASTLE0-4) that should be used for the given part/style/layout combination.
		/// </summary>
		public PalacePartSourceRect GetPartSourceRect(PalacePart part, PalaceStyle style, PalacePictureLayout layout)
			=> (part, style) switch
			{
				(PalacePart.LeftTower, PalaceStyle.Classical) => new(layout.Q4Slices.LeftEnd.StartInclusive, layout.Q4.Y.StartInclusive + 1, layout.Q4Slices.LeftEnd.Length, 99),
				(PalacePart.LeftTower, PalaceStyle.Islamic) => new(104, 101, 27, 99),
				(PalacePart.LeftTower, _) => new(104, 1, 27, 99),
				(PalacePart.RightTower, PalaceStyle.Classical) => new(layout.Q4Slices.RightEnd.StartInclusive, layout.Q4.Y.StartInclusive + 1, layout.Q4Slices.RightEnd.Length, 99),
				(PalacePart.RightTower, PalaceStyle.Islamic) => new(132, 101, 27, 99),
				(PalacePart.RightTower, _) => new(132, 1, 27, 99),
				(PalacePart.LeftTowerWall, PalaceStyle.Classical) => new(layout.Q4Slices.LeftEnd.StartInclusive, layout.Q4.Y.StartInclusive + 1, layout.Q4Slices.LeftEnd.Length, 99),
				(PalacePart.LeftTowerWall, PalaceStyle.Islamic) => new(104, 101, 27, 99),
				(PalacePart.LeftTowerWall, _) => new(104, 1, 27, 99),
				(PalacePart.RightTowerWall, PalaceStyle.Classical) => new(layout.Q4Slices.RightEnd.StartInclusive, layout.Q4.Y.StartInclusive + 1, layout.Q4Slices.RightEnd.Length, 99),
				(PalacePart.RightTowerWallShadow, PalaceStyle.Classical) => new(layout.Q4Slices.RightEnd.StartInclusive, layout.Q4.Y.StartInclusive + 1, layout.Q4Slices.RightEnd.Length, 99),
				(PalacePart.RightTowerWall, PalaceStyle.Islamic) => new(132, 101, 27, 99),
				(PalacePart.RightTowerWallShadow, PalaceStyle.Islamic) => new(132, 101, 27, 99),
				(PalacePart.RightTowerWall, _) => new(132, 1, 27, 99),
				(PalacePart.RightTowerWallShadow, _) => new(132, 1, 27, 99),
				(PalacePart.Center, PalaceStyle.Islamic) => new(0, 101, 52, 99),
				(PalacePart.Center, _) => new(0, 1, 52, 99),
				_ => throw new ArgumentException($"Unknown part/style combination: {part}, {style}")
			};

		/// <summary>
		/// Returns for a specific palace part, style and layout the relevant source offsets and x-coordinates for the middle shadow/normal segments.
		/// E.g., for the wall part, the middle shadow/normal segments are used to create a seamless tiling effect when building walls of arbitrary width.
		/// </summary>
		/// <param name="style">The style of the palace (e.g., Classical, Islamic).</param>
		/// <param name="part">The specific part of the palace (e.g., LeftTower, RightTower).</param>
		/// <param name="layout">The layout of the palace for the given level.</param>
		/// <returns>A PalacePartSourceSelection object containing the relevant offsets and x-coordinates.</returns>
		public PalacePartSourceSelection GetSpriteCoordinatesForPart(PalaceStyle style, PalacePart part, PalacePictureLayout layout)
		{
			// Determine quadrant and slices based on style
			var (quadrant, slices) = style switch
			{
				PalaceStyle.Classical => (layout.Q3, layout.Q3Slices),
				PalaceStyle.Islamic => (layout.Q2, layout.Q2Slices),
				_ => (layout.Q1, layout.Q1Slices)
			};

			int offsetX = quadrant.X.StartInclusive;
			int offsetY = quadrant.Y.StartInclusive;
			int middleShadowX = offsetX + slices.MiddleShadow.StartInclusive;
			int middleNormalX = offsetX + slices.MiddleNormal.StartInclusive;

			return new(
				OffsetX: offsetX,
				OffsetY: offsetY + 1, // +1 to account for the 1 pixel transparent border at the top of the source images
				MiddleShadowX: middleShadowX,
				MiddleNormalX: middleNormalX,
				WallFirstHalfX: part == PalacePart.WallShadow ? middleShadowX : middleNormalX,
				RightTowerWallX: part == PalacePart.RightTowerWall ? middleNormalX : middleShadowX);
		}
	}
}
