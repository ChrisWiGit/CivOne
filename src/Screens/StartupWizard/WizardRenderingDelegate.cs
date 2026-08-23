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
using System.Text;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Screens.StartupWizard.DosFont;

namespace CivOne.Screens.StartupWizard
{
	/// <summary>
	/// Encapsulates all DOS-style rendering logic for the wizard.
	/// Handles drawing text, boxes, menu entries, header, and link areas.
	/// </summary>
	internal sealed class WizardRenderingDelegate(
		BaseScreen screen,
		Func<string, string> translate)
	{
		private readonly BaseScreen _screen = screen ?? throw new ArgumentNullException(nameof(screen));
		private readonly Func<string, string> Translate = translate ?? throw new ArgumentNullException(nameof(translate));

		// Color constants
		private const byte ColourBackground = 0;
		private const byte ColourHeaderBackground = 1;
		private const byte ColourBorder = 15;
		private const byte ColourText = 15;
		private const byte ColourTitle = 14;
		private const byte ColourMuted = 7;
		private const byte ColourLink = 9;
		private const byte ColourStatus = 14;
		private const byte ColourNumber = 9;
		private const int HeaderRows = 11;
		private const int HeaderFrameWidth = 80;


		/// <summary>
		/// Renders complete page with header, content, and menu.
		/// </summary>
		public void Render(WizardState state, WizardPage page, WizardRenderingContext context)
		{
			ArgumentNullException.ThrowIfNull(state);
			ArgumentNullException.ThrowIfNull(page);
			ArgumentNullException.ThrowIfNull(context);

			_screen.Clear(ColourBackground);
			DrawHeaderBox(context);
			DrawPageContent(page, context);
		}

		/// <summary>
		/// Draws the static header box with branding and links.
		/// </summary>
		public void DrawHeaderBox(WizardRenderingContext context)
		{
			int frameWidth = Math.Min(HeaderFrameWidth, context.Cols);
			int left = 0;
			int inner = Math.Max(0, frameWidth - 2);
			DrawBlueBackground(context, frameWidth, left);

			CharPut(KnownCp437Chars.BoxDoubleTopLeft + new string(KnownCp437Chars.BoxDoubleHorizontal, inner) + KnownCp437Chars.BoxDoubleTopRight, left, 0, ColourBorder, context);
			CharPut(KnownCp437Chars.BoxDoubleBottomLeft + new string(KnownCp437Chars.BoxDoubleHorizontal, inner) + KnownCp437Chars.BoxDoubleBottomRight, left, HeaderRows - 1, ColourBorder, context);

			for (int row = 1; row < HeaderRows - 1; row++)
			{
				CharPut(KnownCp437Chars.BoxDoubleVertical.ToString(), left, row, ColourBorder, context);
				CharPut(KnownCp437Chars.BoxDoubleVertical.ToString(), left + frameWidth - 1, row, ColourBorder, context);
			}

			DrawBoxContent(context);
		}

		private void DrawBlueBackground(WizardRenderingContext context, int frameWidth, int left)
		{
			if (frameWidth <= 0 || HeaderRows <= 0)
			{
				return;
			}
			
			int glyphW = (int)(ModernDos8X16.GlyphWidth * context.Scale);
			int glyphH = (int)(ModernDos8X16.GlyphHeight * context.Scale);

			if (glyphW <= 0 || glyphH <= 0)
			{
				return;
			}

			_screen.Bitmap.FillRectangle(
				context.Box.X + (left * glyphW),
				context.Box.Y,
				frameWidth * glyphW,
				HeaderRows * glyphH,
				ColourHeaderBackground);
		}

		/// <summary>
		/// Draws box content: version, title, links, and branding.
		/// </summary>
		private void DrawBoxContent(WizardRenderingContext context)
		{
			string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?";
			string title = $"CIVONE (OSS) - Version {version}";
			string subtitle = Translate("From those open source guys at LEISURE TIME.");
			BoxPutMiddle(title, 1, ColourTitle, context);
			BoxPutMiddle(subtitle, 3, ColourText, context);
			BoxPutMiddle(Translate("This game comes without any warranty, obligations, or guarantees."), 4, ColourText, context);

			// Row 6: Origin line with two links
			string line6prefix = Translate("Origin: ");
			string line6link1 = ProjectPublicLinks.OriginRepository;
			string line6suffix = Translate("  AND");
			int col6start = Math.Max(1, (context.Cols - (line6prefix.Length + line6link1.Length + line6suffix.Length)) / 2);
			BoxPut(line6prefix, col6start, 6, ColourMuted, context);
			BoxPut(line6link1, col6start + line6prefix.Length, 6, ColourLink, context);
			RecordLinkArea(line6link1, 6, col6start + line6prefix.Length, context);
			BoxPut(line6suffix, col6start + line6prefix.Length + line6link1.Length, 6, ColourMuted, context);

			// Row 7: Second link
			string line7prefix = "        ";
			string line7link = ProjectPublicLinks.FireEggsRepository;
			int col7start = Math.Max(1, (context.Cols - (line7prefix.Length + line7link.Length)) / 2);
			BoxPut(line7prefix, col7start, 7, ColourMuted, context);
			BoxPut(line7link, col7start + line7prefix.Length, 7, ColourLink, context);
			RecordLinkArea(line7link, 7, col7start + line7prefix.Length, context);

			// Row 8: Third link
			string line8prefix = Translate("Current Version: ");
			string line8link = ProjectPublicLinks.CurrentGitRepository;
			int col8start = Math.Max(1, (context.Cols - (line8prefix.Length + line8link.Length)) / 2);
			BoxPut(line8prefix, col8start, 8, ColourMuted, context);
			BoxPut(line8link, col8start + line8prefix.Length, 8, ColourLink, context);
			RecordLinkArea(line8link, 8, col8start + line8prefix.Length, context);
		}

		/// <summary>
	/// Draws the page content: title, description lines, links, menu entries, and status.
	/// </summary>
	public void DrawPageContent(WizardPage page, WizardRenderingContext context)
	{
		int row = HeaderRows + 1;
		BoxPutMiddle(page.Title, row++, ColourTitle, context);
		row++; // blank line

		foreach (string line in page.Lines)
		{
			if (row >= context.Rows - 2) break;
			BoxPutMiddle(line, row++, ColourText, context);
		}

		// Draw page-specific links (e.g., Changes, Forum, Discord on final page)
		if (page.Links.Count > 0)
		{
			row++; // blank line before links
			DrawPageLinks(page, row, context);
			row += page.Links.Count;
		}

		context.ContentEndRow = row + 1;

		DrawMenuEntries(page, context);

		if (!string.IsNullOrWhiteSpace(context.StatusMessage))
		{
			CharPut(context.StatusMessage, 2, context.Rows - 1, ColourStatus, context);
		}
	}

	/// <summary>
	/// Draws page-specific links (e.g., Changes, Forum, Discord).
	/// </summary>
	private void DrawPageLinks(WizardPage page, int startRow, WizardRenderingContext context)
	{
		int row = startRow;
		foreach ((string label, string url) in page.Links)
		{
			if (row >= context.Rows - 2) break;
			DrawCenteredLinkLine(label, url, row, context);
			row++;
		}
	}

	/// <summary>
	/// Draws a centered line with a label and clickable URL.
	/// </summary>
	private void DrawCenteredLinkLine(string label, string url, int row, WizardRenderingContext context)
	{
		string text = $"{label} {url}";
		int startCol = Math.Max(1, (context.Cols - text.Length) / 2);
		BoxPut(label, startCol, row, ColourMuted, context);
		BoxPut($" {url}", startCol + label.Length, row, ColourLink, context);
		RecordLinkArea(url, row, startCol + label.Length + 1, context);
	}

	/// <summary>
	/// Draws menu entries centered with left alignment.
	/// </summary>
		public void DrawMenuEntries(WizardPage page, WizardRenderingContext context)
		{
			int gh = (int)(ModernDos8X16.GlyphHeight * context.Scale);
			int gw = (int)(ModernDos8X16.GlyphWidth * context.Scale);
			IReadOnlyList<WizardEntry> fixedEntries = [.. page.Entries.Where(entry => entry.KeepAlwaysLastPosition)];
			IReadOnlyList<WizardEntry> scrollableEntries = [.. page.Entries.Where(entry => !entry.KeepAlwaysLastPosition)];
			int maxVisibleEntries = page.EntriesMaxCount > 0 ? page.EntriesMaxCount : page.Entries.Count;
			int fixedVisibleCount = Math.Min(fixedEntries.Count, maxVisibleEntries);
			int scrollableVisibleCount = Math.Min(scrollableEntries.Count, Math.Max(0, maxVisibleEntries - fixedVisibleCount));
			int totalVisibleCount = fixedVisibleCount + scrollableVisibleCount;
			int scrollOffset = GetClampedScrollOffset(page, scrollableEntries, scrollableVisibleCount, context);
			bool canScrollUp = scrollOffset > 0;
			bool canScrollDown = scrollOffset + scrollableVisibleCount < scrollableEntries.Count;
			bool needsScrolling = scrollableEntries.Count > scrollableVisibleCount;
			int menuRow = needsScrolling
				? context.Rows - totalVisibleCount - 1 + page.EntriesYOffset
				: context.ContentEndRow + page.EntriesYOffset;

			IReadOnlyList<WizardEntry> visibleScrollableEntries = [.. scrollableEntries.Skip(scrollOffset).Take(scrollableVisibleCount)];
			IReadOnlyList<WizardEntry> visibleEntries = [.. visibleScrollableEntries, .. fixedEntries.Take(fixedVisibleCount)];

			int maxWidth = 0;
			foreach (WizardEntry entry in visibleEntries)
			{
				string entryPrefix = GetDisplayPrefix(entry);
				maxWidth = Math.Max(maxWidth, $"{entryPrefix} {entry.Text}".Length);
			}

			int startCol = Math.Max(1, (context.Cols - maxWidth) / 2);
			int arrowCol = Math.Max(1, startCol - 2);
			int firstVisibleRow = menuRow;
			int lastScrollableRow = menuRow + Math.Max(0, visibleScrollableEntries.Count - 1);

			int currentRow = menuRow;
			foreach (WizardEntry entry in visibleScrollableEntries)
			{
				DrawMenuEntry(entry, startCol, currentRow, gh, gw, context);
				currentRow++;
			}

			foreach (WizardEntry entry in fixedEntries.Take(fixedVisibleCount))
			{
				DrawMenuEntry(entry, startCol, currentRow, gh, gw, context);
				currentRow++;
			}

			if (canScrollUp && visibleScrollableEntries.Count > 0)
			{
				CharPut(KnownCp437Chars.ScrollLightUpArrow.ToString(), arrowCol, firstVisibleRow, ColourNumber, context);
				context.EntryHitAreas.Add((
					WizardRenderingContext.ScrollUpHitAreaNumber,
					new Rectangle(
						0, // allow multiple columns to be clickable for easier use
						context.Box.Y + (firstVisibleRow * gh),
						context.Box.X + ((arrowCol + 1) * gw),
						gh)));
			}

			if (canScrollDown && visibleScrollableEntries.Count > 0)
			{
				CharPut(KnownCp437Chars.ScrollLightDownArrow.ToString(), arrowCol, lastScrollableRow, ColourNumber, context);
				context.EntryHitAreas.Add((
					WizardRenderingContext.ScrollDownHitAreaNumber,
					new Rectangle(
						0, // allow multiple columns to be clickable for easier use
						context.Box.Y + (lastScrollableRow * gh),
						context.Box.X + ((arrowCol + 1) * gw),
						gh)));
			}
		}

		private void DrawMenuEntry(WizardEntry entry, int startCol, int menuRow, int gh, int gw, WizardRenderingContext context)
		{
			if (menuRow >= context.Rows - 1)
			{
				return;
			}

			byte textColour = entry.Enabled ? ColourText : ColourMuted;
			string entryPrefix = GetDisplayPrefix(entry);

			CharPut(entryPrefix, startCol, menuRow, ColourNumber, context);
			CharPut($" {entry.Text}", startCol + entryPrefix.Length, menuRow, textColour, context);

			if (entry.Enabled)
			{
				int entryWidth = (entryPrefix.Length + 1 + entry.Text.Length) * gw;
				int entryX = context.Box.X + startCol * gw;
				int entryY = context.Box.Y + menuRow * gh;
				context.EntryHitAreas.Add((entry.Number, new Rectangle(entryX, entryY, entryWidth, gh)));
			}
		}

		private static int GetClampedScrollOffset(WizardPage page, IReadOnlyList<WizardEntry> scrollableEntries, int scrollableVisibleCount, WizardRenderingContext context)
		{
			if (scrollableVisibleCount <= 0)
			{
				return 0;
			}

			int maxOffset = Math.Max(0, scrollableEntries.Count - scrollableVisibleCount);
			return Math.Clamp(context.EntryScrollOffset, 0, maxOffset);
		}

		private static string GetDisplayPrefix(WizardEntry entry)
		{
			char? hotkey = entry.Hotkey.HasValue ? char.ToUpperInvariant(entry.Hotkey.Value) : GetAutoDisplayHotkey(entry.Number);
			return hotkey.HasValue ? $"{hotkey.Value}." : $"{entry.Number}.";
		}

		private static char? GetAutoDisplayHotkey(int entryNumber)
		{
			if (entryNumber >= 1 && entryNumber <= 9)
			{
				return (char)('0' + entryNumber);
			}

			int letterIndex = entryNumber - 10;
			if (letterIndex < 0 || letterIndex >= 26)
			{
				return null;
			}

			return (char)('A' + letterIndex);
		}

		private static char? GetAutoActivationHotkey(int entryNumber)
		{
			if (entryNumber >= 1 && entryNumber <= 9)
			{
				return (char)('0' + entryNumber);
			}

			int letterIndex = entryNumber - 10;
			if (letterIndex < 0 || letterIndex >= 26)
			{
				return null;
			}

			return (char)('a' + letterIndex);
		}

		/// <summary>
		/// Records a clickable link area for hit testing.
		/// </summary>
		public static void RecordLinkArea(string url, int row, int startCol, WizardRenderingContext context)
		{
			int glyphWidth = (int)(ModernDos8X16.GlyphWidth * context.Scale);
			int glyphHeight = (int)(ModernDos8X16.GlyphHeight * context.Scale);
			if (glyphWidth <= 0 || glyphHeight <= 0)
			{
				return;
			}

			int x = context.Box.X + startCol * glyphWidth;
			int y = context.Box.Y + row * glyphHeight;
			int linkWidth = url.Length * glyphWidth;
			context.LinkAreas.Add((url, new Rectangle(x, y, linkWidth, glyphHeight)));
		}

		/// <summary>
		/// Places text at a character-grid position.
		/// </summary>
		public void CharPut(string text, int col, int row, byte colour, WizardRenderingContext context)
		{
			int glyphW = (int)(ModernDos8X16.GlyphWidth * context.Scale);
			int glyphH = (int)(ModernDos8X16.GlyphHeight * context.Scale);
			DrawDosText(text, context.Box.X + col * glyphW, context.Box.Y + row * glyphH, colour, context);
		}

		/// <summary>
		/// Places text at a character-grid position, truncating to fit.
		/// </summary>
		public void BoxPut(string text, int col, int row, byte colour, WizardRenderingContext context)
		{
			int maxLen = Math.Max(0, context.Cols - 1 - col);
			CharPut(text.Length > maxLen ? text[..maxLen] : text, col, row, colour, context);
		}

		/// <summary>
		/// Places text centered horizontally within the box.
		/// </summary>
		public void BoxPutMiddle(string text, int row, byte colour, WizardRenderingContext context)
		{
			int col = Math.Max(1, (context.Cols - text.Length) / 2);
			BoxPut(text, col, row, colour, context);
		}

		/// <summary>
		/// Draws text using DOS font renderer and tracks glyph areas.
		/// </summary>
		private void DrawDosText(string text, int x, int y, byte colour, WizardRenderingContext context)
		{
			string output = text ?? string.Empty;
			int glyphWidth = (int)(ModernDos8X16.GlyphWidth * context.Scale);
			int glyphHeight = (int)(ModernDos8X16.GlyphHeight * context.Scale);
			byte highlightColour = colour == ColourMuted ? colour : ColourStatus;
			var segments = ParseColourSegments(output, colour, highlightColour);

			int visibleLength = 0;
			foreach ((string segmentText, _) in segments)
			{
				visibleLength += segmentText.Length;
			}

			for (int i = 0; i < visibleLength; i++)
			{
				context.GlyphAreas.Add(new Rectangle(x + (i * glyphWidth), y, glyphWidth, glyphHeight));
			}

			int drawX = x;
			foreach ((string segmentText, byte segmentColour) in segments)
			{
				if (segmentText.Length == 0)
				{
					continue;
				}

				ModernDosFontRenderer.DrawString(segmentText, drawX, y, context.Scale, (px, py) =>
				{
					if (px < 0 || py < 0 || px >= _screen.Bitmap.Width || py >= _screen.Bitmap.Height)
					{
						return;
					}

					_screen.Bitmap[px, py] = segmentColour;
				});

				drawX += segmentText.Length * glyphWidth;
			}
		}

		private static (string Text, byte Colour)[] ParseColourSegments(string text, byte baseColour, byte highlightColour)
		{
			if (string.IsNullOrEmpty(text))
			{
				return [];
			}

			bool highlighted = false;
			StringBuilder segment = new();
			var result = new List<(string Text, byte Colour)>();

			void FlushSegment()
			{
				if (segment.Length == 0)
				{
					return;
				}

				byte segmentColour = highlighted ? highlightColour : baseColour;
				result.Add((segment.ToString(), segmentColour));
				segment.Clear();
			}

			foreach (char c in text)
			{
				if (c == '^')
				{
					FlushSegment();
					highlighted = !highlighted;
					continue;
				}

				segment.Append(c);
			}

			FlushSegment();
			return [.. result];
		}
	}
}
