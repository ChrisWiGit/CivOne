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
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.Tasks;
using CivOne.UserInterface;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal class SetPlayerScience : BaseScreen
	{
		private readonly Menu _civSelect;

		private Input _input;

		private Player _selectedPlayer = null;
		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);
		private readonly int _menuWidth;
		private readonly int _menuHeight;

		private void DrawPlayerSelectDialog()
		{
			int xx = OffsetX + ((320 - _menuWidth) / 2);
			int yy = OffsetY + ((200 - _menuHeight) / 2);

			Picture menuGfx = new Picture(_menuWidth, _menuHeight)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.As<Picture>();

			this.Clear();
			this.FillRectangle(xx - 1, yy - 1, _menuWidth + 2, _menuHeight + 2, 5)
				.AddLayer(menuGfx, xx, yy, dispose: true)
				.DrawText("Set Player Science...", 0, 15, xx + 8, yy + 3);

			_civSelect.X = xx + 2;
			_civSelect.Y = yy + 11;
			_civSelect.ForceUpdate();
		}

		private void DrawInputDialog()
		{
			int ox = OffsetX;
			int oy = OffsetY;

			this.Clear();
			this.FillRectangle(80 + ox, 80 + oy, 161, 33, 11)
				.FillRectangle(81 + ox, 81 + oy, 159, 31, 15)
				.DrawText("Set Player Science...", 0, 5, 88 + ox, 82 + oy)
				.FillRectangle(88 + ox, 95 + oy, 105, 14, 5)
				.FillRectangle(89 + ox, 96 + oy, 103, 12, 15);

			if (_input != null)
			{
				_input.X = 90 + ox;
				_input.Y = 97 + oy;
			}
		}

		public string Value { get; private set; }

		public event EventHandler Accept, Cancel;

		private void CivSelect_Accept(object sender, EventArgs args)
		{
			Palette = Common.Screens.Last().OriginalColours;

			_selectedPlayer = Game.GetPlayer((byte)_civSelect.ActiveItem);

			_input = new Input(Palette, _selectedPlayer.Science.ToString(), 0, 5, 11, 90 + OffsetX, 97 + OffsetY, 101, 10, 5);
			_input.Accept += PlayerScience_Accept;
			_input.Cancel += PlayerScience_Cancel;

			DrawInputDialog();

			CloseMenus();
		}

		private void PlayerScience_Accept(object sender, EventArgs args)
		{
			Value = (sender as Input).Text;
			
			short playerScience;
			if (!short.TryParse(Value, out playerScience) || playerScience < 0 || playerScience > 30000)
			{
				GameTask.Enqueue(Message.Error("-- DEBUG: Set Player Science --", $"The value {Value} is invalid or out of range.", "Please enter a value between 0 and", "30000."));
			}
			else
			{
				if (playerScience > _selectedPlayer.ScienceCost) playerScience = _selectedPlayer.ScienceCost;
				_selectedPlayer.Science = playerScience;
				GameTask.Enqueue(Message.General($"{_selectedPlayer.TribeName} science set to {playerScience}~."));
			}

			if (Accept != null)
				Accept(this, null);
			if (sender is Input)
				((Input)sender)?.Close();
			Destroy();
		}

		private void PlayerScience_Cancel(object sender, EventArgs args)
		{
			if (Cancel != null)
				Cancel(this, null);
			if (sender is Input)
				((Input)sender)?.Close();
			Destroy();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (RefreshNeeded())
			{
				if (_selectedPlayer == null)
					DrawPlayerSelectDialog();
				else
					DrawInputDialog();
			}

			if (_selectedPlayer == null && Common.TopScreen.GetType() != typeof(Menu))
			{
				AddMenu(_civSelect);
				return false;
			}
			else if (_selectedPlayer != null && !Common.HasScreenType<Input>())
			{
				Common.AddScreen(_input);
			}
			return false;
		}

		public SetPlayerScience() : base(MouseCursor.Pointer)
		{
			Palette = Common.Screens.Last().OriginalColours;
			int fontHeight = Resources.GetFontHeight(0);
			_menuHeight = (fontHeight * (Game.Players.Count() + 1)) + 5;
			_menuWidth = 120;

			Picture menuGfx = new Picture(_menuWidth, _menuHeight)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.As<Picture>();
			IBitmap menuBackground = menuGfx[2, 11, _menuWidth - 4, _menuHeight - 11].ColourReplace((7, 11), (22, 3));

			_civSelect = new Menu(Palette, menuBackground)
			{
				X = 0,
				Y = 0,
				MenuWidth = _menuWidth - 4,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 3,
				FontId = 0,
				Indent = 8
			};

			foreach (Player player in Game.Players)
			{
				_civSelect.Items.Add(player.TribeNamePlural).OnSelect(CivSelect_Accept);
			}

			_civSelect.Cancel += PlayerScience_Cancel;
			_civSelect.MissClick += PlayerScience_Cancel;
			_civSelect.ActiveItem = Game.PlayerNumber(Human);

			DrawPlayerSelectDialog();
		}
	}
}