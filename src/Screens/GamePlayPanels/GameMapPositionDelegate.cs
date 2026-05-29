// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Screens;
using CivOne.Graphics;
using CivOne.Enums;
using CivOne.Events;

namespace CivOne.Screens.GamePlayPanels
{
	internal partial class GameMap
	{
		/// <summary>
		/// Handles saving and restoring map-position slots via hotkeys.
		/// </summary>
		internal class GameMapPositionDelegate(GameMap gameMap)
		{
			private const int MapPositionNameMaxLength = 70;

			private readonly GameMap _gameMap = gameMap;
			private InputDialogDelegate? _renameDialog;
			private string _renameDialogTitle = string.Empty;

			public bool HasRenameDialog => _renameDialog?.Active == true;

			public string RenameDialogText => _renameDialog?.Text ?? string.Empty;

			public string RenameDialogTitle => _renameDialogTitle;

			public bool TryHandleMapPositionHotkey(KeyboardEventArgs args)
			{
				if ((args.Modifier != KeyModifier.Control && args.Modifier != KeyModifier.Alt) || !TryGetSlotFromInput(args, out var slot))
				{
					return false;
				}

				if (args.Modifier == KeyModifier.Control)
				{
					SaveMapPositionSlot(slot);
					if (HasMapPositionName(slot))
					{
						StartRenameMapPositionSlot(slot);
					}
					return true;
				}

				return JumpToMapPositionSlot(slot);
			}

			public bool KeyDownRenameDialog(KeyboardEventArgs args)
			{
				InputDialogDelegate? renameDialog = _renameDialog;
				if (renameDialog == null)
				{
					return false;
				}

				renameDialog.KeyDown(args);
				_gameMap._update = true;
				return true;
			}

			public bool MouseDownRenameDialog(ScreenEventArgs args)
			{
				InputDialogDelegate? renameDialog = _renameDialog;
				if (renameDialog == null)
				{
					return false;
				}

				renameDialog.MouseDown(args);
				_gameMap._update = true;
				return true;
			}

			public void DrawRenameDialog(IBitmap target, uint gameTick, int width, int height)
			{
				_renameDialog?.Draw(target, gameTick, width, height);
			}

			private void SaveMapPositionSlot(int slot)
			{
				Human.MapPositions[slot] = ((short)_gameMap._x, (short)_gameMap._y);
				_gameMap.MapPositionSaved?.Invoke(_gameMap, slot + 1);
			}

			private static bool HasMapPositionName(int slot)
			{
				return !string.IsNullOrWhiteSpace(Human.MapPositionNames[slot]);
			}

			private void StartRenameMapPositionSlot(int slot)
			{
				string currentName = Human.MapPositionNames[slot] ?? string.Empty;
				_renameDialogTitle = string.IsNullOrWhiteSpace(currentName)
					? $"Rename map position {slot + 1}"
					: "Keep name or change it?";
				_renameDialog = new InputDialogDelegate(_renameDialogTitle, MapPositionNameMaxLength);
				_renameDialog.Accepted += value => ApplyRename(slot, value);
				_renameDialog.Cancelled += (_, _) => CloseRenameDialog();
				_renameDialog.Open(currentName);
				_gameMap._update = true;
				_gameMap._fullRedraw = true;
			}

			private void ApplyRename(int slot, string value)
			{
				string newName = (value ?? string.Empty).Trim();
				if (newName.Length > MapPositionNameMaxLength)
				{
					newName = newName[..MapPositionNameMaxLength];
				}

				Human.MapPositionNames[slot] = newName;
				CloseRenameDialog();
			}

			private void CloseRenameDialog()
			{
				_renameDialog?.Close();
				_renameDialog = null;
				_renameDialogTitle = string.Empty;
				_gameMap._update = true;
				_gameMap._fullRedraw = true;
			}

			public bool JumpToMapPositionSlot(int slot)
			{
				if (slot < 0 || slot >= Human.MapPositions.Length)
				{
					return false;
				}

				var (x, y) = Human.MapPositions[slot];
				if (x < 0 || y < 0)
				{
					return true;
				}

				_gameMap.SetViewOrigin(x, y);
				return true;
			}

			public static bool IsZeroKey(KeyboardEventArgs args)
			{
				ArgumentNullException.ThrowIfNull(args);
				return args[Key.NumPad0] || IsDigitKey(args, '0');
			}

			public bool TryOpenMapPositionSlotList()
			{
				int[] filledSlots = [..
					Enumerable.Range(0, Human.MapPositions.Length)
						.Where(slot => Human.MapPositions[slot].X >= 0 && Human.MapPositions[slot].Y >= 0)
						.Select(slot => slot + 1)
				];

				if (filledSlots.Length == 0)
				{
					return true;
				}

				var slotNames = filledSlots.ToDictionary(slot => slot, slot => Human.MapPositionNames[slot - 1] ?? string.Empty);

				Common.AddScreen(new MapPositionSlotsDialog(
					filledSlots,
					slot => JumpToMapPositionSlot(slot - 1),
					slotNames,
					(slot, name) => Human.MapPositionNames[slot - 1] = name ?? string.Empty));
				return true;
			}

			private static bool IsDigitKey(KeyboardEventArgs args, char key)
			{
				return args[Key.Character] && (args.KeyChar == key || args.KeyChar == char.ToUpperInvariant(key));
			}

			private static bool TryGetSlotFromInput(KeyboardEventArgs args, out int slot)
			{
				slot = args.Key switch
				{
					Key.NumPad1 => 0,
					Key.NumPad2 => 1,
					Key.NumPad3 => 2,
					Key.NumPad4 => 3,
					Key.NumPad5 => 4,
					Key.NumPad6 => 5,
					Key.NumPad7 => 6,
					Key.NumPad8 => 7,
					Key.NumPad9 => 8,
					_ => -1
				};

				if (slot >= 0)
				{
					return true;
				}

				for (var i = 1; i <= 9; i++)
				{
					if (IsDigitKey(args, (char)('0' + i)))
					{
						slot = i - 1;
						return true;
					}
				}

				return false;
			}
		}
	}
}
