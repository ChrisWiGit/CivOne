using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.Mcp.Tools;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	[Modal, ScreenResizeable]
	internal sealed class QuickLoadSlotsDialog : BaseScreen
	{
		private readonly int[] _slots;
		private readonly Action<int> _onSelect;
		private readonly Picture? _capturedBackground;
		private readonly Picture _menuBackground;
		private readonly Menu<int> _menu;
		private readonly HashSet<int> _validSlots = [];

		private int _lastWidth;
		private int _lastHeight;

		private const int DialogBaseX = 6;
		private const int DialogBaseY = 6;
		private const int MenuInnerWidth = 320;

		public QuickLoadSlotsDialog(IReadOnlyList<int> slots, Action<int> onSelect) : base(MouseCursor.Pointer)
		{
			_slots = slots?.OrderBy(x => x).ToArray() ?? throw new ArgumentNullException(nameof(slots));
			_onSelect = onSelect ?? throw new ArgumentNullException(nameof(onSelect));

			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;
			
			if (Common.TopScreen != null)
			{
				_capturedBackground = new Picture(Common.TopScreen);
			}

			int rowHeight = Resources.GetFontHeight(0);
			int rows = Math.Max(1, _slots.Length);
			int menuHeight = rows * rowHeight;

			_menuBackground = new Picture(MenuInnerWidth, menuHeight);
			_menuBackground
				.Tile(Pattern.PanelGrey)
				.ColourReplace((7, 11), (22, 3));

			_menu = new("QuickSaveLoadSlots", Palette, _menuBackground)
			{
				MenuWidth = MenuInnerWidth,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 5,
				FontId = 0,
				Indent = 8,
				RowHeight = rowHeight
			};

			_menu.Cancel += (_, _) => Destroy();
			_menu.MissClick += (_, _) => Destroy();

			if (_slots.Length == 0)
			{
				_menu.Items
					.Add(Translate("No fast savegames available. Use Ctrl+F1-F10 to save."), 0)
					.Disable();
			}
			else
			{
				foreach (int slot in _slots)
				{
					int selectedSlot = slot;
					var slotEntry = BuildSlotEntry(selectedSlot);
					MenuItem<int> menuItem = _menu.Items
						.Add(slotEntry.Text, selectedSlot)
						.OnSelect((_, __) => SelectSlot(selectedSlot));

					if (!slotEntry.IsValid)
					{
						menuItem.Disable();
					}
					else
					{
						_validSlots.Add(selectedSlot);
					}
				}
			}

			_lastWidth = Width;
			_lastHeight = Height;
			RepositionMenu();
			Refresh();
		}

		private void SelectSlot(int slot)
		{
			Destroy();
			_onSelect(slot);
		}

		private static string GetSlotFilePath(int slot)
			=> Path.Combine(Runtime.StorageDirectory, "saves", $"fastsave_f{slot}.cos");

		private static (string Text, bool IsValid) BuildSlotEntry(int slot)
		{
			string filePath = GetSlotFilePath(slot);
			string fileName = Path.GetFileName(filePath);

			if (!CosSaveFileInspector.TryInspect(filePath, out CosSaveFileInspection? inspection))
			{
				return ($"F{slot} - Invalid savegame", false);
			}

			if (!string.IsNullOrWhiteSpace(inspection?.Meta?.DisplayName))
			{
				return ($"F{slot} - {inspection.Meta.DisplayName}", true);
			}

			return ($"F{slot} - {fileName}", true);
		}

		private static bool TryGetSlot(Key key, out int slot)
		{
			slot = key switch
			{
				Key.F1 => 1,
				Key.F2 => 2,
				Key.F3 => 3,
				Key.F4 => 4,
				Key.F5 => 5,
				Key.F6 => 6,
				Key.F7 => 7,
				Key.F8 => 8,
				Key.F9 => 9,
				Key.F10 => 10,
				_ => 0
			};

			return slot > 0;
		}

		private void RepositionMenu()
		{
			int offsetX = Math.Max(0, (Width - 320) / 2);
			int offsetY = Math.Max(0, (Height - 200) / 2);
			_menu.X = offsetX + DialogBaseX + 2;
			_menu.Y = offsetY + DialogBaseY + 11;
			_menu.ForceUpdate();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			bool resized = (_lastWidth != Width || _lastHeight != Height);
			if (resized)
			{
				_lastWidth = Width;
				_lastHeight = Height;
			}

			bool menuChanged = _menu.Update(gameTick);
			if (!RefreshNeeded() && !menuChanged && !resized)
			{
				return false;
			}

			if (resized)
			{
				RepositionMenu();
				_ = _menu.Update(gameTick);
			}

			Bitmap.Clear();

			if (_capturedBackground != null)
			{
				int bgX = Math.Max(0, (Width - _capturedBackground.Width()) / 2);
				int bgY = Math.Max(0, (Height - _capturedBackground.Height()) / 2);
				this.AddLayer(_capturedBackground, bgX, bgY);
			}

			int offsetX = Math.Max(0, (Width - 320) / 2);
			int offsetY = Math.Max(0, (Height - 200) / 2);
			int itemHeight = Resources.GetFontHeight(0);
			int menuHeight = Math.Max(1, _menu.Items.Count) * itemHeight;
			int panelHeight = menuHeight + 12;

			this.FillRectangle(offsetX + DialogBaseX - 1, offsetY + DialogBaseY - 1, MenuInnerWidth + 6, panelHeight + 2, 5)
				.FillRectangle(offsetX + DialogBaseX, offsetY + DialogBaseY, MenuInnerWidth + 4, panelHeight, 11)
				.FillRectangle(offsetX + DialogBaseX + 1, offsetY + DialogBaseY + 1, MenuInnerWidth + 2, panelHeight - 2, 7)
				.DrawText(Translate("Quick Load Slots"), 0, 15, offsetX + DialogBaseX + 4, offsetY + DialogBaseY + 3);

			this.AddLayer(_menu, 0, 0);
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (TryGetSlot(args.Key, out int slot) && _validSlots.Contains(slot))
			{
				SelectSlot(slot);
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
			return handled;
		}

		public override bool MouseUp(ScreenEventArgs args)
		{
			bool handled = _menu.MouseUp(args);
			if (handled)
			{
				Refresh();
			}
			return handled;
		}

		public override bool MouseDrag(ScreenEventArgs args)
		{
			bool handled = _menu.MouseDrag(args);
			if (handled)
			{
				Refresh();
			}
			return handled;
		}

		public override bool MouseMove(ScreenEventArgs args)
		{
			bool handled = _menu.MouseMove(args);
			if (handled)
			{
				Refresh();
			}
			return handled;
		}

		public override void Dispose()
		{
			_menuBackground?.Dispose();
			_capturedBackground?.Dispose();
			_menu?.Dispose();
			base.Dispose();
		}
	}
}