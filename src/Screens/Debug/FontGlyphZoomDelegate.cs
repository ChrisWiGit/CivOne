using System;
using CivOne.IO;

namespace CivOne.Screens.Debug
{
	/// <summary>
	/// Holds the current zoom level and scales glyph bitmaps by it.
	/// <br/>
	/// Level 0 is the unscaled original, positive levels enlarge by whole pixels and negative levels
	/// shrink, so that more characters fit on screen.
	/// Scaling samples pixels instead of filtering them, which keeps the glyph shape recognisable;
	/// when shrinking, a target pixel is set as soon as any source pixel of its block is set, so thin
	/// strokes do not vanish completely.
	/// </summary>
	internal sealed class FontGlyphZoomDelegate
	{
		private readonly int _minFactor;
		private readonly int _maxFactor;

		/// <summary>
		/// Gets the current zoom level. 0 means unscaled.
		/// </summary>
		public int Factor { get; private set; }

		/// <summary>
		/// Gets the numerator of the current scale ratio.
		/// </summary>
		private int Multiplier => Factor > 0 ? Factor + 1 : 1;

		/// <summary>
		/// Gets the denominator of the current scale ratio.
		/// </summary>
		private int Divisor => Factor < 0 ? -Factor + 1 : 1;

		/// <summary>
		/// Creates the delegate.
		/// </summary>
		/// <param name="minFactor">Lowest allowed zoom level, at most 0.</param>
		/// <param name="maxFactor">Highest allowed zoom level, at least 0.</param>
		public FontGlyphZoomDelegate(int minFactor = -4, int maxFactor = 4)
		{
			_minFactor = Math.Min(0, minFactor);
			_maxFactor = Math.Max(0, maxFactor);
		}

		/// <summary>
		/// Changes the zoom level within the allowed range.
		/// </summary>
		/// <param name="delta">Steps to add to the current level.</param>
		/// <returns><see langword="true"/> when the level actually changed.</returns>
		public bool Change(int delta)
		{
			int factor = Math.Clamp(Factor + delta, _minFactor, _maxFactor);
			if (factor == Factor)
			{
				return false;
			}

			Factor = factor;
			return true;
		}

		/// <summary>
		/// Scales a pixel length by the current zoom level.
		/// </summary>
		/// <param name="length">Unscaled length in pixels.</param>
		/// <returns>The scaled length, at least 1 for a positive input.</returns>
		public int ScaleLength(int length)
		{
			if (length <= 0)
			{
				return 0;
			}

			return Math.Max(1, length * Multiplier / Divisor);
		}

		/// <summary>
		/// Returns a copy of <paramref name="source"/> scaled by the current zoom level.
		/// </summary>
		/// <param name="source">Glyph or text bitmap to scale.</param>
		/// <returns>A new bitmap owned by the caller.</returns>
		public Bytemap Scale(Bytemap source)
		{
			if (source == null || source.Width <= 0 || source.Height <= 0)
			{
				return new(1, 1);
			}

			int multiplier = Multiplier;
			int divisor = Divisor;
			int width = Math.Max(1, source.Width * multiplier / divisor);
			int height = Math.Max(1, source.Height * multiplier / divisor);
			Bytemap output = new(width, height);

			for (int y = 0; y < source.Height; y++)
			{
				int targetTop = y * multiplier / divisor;
				int targetBottom = Math.Max(targetTop + 1, (y + 1) * multiplier / divisor);
				if (targetTop >= height)
				{
					break;
				}

				for (int x = 0; x < source.Width; x++)
				{
					byte pixel = source[x, y];
					if (pixel == 0)
					{
						continue;
					}

					int targetLeft = x * multiplier / divisor;
					int targetRight = Math.Max(targetLeft + 1, (x + 1) * multiplier / divisor);
					for (int targetY = targetTop; targetY < targetBottom && targetY < height; targetY++)
					{
						for (int targetX = targetLeft; targetX < targetRight && targetX < width; targetX++)
						{
							output[targetX, targetY] = pixel;
						}
					}
				}
			}

			return output;
		}
	}
}
