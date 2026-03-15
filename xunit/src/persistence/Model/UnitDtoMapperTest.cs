using System;
using System.Linq;
using CivOne.Enums;
using CivOne.Units;
using CivOne.UnitTests;
using Xunit;

namespace CivOne.Persistence.Model
{
	public class UnitFactory : IUnitFactory
	{
		public IUnitRestorable Create(string className, byte player, string HomeCityUuid)
		{
			Assert.Equal("MockedIUnit", className);
			// * var result = new MockedIUnit
			// * {
			// * 	Owner = player //just for testing, do not do this in real code because index of player it game list may not be the same as player id in save file.
			// * };
			// Just for fun, let's use reflection to create the instance instead of hardcoding it. This way we can test that the className is used correctly.
			var result = (IUnitRestorable)Activator.CreateInstance(Type.GetType($"CivOne.UnitTests.{className}"));
			result.Owner = player; 

			return result;
		}
	}
	public class UnitDtoMapperTest
	{
		private UnitDtoMapper _testee;
		public UnitDtoMapperTest()
		{
			_testee = new UnitDtoMapper(new UnitFactory());
		}

		[Fact]
		public void TestUnitToDto()
		{
			var unit = new MockedIUnit
			{
				Owner = 1,
			};
			var dto = _testee.ToDto(unit);
			Assert.NotNull(dto);
			Assert.Equal("MockedIUnit", dto.ClassName);

			var restored = _testee.FromDto(dto);
			Assert.NotNull(restored);
			Assert.Equal(unit.Owner, restored.Owner);
			Assert.Equal(unit.Type, restored.Type);
			Assert.Equal(unit.GetType().Name, dto.ClassName);
			Assert.Equal(unit.Busy, restored.Busy);
			Assert.Equal(unit.HasAction, restored.HasAction);
			Assert.Equal(unit.HasMovesLeft, restored.HasMovesLeft);
			Assert.Equal(unit.Veteran, restored.Veteran);
			Assert.Equal(unit.Sentry, restored.Sentry);
			Assert.Equal(unit.FortifyActive, restored.FortifyActive);
			Assert.Equal(unit.Fortify, restored.Fortify);
			Assert.Equal(unit.FuelOrProgress, restored.FuelOrProgress);
			Assert.Equal(unit.Fuel, restored.Fuel);
			Assert.Equal(unit.WorkProgress, restored.WorkProgress);
			Assert.Equal(unit.order, restored.order);
			Assert.Equal(unit.MovesSkip, restored.MovesSkip);
			Assert.Equal(unit.MovesLeft, restored.MovesLeft);
			Assert.Equal(unit.PartMoves, restored.PartMoves);	
		}
	}
}

// public MockedIUnit()
//         {
//             MenuItems = [];
//             Modifications = [];

//             // standard values
//             RequiredTech = null;
//             RequiredWonder = null;
//             ObsoleteTech = null;
//             Class = UnitClass.Land;
//             Type = UnitType.Settlers;
//             Home = null;
//             Role = UnitRole.Settler;
//             Attack = 0;
//             Defense = 1;
//             Move = 1;
//             X = 2;
//             Y = 3;
//             Goto = new Point(1, 2);
//             Tile = null;
//             Busy = false;
//             HasAction = false;
//             HasMovesLeft = true;
//             Veteran = false;
//             Sentry = false;
//             Fortify = false;
//             FuelOrProgress = 0;
//             Fuel = 0;
//             WorkProgress = 0;
//             Moving = false;
//             Movement = null;
//             Owner = 0;
//             Status = 0;
//             order = Order.None;
//             MovesSkip = 0;
//             MovesLeft = 1;
//             PartMoves = 0;
//             MoveTargets = [];
//             MenuItems = [];
//             Modifications = [];
//             NearestCity = 0;
//             Player = null;
//             Name = "Mocked Unit";
//             Icon = null;
//             PageCount = 0;
//             Price = 0;
//             BuyPrice = 0;
//             ProductionId = 0;
//         }
// public enum UnitType
// 	{
// 		Settlers = 0,
// 		Militia = 1,
// 		Phalanx = 2,
// 		Legion = 3,
// 		Musketeers = 4,
// 		Riflemen = 5,
// 		Cavalry = 6,
// 		Knights = 7,
// 		Catapult = 8,
// 		Cannon = 9,
// 		Chariot = 10,
// 		Armor = 11,
// 		MechInf = 12,
// 		Artillery = 13,

// 		private readonly IReflect _reflect = new GameReflect();
// 		private readonly ProductionDtoMapper _testee;

// 		public ProductionDtoMapperTest()
// 		{
// 			_testee = new ProductionDtoMapper(_reflect);
// 		}

// 		[Fact]
// 		public void ToDto_MapsAllFieldsCorrectly()
// 		{
// 			IProduction production = _reflect.GetProduction().First();

// 			ProductionDto dto = _testee.ToDto(production);

// 			Assert.Equal((uint)production.Price, dto.Price);
// 			Assert.Equal((uint)production.BuyPrice, dto.BuyPrice);
// 			Assert.Equal((uint)production.ProductionId, dto.ProductionId);
// 		}

// 		[Fact]
// 		public void FromDto_WithValidProductionId_ReturnsMatchingProduction()
// 		{
// 			// 0 is okay, but we want to test another one.
// 			IProduction expected = _reflect.GetProduction().First(p => p.Price > 0);
// 			var dto = new ProductionDto { ProductionId = expected.ProductionId };

// 			IProduction actual = _testee.FromDto(dto);

// 			Assert.Equal(expected.ProductionId, actual.ProductionId);
// 		}

// 		[Fact]
// 		public void FromDto_WithInvalidProductionId_ThrowsException()
// 		{
// 			var dto = new ProductionDto { ProductionId = uint.MaxValue };

// 			Assert.Throws<Exception>(() => _testee.FromDto(dto));
// 		}

// 		[Fact]
// 		public void RoundTrip_ToDtoThenFromDto_PreservesProductionId()
// 		{
// 			foreach (IProduction production in _reflect.GetProduction())
// 			{
// 				ProductionDto dto = _testee.ToDto(production);
// 				IProduction restored = _testee.FromDto(dto);

// 				Assert.Equal(production.ProductionId, restored.ProductionId);
// 			}
// 		}
// 	}
// }

// using CivOne.Enums;

// namespace CivOne.Persistence.Model
// {
//     public class UnitDto
//     {
//         public string ClassName { get; set; }
//         public MapLocation Location { get; set; }
//         public MapLocation Goto { get; set; }

//         public string HomeCity { get; set; }

//         public bool Busy { get; set; }
//         public bool HasAction { get; set; }
//         public bool HasMovesLeft { get; set; }
//         public bool Veteran { get; set; }
//         public bool Sentry { get; set; }
//         public bool FortifyActive { get; set; }
//         public bool Fortify { get; set; }

//         public byte FuelOrProgress { get; set; }
//         public byte Fuel { get; set; }
//         public byte WorkProgress { get; set; }

//         public Order Order { get; set; }

//         public int MovesSkip { get; set; }
//         public byte MovesLeft { get; set; }
//         public byte PartMoves { get; set; }

//         // Owner wil be set from Player
//         public byte PlayerId { get; set; }
//     }
// }


// namespace CivOne.Persistence.Model
// {
// 	using System;
// 	using System.Diagnostics;
// 	using System.Drawing;
// 	using System.Linq;
// 	using CivOne.Units;
// 	using CityId = System.UInt32;
// 	using PlayerId = System.Byte;

// 	public interface IUnitFactory
// 	{
// 		IUnitRestorable Create(string className, PlayerId player, string HomeCity);
// 	}


// 	public class UnitDtoMapper(IUnitFactory _unitFactory) : DtoMapper<UnitDto, IUnit>
// 	{
// 		public IUnit FromDto(UnitDto dto)
// 		{
// 			var unit = _unitFactory.Create(dto.ClassName, dto.PlayerId, dto.HomeCity);
// 			unit.X = Math.Abs((int)dto.Location.X);
// 			unit.Y = Math.Abs((int)dto.Location.Y);
// 			unit.Goto = new Point(Math.Abs((int)dto.Goto.X), Math.Abs((int)dto.Goto.Y));
// 			unit.Busy = dto.Busy;
// 			unit.Veteran = dto.Veteran;
// 			unit.Sentry = dto.Sentry;
// 			unit.FortifyActive = dto.FortifyActive;
// 			unit.Fortify = dto.Fortify;
// 			unit.FuelOrProgress = dto.FuelOrProgress;
// 			unit.Fuel = dto.Fuel;
// 			unit.WorkProgress = dto.WorkProgress;
// 			unit.order = dto.Order;
// 			unit.MovesSkip = dto.MovesSkip;
// 			unit.MovesLeft = dto.MovesLeft;
// 			unit.PartMoves = dto.PartMoves;

// 			return unit;
// 		}

// 		public UnitDto ToDto(IUnit domain)
// 		{
// 			Debug.Assert(domain.X >= 0, "Unit X coordinate cannot be negative");
// 			Debug.Assert(domain.Y >= 0, "Unit Y coordinate cannot be negative");

// 			return new UnitDto
// 			{
// 				ClassName = domain.GetType().Name,
// 				Location = new MapLocation((uint)domain.X, (uint)domain.Y),
// 				Goto = new MapLocation(domain.Goto),
// 				HomeCity = domain.Home?.Id.ToString() ?? null,
// 				Busy = domain.Busy,
// 				HasAction = domain.HasAction,
// 				HasMovesLeft = domain.HasMovesLeft,
// 				Veteran = domain.Veteran,
// 				Sentry = domain.Sentry,
// 				FortifyActive = domain.FortifyActive,
// 				Fortify = domain.Fortify,
// 				FuelOrProgress = domain.FuelOrProgress,
// 				Fuel = domain.Fuel,
// 				WorkProgress = domain.WorkProgress,
// 				Order = domain.order,
// 				MovesSkip = domain.MovesSkip,
// 				MovesLeft = domain.MovesLeft,
// 				PartMoves = domain.PartMoves,
// 			};
// 		}
// 	}
// }


// // CivOne
// //
// // To the extent possible under law, the person who associated CC0 with
// // CivOne has waived all copyright and related or neighboring rights
// // to CivOne.
// //
// // You should have received a copy of the CC0 legalcode along with this
// // work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

// using System.Collections.Generic;
// using System.Drawing;
// using CivOne.Advances;
// using CivOne.Enums;
// using CivOne.Screens;
// using CivOne.Tasks;
// using CivOne.Tiles;
// using CivOne.Units;
// using CivOne.UserInterface;
// using CivOne.Wonders;

// namespace CivOne.Units
// {
// 	public interface IUnit : ICivilopedia, IProduction, ITurn
// 	{
// 		IAdvance RequiredTech { get; }
// 		IWonder RequiredWonder { get; }
// 		IAdvance ObsoleteTech { get; }
// 		UnitClass Class { get; }
// 		/// <summary>
// 		/// Defines type of the unit
// 		/// </summary>
// 		UnitType Type { get; }
// 		/// <summary>
// 		/// Defines home (supporting city) of the unit
// 		/// Deprecated: use IsHome(ICityBasic city) method instead
// 		/// to check if unit's home is the given city
// 		/// </summary>
// 		City Home { get; }

// 		virtual bool IsHome(ICityBasic city) => HasHome && Home == city;
// 		bool HasHome => Home != null;
// 		UnitRole Role { get; }
// 		byte Attack { get; }
// 		byte Defense { get; }
// 		byte Move { get; }
// 		int X { get; set; }
// 		int Y { get; set; }
// 		Point Goto { get; set; }
// 		/// <summary>
// 		/// Current tile of `Map` that Unit sit on
// 		/// </summary>
// 		ITile Tile { get; }
// 		/// <summary>
// 		/// Tells either Unit can move/make its turn or not
// 		/// </summary>
// 		bool Busy { get; set; }
// 		/// <summary>
// 		/// Unit has some action to do
// 		/// (e.g. building a road, fortify, sentry, and also goto action)
// 		/// </summary>
// 		bool HasAction { get; }
// 		/// <summary>
// 		/// Unit has some moves left to do
// 		/// MovesLeft or PartMoves are not zero.
// 		/// </summary>
// 		bool HasMovesLeft { get; }
// 		/// <summary>
// 		/// Unit has Veteran grade
// 		/// </summary>
// 		bool Veteran { get; set; }
// 		/// <summary>
// 		/// Unit in Sentry state
// 		/// </summary>
// 		bool Sentry { get; set; }
// 		/// <summary>
// 		/// Unit got Fortify command
// 		/// </summary>
// 		bool FortifyActive { get; }
// 		/// <summary>
// 		/// Unit in Fortify state
// 		/// </summary>
// 		bool Fortify { get; set; }

// 		/// <summary>
// 		/// Fuel (Flight) or Build-Progress (Settlers) for Unit.
// 		/// Use instead of Fuel and Progress properties (contains the same value).
// 		/// </summary>
// 		byte FuelOrProgress { get; set; }
// 		/// <summary>
// 		/// Fuel for Unit (Flight)
// 		/// </summary>
// 		byte Fuel { get; set; }

// 		/// <summary>
// 		/// Build-Progress for Unit (Settlers)
// 		/// </summary>
// 		byte WorkProgress { get; set; }
// 		/// <summary>
// 		/// Unit is Moving now
// 		/// </summary>
// 		bool Moving { get; }
// 		MoveUnit Movement { get; }
// 		bool MoveTo(int relX, int relY);
// 		/// <summary>
// 		/// Tells who is owner [player/civilization/barbarian] for this Unit
// 		/// </summary>
// 		byte Owner { get; set; }
// 		/// <summary>
// 		/// The Status property is for saving/restoring state with the savefile
// 		/// </summary>
// 		byte Status { set; }
// 		/// <summary>
// 		/// Current Order for Unit.
// 		/// Unit can handle only order per turn. Each order can cost some amount of turns.
// 		/// (See `MovesSkip`)
// 		/// </summary>
// 		Order order { get; set; }
// 		/// <summary>
// 		/// How many turns Unit should skip to complete the `Order`
// 		/// </summary>
// 		int MovesSkip { get; set; }
// 		/// <summary>
// 		/// How many movement points the unit has remaining this turn
// 		/// </summary>
// 		byte MovesLeft { get; set; }
// 		/// <summary>
// 		/// How many partial movement points the unit has remaining this turn. A partial
// 		/// movement point may allow moving off a road onto other terrain, depending on 
// 		/// the terrain movement cost.
// 		/// </summary>
// 		byte PartMoves { get; set; }
// 		/// <summary>
// 		/// Completes the turn for Unit
// 		/// </summary>
// 		void SkipTurn();
// 		IEnumerable<ITile> MoveTargets { get; }
// 		void Explore();
// 		/// <summary>
// 		/// Establishes the unit's home (supporting) city [called when unit built in a city]
// 		/// </summary>
// 		void SetHome();
// 		/// <summary>
// 		/// Establishes the unit's home (supporting) city.
// 		/// </summary>
// 		void SetHome(City city);
// 		IEnumerable<MenuItem<int>> MenuItems { get; }
// 		IEnumerable<UnitModification> Modifications { get; }
// 		/// <summary>
// 		/// Perform pillaging activity
// 		/// </summary>
// 		void Pillage();

// 		void SentryOnShip();

// 		int NearestCity { get; }

// 		Player Player { get; }
// 	}
// }

// using CivOne.Enums;

// namespace CivOne.Persistence.Model
// {
//     public class UnitDto
//     {
//         public string ClassName { get; set; }
//         public MapLocation Location { get; set; }
//         public MapLocation Goto { get; set; }

//         public string HomeCity { get; set; }

//         public bool Busy { get; set; }
//         public bool HasAction { get; set; }
//         public bool HasMovesLeft { get; set; }
//         public bool Veteran { get; set; }
//         public bool Sentry { get; set; }
//         public bool FortifyActive { get; set; }
//         public bool Fortify { get; set; }

//         public byte FuelOrProgress { get; set; }
//         public byte Fuel { get; set; }
//         public byte WorkProgress { get; set; }

//         public byte Status { get; set; }

//         public Order Order { get; set; }

//         public int MovesSkip { get; set; }
//         public byte MovesLeft { get; set; }
//         public byte PartMoves { get; set; }

//         // Owner wil be set from Player
//         public byte Player { get; set; }
//     }
// }

// // CivOne
// //
// // To the extent possible under law, the person who associated CC0 with
// // CivOne has waived all copyright and related or neighboring rights
// // to CivOne.
// //
// // You should have received a copy of the CC0 legalcode along with this
// // work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

// using System.Collections.Generic;
// using System.Drawing;
// using CivOne.Advances;
// using CivOne.Enums;
// using CivOne.Screens;
// using CivOne.Tasks;
// using CivOne.Tiles;
// using CivOne.Units;
// using CivOne.UserInterface;
// using CivOne.Wonders;

// namespace CivOne.Units
// {
// 	public interface IUnit : ICivilopedia, IProduction, ITurn
// 	{
// 		IAdvance RequiredTech { get; }
// 		IWonder RequiredWonder { get; }
// 		IAdvance ObsoleteTech { get; }
// 		UnitClass Class { get; }
// 		/// <summary>
// 		/// Defines type of the unit
// 		/// </summary>
// 		UnitType Type { get; }
// 		/// <summary>
// 		/// Defines home (supporting city) of the unit
// 		/// Deprecated: use IsHome(ICityBasic city) method instead
// 		/// to check if unit's home is the given city
// 		/// </summary>
// 		City Home { get; }

// 		virtual bool IsHome(ICityBasic city) => HasHome && Home == city;
// 		bool HasHome => Home != null;
// 		UnitRole Role { get; }
// 		byte Attack { get; }
// 		byte Defense { get; }
// 		byte Move { get; }
// 		int X { get; set; }
// 		int Y { get; set; }
// 		Point Goto { get; set; }
// 		/// <summary>
// 		/// Current tile of `Map` that Unit sit on
// 		/// </summary>
// 		ITile Tile { get; }
// 		/// <summary>
// 		/// Tells either Unit can move/make its turn or not
// 		/// </summary>
// 		bool Busy { get; set; }
// 		/// <summary>
// 		/// Unit has some action to do
// 		/// (e.g. building a road, fortify, sentry, and also goto action)
// 		/// </summary>
// 		bool HasAction { get; }
// 		/// <summary>
// 		/// Unit has some moves left to do
// 		/// MovesLeft or PartMoves are not zero.
// 		/// </summary>
// 		bool HasMovesLeft { get; }
// 		/// <summary>
// 		/// Unit has Veteran grade
// 		/// </summary>
// 		bool Veteran { get; set; }
// 		/// <summary>
// 		/// Unit in Sentry state
// 		/// </summary>
// 		bool Sentry { get; set; }
// 		/// <summary>
// 		/// Unit got Fortify command
// 		/// </summary>
// 		bool FortifyActive { get; }
// 		/// <summary>
// 		/// Unit in Fortify state
// 		/// </summary>
// 		bool Fortify { get; set; }

// 		/// <summary>
// 		/// Fuel (Flight) or Build-Progress (Settlers) for Unit.
// 		/// Use instead of Fuel and Progress properties (contains the same value).
// 		/// </summary>
// 		byte FuelOrProgress { get; set; }
// 		/// <summary>
// 		/// Fuel for Unit (Flight)
// 		/// </summary>
// 		byte Fuel { get; set; }

// 		/// <summary>
// 		/// Build-Progress for Unit (Settlers)
// 		/// </summary>
// 		byte WorkProgress { get; set; }
// 		/// <summary>
// 		/// Unit is Moving now
// 		/// </summary>
// 		bool Moving { get; }
// 		MoveUnit Movement { get; }
// 		bool MoveTo(int relX, int relY);
// 		/// <summary>
// 		/// Tells who is owner [player/civilization/barbarian] for this Unit
// 		/// </summary>
// 		byte Owner { get; set; }
// 		/// <summary>
// 		/// The Status property is for saving/restoring state with the savefile
// 		/// </summary>
// 		byte Status { set; }
// 		/// <summary>
// 		/// Current Order for Unit.
// 		/// Unit can handle only order per turn. Each order can cost some amount of turns.
// 		/// (See `MovesSkip`)
// 		/// </summary>
// 		Order order { get; set; }
// 		/// <summary>
// 		/// How many turns Unit should skip to complete the `Order`
// 		/// </summary>
// 		int MovesSkip { get; set; }
// 		/// <summary>
// 		/// How many movement points the unit has remaining this turn
// 		/// </summary>
// 		byte MovesLeft { get; set; }
// 		/// <summary>
// 		/// How many partial movement points the unit has remaining this turn. A partial
// 		/// movement point may allow moving off a road onto other terrain, depending on 
// 		/// the terrain movement cost.
// 		/// </summary>
// 		byte PartMoves { get; set; }
// 		/// <summary>
// 		/// Completes the turn for Unit
// 		/// </summary>
// 		void SkipTurn();
// 		IEnumerable<ITile> MoveTargets { get; }
// 		void Explore();
// 		/// <summary>
// 		/// Establishes the unit's home (supporting) city [called when unit built in a city]
// 		/// </summary>
// 		void SetHome();
// 		/// <summary>
// 		/// Establishes the unit's home (supporting) city.
// 		/// </summary>
// 		void SetHome(City city);
// 		IEnumerable<MenuItem<int>> MenuItems { get; }
// 		IEnumerable<UnitModification> Modifications { get; }
// 		/// <summary>
// 		/// Perform pillaging activity
// 		/// </summary>
// 		void Pillage();

// 		void SentryOnShip();

// 		int NearestCity { get; }

// 		Player Player { get; }
// 	}
// }