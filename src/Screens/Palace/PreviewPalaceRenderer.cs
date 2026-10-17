using System;
using CivOne.Enums;
using CivOne.Graphics;

namespace CivOne.Screens.PalaceAssets
{
	/// <summary>
	/// Draws the mini palace like the original game: 9 slots on a fixed 7 pixel grid, only slots above level 0,
	/// with sprite stage <c>level - 1</c>. Empty slots stay gaps.
	/// </summary>
	public sealed class PreviewPalaceRenderer(PreviewPalaceResourcesWrapper resources) : IPreviewPalaceRenderer
	{
		private const int PART_HEIGHT = 13;
		private const int BASE_OFFSET = -1;

		/// <inheritdoc />
		public IBitmap RenderPalace(MiniPalaceLayout palace)
		{
			ArgumentNullException.ThrowIfNull(palace);

			if (!palace.HasVisibleSlot)
			{
				return new Picture(1, 1);
			}

			Picture result = new(MiniPalaceLayout.Width, PART_HEIGHT);
			for (int slot = 0; slot < MiniPalaceLayout.SlotCount; slot++)
			{
				if (!palace.IsVisible(slot))
				{
					continue;
				}

				Picture part = resources.GetPreviewPart(palace.PartAt(slot), palace.Level(slot), palace.Style(slot));
				result.AddLayer(part, slot * MiniPalaceLayout.SlotWidth, BASE_OFFSET);
			}

			return result;
		}

		/// <inheritdoc />
		public int GetMaxPalaceHeight(MiniPalaceLayout palace)
		{
			ArgumentNullException.ThrowIfNull(palace);

			int height = 0;
			for (int slot = 0; slot < MiniPalaceLayout.SlotCount; slot++)
			{
				if (palace.IsVisible(slot))
				{
					height = Math.Max(height, PartHeight(palace.Level(slot), palace.Style(slot)));
				}
			}

			return height;
		}

		private static int PartHeight(int level, PalaceStyle style)
		{
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
				(_, _) => throw new ArgumentOutOfRangeException(nameof(level), $"Unexpected palace style/level combination: style={style}, level={level}")
			};
		}
	}
}
