// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Enums;
using CivOne.Graphics;

namespace CivOne.Screens.PalaceAssets
{
	public sealed class PreviewPalaceRenderer(PreviewPalaceResourcesWrapper resources) : IPreviewPalaceRenderer
	{
		private const int PART_WIDTH = 7;
		private const int PART_HEIGHT = 13;

		// Maps PalaceData index (0–6) to the corresponding sprite part.
		// Indices 1 and 2 share the WallLeft sprite; indices 4 and 5 share the WallRight sprite.
		private static readonly PreviewPalacePart[] IndexToPart =
		[
			PreviewPalacePart.Left,
			PreviewPalacePart.WallLeft,
			PreviewPalacePart.WallLeft,
			PreviewPalacePart.Center,
			PreviewPalacePart.WallRight,
			PreviewPalacePart.WallRight,
			PreviewPalacePart.Right,
		];

		public IBitmap RenderPalace(IPalaceData palace)
		{
			int partCount = CountRenderParts(palace);
			if (partCount == 0)
			{
				return new Picture(1, 1);
			}

			Picture result = new(partCount * PART_WIDTH, PART_HEIGHT);
			int xPos = 0;

			for (int i = 0; i < 7; i++)
			{
				byte level = palace.GetPalaceLevel(i);
				PalaceStyle style = palace.GetPalaceStyle(i);

				// Towers are always shown when the adjacent wall is built, even if the tower itself isn't upgraded yet.
				if (i == 0 && palace.GetPalaceLevel(1) > 0)
				{
					level = palace.GetPalaceLevel(1);
					style = palace.GetPalaceStyle(1);
				}
				else if (i == 6 && palace.GetPalaceLevel(5) > 0)
				{
					level = palace.GetPalaceLevel(5);
					style = palace.GetPalaceStyle(5);
				}

				if (level == 0) continue;

				const int baseOffset = -1;

				result.AddLayer(resources.GetPreviewPart(IndexToPart[i], level, style), xPos, baseOffset);
				xPos += PART_WIDTH;
			}

			return result;
		}

		private static int CountRenderParts(IPalaceData palace)
		{
			int count = 0;
			for (int i = 0; i < 7; i++)
			{
				byte level = palace.GetPalaceLevel(i);
				if (level > 0)
				{
					count++;
					continue;
				}
				if (i == 0 && palace.GetPalaceLevel(1) > 0) count++;
				else if (i == 6 && palace.GetPalaceLevel(5) > 0) count++;
			}
			return count;
		}

		public int GetMaxPalaceHeight(IPalaceData palace)
		{
			var style = palace.GetPalaceStyle(3);
			byte level = palace.GetPalaceLevel(3);

			if (level == 0)
			{
				return 0;
			}

			const int baseClassic = 14;
			const int baseMedieval = 30;
			const int baseIslamic = 46;

			return (level, style) switch
			{
				(1, PalaceStyle.Classical) => baseClassic - 9 + 1, // = 6
				(2, PalaceStyle.Classical) => baseClassic - 8 + 1, // = 7
				(3, PalaceStyle.Classical) => baseClassic - 7 + 1, // = 8
				(4, PalaceStyle.Classical) => baseClassic - 6 + 1, // = 9

				(1, PalaceStyle.Medieval) => baseMedieval - 25 + 1, // = 6
				(2, PalaceStyle.Medieval) => baseMedieval - 24 + 1, // = 7
				(3, PalaceStyle.Medieval) => baseMedieval - 23 + 1, // = 8
				(4, PalaceStyle.Medieval) => baseMedieval - 22 + 1, // = 9

				(1, PalaceStyle.Islamic) => baseIslamic - 37 + 1, // = 10
				(2, PalaceStyle.Islamic) => baseIslamic - 36 + 1, // = 11
				(3, PalaceStyle.Islamic) => baseIslamic - 34 + 1, // = 13
				(4, PalaceStyle.Islamic) => baseIslamic - 33 + 1, // = 14
				(_, _) => throw new ArgumentOutOfRangeException($"Unexpected palace style/level combination: style={style}, level={level}")
			};
		}
	}
}
