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
using System.Globalization;
using System.IO;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.IO.Text;
using CivOne.Screens.Reports;
using CivOne.Services;
using CivOne.Services.Random;
using CivOne.Tasks;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	/// <summary>
	/// Displays the animated credits sequence and provides the main menu entry point.
	/// </summary>
	/// <remarks>
	/// Shows the intro animation, handles skipping, and exposes the main menu.
	/// </remarks>
	[ScreenResizeable]
	internal class Credits : BaseScreen, ITranslationLanguageObserver
	{
		private const int NOISE_COUNT = 40;
		private const int MENU_Y_OFFSET = 58;
		
		private readonly int[] SHOW_INTRO_LINE = [312, 279, 254, 221, 196, 171, 146, 121, 96, 71, 46, 21, -4, -37, -62, -95, -120, -145, -170, -195, -220, -245, -270, -295];
		private readonly int[] HIDE_INTRO_LINE = [287, 229, -29, -87, -315];
		
		private readonly byte[] _menuColours;
		private readonly string[] _introText;
		private readonly Picture[] _pictures;
		private readonly byte[,] _noiseMap;
		private readonly List<(string Url, Rectangle Area)> _footerLinkAreas = [];
		
		private int _introLeft = 320;
        private int _logoSwipe;
		private int _cycleCounter;
        private int _noiseCounter = NOISE_COUNT;
		
		private bool _allowEnterSetup = true;
		private bool _done;
		private bool _forceRedraw;
		private bool _showIntroLine;
		private bool _introSkipped;
		private int _introLine = -1;
		
		// Auto-Load a saved game at startup? (--load-slot or --load-cos)
		// Used only once, and then reset to null to avoid re-loading when coming back to the credits screen.
		private static bool? _loadSavedGame = false; 

		private IScreen? _overlay; // TODO fire-eggs: with fix for issue #34, this logic may no longer be required

		private IScreen? _nextScreen;

		private Dictionary<char, Action<object, EventArgs>>? _shortKeyMapping;
		private Action<object, EventArgs>? _shortCutAction;
		private int _mouseX = -1;
		private int _mouseY = -1;

		private void DrawFooterLinks()
		{
			_footerLinkAreas.Clear();

			const int margin = 6;
			const int fontId = 1;
			int fontHeight = Resources.GetFontHeight(fontId);

			string discordText = "Discord";
			string separator = "  ";
			string githubText = "GitHub";

			int linksWidth = Resources.GetTextSize(fontId, discordText + separator + githubText).Width;
			int bottomY = Height - margin - fontHeight;
			int linksX = Math.Max(0, Width - margin - linksWidth);
			int discordWidth = Resources.GetTextSize(fontId, discordText).Width;
			int separatorWidth = Resources.GetTextSize(fontId, separator).Width;
			int githubX = linksX + discordWidth + separatorWidth;
			int githubWidth = Resources.GetTextSize(fontId, githubText).Width;

			Rectangle discordArea = new(linksX, bottomY, discordWidth, fontHeight);
			Rectangle githubArea = new(githubX, bottomY, githubWidth, fontHeight);
			_footerLinkAreas.Add((ProjectPublicLinks.Discord, discordArea));
			_footerLinkAreas.Add((ProjectPublicLinks.CurrentGitRepository, githubArea));

			byte discordColour = discordArea.Contains(_mouseX, _mouseY) ? (byte)15 : (byte)9;
			byte githubColour = githubArea.Contains(_mouseX, _mouseY) ? (byte)15 : (byte)9;

			this.DrawText(discordText, fontId, discordColour, linksX, bottomY, TextAlign.Left);
			this.DrawText(separator, fontId, 5, linksX + discordWidth, bottomY, TextAlign.Left);
			this.DrawText(githubText, fontId, githubColour, githubX, bottomY, TextAlign.Left);
		}

		private int GetHoveredFooterLinkIndex(int x, int y)
		{
			for (int i = 0; i < _footerLinkAreas.Count; i++)
			{
				if (_footerLinkAreas[i].Area.Contains(x, y))
				{
					return i;
				}
			}

			return -1;
		}

		private bool UpdateFooterLinkHover(int x, int y)
		{
			int previousHoveredLink = GetHoveredFooterLinkIndex(_mouseX, _mouseY);
			_mouseX = x;
			_mouseY = y;
			int hoveredLink = GetHoveredFooterLinkIndex(_mouseX, _mouseY);

			if (previousHoveredLink == hoveredLink)
			{
				return false;
			}

			_forceRedraw = true;
			Refresh();
			return true;
		}

		private void HandleFooterLinkClick(ScreenEventArgs args)
		{
			if (args.Buttons != MouseButton.Left)
			{
				return;
			}

			foreach ((string url, Rectangle area) in _footerLinkAreas)
			{
				if (!area.Contains(args.Location))
				{
					continue;
				}

				if (!Runtime.TryOpenUrl(url, out string? errorMessage))
				{
					Log("Could not open URL {0}: {1}", url, errorMessage ?? "unknown error");
				}
				return;
			}
		}

		private void RebuildShortcutMapping()
		{
			var menuItems = GetMenuItems();
			_shortKeyMapping = new Dictionary<char, Action<object, EventArgs>>
			{
				{ menuItems[0].ToUpper(CultureInfo.CurrentCulture)[0], StartNewGame },
				{ menuItems[1].ToUpper(CultureInfo.CurrentCulture)[0], LoadSavedGame },
				{ menuItems[2].ToUpper(CultureInfo.CurrentCulture)[0], Earth },
				{ menuItems[3].ToUpper(CultureInfo.CurrentCulture)[0], CustomizeWorld },
				{ menuItems[4].ToUpper(CultureInfo.CurrentCulture)[0], ViewHallOfFame }
			};
		}

		public void OnLanguageChanged(string? _)
		{
			RebuildShortcutMapping();
			CloseMenus();
			_forceRedraw = true;
			Refresh();
		}
		
		private void HandleIntroText()
		{
			_introLeft--;
			if (SHOW_INTRO_LINE.Contains(_introLeft))
			{
				_showIntroLine = true;
				_introLine++;
				Log(@"Credits: ""{0}""", _introText[_introLine]);
			}
			else if (HIDE_INTRO_LINE.Contains(_introLeft))
			{
				_showIntroLine = false;
				Resources.ClearTextCache();
			}
		}
		
		private bool LoadGameCancel
		{
			get
			{
				return _overlay != null && (_overlay.GetType() == typeof(LoadGame) && ((LoadGame)_overlay).Cancel);
			}
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (_loadSavedGame.GetValueOrDefault(false))
			{
				_loadSavedGame = null;
				LoadSavedGame();
				_done = true;
				return true;
			}
				
			if (_nextScreen != null)
			{
				if (!HandleScreenFadeOut(Speed.Slow))
				{
					Common.AddScreen(_nextScreen);
					Destroy();
					return true;
				}
				return true;
			}

			if (_done && !_forceRedraw && (_overlay == null || !_overlay.Update(gameTick))) return false;

			if (!_forceRedraw && (gameTick % 3) == 0) return false;
			
			// Updates
			if (_introLeft > -320)
			{
				HandleIntroText();
			}
			else if (_logoSwipe < 320)
			{
				this.Cycle(224, 254);
				_logoSwipe += 16;
			}
			else if (_cycleCounter < 98)
			{
				this.Cycle(224, 254);
				_cycleCounter++;
			}
			else if (_noiseCounter > 0)
			{
				_pictures[0].ApplyNoise(_noiseMap, --_noiseCounter);
				_pictures[1].ApplyNoise(_noiseMap, --_noiseCounter);
			}
			
			if (_noiseCounter == 0 && HasMenu && !Common.HasScreenType<Menu>() && (_overlay == null || LoadGameCancel))
			{
				CreateMenu();
			}
			
			// Drawing
			int ox = (Width - 320), cx = (ox / 2), cy = (Height - 200) / 2;
			this.Clear();
			if (_introLeft > -320)
			{
				this.AddLayer(_pictures[0], _introLeft + ox, cy)
					.AddLayer(_pictures[1], _introLeft + ox + 320, cy);
			}
			if (_introLeft > -320 && _showIntroLine)
			{
				this.DrawText(_introText[_introLine], (Width / 2), (Height / 2) - 16);
			}
			if (_introLeft == -320 && _noiseCounter > 0)
			{
				if (!_introSkipped)
				{
					this.AddLayer(_pictures[0], ox - 320, cy)
						.AddLayer(_pictures[1], ox, cy);
				}
				if (_logoSwipe < 320)
				{
					if (_logoSwipe > 0)
					{
						this.AddLayer(_pictures[2][0, 0, _logoSwipe, 200], cx, cy);
					}
				}
				else
				{
					this.AddLayer(_pictures[2], cx, cy);	
				}
				if (_introSkipped)
				{
					this.AddLayer(_pictures[0], ox - 320, cy)
						.AddLayer(_pictures[1], ox, cy);
				}
			}
			else if (_noiseCounter == 0)
			{
				this.AddLayer(_pictures[2], cx, cy);
				this.ResetPalette();
				_done = true;
				DrawFooterLinks();
				
				if (_overlay != null)
				{
					this.AddLayer(_overlay);
					if (_overlay.GetType() == typeof(LoadGame) && ((LoadGame)_overlay).Cancel)
					{
						CreateMenu();
					}
					if (!HasMenu) return true;
				}
				
				// Draw menu background
				int mx = ((Width - 120) / 2), my = Height - (MENU_Y_OFFSET + 4);
				this.FillRectangle(mx, my, 122, 57, 5)
					.FillRectangle(mx + 1, my + 1, 120, 55, _menuColours[0])
					.FillRectangle(mx + 1, my + 2, 119, 54, _menuColours[1])
					.FillRectangle(mx + 2, my + 2, 118, 53, _menuColours[2]);
				
				CreateMenu();
			}
			_forceRedraw = false;
			return true;
		}
		
		public bool SkipIntro()
		{
			if (_introSkipped || _noiseCounter < NOISE_COUNT) return false;
			
			_showIntroLine = false;
			_introLeft = -320;
			_logoSwipe = 320;
			_cycleCounter = 98;
			_pictures[0].Clear(5);
			_pictures[1].Clear(5);
			_introSkipped = true;
			
			return true;
		}

        public void SkipLogo()
        {
            // fire-eggs: part of fix for issue #34
            // when user has cancelled out of "load game": don't animate the logo
            _logoSwipe = 350;
            _noiseCounter = 0;
        }

		// Returns menu items resolved through the active translation service.
		private string[] GetMenuItems()
		{
			return
			[
				Translate("Start a New Game"),
				Translate("Load a Saved Game"),
				Translate("EARTH"),
				Translate("Customize World"),
				Translate("View Hall of Fame")
			];
		}

		private void CreateMenu()
		{
			_allowEnterSetup = false;
			Runtime.SetWindowTitle(Settings.WindowTitle);

			if (HasMenu) return;
			Menu menu = new Menu("MainMenu", Palette)
			{
					X = ((Width - 120) / 2) + 3,
				Y = Height - MENU_Y_OFFSET,
				MenuWidth = 116,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 8,
				FontId = 0,
				OnShiftF1 = () =>
				{
					if (!_allowEnterSetup) return;
					
					GameTask.Enqueue(Show.Screens(typeof(Setup), typeof(Credits)));
					Destroy();
				}
			};
			menu.MissClickAt += (_, args) => HandleFooterLinkClick(args);
			menu.MouseMoveAt += (_, args) => UpdateFooterLinkHover(args.X, args.Y);

			var items = GetMenuItems();
			menu.Items.Add(items[0]).OnSelect(StartNewGame);
			menu.Items.Add(items[1]).OnSelect(LoadSavedGame);
			menu.Items.Add(items[2]).OnSelect(Earth);
			menu.Items.Add(items[3]).OnSelect(CustomizeWorld);
			menu.Items.Add(items[4]).OnSelect(ViewHallOfFame);
			menu.Items.Add(Translate("Change language...")).OnSelect(ChangeLanguage);

			AddMenu(menu);

			_shortCutAction?.Invoke(this, EventArgs.Empty);
			_shortCutAction = null;
		}

		private void StartIntro()
		{
			foreach (IMenu menu in Menus)
				this.AddLayer(menu);
			CloseMenus();
			if (!Runtime.Settings.ShowIntro)
			{
				_nextScreen = new NewGame();
			}
			else
			{
				_nextScreen = new Intro();
			}
		}
		
		private void StartNewGame(object sender, EventArgs args)
		{
			Log("Main Menu: Start a New Game");
			Map.UseDefaultMapSize();
			Map.Generate();
			StartIntro();
		}
		
		private void LoadSavedGame(object sender, EventArgs args)
		{
			Log("Main Menu: Load a Saved Game");
            // fire-eggs: fix issue #34: switch to the LoadGame screen; on cancel there, come back to here
            Destroy();
            Common.AddScreen(new LoadGame());
		}

		private void LoadSavedGame()
		{
			// Check if --load-cos was specified
			if (!string.IsNullOrEmpty(Runtime.Settings.LoadCosFile))
			{
				Log("Main Menu: Load a YAML game from {0}", Runtime.Settings.LoadCosFile);
				Destroy();
				if (!Game.LoadYamlGame(Runtime.Settings.LoadCosFile))
				{
					Log("Failed to load YAML game");
					Common.AddScreen(new Credits());

					var savegameName = Path.GetFileName(Runtime.Settings.LoadCosFile);
					GameTask.Enqueue(Message.Error(
						Translate("-- Civilization Note --"),
						TranslateFormattedArray("Could not load save game from --load-cos.\nFile: {0}", savegameName)));
					return;
				}
				Common.DestroyScreen(Common.Screens.FirstOrDefault(s => s is GamePlay, null));
				Common.AddScreen(new GamePlay());
				return;
			}

			var slot = Runtime.Settings.LoadSaveGameSlot;

			if (slot == null)
			{
				Log("Main Menu: Load Saved Game requested but no slot specified");
				StartIntro();
				return;
			}


			if (slot.Equals(RuntimeSettings.UseLoadingScreen))
			{
				LoadSavedGame(this, EventArgs.Empty);
				return;
			}

			Log("Main Menu: Load a Saved Game with drive letter {0} and item {1}", slot.Item1, slot.Item2);

			Destroy();
			LoadGame.LoadSaveFile(slot.Item1, slot.Item2);
		}
		
		private void Earth(object sender, EventArgs args)
		{
			Log("Main Menu: EARTH");
			Map.LoadEarthMapInThread();
			StartIntro();
		}
		
		private void CustomizeWorld(object sender, EventArgs args)
		{
			Log("Main Menu: Customize World");
			Destroy();
			Common.AddScreen(new CustomizeWorld());
		}

		private static void ViewHallOfFame(object sender, EventArgs args)
		{
			Log("Main Menu: View Hall of Fame");
			Common.AddScreen(HallOfFameScreenFactory.ViewScore());
		}

		private static void ChangeLanguage(object sender, EventArgs args)
		{
			Log("Main Menu: Change language");
			Common.AddScreen(new LanguageScreen());
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_allowEnterSetup && args.Shift && args.Key == Key.F1)
			{
				GameTask.Enqueue(Show.Screens(typeof(Setup), typeof(Credits)));
				Destroy();
				return true;
			}

			if (_done && _overlay != null)
				return _overlay.KeyDown(args);


			if (_shortKeyMapping!.TryGetValue(char.ToUpper(args.KeyChar, CultureInfo.CurrentCulture), out var action))
			{
				_shortCutAction = action;
				// invoke in CreateMenu to show title first and then execute action
			}
			
			return SkipIntro();
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_done && _overlay != null)
				return _overlay.MouseDown(args);
			return SkipIntro();
		}
		
		public override bool MouseUp(ScreenEventArgs args)
		{
			if (_done && _overlay != null)
				return _overlay.MouseUp(args);
			return false;
		}
		
		public override bool MouseDrag(ScreenEventArgs args)
		{
			if (_done && _overlay != null)
				return _overlay.MouseDrag(args);
			return false;
		}

		public override bool MouseMove(ScreenEventArgs args)
		{
			if (_done && _overlay != null)
			{
				return _overlay.MouseMove(args);
			}

			return UpdateFooterLinkHover(args.X, args.Y);
		}
		
		public override MouseCursor Cursor
		{
			get
			{
				if (_overlay != null && !LoadGameCancel)
					return _overlay.Cursor;
				return base.Cursor;
			}
		}

		private void Resize(object? _, ResizeEventArgs args)
		{
			_forceRedraw = true;
			if (_done)
			{
				DrawFooterLinks();
			}
			foreach (Menu menu in Common.Screens.OfType<Menu>().Where(menu => menu.Id == "MainMenu"))
			{
				menu.X = ((Width - 120) / 2) + 3;
				menu.Y = Height - MENU_Y_OFFSET;
				menu.ForceUpdate();
			}
		}

		public Credits()
		{
			Runtime.SetWindowTitle($"{Settings.WindowTitle} (press SHIFT+F1 to enter Setup)");

			OnResize += Resize;
			Closed += (s, a) =>
			{
				TranslationServiceFactory.UnregisterLanguageObserver(this);
				Runtime.SetWindowTitle(Settings.WindowTitle);
			};
			TranslationServiceFactory.RegisterLanguageObserver(this);

			_introText = TextFileFactory.LoadTextFile("CREDITS");
			if (_introText.Length == 0) _introText = new string[25];
			_pictures = new Picture[3];
			for (int i = 0; i < 2; i++)
			{
				_pictures[i] = Resources[$"BIRTH{i}"];
			}
			_pictures[2] = Resources["LOGO"];
			_noiseMap = new byte[320, 200];
			for (int x = 0; x < 320; x++)
			{
				for (int y = 0; y < 200; y++)
				{
					byte noiseMaxExclusive = (byte)Math.Clamp(_noiseCounter, 2, byte.MaxValue);
					_noiseMap[x, y] = RandomServiceFactory.Create().NextByte(1, noiseMaxExclusive);
				}
			}
			DefaultTextSettings = Settings.GraphicsMode switch
			{
				GraphicsMode.Graphics256 => TextSettings.ThreeLayers(244, 248, 242),
				GraphicsMode.Graphics16 => TextSettings.ThreeLayers(15, 15, 7),
				_ => throw new InvalidOperationException($"Unsupported graphics mode: {Settings.GraphicsMode}"),
			};
			DefaultTextSettings.Alignment = TextAlign.Center;
			DefaultTextSettings.FontId = 4;

			_menuColours = [8, 15, 7];

			Palette = _pictures[2].Palette;

			if (Settings.Sound != GameOption.Off)
			{
				// In this stage using Game.PlaySound() is not possible, as the Game instance is not yet created.
				Runtime.PlaySound(Extensions.GetSoundFile("OPENING"));
			}

			if (!Runtime.Settings.ShowCredits) SkipIntro();
			
			bool loadedSavedGameSlot = Runtime.Settings.LoadSaveGameSlot != null;
			bool loadedCosFile = !string.IsNullOrEmpty(Runtime.Settings.LoadCosFile);

			if ((loadedSavedGameSlot || loadedCosFile) && _loadSavedGame.HasValue)
			{
				_loadSavedGame = true;
			}

			RebuildShortcutMapping();
		}
	}
}