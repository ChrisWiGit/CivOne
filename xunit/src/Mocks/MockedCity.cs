using CivOne.Buildings;
using CivOne.Wonders;
using System.Drawing;
using CivOne.Tiles;
using CivOne.Units;
using System;
using System.Linq;
using System.Collections.Generic;

namespace CivOne.UnitTests
{
    public class MockedCity :
        // ICityBasic, ICityBuildings, ICityOnContinent
        ICity
    {
        public Point Location => new Point(0, 0);
        public byte Size { get; set; } = 5;
        public short Luxuries { get; set; } = 0;
        public byte Owner { get; set; } = 0;


        public ITile Tile { get; set; } = null;

        public int ContinentId { get; set; } = 0;
        public Player Player => _player;
        private Player _player = null;
        public Player MockPlayer
        {
            get => _player;
            set => _player = value;
        }

        public int Entertainers { get; set; } = 0;
        public int Scientists { get; set; } = 0;
        public int Taxmen { get; set; } = 0;

        public int EntertainerLuxuries => Entertainers * 3;
        private readonly SupplyMockedValues<bool> _hasBuilding = new SupplyMockedValues<bool>();
        private readonly SupplyMockedValues<bool> _hasWonder = new SupplyMockedValues<bool>();

        private readonly ISet<IWonder> _wonders = new HashSet<IWonder>();
        private readonly ISet<IBuilding> _buildings = new HashSet<IBuilding>();

        public MockedCity ReturnHasBuildingValues(params bool[] values)
        {
            _hasBuilding.Reset(values);
            return this;
        }

        public MockedCity WithBuilding<T>() where T : IBuilding
        {
            _buildings.Add(Activator.CreateInstance<T>());
            return this;
        }

        public MockedCity ReturnHasWonderValues(params bool[] values)
        {
            _hasWonder.Reset(values);
            return this;
        }

        public MockedCity WithWonder<T>() where T : IWonder
        {
            _wonders.Add(Activator.CreateInstance<T>());
            return this;
        }

        public MockedCity WithContinentId(int continentId)
        {
            ContinentId = continentId;
            return this;
        }

        public bool HasBuilding<T>() where T : IBuilding => _hasBuilding.Next() || 
            _buildings.Any(
                t => t.GetType() == typeof(T)
            );
        public bool HasWonder<T>() where T : IWonder => _hasWonder.Next() || 
            _wonders.Any(
                t => t.GetType() == typeof(T)
            );

        public void NewTurn()
        {
            throw new NotImplementedException();
        }
    }
}
