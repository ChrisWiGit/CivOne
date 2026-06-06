// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Screens;
using CivOne.Screens.PalaceAssets;
using CivOne.Screens.Dialogs;
using CivOne.Units;
using CivOne.Services;

namespace CivOne.Tasks
{
	internal class Show : GameTask
	{
		private readonly IScreen _screen;

		public void Closed(object sender, EventArgs args) => EndTask();

		public override void Run()
		{
			_screen.Closed += Closed;
			Common.AddScreen(_screen);
		}

		public static Show Empty => new(Overlay.Empty);

		public static Show InterfaceHelp => new(Overlay.InterfaceHelp);

		public static Show Terrain
		{
			get
			{
				GamePlay gamePlay = (GamePlay)Common.Screens.First(s => (s is GamePlay));
				return new Show(Overlay.TerrainView(gamePlay.X, gamePlay.Y));
			}
		}

		public static Show Goto
		{
			get
			{
				GamePlay gamePlay = (GamePlay)Common.Screens.First(s => (s is GamePlay));
				Goto gotoScreen = new(gamePlay.X, gamePlay.Y);
				gotoScreen.Closed += (s, a) =>
				{
					if (Human != Game.CurrentPlayer) return;
					if (Game.ActiveUnit == null) return;
					if (gotoScreen.X == -1 || gotoScreen.Y == -1) return;
					Game.ActiveUnit.Goto = new Point(gotoScreen.X, gotoScreen.Y);
				};
				return new Show(gotoScreen);
			}
		}

		public static Show TaxRate => new(SetRate.Taxes);

		public static Show LuxuryRate => new(SetRate.Luxuries);

		public static Show AutoSave
		{
			get
			{
				if (Game.GameTurn % 50 != 0) return null;
				int gameId = ((Game.GameTurn / 50) % 6) + 4;
				return new Show(new SaveGame(gameId));
			}
		}

		public static Show CityManager(City city) => new(new CityManager(city));

		public static Show ViewCity(City city) => new(new CityManager(city, true));

		public static Show UnitStack(int x, int y) => new(new UnitStack(x, y));

		public static Show Search
		{
			get
			{
				Search search = new();
				search.Accept += (s, a) =>
				{
					City city = (s as Search).City;
					if (city == null) return;
					GamePlay gamePlay = (GamePlay)Common.Screens.First(x => x.GetType() == typeof(GamePlay));
					gamePlay.CenterOnPoint(city.X, city.Y);
				};
				return new Show(search);
			}
		}

		public static Show ChooseGovernment
		{
			get
			{
				ChooseGovernment chooseGovernment = new();
				chooseGovernment.Closed += (s, a) => {
					Human.Government = (s as ChooseGovernment).Result;
					GameTask.Insert(Message.NewGoverment(null, 
					TranslationServiceFactory.GetCurrent().TranslateFormattedArray("{0} government\nchanged to {1}!", Human.TribeName, Human.Government.TranslatedName)));
				};
				return new Show(chooseGovernment);
			}
		}

		public static Show Nuke(int x, int y) => new(new Nuke(x, y));

		public static Show DestroyUnit(IUnit unit, bool stack) => new(new DestroyUnit(unit, stack));

		public static Show CaptureCity(City city, string []? message = null) => new(CityView.Capture(city, message));

		public static Show DisorderCity(City city) => new(CityView.Disorder(city));

		public static Show WeLovePresidentDayCity(City city) => new(CityView.WeLovePresidentDay(city));

		public static Show BuildPalace(bool keepOpenUntilEscape = false) => new(new PalaceView(true, PalaceSpriteProviderFactory.GetInstance(), keepOpenUntilEscape));

		public static Show BuildSpaceShip() => new(new SpaceShipView(Human));

		public static Show SpaceShipWithInstall(SpaceShipComponentType partType) => new(new SpaceShipView(Human, pendingInstall: partType));

		public static Show CaravanChoice(Caravan unit, City city) => new(CaravanChoiceDialogFactory.CreateDialog(unit, city));

        public static Show WeakAttack(BaseUnit unit, int relx, int rely) => new(new WeakAttack(unit, relx, rely));

		public static Show DiplomatBribe(BaseUnitLand unitToBribe, Diplomat diplomat) => new(DiplomatBribeDialogFactory.CreateDialog(unitToBribe, diplomat));

		public static Show DiplomatCity(City enemyCity, Diplomat diplomat) => new(DiplomatCityDialogFactory.CreateDialog(enemyCity, diplomat));

		public static Show DiplomatIncite(City enemyCity, Diplomat diplomat) => new(DiplomatInciteDialogFactory.CreateDialog(enemyCity, diplomat));

		public static Show SelectAdvanceAfterCityCapture(Player player, IList<IAdvance> advances) => new(new SelectAdvanceAfterCityCapture(player, advances));

		public static Show MeetKing(Player player) => new(new King(player));

		public static Show Screen<T>() where T : IScreen, new() => new(new T());

        private static Show Screen(Type type)
		{
			if (!typeof(IScreen).IsAssignableFrom(type)) return null;
			return new Show((IScreen)Activator.CreateInstance(type));
		}

		public static Show Screen(IScreen screen) => new(screen);

		public static Show Screens(IEnumerable<Type> types)
		{
			Queue<Type> screenTypeQueue = new(types.Where(x => typeof(IScreen).IsAssignableFrom(x)));
			if (screenTypeQueue.Count == 0) return null;
			Func<Show> nextTask = null;
			nextTask = () =>
			{
				if (screenTypeQueue.Count == 0) return null;
				Show showScreen = Show.Screen(screenTypeQueue.Dequeue());
				showScreen.Done += (_, __) => GameTask.Insert(nextTask());
				return showScreen;
			};
			return nextTask();
		}

		public static Show Screens(params Type[] types) => Screens(types.ToList());

		private Show(IScreen screen)
		{
			_screen = screen;
		}
	}
}