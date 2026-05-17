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
using CivOne.Screens.Reports;
using CivOne.Graphics.Sprites;
using CivOne.Tasks;
using CivOne.Units;
using CivOne.Events;
using System.Collections.Generic;
using CivOne.Services.SpaceShip;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class DebugOptions : BaseScreen
	{
		private readonly GridMenuDelegate _gridMenu;

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
			GameTask.Enqueue(Show.BuildPalace(keepOpenUntilEscape: true));
			Destroy();
		}

		private void MenuBuildSpaceShip(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen(new SpaceShipView(Human, true, SpaceShipViewServicesFactory.CreateDefault(Translation))));
			Destroy();
		}

		private void MenuPaletteViewer(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<PaletteViewerScreen>());
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
			conquest.Done += (s, a) => RuntimeHandler.EndGame();
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
			Game.GlobalWarmingService.RefreshPollutionState();
			Common.GamePlay.RefreshMap();
			GameTask.Enqueue(Message.Newspaper(null, "Your civilization", "has caused", "instant global warming!"));
			Destroy();
		}

		private void MenuShowPowerGraph(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<PowerGraph>());
			Destroy();
		}

		private void MenuShowCivilizationRanking(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen(CivilizationRankingScreenFactory.CreateDebug()));
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

		// This is a simple test for showing the diplomat incite screen.
		private void MenuRunDiplomatIncite(object sender, EventArgs args)
		{
			Diplomat diplomat = Game.GetUnits()
				.OfType<Diplomat>()
				.FirstOrDefault(x => x.Player == Human);

			if (diplomat == null)
			{
				GameTask.Enqueue(Message.General("No human Diplomat/Spy available.", "You need to spawn one first."));
				Destroy();
				return;
			}

			City cityToIncite = Game.Cities.FirstOrDefault(x => x.Owner != diplomat.Owner);
			if (cityToIncite == null)
			{
				GameTask.Enqueue(Message.General("No foreign city available for incite test."));
				Destroy();
				return;
			}

			GameTask.Enqueue(Show.DiplomatIncite(cityToIncite, diplomat));
			Destroy();
		}

		private void LoadGame(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<LoadGame>());
			Destroy();
		}

		private void MenuCityGridTest(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<TestCityGridMenu>());
			Destroy();
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			bool handled = _gridMenu.KeyDown(args);
			if (handled) Refresh();
			return handled;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			bool handled = _gridMenu.MouseDown(args.X, args.Y);
			if (handled) Refresh();
			return handled;
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded()) return false;
			_gridMenu.Draw(this, "Debug Options (F12):", CanvasHeight);
			return true;
		}

		private record MenuEntry(string Text, Action Handler);

		private readonly List<MenuEntry> _menuEntries;


		public DebugOptions() : base(MouseCursor.Pointer)
		{
			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;

			_menuEntries =
			[
				new("Load a Game", () => LoadGame(null, EventArgs.Empty)),
				new("Set Game Year", () => MenuSetGameYear(null, EventArgs.Empty)),
				new("Set Player Gold", () => MenuSetPlayerGold(null, EventArgs.Empty)),
				new("Set Player Science", () => MenuSetPlayerScience(null, EventArgs.Empty)),
				new("Set Player Advances", () => MenuSetPlayerAdvances(null, EventArgs.Empty)),
				new("Set City Size", () => MenuSetCitySize(null, EventArgs.Empty)),
				new("Cause City Disaster", () => MenuCityDisaster(null, EventArgs.Empty)),
				new("Add building to city", () => MenuAddBuilding(null, EventArgs.Empty)),
				new("DiplomatIncite", () => MenuRunDiplomatIncite(null, EventArgs.Empty)),
				new("Change Human Player", () => MenuChangeHumanPlayer(null, EventArgs.Empty)),
				new("Spawn Unit", () => MenuSpawnUnit(null, EventArgs.Empty)),
				new("Meet With King", () => MenuMeetWithKing(null, EventArgs.Empty)),
				new("Toggle Reveal World", () => MenuRevealWorld(null, EventArgs.Empty)),
				new("Build Palace", () => MenuBuildPalace(null, EventArgs.Empty)),
				new("City Menu Grid (Test)", () => MenuCityGridTest(null, EventArgs.Empty)),
				new("Ranking (Random)", () => MenuShowCivilizationRanking(null, EventArgs.Empty)),
				new("Show Power Graph", () => MenuShowPowerGraph(null, EventArgs.Empty)),
				new("Instant Conquest", () => InstantConquest(null, EventArgs.Empty)),
				new("Instant Global Warming", () => InstantGlobalWarming(null, EventArgs.Empty)),
				new("Palette Viewer", () => MenuPaletteViewer(null, EventArgs.Empty)),
				new("Settings", () => ShowSettings(null, EventArgs.Empty)),
				new("Build SpaceShip", () => MenuBuildSpaceShip(null, EventArgs.Empty))
			];

			string[] labels = [.. _menuEntries.Select(e => e.Text)];
			_gridMenu = new GridMenuDelegate(labels, GridMenuDelegate.SelectionMode.Select, fontId: 0, enableHotkeys: true);
			_gridMenu.ItemSelected += index => _menuEntries[index].Handler();
			_gridMenu.Cancelled += (_, _) => Destroy();

			this.AddLayer(Common.Screens.Last(), 0, 0);
			Refresh();
		}
	}
}