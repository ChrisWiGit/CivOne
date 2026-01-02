// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Drawing;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Screens;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.UserInterface;
using CivOne.Wonders;

namespace CivOne.Units
{
	public interface IUnit : ICivilopedia, IProduction, ITurn
	{
		IAdvance RequiredTech { get; }
		IWonder RequiredWonder { get; }
		IAdvance ObsoleteTech { get; }
		UnitClass Class { get; }
		/// <summary>
		/// Defines type of the unit
		/// </summary>
		UnitType Type { get; }
		/// <summary>
		/// Defines home (supporting city) of the unit
		/// Deprecated: use IsHome(ICityBasic city) method instead
		/// to check if unit's home is the given city
		/// </summary>
		City Home { get; }

		virtual bool IsHome(ICityBasic city) => HasHome && Home == city;
		bool HasHome => Home != null;
		UnitRole Role { get; }
		byte Attack { get; }
		byte Defense { get; }
		byte Move { get; }
		int X { get; set; }
		int Y { get; set; }
		Point Goto { get; set; }
		/// <summary>
		/// Current tile of `Map` that Unit sit on
		/// </summary>
		ITile Tile { get; }
		/// <summary>
		/// Tells either Unit can move/make its turn or not
		/// </summary>
		bool Busy { get; set; }
		/// <summary>
		/// Unit has some action to do
		/// (e.g. building a road, fortify, sentry, and also goto action)
		/// </summary>
		bool HasAction { get; }
		/// <summary>
		/// Unit has some moves left to do
		/// MovesLeft or PartMoves are not zero.
		/// </summary>
		bool HasMovesLeft { get; }
		/// <summary>
		/// Unit has Veteran grade
		/// </summary>
		bool Veteran { get; set; }
		/// <summary>
		/// Unit in Sentry state
		/// </summary>
		bool Sentry { get; set; }
		/// <summary>
		/// Unit got Fortify command
		/// </summary>
		bool FortifyActive { get; }
		/// <summary>
		/// Unit in Fortify state
		/// </summary>
		bool Fortify { get; set; }

		/// <summary>
		/// Fuel (Flight) or Build-Progress (Settlers) for Unit.
		/// Use instead of Fuel and Progress properties (contains the same value).
		/// </summary>
		byte FuelOrProgress { get; set; }
		/// <summary>
		/// Fuel for Unit (Flight)
		/// </summary>
		byte Fuel { get; set; }

		/// <summary>
		/// Build-Progress for Unit (Settlers)
		/// </summary>
		byte WorkProgress { get; set; }
		/// <summary>
		/// Unit is Moving now
		/// </summary>
		bool Moving { get; }
		MoveUnit Movement { get; }
		bool MoveTo(int relX, int relY);
		/// <summary>
		/// Tells who is owner [player/civilization/barbarian] for this Unit
		/// </summary>
		byte Owner { get; set; }
		/// <summary>
		/// The Status property is for saving/restoring state with the savefile
		/// </summary>
		byte Status { set; }
		/// <summary>
		/// Current Order for Unit.
		/// Unit can handle only order per turn. Each order can cost some amount of turns.
		/// (See `MovesSkip`)
		/// </summary>
		Order order { get; set; }
		/// <summary>
		/// How many turns Unit should skip to complete the `Order`
		/// </summary>
		int MovesSkip { get; set; }
		/// <summary>
		/// How many movement points the unit has remaining this turn
		/// </summary>
		byte MovesLeft { get; set; }
		/// <summary>
		/// How many partial movement points the unit has remaining this turn. A partial
		/// movement point may allow moving off a road onto other terrain, depending on 
		/// the terrain movement cost.
		/// </summary>
		byte PartMoves { get; set; }
		/// <summary>
		/// Completes the turn for Unit
		/// </summary>
		void SkipTurn();
		IEnumerable<ITile> MoveTargets { get; }
		void Explore();
		/// <summary>
		/// Establishes the unit's home (supporting) city [called when unit built in a city]
		/// </summary>
		void SetHome();
		/// <summary>
		/// Establishes the unit's home (supporting) city.
		/// </summary>
		void SetHome(City city);
		IEnumerable<MenuItem<int>> MenuItems { get; }
		IEnumerable<UnitModification> Modifications { get; }
		/// <summary>
		/// Perform pillaging activity
		/// </summary>
		void Pillage();

		void SentryOnShip();

		int NearestCity { get; }

		Player Player { get; }
	}
}