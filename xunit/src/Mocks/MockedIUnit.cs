using System.Collections.Generic;
using CivOne.Tiles;
using CivOne.Units;
using System;
using CivOne.UserInterface;
using CivOne.Advances;
using CivOne.Wonders;
using CivOne.Enums;
using System.Drawing;
using CivOne.Tasks;
using CivOne.Graphics;

namespace CivOne.UnitTests
{
	class MockedIUnit : IUnitRestorable
	{
        public MockedIUnit()
        {
            MenuItems = [];
            Modifications = [];

            // standard values
            RequiredTech = null;
            RequiredWonder = null;
            ObsoleteTech = null;
            Class = UnitClass.Land;
            Type = UnitType.Settlers;
            Home = null;
            Role = UnitRole.Settler;
            Attack = 0;
            Defense = 1;
            Move = 1;
            X = 2;
            Y = 3;
            Goto = new Point(1, 2);
            Tile = null;
            Busy = false;
            HasAction = false;
            HasMovesLeft = true;
            Veteran = false;
            Sentry = false;
            Fortify = false;
            FuelOrProgress = 0;
            Fuel = 0;
            WorkProgress = 0;
            Moving = false;
            Movement = null;
            Owner = 0;
            Status = 0;
            order = Order.None;
            MovesSkip = 0;
            MovesLeft = 1;
            PartMoves = 0;
            MoveTargets = [];
            MenuItems = [];
            Modifications = [];
            NearestCity = 0;
            Player = null;
            Name = "Mocked Unit";
            Icon = null;
            PageCount = 0;
            Price = 0;
            BuyPrice = 0;
            ProductionId = 0;
        }
		public bool FortifyActive { get; set; }

		public IAdvance RequiredTech { get; set; }

		public IWonder RequiredWonder { get; set; }

		public IAdvance ObsoleteTech { get; set; }

		public UnitClass Class { get; set; }

		public UnitType Type { get; set; }

		public City Home { get; set; }

		public UnitRole Role { get; set; }

		public byte Attack { get; set; }

		public byte Defense { get; set; }

		public byte Move { get; set; }

		public int X { get; set; }
		public int Y { get; set; }
		public Point Goto { get; set; }

		public ITile Tile { get; set; }

		public bool Busy { get; set; }

		public bool HasAction { get; set; }

		public bool HasMovesLeft { get; set; }

		public bool Veteran { get; set; }
		public bool Sentry { get; set; }
		public bool Fortify { get; set; }
		public byte FuelOrProgress { get; set; }
		public byte Fuel { get; set; }
		public byte WorkProgress { get; set; }

		public bool Moving { get; set; }

		public MoveUnit Movement { get; set; }

		public byte Owner { get; set; }
		public byte Status { get; set; }
		public Order order { get; set; }
		public int MovesSkip { get; set; }
		public byte MovesLeft { get; set; }
		public byte PartMoves { get; set; }

		public IEnumerable<ITile> MoveTargets { get; set; }

		public IEnumerable<MenuItem<int>> MenuItems { get; set; }

		public IEnumerable<UnitModification> Modifications { get; set; }

		public int NearestCity { get; set; }

		public Player Player { get; set; }

		public string Name { get; set; }

		public IBitmap Icon { get; set; }

		public byte PageCount { get; set; }

		public byte Price { get; set; }

		public short BuyPrice { get; set; }

		public byte ProductionId { get; set; }

		public Picture DrawPage(byte pageNumber)
		{
			throw new NotImplementedException();
		}

		public void Explore()
		{
			throw new NotImplementedException();
		}

		public bool MoveTo(int relX, int relY)
		{
			throw new NotImplementedException();
		}

		public void NewTurn()
		{
			throw new NotImplementedException();
		}

		public void Pillage()
		{
			throw new NotImplementedException();
		}

		public void SentryOnShip()
		{
			throw new NotImplementedException();
		}

		public void SetHome()
		{
			throw new NotImplementedException();
		}

		public void SetHome(City city)
		{
			throw new NotImplementedException();
		}

		public void SkipTurn()
		{
			throw new NotImplementedException();
		}
	}
}