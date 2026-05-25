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
using CivOne.Events;
using CivOne.Graphics;

namespace CivOne.Screens.GamePlayPanels
{
	internal class MenuBar : BaseScreen
	{
		private static readonly int[] MenuXPositions =
		[
			8,
			64,
			128,
			192,
			240
		];
		
		public event EventHandler GameSelected;
		public event EventHandler OrdersSelected;
		public event EventHandler AdvisorsSelected;
		public event EventHandler WorldSelected;
		public event EventHandler CivilopediaSelected;

		public bool MenuDrag { get; private set; }
		
		private readonly Rectangle[] _rectMenus;
		private readonly MenuBarHotkeyDelegate _menuBarHotkeyDelegate;
		
		private MenuBarTitle[] _menuTitles;
		private bool _update;
		private int _mouseX, _mouseY;

		private void RebuildMenuTitles()
		{
			_menuTitles =
			[
				// it is necessary to provide the translation key as well as the translated text
				// because the translated text may not contain the marker character '~' to indicate the hotkey position
				// Futhermore the civtranslate cli will parse Translate(" to extract the translation keys.
				_menuBarHotkeyDelegate.Create(translationKey: "GAME", translatedText: Translate("GAME")),
				_menuBarHotkeyDelegate.Create(translationKey: "ORDERS", translatedText: Translate("ORDERS")),
				_menuBarHotkeyDelegate.Create(translationKey: "ADVISORS", translatedText: Translate("ADVISORS")),
				_menuBarHotkeyDelegate.Create(translationKey: "WORLD", translatedText: Translate("WORLD")),
				_menuBarHotkeyDelegate.Create(translationKey: "CIVILOPEDIA", translatedText: Translate("CIVILOPEDIA"))
			];
		}

		private void EnsureMenuTitles()
		{
			if (_update || _menuTitles == null)
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
			}
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (_update)
			{
				RebuildMenuTitles();
				this.Clear(5);
				for (int i = 0; i < MenuXPositions.Length; i++)
				{
					this.DrawText(_menuTitles[i].VisibleText, MenuXPositions[i], 1, TextSettings.DifferentCharacter(15, 7, _menuTitles[i].HighlightedCharacterIndex));
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

			for (int i = 0; i < _rectMenus.Length; i++)
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
			
			_rectMenus = new Rectangle[5];
			_rectMenus[0] = new Rectangle(0, 0, 56, 8);
			_rectMenus[1] = new Rectangle(56, 0, 64, 8);
			_rectMenus[2] = new Rectangle(120, 0, 64, 8);
			_rectMenus[3] = new Rectangle(184, 0, 48, 8);
			_rectMenus[4] = new Rectangle(232, 0, 88, 8);
		}
	}
}