// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Diagnostics.CodeAnalysis;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Screens.Dialogs;
using CivOne.Graphics.Sprites;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne.Screens.CityManagerPanels
{
	internal class CityProduction(City city, bool viewCity) : BaseScreen(90, 99)
	{
		private const int SHIELD_HEIGHT = 8;
		private const int BUTTON_TOP = 7;
		private const int BUTTON_BOTTOM = 15;
		private const int CHANGE_BUTTON_X = 1;
		private const int CHANGE_BUTTON_WIDTH = 33;
		private const int BUY_BUTTON_X = 50;
		private const int BUY_BUTTON_RIGHT_MARGIN = 1;

		private readonly City _city = city;
		private readonly bool _viewCity = viewCity;
		
		private int _shieldPrice, _totalShields, _shieldsPerLine;
		private double _shieldWidth;

		private int BuyButtonWidth => Math.Max(3, Width - BUY_BUTTON_X - BUY_BUTTON_RIGHT_MARGIN);

		private void ForceUpdate(object? _, EventArgs __)
		{
			Refresh();
		}

		private void AcceptBuy(object? _, EventArgs __)
		{
			_city.Buy();
			Refresh();
		}

		private void DrawShields()
		{
			for (int i = 0; i < _city.Shields; i++)
			{
				double x = 1 + (_shieldWidth * (i % _shieldsPerLine));
				int y = 17 + ((i - (i % _shieldsPerLine)) / _shieldsPerLine * SHIELD_HEIGHT);
				this.AddLayer(Icons.Shield, (int)Math.Floor(x), y);
			}
		}

		private bool ProductionInvalid
		{
			get
			{
				if (_city.CurrentProduction is IBuilding production)
				{
					return _city.HasBuilding(production);
				}
				if (_city.CurrentProduction is IWonder wonder)
				{
					return Game.WonderBuilt(wonder);
				}
				return false;
			}
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (RefreshNeeded() || ProductionInvalid)
			{
				int maxShieldDisplayWidth = Width - 2;

				_shieldsPerLine = 10;
				_shieldPrice = _city.CurrentProduction.Price * 10;
				_totalShields = _shieldPrice;
				if (_city.Shields > _totalShields) _totalShields = _city.Shields;
				
				_shieldWidth = 8;
				if (_totalShields > 100)
				{
					_shieldsPerLine = (int)Math.Ceiling((double)_totalShields / 10);
					_shieldWidth = (double)maxShieldDisplayWidth / _shieldsPerLine;
				}

				this.Tile(Pattern.PanelBlue)
					.DrawRectangle(0, 0, Width, Height, 1)
					.FillRectangle(1, 1, Width - 2, 16, 1);
				bool blink = ProductionInvalid && (gameTick % 4 > 1);
				if (Common.TopScreen is not CityManager) blink = ProductionInvalid;
				if (!_viewCity)
				{
					DrawButton(_city.AutoBuild ? Translate("AUTO.") : Translate("Change"), (byte)(blink ? 14 : 9), 1, CHANGE_BUTTON_X, BUTTON_TOP, CHANGE_BUTTON_WIDTH);
					DrawButton(Translate("Buy"), 9, 1, BUY_BUTTON_X, BUTTON_TOP, BuyButtonWidth);
				}

				DrawShields();

				if (_city.CurrentProduction is IUnit unit)
				{
					this.AddLayer(unit.ToBitmap(_city.CityOwnerPlayerIndex), 33, 0);
				}
				else if (_city.CurrentProduction is ICivilopedia production)
				{
					string name = production.TranslatedName;
					while (Resources.GetTextSize(1, name).Width > Width - 2)
					{
						name = $"{name[..^2]}.";
					}
					this.DrawText(name, 1, 15, Width / 2, 1, TextAlign.Center);
				}
				
				Refresh();
			}
			return true;
		}

		public void Update()
		{
			Refresh();
		}

		[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Screen ownership is transferred to Common.AddScreen and released via Common.DestroyScreen.")]
		private bool Change()
		{
			_city.AutoBuild = false;
			Refresh();

			CityChooseProduction cityProductionMenu = new CityChooseProduction(_city);
			cityProductionMenu.Closed += ForceUpdate;
			Common.AddScreen(cityProductionMenu);
			return true;
		}

		[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Screen ownership is transferred to Common.AddScreen and released via Common.DestroyScreen.")]
		private bool Buy()
		{
			if (_city.CurrentProduction is not ICivilopedia currentProduction)
				return true;

			string name = currentProduction.TranslatedName;
			short playerGold = Game.CurrentPlayer.Gold;
			short buyPrice = _city.BuyPrice;
			if (_city.IsRiot && _city.CurrentProduction is IBuilding)
			{
				Common.AddScreen(new MessageBox(
					Translate("Civil Disorder"),
					TranslateFormatted("{0} cannot buy", _city.Name),
					Translate("city improvements now.")));
				return true;
			}
			if (buyPrice <= 0)
				return true;
			if (playerGold < buyPrice)
			{
				Common.AddScreen(new MessageBox(
					Translate("Cost to complete"),
					TranslateFormatted("{0}: ${1}", name, buyPrice),
					TranslateFormatted("Treasury: ${0}", playerGold)));
				return true;
			}

			ConfirmBuy confirmBuy = new(name, buyPrice, playerGold);
			confirmBuy.Buy += AcceptBuy;
			Common.AddScreen(confirmBuy);
			return true;
		}

		private bool AIControl()
		{
			if (!Game.CurrentPlayer.IsHuman) return false;

			_city.AutoBuild = !_city.AutoBuild;
			_city.UpdateAutoBuild();
			
			Refresh();
			return true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			return args.KeyChar switch
			{
				'A' => args.Modifier == KeyModifier.Shift && AIControl(),
				'B' => args.Modifier == KeyModifier.None && Buy(),
				'C' => args.Modifier == KeyModifier.None && Change(),
				_ => false,
			};
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (args.Y < BUTTON_TOP || args.Y > BUTTON_BOTTOM) return false;
			if (args.X >= CHANGE_BUTTON_X && args.X < CHANGE_BUTTON_X + CHANGE_BUTTON_WIDTH) return true;
			if (args.X >= BUY_BUTTON_X && args.X < BUY_BUTTON_X + BuyButtonWidth) return true;
			return false;
		}

		public override bool MouseUp(ScreenEventArgs args)
		{
			if (args.Y < BUTTON_TOP || args.Y > BUTTON_BOTTOM) return false;
			if (args.X >= CHANGE_BUTTON_X && args.X < CHANGE_BUTTON_X + CHANGE_BUTTON_WIDTH) return Change();
			if (args.X >= BUY_BUTTON_X && args.X < BUY_BUTTON_X + BuyButtonWidth) return Buy();
			return false;
		}
	}
}