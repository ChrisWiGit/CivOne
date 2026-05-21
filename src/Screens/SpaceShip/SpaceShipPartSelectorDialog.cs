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
using CivOne.Services.SpaceShip;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	/// <summary>
	/// Modal chooser for concrete spaceship parts when the user selects a generic family
	/// such as <see cref="SpaceShipComponentType.Component"/> or <see cref="SpaceShipComponentType.Module"/>.
	/// </summary>
	[Modal, ScreenResizeable]
	internal sealed class SpaceShipPartSelectorDialog : BaseScreen
	{
		private readonly ISpaceShipService _service;
		private readonly Action _onPartPlaced;
		private readonly bool _debug;
		private readonly Menu<SpaceShipComponentType> _menu;
		private readonly Picture _capturedBackground;
		private readonly SpaceShipComponentType[] _options;

		private int _lastWidth;
		private int _lastHeight;

		private const int DialogWidth = 176;
		private const int HeaderHeight = 12;

		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		private static bool IsDigitKey(KeyboardEventArgs args, char key) => args[Key.Character] && (args.KeyChar == key || args.KeyChar == char.ToUpperInvariant(key));

		private static string GetLabel(SpaceShipComponentType partType) => partType switch
		{
			SpaceShipComponentType.FuelComponent => "Fuel Component",
			SpaceShipComponentType.PropulsionComponent => "Propulsion Component",
			SpaceShipComponentType.SolarPanelModule => "Solar Panel Module",
			SpaceShipComponentType.LifeSupportModule => "Life Support Module",
			SpaceShipComponentType.HabitationModule => "Habitation Module",
			_ => "Unknown"
		};

		private void PositionMenu()
		{
			int menuX = OffsetX + ((320 - DialogWidth) / 2) + 2;
			int menuY = OffsetY + ((200 - GetDialogHeight()) / 2) + HeaderHeight;
			_menu.X = menuX;
			_menu.Y = menuY;
			_menu.ForceUpdate();
		}

		private int GetDialogHeight()
		{
			int rowHeight = Resources.GetFontHeight(0);
			return HeaderHeight + (_menu.Items.Count * rowHeight) + 2;
		}

		private bool SelectByNumber(int index)
		{
			if (index < 0 || index >= _menu.Items.Count)
			{
				return false;
			}

			MenuItem<SpaceShipComponentType> item = _menu.Items[index];
			if (!item.Enabled)
			{
				return true;
			}

			item.Select();
			return true;
		}

		private bool RefreshMenuAvailability()
		{
			bool changed = false;

			for (int i = 0; i < _options.Length; i++)
			{
				bool isAvailable = _service.CanAddPart(_options[i]);
				if (_menu.Items[i].Enabled == isAvailable)
				{
					continue;
				}

				_menu.Items[i].Enabled = isAvailable;
				changed = true;
			}

			if (!changed)
			{
				return false;
			}

			if (!_menu.Items[_menu.ActiveItem].Enabled)
			{
				for (int i = 0; i < _menu.Items.Count; i++)
				{
					if (!_menu.Items[i].Enabled)
					{
						continue;
					}

					_menu.ActiveItem = i;
					break;
				}
			}

			_menu.ForceUpdate();
			return true;
		}

		private void OnSelect(object sender, MenuItemEventArgs<SpaceShipComponentType> args)
		{
			if (!_service.TryAddPart(args.Value))
			{
				return;
			}

			_onPartPlaced?.Invoke();
			Destroy();
		}

		public SpaceShipPartSelectorDialog(ISpaceShipService service, SpaceShipComponentType genericType, Action onPartPlaced, bool debug = false) : base(MouseCursor.Pointer)
		{
			if (genericType is not SpaceShipComponentType.Component and not SpaceShipComponentType.Module)
			{
				throw new ArgumentException("SpaceShip part selector supports only Component or Module generic types.", nameof(genericType));
			}

			_service = service ?? throw new ArgumentNullException(nameof(service));
			_onPartPlaced = onPartPlaced;
			_debug = debug;

			Palette = Common.TopScreen?.Palette.Copy() ?? Common.DefaultPalette;
			if (Common.TopScreen != null)
			{
				_capturedBackground = new Picture(Common.TopScreen);
			}

			_options = SpaceShipPartOptions.GetOptions(genericType);
			Picture menuBackground = new Picture(DialogWidth - 4, Resources.GetFontHeight(0) * _options.Length)
				.Tile(Pattern.PanelGrey)
				.As<Picture>();
			menuBackground.ColourReplace((7, 11), (22, 3));

			_menu = new("SpaceShipPartSelector", Palette, menuBackground)
			{
				MenuWidth = DialogWidth - 4,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 8,
				FontId = 0,
				Indent = 8,
				RowHeight = Resources.GetFontHeight(0)
			};

			for (int i = 0; i < _options.Length; i++)
			{
				SpaceShipComponentType option = _options[i];
				bool isAvailable = _service.CanAddPart(option);
				_menu.Items.Add($"{i + 1}. {GetLabel(option)}", option)
					.SetEnabled(isAvailable)
					.OnSelect(OnSelect);
			}

			int firstEnabled = -1;
			for (int i = 0; i < _menu.Items.Count; i++)
			{
				if (_menu.Items[i].Enabled)
				{
					firstEnabled = i;
					break;
				}
			}
			if (firstEnabled >= 0)
			{
				_menu.ActiveItem = firstEnabled;
			}

			_lastWidth = Width;
			_lastHeight = Height;
			PositionMenu();
			Refresh();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			bool availabilityChanged = RefreshMenuAvailability();
			bool resized = _lastWidth != Width || _lastHeight != Height;
			if (resized)
			{
				_lastWidth = Width;
				_lastHeight = Height;
				PositionMenu();
			}

			bool menuChanged = _menu.Update(gameTick);
			if (!RefreshNeeded() && !resized && !availabilityChanged && !menuChanged)
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
				.DrawText("What shall we build?", 0, 15, x + 8, y + 3)
				.AddLayer(_menu, 0, 0);

			panel.Dispose();
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args[Key.Escape])
			{
				if (_debug)
				{
					Destroy();
				}
				return true;
			}

			if (args[Key.NumPad1] || IsDigitKey(args, '1')) return SelectByNumber(0);
			if (args[Key.NumPad2] || IsDigitKey(args, '2')) return SelectByNumber(1);
			if (args[Key.NumPad3] || IsDigitKey(args, '3')) return SelectByNumber(2);

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