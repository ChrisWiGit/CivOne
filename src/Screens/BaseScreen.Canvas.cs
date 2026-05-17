// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Drawing;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Graphics.Sprites;

namespace CivOne.Screens
{
	public abstract partial class BaseScreen : IDefaultTextSettings, IButtonDrawer
	{
		public TextSettings DefaultTextSettings { get; set; }

		protected int Width => Bitmap.Width;
		protected int Height => Bitmap.Height;

		private Bytemap _bitmap;
		public Bytemap Bitmap
		{
			get
			{
				return _bitmap;
			}
			protected set
			{
				_bitmap?.Dispose();
				_bitmap = value;
			}
		}
		private Palette _palette, _originalColours;
		/// <summary>
		/// Gets or sets the active screen palette.
		///
		/// Setting this property replaces the entire active palette with a copy of the provided palette.
		///
		/// Use this when you intentionally want a full palette switch for the screen.
		///
		/// This includes index 0, which is commonly used as transparency.
		///
		/// If you need to preserve transparency index 0 while applying colors from another palette, use <see cref="SetPalette(Palette)"/> instead.
		/// </summary>
		public Palette Palette
		{
			get
			{
				return _palette;
			}
			set
			{
				_palette = value.Copy();
				if (_originalColours == null)
					_originalColours = value.Copy();
			}
		}
		public Palette OriginalColours => _originalColours;
		public void SetOriginalColours() => _originalColours.Merge(_palette);

		protected void DrawPanel(int x, int y, int width, int height, bool border = true)
		{
			int xx = x, yy = y, ww = width, hh = height;
			if (border)
			{
				xx++;
				yy++;
				ww -= 2;
				hh -= 2;
				this.DrawRectangle(x, y, width, height);
			}
			this.AddLayer(new Picture(ww, hh)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D(), xx, yy, dispose: true);
		}

		protected void DrawBorder(int border)
		{
			border %= 2;
			Picture[] borders = new Picture[8];
			int index = 0;
			for (int yy = 0; yy < 2; yy++)
				for (int xx = 0; xx < 4; xx++)
				{
					borders[index] = Resources["SP299"][((border == 0) ? 192 : 224) + (8 * xx), 120 + (8 * yy), 8, 8];
					index++;
				}

			for (int x = 8; x < Width - 8; x += 8)
			{
				this.AddLayer(borders[4], x, 0)
					.AddLayer(borders[6], x, Height - 8);
			}
			for (int y = 8; y < Height - 8; y += 8)
			{
				this.AddLayer(borders[7], 0, y)
					.AddLayer(borders[5], Width - 8, y);
			}
			this.AddLayer(borders[0], 0, 0)
				.AddLayer(borders[1], Width - 8, 0)
				.AddLayer(borders[2], 0, Height - 8)
				.AddLayer(borders[3], Width - 8, Height - 8);
		}

		public void DrawButton(string text, byte fontId, byte colour, byte colourDark, int x, int y, int width, int height)
		{
			this.FillRectangle(x, y, width, 1, 7)
				.FillRectangle(x, y + 1, 1, (height - 1), 7)
				.FillRectangle(x + 1, y + (height - 1), width - 1, 1, colourDark)
				.FillRectangle(x + width - 1, y, 1, (height - 1), colourDark)
				.FillRectangle(x + 1, y + 1, width - 2, (height - 2), colour)
				.DrawText(text, fontId, colourDark, x + (int)Math.Ceiling((double)width / 2), y + 2, TextAlign.Center);
		}

		public void DrawButton(string text, byte colour, byte colourDark, int x, int y, int width) => DrawButton(text, 1, colour, colourDark, x, y, width, Resources.GetFontHeight(1) + 3);

		public void ResetPalette()
		{
			for (int i = 0; i < Palette.Length && i < OriginalColours.Length && i < 256; i++)
				Palette[i] = OriginalColours[i];
		}

		/// <summary>
		/// Merges the provided palette into the current active palette.
		///
		/// This method preserves index 0 from the current palette.
		///
		/// Index 0 is often used as transparency, so preserving it avoids transparency artifacts.
		///
		/// Use this when you want to apply another palette's colors without fully replacing the current palette.
		///
		/// Use <see cref="Palette"/> assignment for a complete palette replacement.
		/// </summary>
		/// <param name="palette">The palette whose colors should be merged into the current palette.</param>
		public void SetPalette(Palette palette)
		{
			Colour indexZero = Palette[0];
			Palette.Merge(palette);
			Palette[0] = indexZero;
		}

		public virtual void Dispose()
		{
			Bitmap?.Dispose();
			Palette?.Dispose();
			OriginalColours?.Dispose();
		}
	}
}