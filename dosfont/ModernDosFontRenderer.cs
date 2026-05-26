using System;

namespace DosFont
{
    /// <summary>
    /// Renders ModernDos 8x16 glyphs onto any pixel target via a delegate.
    /// Scale support: each source pixel becomes a scale×scale block.
    /// </summary>
    /// <example>
    /// <code>
    ///  ModernDosFontRenderer.DrawString(text, x, y, scale, (px, py) => bytemap[px, py] = color);
    /// </code>
    /// </example>
    public static class ModernDosFontRenderer
    {
        /// <summary>Scaled width of one glyph.</summary>
        public static int GlyphWidth(int scale = 1) => ModernDos8X16.GlyphWidth * scale;

        /// <summary>Scaled height of one glyph.</summary>
        public static int GlyphHeight(int scale = 1) => ModernDos8X16.GlyphHeight * scale;

        /// <summary>Scaled width of a string.</summary>
        public static int StringWidth(string text, int scale = 1)
            => (text?.Length ?? 0) * ModernDos8X16.GlyphWidth * scale;

        // ----------------------------------------------------------------
        // Single glyph — by CP437 code
        // ----------------------------------------------------------------

        /// <summary>
        /// Draws one glyph at (destX, destY).
        /// <paramref name="setPixel"/> receives the final destination coordinates
        /// and is called only for set (foreground) pixels.
        /// </summary>
        public static void DrawGlyph(byte cp437, int destX, int destY, int scale, Action<int, int> setPixel)
        {
            if (scale < 1) scale = 1;

            for (int row = 0; row < ModernDos8X16.GlyphHeight; row++)
			{
				byte mask = ModernDos8X16.FontData[cp437, row];
				if (mask == 0) continue;

				int baseY = destY + row * scale;

				DrawGlyphRow(destX, scale, setPixel, mask, baseY);
			}
		}

		private static void DrawGlyphRow(int destX, int scale, Action<int, int> setPixel, byte mask, int baseY)
		{
			for (int col = 0; col < ModernDos8X16.GlyphWidth; col++)
			{
				if ((mask & (0x80 >> col)) == 0) continue;

				int baseX = destX + col * scale;

				for (int sy = 0; sy < scale; sy++)
				{
					for (int sx = 0; sx < scale; sx++)
					{
						setPixel(baseX + sx, baseY + sy);
					}
				}
			}
		}

		/// <summary>
		/// Float-scale overload.
		/// Each source pixel maps to dest rect [floor(col*scale), floor((col+1)*scale)) × [floor(row*scale), floor((row+1)*scale)).
		/// Avoids gaps while keeping crisp block appearance.
		/// </summary>
		public static void DrawGlyph(byte cp437, int destX, int destY, float scale, Action<int, int> setPixel)
        {
            if (scale < 1.0f) scale = 1.0f;

            for (int row = 0; row < ModernDos8X16.GlyphHeight; row++)
            {
                byte mask = ModernDos8X16.FontData[cp437, row];
                if (mask == 0) continue;

                int baseY = destY + (int)(row * scale);
                int nextY = destY + (int)((row + 1) * scale);

                for (int col = 0; col < ModernDos8X16.GlyphWidth; col++)
                {
                    if ((mask & (0x80 >> col)) == 0) continue;

                    FillPixelBlock(
                        destX + (int)(col * scale), destX + (int)((col + 1) * scale),
                        baseY, nextY,
                        setPixel);
                }
            }
        }

        private static void FillPixelBlock(int x0, int x1, int y0, int y1, Action<int, int> setPixel)
        {
            for (int sy = y0; sy < y1; sy++)
            {
                for (int sx = x0; sx < x1; sx++)
                {
                    setPixel(sx, sy);
                }
            }
        }

        // ----------------------------------------------------------------
        // Single glyph — by Unicode char
        // ----------------------------------------------------------------

        /// <summary>
        /// Draws the glyph for <paramref name="ch"/> at (destX, destY).
        /// Falls back to '?' (0x3F) when the character is not in CP437.
        /// </summary>
        public static void DrawChar(char ch, int destX, int destY, int scale, Action<int, int> setPixel)
        {
            if (!ModernDos8X16.TryGetCp437(ch, out byte cp437))
                cp437 = 0x3F; // '?'

            DrawGlyph(cp437, destX, destY, scale, setPixel);
        }

        /// <inheritdoc cref="DrawChar(char, int, int, int, Action{int, int})"/>
        public static void DrawChar(char ch, int destX, int destY, float scale, Action<int, int> setPixel)
        {
            if (!ModernDos8X16.TryGetCp437(ch, out byte cp437))
                cp437 = 0x3F; // '?'

            DrawGlyph(cp437, destX, destY, scale, setPixel);
        }

        // ----------------------------------------------------------------
        // String
        // ----------------------------------------------------------------

        /// <summary>
        /// Draws <paramref name="text"/> starting at (destX, destY), left to right.
        /// Each character advances by GlyphWidth * scale pixels.
        /// </summary>
        public static void DrawString(string text, int destX, int destY, int scale, Action<int, int> setPixel)
        {
            if (string.IsNullOrEmpty(text)) return;

            int advance = ModernDos8X16.GlyphWidth * scale;
            int x = destX;

            foreach (char ch in text)
            {
                DrawChar(ch, x, destY, scale, setPixel);
                x += advance;
            }
        }

        /// <inheritdoc cref="DrawString(string, int, int, int, Action{int, int})"/>
        public static void DrawString(string text, int destX, int destY, float scale, Action<int, int> setPixel)
        {
            if (string.IsNullOrEmpty(text)) return;

            int advance = (int)(ModernDos8X16.GlyphWidth * scale);
            int x = destX;

            foreach (char ch in text)
            {
                DrawChar(ch, x, destY, scale, setPixel);
                x += advance;
            }
        }
    }
}
