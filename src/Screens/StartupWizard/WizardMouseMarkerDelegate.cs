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
using System.Drawing;
using System.Linq;
using CivOne.Screens.StartupWizard.DosFont;

namespace CivOne.Screens.StartupWizard
{
	/// <summary>
	/// Handles rendering of the mouse inversion marker with special handling for links and menu entries.
	/// </summary>
	internal sealed class WizardMouseMarkerDelegate(BaseScreen screen)
	{
		private readonly BaseScreen _screen = screen ?? throw new ArgumentNullException(nameof(screen));

		private const byte ColourMenuArrow = 12;

		/// <summary>
		/// Draws the mouse inversion marker at the current mouse position.
		/// Shows arrow over links/menus, or standard inversion elsewhere.
		/// </summary>
		public void DrawMarker(int mouseX, int mouseY, WizardRenderingContext context)
		{
			ArgumentNullException.ThrowIfNull(context);

			if (!IsValidMousePosition(mouseX, mouseY, context))
			{
				return;
			}

			int glyphWidth = (int)(ModernDos8X16.GlyphWidth * context.Scale);
			int glyphHeight = (int)(ModernDos8X16.GlyphHeight * context.Scale);
			if (glyphWidth <= 0 || glyphHeight <= 0)
			{
				return;
			}

			int col = (mouseX - context.Box.X) / glyphWidth;
			int row = (mouseY - context.Box.Y) / glyphHeight;
			if (!IsWithinGrid(col, row, context))
			{
				return;
			}

			Rectangle cell = new(context.Box.X + (col * glyphWidth), context.Box.Y + (row * glyphHeight), glyphWidth, glyphHeight);
			DrawMarkerForCell(mouseX, mouseY, cell, context);
		}

		private static bool IsValidMousePosition(int mouseX, int mouseY, WizardRenderingContext context)
		{
			if (mouseX < 0 || mouseY < 0)
			{
				return false;
			}

			return context.Box.Contains(mouseX, mouseY);
		}

		private static bool IsWithinGrid(int col, int row, WizardRenderingContext context)
		{
			return col >= 0 && col < context.Cols && row >= 0 && row < context.Rows;
		}

		private void DrawMarkerForCell(int mouseX, int mouseY, Rectangle cell, WizardRenderingContext context)
		{
			bool isOverLink = context.LinkAreas.Any(link => link.Area.Contains(mouseX, mouseY));
			bool isOverMenu = context.EntryHitAreas.Any(entry => entry.Area.Contains(mouseX, mouseY));

			if (isOverLink)
			{
			DrawLinkMarker(cell, context);
		}
		else if (isOverMenu)
		{
			DrawMenuMarker(cell, context);
		}
		else
		{
			DrawInversionMarker(cell);
		}
	}

	private void DrawLinkMarker(Rectangle cell, WizardRenderingContext context)
	{
		// Draw character 18 (→) in white over links
		ModernDosFontRenderer.DrawGlyph(18, cell.X, cell.Y, context.Scale, (x, y) =>
		{
			if (x >= 0 && y >= 0 && x < _screen.Bitmap.Width && y < _screen.Bitmap.Height)
			{
				_screen.Bitmap[x, y] = 15;
			}
		});
	}

	private void DrawMenuMarker(Rectangle cell, WizardRenderingContext context)
	{
		// Draw character 17 (←) in red over menu entries
		ModernDosFontRenderer.DrawGlyph(17, cell.X, cell.Y, context.Scale, (x, y) =>
		{
			if (x >= 0 && y >= 0 && x < _screen.Bitmap.Width && y < _screen.Bitmap.Height)
			{
				_screen.Bitmap[x, y] = ColourMenuArrow;
			}
		});
	}

	private void DrawInversionMarker(Rectangle cell)
		{
			// Draw normal color inversion
			for (int yy = 0; yy < cell.Height; yy++)
			{
				for (int xx = 0; xx < cell.Width; xx++)
				{
					int px = cell.X + xx;
					int py = cell.Y + yy;
					if (px < 0 || py < 0 || px >= _screen.Bitmap.Width || py >= _screen.Bitmap.Height)
					{
						continue;
					}

					_screen.Bitmap[px, py] = InvertColour(_screen.Bitmap[px, py]);
				}
			}
		}

		private static byte InvertColour(byte colour)
		{
			if (colour == 0)
			{
				return 15;
			}

			if (colour <= 15)
			{
				return (byte)(15 - colour);
			}

			return colour;
		}
	}
}
