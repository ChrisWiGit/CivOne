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
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Services;
using CivOne.Services.Browser;
using CivOne.Services.Translation;
using DosFont;

namespace CivOne.Screens.StartupWizard
{
	/// <summary>
	/// DOS style startup wizard shown before normal startup when setup is required.
	/// </summary>
	[Break]
	[ScreenResizeable]
	public sealed class WizardScreen : BaseScreen
	{
		private const int TargetRows = 25;
		private const int TargetCols = 80;
		private const int HeaderRows = 11;
		private const int HeaderFrameWidth = 80;
		private const byte ColourBackground = 0;
		private const byte ColourBorder = 15;
		private const byte ColourText = 15;
		private const byte ColourTitle = 14;
		private const byte ColourMuted = 7;
		private const byte ColourLink = 9;
		private const byte ColourStatus = 14;
		private const char BoxTopLeft = '\u2554';
		private const byte ColourNumber = 9;
		private const byte ColourMenuArrow = 12;
		private static readonly string ChangesUrl = BuildHttpsUrl("github.com", "ChrisWiGit/CivOne/blob/master/CHANGES.md");
		private static readonly string ForumUrl = BuildHttpsUrl("forums.civfanatics.com", string.Empty);
		private static readonly string DiscordUrl = BuildHttpsUrl("discord.gg", "kfaFcTnCX");
		private const char BoxTopRight = '\u2557';
		private const char BoxBottomLeft = '\u255A';
		private const char BoxBottomRight = '\u255D';
		private const char BoxHorizontal = '\u2550';
		private const char BoxVertical = '\u2551';

		private readonly WizardEngine _engine;
		private readonly IReadOnlyList<TranslationLanguageInfo> _availableLanguages;
		private readonly List<(int Number, Rectangle Area)> _entryHitAreas = [];
		private readonly List<(string Url, Rectangle Area)> _linkAreas = [];
		private readonly List<Rectangle> _glyphAreas = [];

		private Rectangle _box;
		private int _cols;
		private int _rows;
		private float _scale = 1.0f;
		private int _mouseX = -1;
		private int _mouseY = -1;

	/// <inheritdoc />
		public override bool UseFullWindowCanvas => true;

		/// <summary>
		/// Initializes the startup wizard screen.
		/// </summary>
		public WizardScreen() : base(MouseCursor.None)
		{
			Palette = Common.GetPalette256;
			_engine = new WizardEngine(TranslationServiceFactory.ActiveLanguagePostfix);
			_availableLanguages = TranslationServiceFactory.GetAvailableLanguages(Runtime.StorageDirectory, message => Log(message));
			Refresh();
	}

		/// <summary>
		/// Handles per-frame update and redraw.
		/// </summary>
		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded())
			{
				return false;
			}

			RenderCurrentPage();
			return true;
		}

		/// <summary>
		/// Handles keyboard activation for numbered entries and navigation.
		/// </summary>
		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args[Key.Escape])
			{
				if (_engine.PageIndex == 0)
				{
					Destroy();
				}
				else
				{
					_engine.MoveBack();
					Refresh();
				}
				return true;
			}

			if (args[Key.Character] && TryActivateHotkey(args.KeyChar))
			{
				return true;
			}

			if (args[Key.Character] && char.IsDigit(args.KeyChar) && args.KeyChar != '0')
			{
				ActivateEntry(args.KeyChar - '0');
				return true;
			}

			int numPadNumber = args.Key switch
			{
				Key.NumPad1 => 1,
				Key.NumPad2 => 2,
				Key.NumPad3 => 3,
				Key.NumPad4 => 4,
				Key.NumPad5 => 5,
				Key.NumPad6 => 6,
				Key.NumPad7 => 7,
				Key.NumPad8 => 8,
				Key.NumPad9 => 9,
				_ => 0
			};

			if (numPadNumber > 0)
			{
				ActivateEntry(numPadNumber);
				return true;
			}

			return false;
		}

		private bool TryActivateHotkey(char keyChar)
		{
			char normalizedKey = char.ToLowerInvariant(keyChar);
			WizardEntry entry = BuildPage().Entries.FirstOrDefault(x => x.Enabled && x.Hotkey.HasValue && char.ToLowerInvariant(x.Hotkey.Value) == normalizedKey);
			if (entry == null)
			{
				return false;
			}

			ActivateEntry(entry.Number);
			return true;
		}

		/// <summary>
		/// Handles mouse activation for numbered entry rows.
		/// </summary>
		public override bool MouseDown(ScreenEventArgs args)
		{
			if (args.Buttons != MouseButton.Left)
			{
				return false;
			}

			// Check links first
			foreach ((string url, Rectangle area) in _linkAreas)
			{
				if (!area.Contains(args.X, args.Y))
				{
					continue;
				}

				OpenUrl(url);
				return true;
			}

			// Then check entries
			foreach ((int number, Rectangle area) in _entryHitAreas)
			{
				if (!area.Contains(args.X, args.Y))
				{
					continue;
				}

				ActivateEntry(number);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Tracks mouse position and refreshes the DOS inversion marker.
		/// </summary>
		public override bool MouseMove(ScreenEventArgs args)
		{
			if (_mouseX == args.X && _mouseY == args.Y)
			{
				return false;
			}

			_mouseX = args.X;
			_mouseY = args.Y;
			Refresh();
			return true;
		}

		private void ActivateEntry(int number)
		{
			WizardPage page = BuildPage();
			WizardEntry entry = page.Entries.FirstOrDefault(x => x.Number == number);
			if (entry == null || !entry.Enabled)
			{
				return;
			}

			switch (entry.Action)
			{
				case WizardEntryAction.SelectLanguage:
					ApplyLanguage(entry.Value ?? string.Empty);
					break;
				case WizardEntryAction.BrowseDataFolder:
					HandleBrowseDataFolder();
					break;
				case WizardEntryAction.Continue:
					_engine.MoveNext();
					_engine.StatusMessage = string.Empty;
					break;
				case WizardEntryAction.Back:
					_engine.MoveBack();
					_engine.StatusMessage = string.Empty;
					break;
				case WizardEntryAction.ToggleSound:
					_engine.SoundEnabled = !_engine.SoundEnabled;
					Settings.Instance.Sound = _engine.SoundEnabled ? GameOption.On : GameOption.Off;
					_engine.StatusMessage = _engine.SoundEnabled
						? Translate("Sound enabled.")
						: Translate("Sound disabled.");
					break;
				case WizardEntryAction.Finish:
					Destroy();
					return;
			}

			Refresh();
		}

		private void ApplyLanguage(string postfix)
		{
			if (string.IsNullOrEmpty(postfix))
			{
				Settings.Instance.LanguagePostfix = string.Empty;
				TranslationServiceFactory.UseIdentity();
				_engine.SelectedLanguagePostfix = string.Empty;
				_engine.StatusMessage = Translate("Language switched to Identity.");
				return;
			}

			if (!TranslationServiceFactory.TryUseLanguage(Runtime.StorageDirectory, postfix, out string error, message => Log(message)))
			{
				_engine.StatusMessage = TranslateFormatted("Could not load language '{0}'.", postfix);
				Log("Could not activate language '{0}': {1}", postfix, error);
				return;
			}

			Settings.Instance.LanguagePostfix = postfix;
			_engine.SelectedLanguagePostfix = postfix;
			_engine.StatusMessage = Translate(postfix);
		}

		private void HandleBrowseDataFolder()
		{
			string path = Runtime.BrowseFolder(Translate("Location of Civilization data files"));
			if (path == null)
			{
				_engine.StatusMessage = Translate("Folder selection cancelled.");
				return;
			}

			_engine.DataFolder = path;
			if (!FileSystem.CopyDataFiles(path) || !FileSystem.DataFilesExist())
			{
				_engine.StatusMessage = Translate("Copying data files failed.");
				return;
			}

			Resources.ClearInstance();
			_engine.StatusMessage = Translate("Data files copied successfully.");
		}

		private void OpenUrl(string url)
		{
			var browserService = BrowserServiceFactory.Instance;
			if (!browserService.TryOpenUrl(url, out _))
			{
				// Try clipboard fallback
				if (browserService.TryCopyToClipboard(url, out _))
				{
					_engine.StatusMessage = Translate("Link copied to clipboard.");
				}
				else
				{
					_engine.StatusMessage = Translate("Could not open URL.");
				}
			}
			else
			{
				_engine.StatusMessage = Translate("Opened URL in browser.");
			}
		}

		private void RecordLinkArea(string url, int row, int startCol)
		{
			int glyphWidth = (int)(ModernDos8X16.GlyphWidth * _scale);
			int glyphHeight = (int)(ModernDos8X16.GlyphHeight * _scale);
			if (glyphWidth <= 0 || glyphHeight <= 0)
			{
				return;
			}

			int x = _box.X + startCol * glyphWidth;
			int y = _box.Y + row * glyphHeight;
			int linkWidth = url.Length * glyphWidth;
			_linkAreas.Add((url, new Rectangle(x, y, linkWidth, glyphHeight)));
		}

		private WizardPage BuildPage()
		{
			return _engine.PageIndex switch
			{
				0 => BuildLanguagePage(),
				1 => BuildWelcomePage(),
				2 => BuildDataFolderPage(),
				3 => BuildSoundPage(),
				4 => BuildFinalPage(),
				_ => BuildLanguagePage()
			};
		}

		private WizardPage BuildFinalPage()
		{
			return new WizardPage
			{
				Title = Translate("Startup Wizard: Ready"),
				Lines =
				[
					Translate("A list of changes is available below."),
					Translate("Forum and Discord links are included too."),
					Translate("Click any link or start the game now.")
				],
				Entries =
				[
					new WizardEntry { Number = 1, Text = Translate("Start game now"), Action = WizardEntryAction.Finish, Hotkey = 'c' },
					new WizardEntry { Number = 2, Text = Translate("Back"), Action = WizardEntryAction.Back, Hotkey = 'b' }
				],
				EntriesYOffset = 3
			};
		}

		private WizardPage BuildWelcomePage()
		{
			return new WizardPage
			{
				Title = Translate("Startup Wizard"),
				Lines =
				[
					Translate("Welcome to CivOne."),
					Translate("This screen appears because startup setup still needs attention."),
					Translate("Use number keys or click with the mouse to choose entries."),
					Translate("Links in the header can also be clicked.")
				],
				Entries =
				[
					new WizardEntry { Number = 1, Text = ContinueText(), Action = WizardEntryAction.Continue, Hotkey = 'c' },
					new WizardEntry { Number = 2, Text = Translate("Back"), Action = WizardEntryAction.Back, Hotkey = 'b' }
				]
			};
		}

		private WizardPage BuildLanguagePage()
		{
			var entries = new List<WizardEntry>();
			int number = 1;

			entries.Add(new WizardEntry
			{
				Number = number++,
				Text = Translate("Original (default)"),
				Action = WizardEntryAction.SelectLanguage,
				Value = string.Empty
			});

			foreach (string languagePostfix in _availableLanguages.Select(language => language.Postfix))
			{
				entries.Add(new WizardEntry
				{
					Number = number++,
					Text = Translate(languagePostfix),
					Action = WizardEntryAction.SelectLanguage,
					Value = languagePostfix
				});
			}

			entries.Add(new WizardEntry
			{
				Number = number,
				Text = ContinueText(),
				Action = WizardEntryAction.Continue,
				Hotkey = 'c'
			});

			if (_engine.PageIndex > 0)
			{
				entries.Add(new WizardEntry
				{
					Number = number + 1,
					Text = Translate("Back"),
					Action = WizardEntryAction.Back,
					Hotkey = 'b'
				});
			}

			string activeLanguage = string.IsNullOrEmpty(_engine.SelectedLanguagePostfix)
				? Translate("Identity")
				: Translate(_engine.SelectedLanguagePostfix);

			return new WizardPage
			{
				Title = Translate("Startup Wizard: Language"),
				Lines =
				[
					Translate("Select startup language."),
					TranslateFormatted("Current: {0}", activeLanguage)
				],
				Entries = entries
			};
		}

		private WizardPage BuildDataFolderPage()
		{
			bool hasDataFiles = FileSystem.DataFilesExist();
			string dataState = hasDataFiles
				? Translate("Data files available.")
				: Translate("Data files still missing.");

			string selectedPath = Translate("No folder selected.");
			if (hasDataFiles)
			{
				selectedPath = Settings.Instance.DataDirectory;
			}
			else if (!string.IsNullOrWhiteSpace(_engine.DataFolder))
			{
				selectedPath = _engine.DataFolder;
			}

			return new WizardPage
			{
				Title = Translate("Startup Wizard: Data Files"),
				Lines =
				[
					Translate("Pick DOS Civilization data folder."),
					selectedPath,
					dataState
				],
				Entries =
				[
					new WizardEntry { Number = 1, Text = Translate("Browse data folder"), Action = WizardEntryAction.BrowseDataFolder },
					new WizardEntry { Number = 2, Text = ContinueText(), Action = WizardEntryAction.Continue, Enabled = hasDataFiles, Hotkey = 'c' },
					new WizardEntry { Number = 3, Text = Translate("Back"), Action = WizardEntryAction.Back, Hotkey = 'b' }
				]
			};
		}

		private WizardPage BuildSoundPage()
		{
			string soundState = _engine.SoundEnabled ? Translate("On") : Translate("Off");
			return new WizardPage
			{
				Title = Translate("Startup Wizard: Sound"),
				Lines =
				[
					TranslateFormatted("Sound: {0}", soundState),
					Translate("Save choice and start game.")
				],
				Entries =
				[
					new WizardEntry { Number = 1, Text = Translate("Toggle sound"), Action = WizardEntryAction.ToggleSound },
					new WizardEntry { Number = 2, Text = ContinueText(), Action = WizardEntryAction.Continue, Hotkey = 'c' },
					new WizardEntry { Number = 3, Text = Translate("Back"), Action = WizardEntryAction.Back, Hotkey = 'b' }
				]
			};
		}

		private string ContinueText()
		{
			return Translate("Continue");
		}

		private void DrawFinalPageLinks(int startRow)
		{
			DrawCenteredLinkLine(Translate("Changes:"), ChangesUrl, startRow);
			DrawCenteredLinkLine(Translate("Forum:"), ForumUrl, startRow + 1);
			DrawCenteredLinkLine(Translate("Discord:"), DiscordUrl, startRow + 2);
		}

		private void DrawCenteredLinkLine(string label, string url, int row)
		{
			string text = $"{label} {url}";
			int startCol = Math.Max(1, (_cols - text.Length) / 2);
			BoxPut(label, startCol, row, ColourMuted);
			BoxPut($" {url}", startCol + label.Length, row, ColourLink);
			RecordLinkArea(url, row, startCol + label.Length + 1);
		}

		private static string BuildHttpsUrl(string host, string path)
		{
			var builder = new UriBuilder(Uri.UriSchemeHttps, host);
			builder.Path = path;
			return builder.Uri.ToString();
		}

		private void RenderCurrentPage()
		{
			_entryHitAreas.Clear();
			_linkAreas.Clear();
			_glyphAreas.Clear();
			WizardPage page = BuildPage();
			int baseWidth = TargetCols * ModernDos8X16.GlyphWidth;
			int baseHeight = TargetRows * ModernDos8X16.GlyphHeight;

			_cols = TargetCols;
			_rows = TargetRows;
			Size window = new(CanvasWidth, CanvasHeight);
			float scaleByWidth = (float)window.Width / baseWidth;
			float scaleByHeight = (float)window.Height / baseHeight;
			_scale = Math.Max(1.0f, Math.Min(scaleByWidth, scaleByHeight));
			float boxWidth = (baseWidth * _scale);
			float boxHeight = (baseHeight * _scale);
			int boxX = Math.Max(0, (int)((window.Width - boxWidth) / 2));
			int boxY = Math.Max(0, (int)((window.Height - boxHeight) / 2));
			_box = new Rectangle(boxX, boxY, (int)boxWidth, (int)boxHeight);

			this.Clear(ColourBackground);
			DrawHeaderBox();
			DrawPageContent(page);
			DrawMouseInversionMarker();
		}

		/// <summary>
		/// Draws the static 80-column header box (rows 0–9) with branding content.
		/// All glyphs are tracked in <c>_glyphAreas</c> and participate in mouse inversion.
		/// </summary>
		private void DrawHeaderBox()
		{
			int frameWidth = Math.Min(HeaderFrameWidth, _cols);
			int left = 0;
			int inner = Math.Max(0, frameWidth - 2);

			CharPut(BoxTopLeft + new string(BoxHorizontal, inner) + BoxTopRight, left, 0, ColourBorder);
			CharPut(BoxBottomLeft + new string(BoxHorizontal, inner) + BoxBottomRight, left, HeaderRows - 1, ColourBorder);

			for (int row = 1; row < HeaderRows - 1; row++)
			{
				CharPut(BoxVertical.ToString(), left, row, ColourBorder);
				CharPut(BoxVertical.ToString(), left + frameWidth - 1, row, ColourBorder);
			}

			DrawBoxContent();
		}

		private void DrawBoxContent()
		{
			string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?";
			string title = $"CIVONE (OSS) - Version {version}";
			string subtitle = "From those open source guys at LEISURE TIME.";
			BoxPutMiddle(title, 1, ColourTitle);
			BoxPutMiddle(subtitle, 3, ColourText);
			BoxPutMiddle(Translate("This game comes without any warranty, obligations, or guarantees."), 4, ColourText);
			
			// Row 6: Origin line with two links
			string line6prefix = "Origin: ";
			string line6link1 = "https://github.com/Solen1985/CivOne";
			string line6suffix = "  AND";
			int col6start = Math.Max(1, (_cols - (line6prefix.Length + line6link1.Length + line6suffix.Length)) / 2);
			BoxPut(line6prefix, col6start, 6, ColourMuted);
			BoxPut(line6link1, col6start + line6prefix.Length, 6, ColourLink);
			RecordLinkArea(line6link1, 6, col6start + line6prefix.Length);
			BoxPut(line6suffix, col6start + line6prefix.Length + line6link1.Length, 6, ColourMuted);
			
			// Row 7: Second link
			string line7prefix = "        ";
			string line7link = "https://github.com/fire-eggs/CivOne/";
			int col7start = Math.Max(1, (_cols - (line7prefix.Length + line7link.Length)) / 2);
			BoxPut(line7prefix, col7start, 7, ColourMuted);
			BoxPut(line7link, col7start + line7prefix.Length, 7, ColourLink);
			RecordLinkArea(line7link, 7, col7start + line7prefix.Length);
			
			// Row 8: Third link
			string line8prefix = "Current Version: ";
			string line8link = "https://github.com/ChrisWiGit";
			int col8start = Math.Max(1, (_cols - (line8prefix.Length + line8link.Length)) / 2);
			BoxPut(line8prefix, col8start, 8, ColourMuted);
			BoxPut(line8link, col8start + line8prefix.Length, 8, ColourLink);
			RecordLinkArea(line8link, 8, col8start + line8prefix.Length);
		}

		/// <summary>
		/// Draws the wizard page content below the header box (rows 11–24).
		/// </summary>
		private void DrawPageContent(WizardPage page)
		{
			int row = HeaderRows + 1;
			BoxPutMiddle(page.Title, row++, ColourTitle);
			row++; // blank line

			foreach (string line in page.Lines)
			{
				if (row >= _rows - 2) break;
				BoxPutMiddle(line, row++, ColourText);
			}

			if (_engine.PageIndex == 4)
			{
				row++;
				DrawFinalPageLinks(row);
			}

			DrawMenuEntries(page);
			
			if (!string.IsNullOrWhiteSpace(_engine.StatusMessage))
			{
				CharPut(_engine.StatusMessage, 2, _rows - 1, ColourStatus);
			}
		}

		/// <summary>
		/// Draws menu entries centered with left alignment of all items.
		/// Numbers displayed in blue, text in normal color.
		/// </summary>
		private void DrawMenuEntries(WizardPage page)
		{
			int gh = (int)(ModernDos8X16.GlyphHeight * _scale);
			int gw = (int)(ModernDos8X16.GlyphWidth * _scale);
			int menuRow = _rows - page.Entries.Count - 4 + page.EntriesYOffset;
			
			// Calculate max width needed for all entries ("N. Text").
			int maxWidth = 0;
			foreach (WizardEntry entry in page.Entries)
			{
				string entryText = $"{entry.Number}. {entry.Text}";
				maxWidth = Math.Max(maxWidth, entryText.Length);
			}
			
			// Center menu block while keeping a shared left edge.
			int startCol = Math.Max(1, (_cols - maxWidth) / 2);
			
			foreach (WizardEntry entry in page.Entries)
			{
				if (menuRow >= _rows - 1) break;
				byte textColour = entry.Enabled ? ColourText : ColourMuted;
				string entryPrefix = entry.Hotkey.HasValue
					? $"{char.ToLowerInvariant(entry.Hotkey.Value)}."
					: $"{entry.Number}.";
				
				// Draw hotkey or number prefix in blue.
				CharPut(entryPrefix, startCol, menuRow, ColourNumber);
				
				// Draw menu text in normal or muted colour.
				CharPut($" {entry.Text}", startCol + entryPrefix.Length, menuRow, textColour);
				
				// Only enabled rows are clickable/hoverable.
				if (entry.Enabled)
				{
					int entryWidth = (entryPrefix.Length + 1 + entry.Text.Length) * gw;
					int entryX = _box.X + startCol * gw;
					int entryY = _box.Y + menuRow * gh;
					_entryHitAreas.Add((entry.Number, new Rectangle(entryX, entryY, entryWidth, gh)));
				}
				
				menuRow++;
			}
		}

		/// <summary>
		/// Places text at a character-grid position within the wizard canvas.
		/// Uses integer-per-glyph advance to stay consistent with multi-character string rendering.
		/// </summary>
		private void CharPut(string text, int col, int row, byte colour)
		{
			int glyphW = (int)(ModernDos8X16.GlyphWidth * _scale);
			int glyphH = (int)(ModernDos8X16.GlyphHeight * _scale);
			DrawDosText(text, _box.X + col * glyphW, _box.Y + row * glyphH, colour);
		}
		/// <summary>
		/// Places text at a character-grid position, truncating to fit inside the box right border.
		/// </summary>
		private void BoxPut(string text, int col, int row, byte colour)
		{
			int maxLen = Math.Max(0, _cols - 1 - col);
			CharPut(text.Length > maxLen ? text[..maxLen] : text, col, row, colour);
		}

		/// <summary>
		/// Places text centered horizontally within the box.
		/// </summary>
		private void BoxPutMiddle(string text, int row, byte colour)
		{
			int col = Math.Max(1, (_cols - text.Length) / 2);
			BoxPut(text, col, row, colour);
		}

		private void DrawMouseInversionMarker()
		{
			if (_mouseX < 0 || _mouseY < 0)
			{
				return;
			}

			if (!_box.Contains(_mouseX, _mouseY))
			{
				return;
			}

			int glyphWidth = (int)(ModernDos8X16.GlyphWidth * _scale);
			int glyphHeight = (int)(ModernDos8X16.GlyphHeight * _scale);
			if (glyphWidth <= 0 || glyphHeight <= 0)
			{
				return;
			}

			int col = (_mouseX - _box.X) / glyphWidth;
			int row = (_mouseY - _box.Y) / glyphHeight;
			if (col < 0 || col >= _cols || row < 0 || row >= _rows)
			{
				return;
			}

			Rectangle cell = new(_box.X + (col * glyphWidth), _box.Y + (row * glyphHeight), glyphWidth, glyphHeight);

			// Check if mouse is over a link or menu entry
			bool isOverLink = _linkAreas.Any(link => link.Area.Contains(_mouseX, _mouseY));
			bool isOverMenu = _entryHitAreas.Any(entry => entry.Area.Contains(_mouseX, _mouseY));

			if (isOverLink)
			{
				// Draw character 18 (→) in white
				ModernDosFontRenderer.DrawGlyph(18, cell.X, cell.Y, _scale, (x, y) =>
				{
					if (x >= 0 && y >= 0 && x < Bitmap.Width && y < Bitmap.Height)
					{
						Bitmap[x, y] = 15;
					}
				});
			}
			else if (isOverMenu)
			{
				// Draw character 17 (←) in red over menu entries
				ModernDosFontRenderer.DrawGlyph(17, cell.X, cell.Y, _scale, (x, y) =>
				{
					if (x >= 0 && y >= 0 && x < Bitmap.Width && y < Bitmap.Height)
					{
						Bitmap[x, y] = ColourMenuArrow;
					}
				});
			}
			else
			{
				// Draw normal inversion
				for (int yy = 0; yy < cell.Height; yy++)
				{
					for (int xx = 0; xx < cell.Width; xx++)
					{
						int px = cell.X + xx;
						int py = cell.Y + yy;
						if (px < 0 || py < 0 || px >= Bitmap.Width || py >= Bitmap.Height)
						{
							continue;
						}

						Bitmap[px, py] = InvertColour(Bitmap[px, py]);
					}
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



		private void DrawDosText(string text, int x, int y, byte colour)
		{
			string output = text ?? string.Empty;
			int glyphWidth = (int)(ModernDos8X16.GlyphWidth * _scale);
			int glyphHeight = (int)(ModernDos8X16.GlyphHeight * _scale);

			for (int i = 0; i < output.Length; i++)
			{
				_glyphAreas.Add(new Rectangle(x + (i * glyphWidth), y, glyphWidth, glyphHeight));
			}

			ModernDosFontRenderer.DrawString(output, x, y, _scale, (px, py) =>
			{
				if (px < 0 || py < 0 || px >= Bitmap.Width || py >= Bitmap.Height)
				{
					return;
				}

				Bitmap[px, py] = colour;
			});
		}
	}
}
