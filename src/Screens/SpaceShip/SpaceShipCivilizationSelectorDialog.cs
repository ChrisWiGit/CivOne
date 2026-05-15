// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.Services;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	[Modal, ScreenResizeable]
	internal sealed class SpaceShipCivilizationSelectorDialog : BaseScreen
	{
		private readonly Action<Player> _onSelected;
		private readonly ITranslationService _translationService;
		private readonly Menu<Player> _menu;
		private readonly Picture _capturedBackground;

		private int _lastWidth;
		private int _lastHeight;

		private const int DialogWidth = 188;
		private const int HeaderHeight = 12;

		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);
		private string T(string key) => _translationService.Translate(key);

		private static void OpenViewOnlySpaceShip(Player player)
		{
			if (player == null)
			{
				return;
			}

			Common.AddScreen(new SpaceShipView(player, viewOnly: true));
		}

		private int GetDialogHeight()
		{
			int rowHeight = Resources.GetFontHeight(0);
			return HeaderHeight + (_menu.Items.Count * rowHeight) + 2;
		}

		private void PositionMenu()
		{
			int menuX = OffsetX + ((320 - DialogWidth) / 2) + 2;
			int menuY = OffsetY + ((200 - GetDialogHeight()) / 2) + HeaderHeight;
			_menu.X = menuX;
			_menu.Y = menuY;
			_menu.ForceUpdate();
		}

		private void OnSelect(object sender, MenuItemEventArgs<Player> args)
		{
			_onSelected?.Invoke(args.Value);
			Destroy();
		}

		private static SpaceShipCivilizationSelectorServices ResolveServices(SpaceShipCivilizationSelectorServices services)
		{
			return services ?? SpaceShipCivilizationSelectorServicesFactory.CreateDefault();
		}

		private static Picture CaptureBackground()
		{
			if (Common.TopScreen == null)
			{
				return null;
			}

			return new Picture(Common.TopScreen);
		}

		private static Picture CreateMenuBackground(int rowCount)
		{
			Picture menuBackground = new Picture(DialogWidth - 4, Resources.GetFontHeight(0) * rowCount)
				.Tile(Pattern.PanelGrey)
				.As<Picture>();
			menuBackground.ColourReplace((7, 11), (22, 3));
			return menuBackground;
		}

		private Menu<Player> CreateMenu(Picture menuBackground)
		{
			return new Menu<Player>("SpaceShipCivilizationSelector", Palette, menuBackground)
			{
				MenuWidth = DialogWidth - 4,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 8,
				FontId = 0,
				Indent = 8,
				RowHeight = Resources.GetFontHeight(0)
			};
		}

		private void PopulateMenuItems(SpaceShipCivilizationListItem[] civilizations)
		{
			if (civilizations.Length == 0)
			{
				_menu.Items.Add(T("No civilizations available"), null).SetEnabled(false);
				return;
			}

			for (int i = 0; i < civilizations.Length; i++)
			{
				SpaceShipCivilizationListItem civilization = civilizations[i];
				string tribeName = _translationService.Translate(civilization.Player.TribeNamePlural);
				_menu.Items.Add($"{i + 1}. {tribeName}", civilization.Player)
					.SetEnabled(civilization.IsEnabled)
					.OnSelect(OnSelect);
			}
		}

		private void SelectHumanCivilizationAsDefault()
		{
			for (int i = 0; i < _menu.Items.Count; i++)
			{
				if (_menu.Items[i].Value == Human)
				{
					_menu.ActiveItem = i;
					return;
				}
			}
		}

		private void InitializeLayout()
		{
			_lastWidth = Width;
			_lastHeight = Height;
			PositionMenu();
			Refresh();
		}

		public SpaceShipCivilizationSelectorDialog(Action<Player> onSelected = null, SpaceShipCivilizationSelectorServices services = null) : base(MouseCursor.Pointer)
		{
			SpaceShipCivilizationSelectorServices resolvedServices = ResolveServices(services);
			ISpaceShipCivilizationSelectorService selectorService = resolvedServices.SelectorService ?? throw new InvalidOperationException("SelectorService is required to create SpaceShipCivilizationSelectorDialog");
			_translationService = resolvedServices.TranslationService ?? throw new InvalidOperationException("TranslationService is required to create SpaceShipCivilizationSelectorDialog");
			_onSelected = onSelected ?? OpenViewOnlySpaceShip;

			Palette = Common.TopScreen?.Palette.Copy() ?? Common.DefaultPalette;
			_capturedBackground = CaptureBackground();

			SpaceShipCivilizationListItem[] civilizations = selectorService.GetCivilizations();
			int rowCount = Math.Max(1, civilizations.Length);

			Picture menuBackground = CreateMenuBackground(rowCount);
			_menu = CreateMenu(menuBackground);
			PopulateMenuItems(civilizations);
			SelectHumanCivilizationAsDefault();
			InitializeLayout();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			bool resized = _lastWidth != Width || _lastHeight != Height;
			if (resized)
			{
				_lastWidth = Width;
				_lastHeight = Height;
				PositionMenu();
			}

			bool menuChanged = _menu.Update(gameTick);
			if (!RefreshNeeded() && !resized && !menuChanged)
			{
				return false;
			}

			this.Clear();

			if (_capturedBackground != null)
			{
				int bgX = Math.Max(0, (Width - _capturedBackground.Width()) / 2);
				int bgY = Math.Max(0, (Height - _capturedBackground.Height()) / 2);
				this.AddLayer(_capturedBackground, bgX, bgY);
			}

			int dialogHeight = GetDialogHeight();
			int x = OffsetX + ((320 - DialogWidth) / 2);
			int y = OffsetY + ((200 - dialogHeight) / 2);

			Picture panel = new Picture(DialogWidth, dialogHeight)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.As<Picture>();

			this.FillRectangle(x - 1, y - 1, DialogWidth + 2, dialogHeight + 2, 5)
				.AddLayer(panel, x, y)
				.DrawText(T("Whose spaceship to view?"), 0, 15, x + 8, y + 3)
				.AddLayer(_menu, 0, 0);

			panel.Dispose();
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args[Key.Escape])
			{
				Destroy();
				return true;
			}

			bool handled = _menu.KeyDown(args);
			if (handled)
			{
				Refresh();
			}

			return true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			bool handled = _menu.MouseDown(args);
			if (handled)
			{
				Refresh();
			}

			return true;
		}

		public override bool MouseUp(ScreenEventArgs args)
		{
			bool handled = _menu.MouseUp(args);
			if (handled)
			{
				Refresh();
			}

			return true;
		}

		public override bool MouseDrag(ScreenEventArgs args)
		{
			bool handled = _menu.MouseDrag(args);
			if (handled)
			{
				Refresh();
			}

			return true;
		}

		public override bool MouseMove(ScreenEventArgs args)
		{
			bool handled = _menu.MouseMove(args);
			if (handled)
			{
				Refresh();
			}

			return true;
		}

		public override void Dispose()
		{
			_menu?.Dispose();
			_capturedBackground?.Dispose();
			base.Dispose();
		}
	}
}
