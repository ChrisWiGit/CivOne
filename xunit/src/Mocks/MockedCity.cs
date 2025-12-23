// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

// Author: Kevin Routley : July, 2019

using CivOne.Buildings;
using CivOne.Wonders;
using System.Drawing;
using CivOne.Tiles;
using CivOne.Units;
using System;

namespace CivOne.UnitTests
{

public partial class CityCitizenServiceImplTests
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
			private readonly SupplyMockedValues<bool> _hasBuilding;
            private readonly SupplyMockedValues<bool> _hasWonder;

            public MockedCity()
            {
                _hasBuilding = new SupplyMockedValues<bool>();
                _hasWonder = new SupplyMockedValues<bool>();
            }

            public MockedCity ReturnHasBuildingValues(params bool[] values)
            {
                _hasBuilding.Reset(values);
                return this;
            }

            public MockedCity ReturnHasWonderValues(params bool[] values)
            {
                _hasWonder.Reset(values);
                return this;
            }

            public MockedCity WithContinentId(int continentId)
            {
                ContinentId = continentId;
                return this;
            }

            public bool HasBuilding<T>() where T : IBuilding => _hasBuilding.Next();

            public bool HasWonder<T>() where T : IWonder => _hasWonder.Next();

            public void NewTurn()
            {
                throw new NotImplementedException();
            }
        }
    }
}
