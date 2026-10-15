using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CivOne.Screens.Reports
{
	/// <summary>
	/// Lays out the civilization score screen like the original game.
	/// </summary>
	/// <remarks>
	/// Citizen icons advance 8 px and wrap after x 292 to x 16 or 20 (alternating by row); icons below y 160 are cut.
	/// Wonders sit in two columns, 152 px apart, rows <c>clamp(lineHeight + 2, 8, 99)</c> apart; wonders below y 180 are cut.
	/// When the total line would land at y 186 or lower, the line height shrinks by one and the layout starts over.
	/// Like the original, it may shrink down to 0, which stacks every citizen icon row on the first one;
	/// the total line then always fits, because the wonder rows stay at least 8 px apart.
	/// The original's starting line height is a data segment value that is not known; 8 is assumed,
	/// matching the 8-px citizen icons.
	/// </remarks>
	internal sealed class CivilizationScoreLayoutDelegate
	{
		/// <summary>
		/// The line height the layout starts with.
		/// </summary>
		public const int InitialLineHeight = 8;

		private const int TopY = 26;
		private const int LeftX = 16;
		private const int IconStep = 8;
		private const int IconMaxX = 292;
		private const int IconMaxY = 160;
		private const int WonderColumnStep = 152;
		private const int WonderMaxX = 300;
		private const int WonderMaxY = 180;
		private const int TextLineStep = 10;
		private const int TextMaxY = 184;
		private const int TotalMaxY = 186;
		private const int Offscreen = 999;

		/// <summary>
		/// Computes the layout, shrinking the line height until the total line fits.
		/// </summary>
		/// <param name="citizenIconCount">The number of citizen icons over all cities.</param>
		/// <param name="wonderCount">The number of wonders held.</param>
		/// <param name="hasSpaceShipLine">Whether the spaceship line is shown.</param>
		/// <param name="hasOtherTermsLine">Whether the pollution/peace/future tech line is shown.</param>
		/// <returns>The positions of every element that fits.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public CivilizationScoreLayout Calculate(int citizenIconCount, int wonderCount, bool hasSpaceShipLine, bool hasOtherTermsLine)
		{
			CivilizationScoreLayout layout;
			int lineHeight = InitialLineHeight;
			do
			{
				layout = Calculate(citizenIconCount, wonderCount, hasSpaceShipLine, hasOtherTermsLine, lineHeight--);
			}
			while (layout.TotalY >= TotalMaxY && lineHeight >= 0);

			return layout;
		}

		private static CivilizationScoreLayout Calculate(int citizenIconCount, int wonderCount, bool hasSpaceShipLine, bool hasOtherTermsLine, int lineHeight)
		{
			List<(int X, int Y)> icons = [];
			int x = Offscreen;
			int y = TopY;
			for (int i = 0; i < citizenIconCount; i++)
			{
				x += IconStep;
				if (x > IconMaxX)
				{
					y += lineHeight;
					x = ((y >> 1) & 4) + LeftX;
				}
				if (y <= IconMaxY)
				{
					icons.Add((x, y));
				}
			}

			int achievementsY = y + 16;
			y = achievementsY;
			List<(int, int, int, int)> wonders = [];
			x = Offscreen;
			for (int i = 0; i < wonderCount; i++)
			{
				x += WonderColumnStep;
				if (x > WonderMaxX)
				{
					y += Math.Clamp(lineHeight + 2, 8, 99);
					x = LeftX;
				}
				if (y < WonderMaxY)
				{
					wonders.Add((x, y - 2, x + 20, y));
				}
			}

			y += TextLineStep;
			int? spaceShipY = null;
			if (hasSpaceShipLine)
			{
				if (y < TextMaxY)
				{
					spaceShipY = y;
				}
				y += TextLineStep;
			}

			int? otherTermsY = null;
			if (hasOtherTermsLine && y < TextMaxY)
			{
				otherTermsY = y;
				y += TextLineStep;
			}

			return new CivilizationScoreLayout(TopY, icons, achievementsY, wonders, spaceShipY, otherTermsY, y);
		}
	}
}
