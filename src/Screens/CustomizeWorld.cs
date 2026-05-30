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
using System.Drawing;
using System.Text.RegularExpressions;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Screens.Dialogs;
using CivOne.Tasks;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class CustomizeWorld : BaseScreen
	{
		private const string MenuIdMapSize = "CustomizeWorld.MapSize";
		private const string MenuIdLandMass = "CustomizeWorld.LandMass";
		private const string MenuIdTemperature = "CustomizeWorld.Temperature";
		private const string MenuIdClimate = "CustomizeWorld.Climate";
		private const string MenuIdAge = "CustomizeWorld.Age";

		private const int MinMapAxis = 20;
		private const int MaxMapAxis = 1000;
		private const int LargeMapWarningAxis = 225;
		private const int MapSizeInputMaxLength = 9;

		private static readonly Regex MapSizeRegex = new(@"^\s*(\d{1,4})\s*[xX]\s*(\d{1,4})\s*$", RegexOptions.Compiled);
		private static readonly Regex SingleMapAxisRegex = new(@"^\s*(\d{1,4})\s*$", RegexOptions.Compiled);

		private int _landMass = -1, _temperature = -1, _climate = -1, _age = -1;
		private int _mapWidth = Map.DefaultMapWidth;
		private int _mapHeight = Map.DefaultMapHeight;
		private bool _mapSizeSelected;
		private bool _hasUpdate = true;
		private bool _mapSizeInputWasActive;

		private bool _closing;
		private readonly Picture _background;
		private InputDialogDelegate? _mapSizeInputDialog;

		private int OffsetX => ((Width - 320) / 2);
		private int OffsetY => ((Height - 200) / 2);

		private void DrawBackground()
		{
			this.Clear();
			this.AddLayer(_background, OffsetX, OffsetY);
		}
		
		private static int GetMenuWidth(string title, string[] items)
		{
			int i = 0;
			Picture[] texts = new Picture[items.Length + 1];
			texts[i++] = Resources.GetText(" " + title, 0, 15);
			foreach (string item in items)
				texts[i++] = Resources.GetText(" " + item, 0, 5);
			return (texts.Select(t => t.Width).Max()) + 6;
		}
		
		private Menu CreateMenu(string menuId, int y, string title, MenuItemEventHandler<int> setChoice, params string[] menuTexts)
		{
			Menu menu = new Menu(menuId, Palette)
			{
				Title = title,
				X = 203,
				Y = y,
				MenuWidth = GetMenuWidth(title, menuTexts),
				TitleColour = 15,
				ActiveColour = 11,
				TextColour = 79,
				DisabledColour = 8,
				FontId = 0,
				CenterTo320Coordinates = true
			};
			
			for (int i = 0; i < menuTexts.Length; i++)
			{
				menu.Items.Add(menuTexts[i], i).OnSelect(setChoice);
			}
			menu.Cancel += CancelCustomizeWorld;
			menu.ActiveItem = 1;
			return menu;
		}

		private void CancelCustomizeWorld(object? sender, EventArgs? args)
		{
			CloseMenus();
			_mapSizeInputDialog = null;
			Destroy();
			Common.AddScreen(new Credits());
		}
		
		private void SetLandMass(object sender, MenuItemEventArgs<int> args)
		{
			_landMass = args.Value;
			Log("Customize World - Land Mass: {0}", _landMass);
			_hasUpdate = true;
		}

		private string[] GetTranslatedMapSizePresetTexts()
		{
			// Keep Translate with string to allow translator cli to recognize and parse the text.
			return
			[
				Translate("Tiny (40x25)"),
				Translate("Small (60x40)"),
				Translate("Normal (80x50)"),
				Translate("Large (120x75)"),
				Translate("Huge (160x100)")
			];
		}

		private static int GetMapSizePresetCount()
		{
			return 5;
		}

		private static Size GetMapSizePreset(int presetIndex)
		{
			return presetIndex switch
			{
				0 => new Size(40, 25),
				1 => new Size(60, 40),
				2 => new Size(80, 50),
				3 => new Size(120, 75),
				4 => new Size(160, 100),
				_ => Size.Empty
			};
		}

		private Menu CreateMapSizeMenu()
		{
			string[] presetTexts = GetTranslatedMapSizePresetTexts();
			string[] menuTexts = [.. presetTexts, Translate("Custom size...")];
			return CreateMenu(MenuIdMapSize, 6, Translate("WORLD SIZE:"), SetMapSize, menuTexts);
		}

		private bool HasMenuById(string menuId)
		{
			return _menus.Any(x => x.Id == menuId);
		}

		private void SetMapSize(object sender, MenuItemEventArgs<int> args)
		{
			int presetCount = GetMapSizePresetCount();
			if (args.Value == presetCount)
			{
				OpenCustomMapSizeInputDialog();
				_hasUpdate = true;
				return;
			}

			if (args.Value < 0 || args.Value >= presetCount)
			{
				return;
			}

			Size size = GetMapSizePreset(args.Value);
			if (size == Size.Empty)
			{
				return;
			}
			_mapWidth = size.Width;
			_mapHeight = size.Height;
			_mapSizeSelected = true;
			CloseMenus(MenuIdMapSize);
			ShowLargeMapWarningIfNeeded(_mapWidth, _mapHeight);
			_hasUpdate = true;
			Log("Customize World - Map Size: {0}x{1}", _mapWidth, _mapHeight);
		}

		private void OpenCustomMapSizeInputDialog()
		{
			if (_mapSizeInputDialog?.Active == true)
			{
				return;
			}

			// Menus are separate screens; close them so keyboard focus goes to the dialog.
			CloseMenus(MenuIdMapSize);

			_mapSizeInputDialog = new InputDialogDelegate(
				Translate("Enter map size (WidthxHeight)"),
				MapSizeInputMaxLength,
				acceptValidator: IsValidMapSizeInput,
				validationFailedAction: _ => ShowMapSizeValidationError(),
				textColour: 79,
				frameColour: 1);
			_mapSizeInputDialog.Accepted += HandleCustomMapSizeAccepted;
			_mapSizeInputDialog.Cancelled += (_, _) =>
			{
				_mapSizeInputDialog = null;
				_hasUpdate = true;
				Refresh();
			};
			_mapSizeInputDialog.Open($"{_mapWidth}x{_mapHeight}");
		}

		private bool IsValidMapSizeInput(string input)
		{
			return TryParseMapSize(input, out _);
		}

		private void ShowMapSizeValidationError()
		{
			GameTask.Insert(Message.Error(
				Translate("Invalid map size"),
				TranslateFormattedArray("Use WidthxHeight or a single value with values between {0} and {1}.\nExamples: 40x25, 160x100, and 200.", MinMapAxis, MaxMapAxis)));
		}

		private void ShowLargeMapWarningIfNeeded(int width, int height)
		{
			if (width <= LargeMapWarningAxis && height <= LargeMapWarningAxis)
			{
				return;
			}

			GameTask.Insert(Message.General(
				TranslateFormattedArray("Map sizes above {0}x{0}\ncan cause long processing times\nfor map generation\nand other game mechanics.", LargeMapWarningAxis)));
		}

		private static bool IsModalMessageActive()
		{
			return GameTask.Is<Message>()
				|| Common.HasScreenType<MessageBox>()
				|| Common.HasScreenType<PopupMessage>();
		}

		private void HandleCustomMapSizeAccepted(string input)
		{
			if (!TryParseMapSize(input, out Size size))
			{
				// Defensive guard; validator should already prevent this path.
				ShowMapSizeValidationError();
				return;
			}

			_mapWidth = size.Width;
			_mapHeight = size.Height;
			_mapSizeSelected = true;
			CloseMenus(MenuIdMapSize);
			ShowLargeMapWarningIfNeeded(_mapWidth, _mapHeight);
			_mapSizeInputDialog = null;
			_hasUpdate = true;
			Refresh();
			Log("Customize World - Map Size: {0}x{1}", _mapWidth, _mapHeight);
		}

		internal static bool TryParseMapSize(string input, out Size size)
		{
			size = Size.Empty;
			if (string.IsNullOrWhiteSpace(input))
			{
				return false;
			}

			Match singleAxisMatch = SingleMapAxisRegex.Match(input);
			if (singleAxisMatch.Success)
			{
				if (!int.TryParse(singleAxisMatch.Groups[1].Value, out int axis))
				{
					return false;
				}

				if (axis < MinMapAxis || axis > MaxMapAxis)
				{
					return false;
				}

				size = new Size(axis, axis);
				return true;
			}

			Match match = MapSizeRegex.Match(input);
			if (!match.Success)
			{
				return false;
			}

			if (!int.TryParse(match.Groups[1].Value, out int width) || !int.TryParse(match.Groups[2].Value, out int height))
			{
				return false;
			}

			if (width < MinMapAxis || width > MaxMapAxis || height < MinMapAxis || height > MaxMapAxis)
			{
				return false;
			}

			size = new Size(width, height);
			return true;
		}
		
		private void SetTemperature(object sender, MenuItemEventArgs<int> args)
		{
			_temperature = args.Value;
			Log("Customize World - Temperature: {0}", _temperature);
			_hasUpdate = true;
		}
		
		private void SetClimate(object sender, MenuItemEventArgs<int> args)
		{
			_climate = args.Value;
			Log("Customize World - Climate: {0}", _climate);
			_hasUpdate = true;
		}
		
		private void SetAge(object sender, MenuItemEventArgs<int> args)
		{
			_age = args.Value;
			Log("Customize World - Age: {0}", _age);
			_hasUpdate = true;
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			InputDialogDelegate? mapSizeInputDialog = _mapSizeInputDialog;
			bool mapSizeInputActive = mapSizeInputDialog?.Active == true;
			if (_mapSizeInputWasActive && !mapSizeInputActive)
			{
				// Dialog draws directly on this bitmap, so redraw background after closing it.
				DrawBackground();
				_hasUpdate = true;
				Refresh();
			}
			_mapSizeInputWasActive = mapSizeInputActive;

			if (_closing)
			{
				if (!HandleScreenFadeOut())
				{
					Destroy();
					Map.SetMapSize(_mapWidth, _mapHeight);
					Map.Generate((LandMass)_landMass, (Temperature)_temperature, (Climate)_climate, (EarthAge)_age);
					if (!Runtime.Settings.ShowIntro)
					{
						Common.AddScreen(new NewGame());
					}
					else
					{
						Common.AddScreen(new Intro());
					}
				}
				return true;
			}

			if (IsModalMessageActive())
			{
				// Keep current state until the blocking message is closed.
				return true;
			}

			if (!_hasUpdate && !mapSizeInputActive)
			{
				return false;
			}

			if (_hasUpdate && !mapSizeInputActive)
			{
				if (!_mapSizeSelected)
				{
					if (!HasMenuById(MenuIdMapSize))
					{
						AddMenu(CreateMapSizeMenu());
					}
				}
				else if (_landMass < 0)
				{
					if (!HasMenuById(MenuIdLandMass))
					{
						AddMenu(CreateMenu(MenuIdLandMass, 6, Translate("LAND MASS:"), SetLandMass, Translate("Small"), Translate("Normal"), Translate("Large")));
					}
				}
				else if (_temperature < 0)
				{
					if (!HasMenuById(MenuIdTemperature))
					{
						AddMenu(CreateMenu(MenuIdTemperature, 56, Translate("TEMPERATURE:"), SetTemperature, Translate("Cool"), Translate("Temperate"), Translate("Warm")));
					}
				}
				else if (_climate < 0)
				{
					if (!HasMenuById(MenuIdClimate))
					{
						AddMenu(CreateMenu(MenuIdClimate, 106, Translate("CLIMATE:"), SetClimate, Translate("Arid"), Translate("Normal"), Translate("Wet")));
					}
				}
				else if (_age < 0)
				{
					if (!HasMenuById(MenuIdAge))
					{
						AddMenu(CreateMenu(MenuIdAge, 156, Translate("AGE:"), SetAge, Translate("3 billion years"), Translate("4 billion years"), Translate("5 billion years")));
					}
				}
				else
				{
					_closing = true;
					foreach (IScreen menu in _menus)
					{
						this.AddLayer(menu);
					}
					CloseMenus();
					return true;
				}
			}

			if (mapSizeInputDialog?.Active == true)
			{
				mapSizeInputDialog.Draw(this, gameTick, Width, Height);
			}

			_hasUpdate = false;
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_mapSizeInputDialog?.Active == true)
			{
				bool handled = _mapSizeInputDialog.KeyDown(args);
				if (handled)
				{
					Refresh();
				}
				return true;
			}

			if (args.Key == Key.Escape)
			{
				CancelCustomizeWorld(this, EventArgs.Empty);
				return true;
			}

			return base.KeyDown(args);
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_mapSizeInputDialog?.Active == true)
			{
				bool handled = _mapSizeInputDialog.MouseDown(args);
				if (handled)
				{
					Refresh();
				}
				return true;
			}

			return base.MouseDown(args);
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			DrawBackground();
		}
		
		public CustomizeWorld()
		{
			_background = Resources["CUSTOM"];
			Palette = _background.Palette;
			DrawBackground();
		}
	}
}