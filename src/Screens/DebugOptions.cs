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
	[ScreenResizeable]
	internal class DebugOptions : BaseScreen
	{
		private Menu _menu;

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

		private void InstantConquest(object sender, EventArgs args)
		{
			Game.Players.ToList().ForEach(p =>
			{
				if (p.IsHuman || p.Civilization.Id == 0) return;

				Game.GetUnits().Where(u => u.Player == p).ToList().ForEach(u =>
				{
					Game.DisbandUnit(u);
				});
				Game.Cities.Where(c => c.Player == p).ToList().ForEach(c =>
				{
					Game.DestroyCity(c);
				});
				p.HandleExtinction(true);
				// Console.WriteLine($"Instantly conquered {p.Civilization.Name} ({p.Civilization.Id})");
			});

			GameTask conquest;
			GameTask.Enqueue(Message.Newspaper(null, "Your civilization", "has cheated", "the entire planet!"));
			GameTask.Enqueue(conquest = Show.Screen<Conquest>());
			conquest.Done += (s, a) => Runtime.Quit();
			Destroy();
		}

		private void PolluteTiles(bool pollution)
		{
			Map.AllTiles().ToList().ForEach(t => t.Pollution = pollution);
		}
		private void InstantGlobalWarming(object sender, EventArgs args)
		{
			PolluteTiles(true);
			if (Game.GlobalWarmingService.IsGlobalWarmingOnNewTurn())
			{
				Game.globalWarmingScourgeService.UnleashScourgeOfPollution();
			}
			PolluteTiles(false);
			Common.GamePlay.RefreshMap();
			GameTask.Enqueue(Message.Newspaper(null, "Your civilization", "has caused", "instant global warming!"));
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
			if (!RefreshNeeded())
			{
				return false;
			}

			this.Clear();

			int textFontId = 0;

			const int menuBoxWidth = 131;
			int itemHeight = Resources.GetFontHeight(textFontId);
			int menuHeight = (itemHeight + 1) * _menuEntries.Count;

			using Picture menuGfx = new(menuBoxWidth, menuHeight);
			menuGfx
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.DrawText("Debug Options (F12):", textFontId, 15, 4, 4);

			using Picture menuBackground = menuGfx[2, 11, menuBoxWidth, menuHeight];
			menuBackground.ColourReplace((7, 11), (22, 3));

			this.FillRectangle(24, 16, menuBoxWidth + 2, menuHeight + 2, colour: 5); // produces black border, +2 because of round errors when resizing
			this.AddLayer(menuGfx, 25, 17);
			CreateMenu(textFontId, menuBoxWidth, itemHeight, menuBackground);
			return true;
		}

		private void CreateMenu(int textFontId, int menuBoxWidth, int itemHeight, Picture menuBackground)
		{
			if (_menu != null)
			{
				return;
			}
			_menu = new(Palette, menuBackground)
			{
				X = 27,
				Y = 28,
				MenuWidth = menuBoxWidth - 4,
				ActiveColour = 11, // Light blue
				TextColour = 5, // Black
				DisabledColour = 3, // Light grey
				FontId = textFontId,
				Indent = 8, // Left margin
				RowHeight = itemHeight

			};
			_menu.MissClick += MenuCancel;
			_menu.Cancel += MenuCancel;

			foreach (var entry in _menuEntries)
			{
				_menu.Items.Add(entry.Text).OnSelect(entry.Handler);
			}

			AddMenu(_menu);
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
				new("Instant Conquest", InstantConquest),
				new("Instant Global Warming", InstantGlobalWarming),
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