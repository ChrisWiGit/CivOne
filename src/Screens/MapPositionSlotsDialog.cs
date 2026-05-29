using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	[Modal, ScreenResizeable]
	internal sealed class MapPositionSlotsDialog : BaseScreen
	{
		private const int MapPositionRenameMaxLength = 70;

		private readonly int[] _slots;
		private readonly Action<int> _onSelect;
		private readonly Action<int, string> _onRename;
		private readonly Picture? _capturedBackground;
		private readonly Picture _menuBackground;
		private readonly Menu<int> _menu;
		private readonly HashSet<int> _validSlots;
		private readonly Dictionary<int, string> _slotNames;
		private readonly Dictionary<int, MenuItem<int>> _slotItems = [];
		private InputDialogDelegate? _inputDialog;

		private int _lastWidth;
		private int _lastHeight;

		private const int DialogBaseX = 6;
		private const int DialogBaseY = 6;
		private const int MenuInnerWidth = 320;

		public MapPositionSlotsDialog(
			IReadOnlyList<int> slots,
			Action<int> onSelect,
			IReadOnlyDictionary<int, string> slotNames,
			Action<int, string> onRename) : base(MouseCursor.Pointer)
		{
			_slots = slots?.OrderBy(x => x).ToArray() ?? throw new ArgumentNullException(nameof(slots));
			_onSelect = onSelect ?? throw new ArgumentNullException(nameof(onSelect));
			_onRename = onRename ?? throw new ArgumentNullException(nameof(onRename));
			_validSlots = [.. _slots];
			_slotNames = _slots.ToDictionary(
				slot => slot,
				slot => slotNames != null && slotNames.TryGetValue(slot, out var name) ? (name ?? string.Empty) : string.Empty);

			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;

			if (Common.TopScreen != null)
			{
				_capturedBackground = new Picture(Common.TopScreen);
			}

			int rowHeight = Resources.GetFontHeight(0);
			int rows = Math.Max(1, _slots.Length);
			int menuHeight = rows * rowHeight;

			_menuBackground = new Picture(MenuInnerWidth, menuHeight)
				.Tile(Pattern.PanelGrey)
				.As<Picture>();
			_menuBackground.ColourReplace((7, 11), (22, 3));

			_menu = new("MapPositionSlots", Palette, _menuBackground)
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

			foreach (int slot in _slots)
			{
				int selectedSlot = slot;
				var menuItem = _menu.Items
					.Add(BuildSlotText(selectedSlot), selectedSlot)
					.OnSelect((_, __) => SelectSlot(selectedSlot));

				_slotItems[selectedSlot] = menuItem;
			}

			_lastWidth = Width;
			_lastHeight = Height;
			RepositionMenu();
			Refresh();
		}

		private bool RenameActive => _inputDialog?.Active == true;

		private string BuildSlotText(int slot)
		{
			if (!_slotNames.TryGetValue(slot, out var name) || string.IsNullOrWhiteSpace(name))
			{
				return $"{slot} - {TranslateFormatted("Map position {0}", slot)}";
			}

			return $"{slot} - {name}";
		}

		private static bool IsRenameKey(KeyboardEventArgs args)
		{
			return args.Modifier == KeyModifier.None && args[Key.Character] && (args.KeyChar == 'r' || args.KeyChar == 'R');
		}

		private void StartRenameSelectedSlot()
		{
			if (RenameActive)
			{
				return;
			}

			int slot = _menu.SelectedItem.Value;
			if (!_validSlots.Contains(slot))
			{
				return;
			}

			_slotNames.TryGetValue(slot, out string? currentName);
			_inputDialog = new InputDialogDelegate(TranslateFormatted("Rename map position {0}", slot), MapPositionRenameMaxLength);
			_inputDialog.Accepted += value => ApplyRename(slot, value);
			_inputDialog.Cancelled += (_, _) =>
			{
				_inputDialog.Close();
				_inputDialog = null;
				Refresh();
			};
			_inputDialog.Open(currentName ?? string.Empty);
			Refresh();
		}

		private void ApplyRename(int slot, string value)
		{
			string newName = (value ?? string.Empty).Trim();
			if (newName.Length > MapPositionRenameMaxLength)
			{
				newName = newName[..MapPositionRenameMaxLength];
			}

			_slotNames[slot] = newName;
			_onRename(slot, newName);
			if (_slotItems.TryGetValue(slot, out MenuItem<int>? menuItem))
			{
				menuItem.Text = BuildSlotText(slot);
			}

			_menu.ForceUpdate();
			_inputDialog = null;
			Refresh();
		}

		private void SelectSlot(int slot)
		{
			Destroy();
			_onSelect(slot);
		}

		private static bool TryGetSlotFromArgs(KeyboardEventArgs args, out int slot)
		{
			slot = args.Key switch
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

			if (slot > 0)
			{
				return true;
			}

			if (!args[Key.Character])
			{
				return false;
			}

			if (args.KeyChar < '1' || args.KeyChar > '9')
			{
				return false;
			}

			slot = args.KeyChar - '0';
			return true;
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

			int offsetPanelX = Math.Max(0, (Width - 320) / 2);
			int offsetPanelY = Math.Max(0, (Height - 200) / 2);
			int itemHeight = Resources.GetFontHeight(0);
			int menuHeight = Math.Max(1, _menu.Items.Count) * itemHeight;
			int panelHeight = menuHeight + 12;

			this.FillRectangle(offsetPanelX + DialogBaseX - 1, offsetPanelY + DialogBaseY - 1, MenuInnerWidth + 6, panelHeight + 2, 5)
				.FillRectangle(offsetPanelX + DialogBaseX, offsetPanelY + DialogBaseY, MenuInnerWidth + 4, panelHeight, 11)
				.FillRectangle(offsetPanelX + DialogBaseX + 1, offsetPanelY + DialogBaseY + 1, MenuInnerWidth + 2, panelHeight - 2, 7)
				.DrawText(Translate("Map position. Select a number..."), 0, 15, offsetPanelX + DialogBaseX + 4, offsetPanelY + DialogBaseY + 3);

			this.AddLayer(_menu, 0, 0);
			_inputDialog?.Draw(this, gameTick, Width, Height);
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (RenameActive)
			{
				bool renameHandled = _inputDialog?.KeyDown(args) ?? false;
				if (renameHandled)
				{
					Refresh();
				}
				return true;
			}

			if (IsRenameKey(args))
			{
				StartRenameSelectedSlot();
				return true;
			}

			if (TryGetSlotFromArgs(args, out int slot) && _validSlots.Contains(slot))
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
			if (RenameActive && (_inputDialog?.MouseDown(args) ?? false))
			{
				Refresh();
				return true;
			}

			bool handled = _menu.MouseDown(args);
			if (handled)
			{
				Refresh();
			}
			return handled;
		}

		public override bool MouseUp(ScreenEventArgs args)
		{
			if (RenameActive)
			{
				return true;
			}

			bool handled = _menu.MouseUp(args);
			if (handled)
			{
				Refresh();
			}
			return handled;
		}

		public override bool MouseDrag(ScreenEventArgs args)
		{
			if (RenameActive)
			{
				return true;
			}

			bool handled = _menu.MouseDrag(args);
			if (handled)
			{
				Refresh();
			}
			return handled;
		}

		public override bool MouseMove(ScreenEventArgs args)
		{
			if (RenameActive)
			{
				return true;
			}

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