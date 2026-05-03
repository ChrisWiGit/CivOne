// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;

namespace CivOne.Graphics
{
	internal readonly record struct PixelRange(int StartInclusive, int EndInclusive)
	{
		public int Length => EndInclusive - StartInclusive + 1;
	}

	internal sealed record PalaceQuadrant(
		string Name,
		PixelRange X,
		PixelRange Y);

	internal sealed record PalaceMainSlices(
		PixelRange Center,
		PixelRange Separator,
			PixelRange MiddleShadow,
			PixelRange MiddleNormal,
		PixelRange LeftTower,
		PixelRange RightTower);

	internal sealed record PalaceTowerEndSlices(
		PixelRange LeftEnd,
		PixelRange RightEnd,
		int DrawOffsetY);

	internal sealed record PalacePictureLayout(
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

	internal static class PalaceSpriteLayout
	{
		private static readonly PalaceQuadrant Quadrant1 = new("Q1-Medieval", new PixelRange(0, 159), new PixelRange(0, 99));
		private static readonly PalaceQuadrant Quadrant2 = new("Q2-Oriental", new PixelRange(0, 159), new PixelRange(100, 199));
		private static readonly PalaceQuadrant Quadrant3 = new("Q3-ClassicalMain", new PixelRange(160, 319), new PixelRange(0, 99));
		private static readonly PalaceQuadrant Quadrant4 = new("Q4-ClassicalEnds", new PixelRange(160, 319), new PixelRange(100, 199));

		// Q1/Q2/Q3 share the same internal x partitioning.
		private static readonly PalaceMainSlices MainSlices = new(
			Center: new PixelRange(0, 51),
			Separator: new PixelRange(52, 52),
			MiddleShadow: new PixelRange(53, 76),
			MiddleNormal: new PixelRange(77, 100),
			LeftTower: new PixelRange(104, 131),
			RightTower: new PixelRange(132, 159));

		// Q4 is special and contains only outer classical end pieces.
		private static readonly PalaceTowerEndSlices ClassicalTowerEndSlices = new(
			LeftEnd: new PixelRange(160, 193),
			RightEnd: new PixelRange(196, 228),
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

		public static PalacePictureLayout GetLayout(int level)
		{
			if (Castle1To4.TryGetValue(level, out PalacePictureLayout layout))
			{
				return layout;
			}

			// Castle0 uses the legacy/base composition and falls back to a default split definition.
			return CreateLayout(level: 0, classicalMainContainsTowerSegments: false);
		}
	}
}