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
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;

namespace CivOne.Screens.GamePlayPanels
{
	internal class MenuBar : BaseScreen
	{
		private static bool DebugMenuEnabled => Settings.DebugMenu || RuntimeHandler.Runtime?.Settings.Get<bool>("debug") == true;
		
		public event EventHandler? GameSelected;
		public event EventHandler? OrdersSelected;
		public event EventHandler? AdvisorsSelected;
		public event EventHandler? WorldSelected;
		public event EventHandler? CivilopediaSelected;
		public event EventHandler? TerrainSelected;

		public bool MenuDrag { get; private set; }
		
		private readonly Rectangle[] _rectMenus;
		private readonly MenuBarHotkeyDelegate _menuBarHotkeyDelegate;
		
		private MenuBarTitle[] _menuTitles = [];
		private int[] _menuXPositions = [];
		private bool _update;
		private int _mouseX, _mouseY;

		private static int ExpectedMenuCount => DebugMenuEnabled ? 6 : 5;

		private bool MenuTitlesOutOfDate() => _menuTitles.Length != ExpectedMenuCount;

		private void BuildMenuLayout()
		{
			if (_menuTitles.Length == 0)
			{
				_menuXPositions = [];
				return;
			}

			int[] titleWidths = new int[_menuTitles.Length];
			for (int i = 0; i < _menuTitles.Length; i++)
			{
				titleWidths[i] = Resources.GetTextSize(0, _menuTitles[i].VisibleText).Width;
			}

			int leftPadding = 8;
			int rightPadding = 4;
			int availableWidth = Math.Max(0, Width - leftPadding - rightPadding);
			int totalTitleWidth = titleWidths.Sum();
			int menuCount = _menuTitles.Length;
			int gapCount = Math.Max(0, menuCount - 1);
			int gap = (gapCount > 0)
				? Math.Max(1, (availableWidth - totalTitleWidth) / gapCount)
				: 0;

			_menuXPositions = new int[menuCount];
			int x = leftPadding;
			for (int i = 0; i < menuCount; i++)
			{
				_menuXPositions[i] = x;
				x += titleWidths[i] + gap;
			}

			for (int i = 0; i < _rectMenus.Length; i++)
			{
				if (i < menuCount)
				{
					int left = (i == 0) ? 0 : Math.Max(0, _menuXPositions[i] - 2);
					int right = (i == (menuCount - 1))
						? (Width - 1)
						: Math.Min(Width - 1, _menuXPositions[i + 1] - 1);
					int rectWidth = Math.Max(1, right - left + 1);
					_rectMenus[i] = new Rectangle(left, 0, rectWidth, Height);
				}
				else
				{
					_rectMenus[i] = Rectangle.Empty;
				}
			}
		}

		private void RebuildMenuTitles()
		{
			var titles = new System.Collections.Generic.List<MenuBarTitle>
			{
				// it is necessary to provide the translation key as well as the translated text
				// because the translated text may not contain the marker character '~' to indicate the hotkey position
				// Futhermore the civtranslate cli will parse Translate(" to extract the translation keys.
				_menuBarHotkeyDelegate.Create(translationKey: "GAME", translatedText: Translate("GAME")),
				_menuBarHotkeyDelegate.Create(translationKey: "ORDERS", translatedText: Translate("ORDERS")),
				_menuBarHotkeyDelegate.Create(translationKey: "ADVISORS", translatedText: Translate("ADVISORS")),
				_menuBarHotkeyDelegate.Create(translationKey: "WORLD", translatedText: Translate("WORLD")),
				_menuBarHotkeyDelegate.Create(translationKey: "CIVILOPEDIA", translatedText: Translate("CIVILOPEDIA"))
			};

			if (DebugMenuEnabled)
			{
				titles.Add(_menuBarHotkeyDelegate.Create(translationKey: "TERRAIN", translatedText: Translate("TERRAIN")));
			}

			_menuTitles = [.. titles];
			BuildMenuLayout();
		}

		private void EnsureMenuTitles()
		{
			if (_update || MenuTitlesOutOfDate())
			{
				RebuildMenuTitles();
			}
		}

		private bool TryGetMenuIndex(char keyChar, out int menuIndex)
		{
			EnsureMenuTitles();
			char normalizedKey = char.ToUpperInvariant(keyChar);
			for (int i = 0; i < _menuTitles.Length; i++)
			{
				if (_menuTitles[i].Hotkey == normalizedKey)
				{
					menuIndex = i;
					return true;
				}
			}

			menuIndex = -1;
			return false;
		}

		private void SelectMenu(int menuIndex)
		{
			switch (menuIndex)
			{
				case 0:
					GameSelected?.Invoke(this, EventArgs.Empty);
					break;
				case 1:
					OrdersSelected?.Invoke(this, EventArgs.Empty);
					break;
				case 2:
					AdvisorsSelected?.Invoke(this, EventArgs.Empty);
					break;
				case 3:
					WorldSelected?.Invoke(this, EventArgs.Empty);
					break;
				case 4:
					CivilopediaSelected?.Invoke(this, EventArgs.Empty);
					break;
				case 5:
					TerrainSelected?.Invoke(this, EventArgs.Empty);
					break;
			}
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (MenuTitlesOutOfDate())
			{
				// if the menu titles change due to a language change or debug mode toggle, we need to rebuild the menu layout to update the 
				// title positions and menu rectangles.
				_update = true;
			}

			if (_update)
			{
				RebuildMenuTitles();
				this.Clear(5);
				for (int i = 0; i < _menuTitles.Length; i++)
				{
					this.DrawText(_menuTitles[i].VisibleText, _menuXPositions[i], 1, TextSettings.DifferentCharacter(15, 7, _menuTitles[i].HighlightedCharacterIndex));
				}

				_update = false;
				return true;
			}
			return false;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (!args.Alt) return false;

			if (!TryGetMenuIndex(args.KeyChar, out int menuIndex)) return false;
			SelectMenu(menuIndex);
			MenuDrag = false;
			return true;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			_mouseX = args.X;
			_mouseY = args.Y;
			EnsureMenuTitles();

			for (int i = 0; i < _menuTitles.Length; i++)
			{
				if (_rectMenus[i].Contains(args.Location))
				{
					SelectMenu(i);
				}
			}
			
			return false;
		}
		
		public override bool MouseUp(ScreenEventArgs args)
		{
			MenuDrag = !(args.X == _mouseX && args.Y == _mouseY);

			return false;
		}

		public void Resize()
		{
			_update = true;
		}
		
		public MenuBar(Palette palette) : base(320, 8)
		{
			_menuBarHotkeyDelegate = new();
			Palette = palette.Copy();
			this.Clear(5);
			_update = true;

			DefaultTextSettings = TextSettings.DifferentCharacter(15, 7, 0);
			
			_rectMenus = new Rectangle[6];
			_rectMenus[0] = new Rectangle(0, 0, 50, 8);
			_rectMenus[1] = new Rectangle(50, 0, 54, 8);
			_rectMenus[2] = new Rectangle(104, 0, 54, 8);
			_rectMenus[3] = new Rectangle(158, 0, 54, 8);
			_rectMenus[4] = new Rectangle(212, 0, 54, 8);
			_rectMenus[5] = new Rectangle(266, 0, 54, 8);
		}
	}
}