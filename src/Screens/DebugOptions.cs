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
using CivOne.Screens.Debug;
using CivOne.Graphics.Sprites;
using CivOne.Tasks;
using CivOne.UserInterface;
using System.Collections.Generic;

namespace CivOne.Screens
{
	internal class DebugOptions : BaseScreen
	{
		private bool _update = true;

		private void MenuCancel(object sender, EventArgs args)
		{
			Destroy();
		}

		private void MenuSetGameYear(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<SetGameYear>());
			Destroy();
		}

		private void MenuSetPlayerGold(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<SetPlayerGold>());
			Destroy();
		}

		private void MenuSetPlayerScience(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<SetPlayerScience>());
			Destroy();
		}

		private void MenuSetPlayerAdvances(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<SetPlayerAdvances>());
			Destroy();
		}

		private void MenuSetCitySize(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<SetCitySize>());
			Destroy();
		}

		private void MenuCityDisaster(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<CauseDisaster>());
			Destroy();
		}

		private void MenuChangeHumanPlayer(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<ChangeHumanPlayer>());
			Destroy();
		}

		private void MenuSpawnUnit(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<SpawnUnit>());
			Destroy();
		}

		private void MenuMeetWithKing(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<MeetWithKing>());
			Destroy();
		}

		private void MenuRevealWorld(object sender, EventArgs args)
		{
			Settings.Instance.RevealWorldCheat();
			Destroy();
		}

		private void MenuBuildPalace(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.BuildPalace());
			Destroy();
		}

		private void MenuShowPowerGraph(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<PowerGraph>());
			Destroy();
		}

		private void ShowSettings(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screens(typeof(Setup)));
			Destroy();
		}

		private void MenuAddBuilding(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<AddBuilding>());
			Destroy();
		}

		private void LoadGame(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<LoadGame>());
			Destroy();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (_update)
			{
				_update = false;

				const int itemHeight = 8 + 1;
				const int menuWidth = 131;
				int menuHeight = itemHeight * _menuEntries.Count;

				// TODO fire-eggs picture height should be derived from number of menu entries!
				Picture menuGfx = new Picture(menuWidth, menuHeight)
					.Tile(Pattern.PanelGrey)
					.DrawRectangle3D()
					.DrawText("Debug Options:", 0, 15, 4, 4)
					.As<Picture>();

				IBitmap menuBackground = menuGfx[2, 11, menuWidth, menuHeight].ColourReplace((7, 11), (22, 3));

				this.AddLayer(menuGfx, 25, 17);

				Menu menu = new(Palette, menuBackground)
				{
					X = 27,
					Y = 28,
					MenuWidth = 127,
					ActiveColour = 11,
					TextColour = 5,
					DisabledColour = 3,
					FontId = 0,
					Indent = 8,
					RowHeight = 8
					
				};
				menu.MissClick += MenuCancel;
				menu.Cancel += MenuCancel;

				foreach (var entry in _menuEntries)
				{
					menu.Items.Add(entry.Text).OnSelect(entry.Handler);
				}



				AddMenu(menu);
			}
			return true;
		}


		private record MenuEntry(string Text, Events.MenuItemEventHandler<int> Handler);

		private readonly List<MenuEntry> _menuEntries;


		public DebugOptions() : base(MouseCursor.Pointer)
		{
			Palette = Common.DefaultPalette;

			_menuEntries =
			[
				new("Load a Game", LoadGame),
				new("Set Game Year", MenuSetGameYear),
				new("Set Player Gold", MenuSetPlayerGold),
				new("Set Player Science", MenuSetPlayerScience),
				new("Set Player Advances", MenuSetPlayerAdvances),
				new("Set City Size", MenuSetCitySize),
				new("Cause City Disaster", MenuCityDisaster),
				new("Add building to city", MenuAddBuilding),
				new("Change Human Player", MenuChangeHumanPlayer),
				new("Spawn Unit", MenuSpawnUnit),
				new("Meet With King", MenuMeetWithKing),
				new("Toggle Reveal World", MenuRevealWorld),
				new("Build Palace", MenuBuildPalace),
				new("Settings", ShowSettings)
			];

			const int itemHeight = 8 + 1;
			const int menuWidth = 133;
				int menuHeight = itemHeight * _menuEntries.Count;
			
			this.AddLayer(Common.Screens.Last(), 0, 0)
				.FillRectangle(24, 16, menuWidth, menuHeight + 2, 5);
		}
	}
}