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
using CivOne.Services.EndGame;
using CivOne.Graphics.Sprites;
using CivOne.Tasks;
using CivOne.Units;
using CivOne.Events;
using System.Collections.Generic;
using CivOne.Services.SpaceShip;
using CivOne.Screens.Dialogs;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Civilizations;
using CivOne.Services;

namespace CivOne.Screens
{
	/// <summary>
	/// Developer debug options screen exposing various test and cheat utilities.
	/// </summary>
	/// <remarks>
	/// Used during development to quickly access debug screens and end-game flows.
	/// </remarks>
	[ScreenResizeable]
	internal class DebugOptions : BaseScreen
	{
		private readonly GridMenuDelegate _gridMenu;

		private void MenuCancel(object? _, EventArgs args)
		{
			Destroy();
		}

		private void MenuSetGameYear(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<SetGameYear>());
			Destroy();
		}

		private void MenuSetPlayerGold(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<SetPlayerGold>());
			Destroy();
		}

		private void MenuSetPlayerScience(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<SetPlayerScience>());
			Destroy();
		}

		private void MenuSetPlayerAdvances(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<SetPlayerAdvances>());
			Destroy();
		}

		private void MenuSetPlayerGovernment(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<DebugChangeGovernment>());
			Destroy();
		}

		private void MenuSetCitySize(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<SetCitySize>());
			Destroy();
		}

		private void MenuCityDisaster(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<CauseDisaster>());
			Destroy();
		}

		private void MenuChangeHumanPlayer(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<ChangeHumanPlayer>());
			Destroy();
		}

		private void MenuSpawnUnit(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<SpawnUnit>());
			Destroy();
		}

		private void MenuMeetWithKing(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<MeetWithKing>());
			Destroy();
		}

		private void MenuRevealWorld(object? _, EventArgs args)
		{
			Settings.Instance.RevealWorldCheat();
			Common.GamePlay?.RefreshMap();
			Destroy();
		}

		private void MenuBuildPalace(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.BuildPalace(keepOpenUntilEscape: true));
			Destroy();
		}

		private void MenuBuildSpaceShip(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen(new SpaceShipView(Human, true, SpaceShipViewServicesFactory.CreateDefault(Translation))));
			Destroy();
		}

		private void MenuPaletteViewer(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<PaletteViewerScreen>());
			Destroy();
		}

		private void InstantConquest(object? _, EventArgs args)
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
			});

			GameTask conquest;
			GameTask.Enqueue(Message.Newspaper(null, TranslateArray("Your civilization\nhas cheated\nthe entire planet!")));
			conquest = Show.Screen<Conquest>();
			GameTask.Enqueue(conquest);
			conquest.Done += (_, __) => RuntimeHandler.EndGame();
			Destroy();
		}

		private static void PolluteTiles(bool pollution)
		{
			Map.AllTiles().ToList().ForEach(t => t.Pollution = pollution);
		}
		private void InstantGlobalWarming(object? _, EventArgs args)
		{
			PolluteTiles(true);
			if (Game.GlobalWarmingService.IsGlobalWarmingOnNewTurn())
			{
				Game._globalWarmingScourgeService.UnleashScourgeOfPollution();
			}
			PolluteTiles(false);
			Game.GlobalWarmingService.RefreshPollutionState();
			Common.GamePlay!.RefreshMap();
			GameTask.Enqueue(Message.Newspaper(null, TranslateArray("Your civilization\nhas caused\ninstant global warming!")));
			Destroy();
		}

		private void MenuShowPowerGraph(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<PowerGraph>());
			Destroy();
		}

		private void MenuShowCivilizationRanking(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen(CivilizationRankingScreenFactory.CreateDebug()));
			Destroy();
		}

		private void MenuShowTopLeaderScreen(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen(TopLeaderScreenFactory.CreateDebug()));
			Destroy();
		}

		private void MenuShowHallOfFameScreen(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen(HallOfFameScreenFactory.ViewScore()));
			Destroy();
		}

		private void ShowSettings(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screens(typeof(Setup)));
			Destroy();
		}

		private void MenuAddBuilding(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<AddBuilding>());
			Destroy();
		}



		private void EndGameConquest(object? _, EventArgs args)
		{
			_ = EndGameServiceFactory.CreateForHuman().HandleConquestAsync();
			Destroy();
		}

		private void EndGameDefeat(object? _, EventArgs args)
		{
			_ = EndGameServiceFactory.CreateForHuman().HandleDefeatAsync();
			Destroy();
		}

		private void EndGameAlphaCentauri(object? _, EventArgs args)
		{
			_ = EndGameServiceFactory.CreateForHuman().HandleAlphaCentauriAsync();
			Destroy();
		}

		private void LoadGame(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<LoadGame>());
			Destroy();
		}

		private void MenuCityGridTest(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<TestCityGridMenu>());
			Destroy();
		}

		private void MenuRunConfirmBuy(object? _, EventArgs args)
		{
			short treasury = Human?.Gold ?? 0;
			GameTask.Enqueue(Show.Screen(new ConfirmBuy("Debug Building", 80, treasury)));
			Destroy();
		}

		private void MenuRunConfirmSell(object? _, EventArgs args)
		{
			byte humanOwner = Game.PlayerNumber(Human);
			IBuilding? building = Game.Cities
				.Where(c => c.Owner == humanOwner)
				.SelectMany(c => c.Buildings)
				.FirstOrDefault();

			if (building == null)
			{
				return;
			}

			building ??= new Barracks();

			GameTask.Enqueue(Show.Screen(new ConfirmSell(building)));
			Destroy();
		}

		private void MenuRunConfirmQuit(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<ConfirmQuit>());
			Destroy();
		}

		private void MenuRunConfirmRetire(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<ConfirmRetire>());
			Destroy();
		}

		private void MenuRunRevolution(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen<Revolution>());
			Destroy();
		}

		private void MenuRunSetRateTaxes(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen(SetRate.Taxes));
			Destroy();
		}

		private void MenuRunSetRateLuxuries(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen(SetRate.Luxuries));
			Destroy();
		}

		private void MenuRunDisbandUnitDialog(object? _, EventArgs args)
		{
			byte humanOwner = Game.PlayerNumber(Human);
			var city = Game.Cities.FirstOrDefault(c => c.Owner == humanOwner);
			if (city == null)
			{
				GameTask.Enqueue(Message.General(Translate("No human city available for DisbandUnit test.")));
				Destroy();
				return;
			}

			var unit = Game.GetUnits().FirstOrDefault(u => u.Owner == humanOwner);
			if (unit == null)
			{
				GameTask.Enqueue(Message.General(Translate("No human unit available for DisbandUnit test.")));
				Destroy();
				return;
			}

			GameTask.Enqueue(Show.Screen(new DisbandUnit(city, unit)));
			Destroy();
		}

		private void MenuRunSelectAdvanceAfterCityCapture(object? _, EventArgs args)
		{
			var enemy = Game.Players.FirstOrDefault(p => p != Human && p.Civilization.Id != 0);
			if (enemy == null)
			{
				GameTask.Enqueue(Message.General(Translate("No enemy player available for advance capture test.")));
				Destroy();
				return;
			}

			List<IAdvance>? advances = [.. Common.Advances
				.Where(a => enemy.HasAdvance(a) && !Human.HasAdvance(a))
				.Take(5)];

			if (advances == null || advances.Count == 0)
			{
				advances = [.. Common.Advances.Take(5)];
			}

			GameTask.Enqueue(Show.SelectAdvanceAfterCityCapture(Human, advances));
			Destroy();
		}

		private void MenuRunWeakAttack(object? _, EventArgs args)
		{
			byte humanOwner = Game.PlayerNumber(Human);
			var baseUnit = Game.GetUnits().OfType<BaseUnit>().FirstOrDefault(u => u.Owner == humanOwner);
			if (baseUnit == null)
			{
				GameTask.Enqueue(Message.General(Translate("No human base unit available for WeakAttack test.")));
				Destroy();
				return;
			}

			GameTask.Enqueue(Show.WeakAttack(baseUnit, 1, 0));
			Destroy();
		}

		private void MenuRunDiplomatBribe(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen(new DiplomatBribe(new StubDiplomatBribeService(), true)));
			Destroy();
		}

		private void MenuRunDiplomatIncite(object? _, EventArgs args)
		{
			byte humanOwner = Game.PlayerNumber(Human);
			var diplomat = Game.GetUnits()
				.OfType<Diplomat>()
				.FirstOrDefault(u => u.Owner == humanOwner);
			if (diplomat == null)
			{
				GameTask.Enqueue(Message.General(Translate("No human Diplomat/Spy available.")));
				Destroy();
				return;
			}

			var enemyCity = Game.Cities.FirstOrDefault(c => c.Owner != humanOwner);
			if (enemyCity == null)
			{
				GameTask.Enqueue(Message.General(Translate("No enemy city available for incite test.")));
				Destroy();
				return;
			}

			GameTask.Enqueue(Show.Screen(new DiplomatIncite(enemyCity, diplomat, new StubDiplomatInciteService())));
			Destroy();
		}

		private void MenuRunCaravanChoice(object? _, EventArgs args)
		{
			byte humanOwner = Game.PlayerNumber(Human);
			var caravan = Game.GetUnits()
				.OfType<Caravan>()
				.FirstOrDefault(u => u.Owner == humanOwner);
			if (caravan == null)
			{
				GameTask.Enqueue(Message.General(Translate("No human Caravan available.")));
				Destroy();
				return;
			}

			var city = Game.Cities.FirstOrDefault(c => c.Owner == humanOwner);
			if (city == null)
			{
				GameTask.Enqueue(Message.General(Translate("No human city available for caravan choice test.")));
				Destroy();
				return;
			}

			GameTask.Enqueue(Show.Screen(new CaravanChoice(caravan, city, new StubCaravanChoiceService())));
			Destroy();
		}

		private void MenuRunDiplomatCity(object? _, EventArgs args)
		{
			byte humanOwner = Game.PlayerNumber(Human);
			City[] enemyCities = [.. Game.Cities
				.Where(c => c.Owner != humanOwner)
				.OrderBy(c => c.Name)];

			if (enemyCities.Length == 0)
			{
				GameTask.Enqueue(Message.General(Translate("No enemy city available for DiplomatCity test.")));
				Destroy();
				return;
			}

			GameTask.Enqueue(Show.Screen(new DiplomatCitySelection(enemyCities, humanOwner)));
			Destroy();
		}

		private void MenuRunOverwritePlugin(object? _, EventArgs args)
		{
			GameTask.Enqueue(Show.Screen(new OverwritePlugin(
				"plugin-source.dll",
				"plugin-destination.dll",
				new StubPluginOverwriteService())));
			Destroy();
		}

		private void MenuRunChooseTech(object? _, EventArgs args)
		{
			if (Human == null)
			{
				GameTask.Enqueue(Message.General(Translate("No human player available for ChooseTech test.")));
				Destroy();
				return;
			}

			var availableAdvances = Human.AvailableResearch.ToList();
			if (availableAdvances.Count == 0)
			{
				GameTask.Enqueue(Message.General(Translate("No available advances for ChooseTech test.")));
				Destroy();
				return;
			}

			GameTask.Enqueue(Show.Screen<ChooseTech>());
			Destroy();
		}

		private void MenuRunDiscovery(object? _, EventArgs args)
		{
			if (Human == null)
			{
				GameTask.Enqueue(Message.General(Translate("No human player available for Discovery test.")));
				Destroy();
				return;
			}

			IAdvance? advance = Human.AvailableResearch.FirstOrDefault()
				?? Common.Advances.FirstOrDefault(a => !Human.HasAdvance(a))
				?? Common.Advances.FirstOrDefault();

			if (advance == null)
			{
				GameTask.Enqueue(Message.General(Translate("No advance available for Discovery test.")));
				Destroy();
				return;
			}

			GameTask.Enqueue(Show.Screen(new Discovery(advance)));
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
			_gridMenu.Draw(this, Translate("Debug Options (F12):"), CanvasHeight);
			return true;
		}

		private sealed record MenuEntry(string Text, Action Handler);

		private readonly List<MenuEntry> _menuEntries;


		public DebugOptions() : base(MouseCursor.Pointer)
		{
			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;

			_menuEntries =
			[
				new(Translate("Load a Game"), () => LoadGame(null, EventArgs.Empty)),
				new(Translate("Set Game Year"), () => MenuSetGameYear(null, EventArgs.Empty)),
				new(Translate("Set Player Gold"), () => MenuSetPlayerGold(null, EventArgs.Empty)),
				new(Translate("Set Player Science"), () => MenuSetPlayerScience(null, EventArgs.Empty)),
				new(Translate("Set Player Advances"), () => MenuSetPlayerAdvances(null, EventArgs.Empty)),
				new(Translate("Set Player Government"), () => MenuSetPlayerGovernment(null, EventArgs.Empty)),
				new(Translate("Set City Size"), () => MenuSetCitySize(null, EventArgs.Empty)),
				new(Translate("Cause City Disaster"), () => MenuCityDisaster(null, EventArgs.Empty)),
				new(Translate("Add building to city"), () => MenuAddBuilding(null, EventArgs.Empty)),
				new(Translate("Test Dialog: ConfirmBuy"), () => MenuRunConfirmBuy(null, EventArgs.Empty)),
				new(Translate("Test Dialog: ConfirmSell"), () => MenuRunConfirmSell(null, EventArgs.Empty)),
				new(Translate("Test Dialog: ConfirmQuit"), () => MenuRunConfirmQuit(null, EventArgs.Empty)),
				new(Translate("Test Dialog: ConfirmRetire"), () => MenuRunConfirmRetire(null, EventArgs.Empty)),
				new(Translate("Test Dialog: Revolution"), () => MenuRunRevolution(null, EventArgs.Empty)),
				new(Translate("Test Dialog: SetRate Taxes"), () => MenuRunSetRateTaxes(null, EventArgs.Empty)),
				new(Translate("Test Dialog: SetRate Luxuries"), () => MenuRunSetRateLuxuries(null, EventArgs.Empty)),
				new(Translate("Test Dialog: DisbandUnit"), () => MenuRunDisbandUnitDialog(null, EventArgs.Empty)),
				new(Translate("Test Dialog: SelectAdvanceAfterCapture"), () => MenuRunSelectAdvanceAfterCityCapture(null, EventArgs.Empty)),
				new(Translate("Test Dialog: WeakAttack"), () => MenuRunWeakAttack(null, EventArgs.Empty)),
				new(Translate("Test Dialog: DiplomatBribe"), () => MenuRunDiplomatBribe(null, EventArgs.Empty)),
				new(Translate("Test Dialog: DiplomatIncite"), () => MenuRunDiplomatIncite(null, EventArgs.Empty)),
				new(Translate("Test Dialog: CaravanChoice"), () => MenuRunCaravanChoice(null, EventArgs.Empty)),
				new(Translate("Test Dialog: DiplomatCity"), () => MenuRunDiplomatCity(null, EventArgs.Empty)),
				new(Translate("Test Dialog: OverwritePlugin"), () => MenuRunOverwritePlugin(null, EventArgs.Empty)),
				new(Translate("Test Dialog: ChooseTech"), () => MenuRunChooseTech(null, EventArgs.Empty)),
				new(Translate("Test Dialog: Discovery"), () => MenuRunDiscovery(null, EventArgs.Empty)),
				new(Translate("Change Human Player"), () => MenuChangeHumanPlayer(null, EventArgs.Empty)),
				new(Translate("Spawn Unit"), () => MenuSpawnUnit(null, EventArgs.Empty)),
				new(Translate("Meet With King"), () => MenuMeetWithKing(null, EventArgs.Empty)),
				new(Translate("Toggle Reveal World"), () => MenuRevealWorld(null, EventArgs.Empty)),
				new(Translate("Build Palace"), () => MenuBuildPalace(null, EventArgs.Empty)),
				new(Translate("City Menu Grid (Test)"), () => MenuCityGridTest(null, EventArgs.Empty)),
				new(Translate("Ranking (Random)"), () => MenuShowCivilizationRanking(null, EventArgs.Empty)),
				new(Translate("Top Leader Screen"),  () => MenuShowTopLeaderScreen(null, EventArgs.Empty)),
				new(Translate("Hall Of Fame Screen"), () => MenuShowHallOfFameScreen(null, EventArgs.Empty)),
				new(Translate("Show Power Graph"), () => MenuShowPowerGraph(null, EventArgs.Empty)),
				new(Translate("Instant Conquest"), () => InstantConquest(null, EventArgs.Empty)),
				new(Translate("Instant Global Warming"), () => InstantGlobalWarming(null, EventArgs.Empty)),
				new(Translate("Palette Viewer"), () => MenuPaletteViewer(null, EventArgs.Empty)),
				new(Translate("Settings"), () => ShowSettings(null, EventArgs.Empty)),
				new(Translate("End Game: Conquest"),  () => EndGameConquest(null, EventArgs.Empty)),
				new(Translate("End Game: Defeat"), () => EndGameDefeat(null, EventArgs.Empty)),
				new(Translate("End Game: Alpha Centauri"), () => EndGameAlphaCentauri(null, EventArgs.Empty)),
				new(Translate("Build SpaceShip"), () => MenuBuildSpaceShip(null, EventArgs.Empty))
			];

			string[] labels = [.. _menuEntries.Select(e => e.Text)];
			_gridMenu = new GridMenuDelegate(labels, GridMenuDelegate.SelectionMode.Select, fontId: 0, enableHotkeys: true);
			_gridMenu.ItemSelected += index => _menuEntries[index].Handler();
			_gridMenu.Cancelled += (_, _) => Destroy();

			this.AddLayer(Common.LastScreen!, 0, 0);
			Refresh();
		}

		private sealed class StubDiplomatBribeService : IDiplomatBribeService
		{
			public string UnitName => "Stub Unit";

			public string TribeName => "Stub Tribe";

			public int Gold => 1000;

			public void BribeUnit()
			{
				ITranslationService translation = TranslationServiceFactory.GetCurrent();
				GameTask.Enqueue(Message.General(
					translation.TranslateFormattedArray("[STUB] Bribing {0}\nfor {1} gold", UnitName, Gold)));
			}

			public int CalculateBribeCost()
			{
				return 500;
			}

			public bool CanBribe()
			{
				return true;
			}
		}

		private sealed class StubDiplomatInciteService : IDiplomatInciteService
		{
			public void InciteRevolt(City cityToIncite, Diplomat diplomat)
			{
				ITranslationService translation = TranslationServiceFactory.GetCurrent();
				GameTask.Enqueue(Message.General(
					translation.TranslateFormattedArray("[STUB] Inciting revolt\nin {0}", cityToIncite.Name)));
			}
		}

		private sealed class StubCaravanChoiceService : ICaravanChoiceService
		{
			public void KeepMoving(Caravan unit, City city)
			{
				GameTask.Enqueue(Message.General(TranslationServiceFactory.GetCurrent().Translate("[STUB] Caravan keeps moving")));
			}

			public void EstablishTradeRoute(Caravan unit, City city)
			{
				ITranslationService translation = TranslationServiceFactory.GetCurrent();
				GameTask.Enqueue(Message.General(
					translation.TranslateFormattedArray("[STUB] Trade route established\nto {0}", city.Name)));
			}

			public void HelpBuildWonder(Caravan unit, City city)
			{
				GameTask.Enqueue(Message.General(TranslationServiceFactory.GetCurrent().TranslateArray("[STUB] Caravan helps\nbuild wonder")));
			}

			public bool CanEstablishTradeRoute(Caravan unit, City city)
			{
				return true;
			}
		}

		private sealed class StubPluginOverwriteService : IPluginOverwriteService
		{
			public void ConfirmOverwrite(string source, string destination, string filename)
			{
				ITranslationService translation = TranslationServiceFactory.GetCurrent();
				GameTask.Enqueue(Message.General(
					translation.TranslateFormattedArray("[STUB] Overwrite plugin\n{0}\n{1} -> {2}", filename, source, destination)));
			}
		}

		[ScreenResizeable]
		private sealed class DiplomatCitySelection : BaseScreen
		{
			private readonly City[] _enemyCities;
			private readonly byte _humanOwner;
			private CityGridMenuDelegate _citySelect;

			private void DrawCityMenuDialog()
			{
				if (_citySelect == null)
				{
					Palette = Common.Screens[^1].OriginalColours;
					_citySelect = new CityGridMenuDelegate(
						_enemyCities,
						city => $"{city.Name} ({Game.GetPlayer(city.Owner)?.TribeName ?? "Unknown"})");
					_citySelect.CitySelected += OnCitySelected;
					_citySelect.Cancelled += CitySelection_Cancel;
				}

				_citySelect.Draw(this, Translate("DiplomatCity Test - Select City"), CanvasHeight);
			}

			private void OnCitySelected(City enemyCity)
			{
				var diplomat = (Diplomat)Game.CreateUnit(UnitType.Diplomat)!;
				diplomat.Owner = _humanOwner;

				var service = DiplomatCityDialogFactory.CreateService(enemyCity, diplomat, Translation);
				GameTask diplomatCityDialog = Show.Screen(DiplomatCityDialogFactory.CreateDialog(service));
				diplomatCityDialog.Done += (_, _) =>
				{
					// Show the city selection screen again for next round
					GameTask.Enqueue(Show.Screen(new DiplomatCitySelection(_enemyCities, _humanOwner)));
				};
				GameTask.Enqueue(diplomatCityDialog);
				Destroy();
			}

			private void CitySelection_Cancel(object? _, EventArgs args)
			{
				Destroy();
			}

			protected override bool HasUpdate(uint gameTick)
			{
				if (!RefreshNeeded())
				{
					return false;
				}

				DrawCityMenuDialog();
				return true;
			}

			public override bool KeyDown(KeyboardEventArgs args)
			{
				if (_citySelect == null)
				{
					return false;
				}

				bool handled = _citySelect.KeyDown(args);
				if (handled)
				{
					Refresh();
				}
				return handled;
			}

			public override bool MouseDown(ScreenEventArgs args)
			{
				if (_citySelect == null)
				{
					return false;
				}

				bool handled = _citySelect.MouseDown(args.X, args.Y);
				if (handled)
				{
					Refresh();
				}
				return handled;
			}

			public DiplomatCitySelection(City[] enemyCities, byte humanOwner) : base(MouseCursor.Pointer)
			{
				_enemyCities = enemyCities;
				_humanOwner = humanOwner;
				DrawCityMenuDialog();
			}
		}
	}
}
