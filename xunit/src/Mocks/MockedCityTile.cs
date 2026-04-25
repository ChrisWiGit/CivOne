using CivOne.Buildings;
using CivOne.Wonders;
using System.Drawing;
using CivOne.Tiles;
using CivOne.Units;
using System;
using System.Linq;
using System.Collections.Generic;
using CivOne.Enums;
using CivOne.Persistence.Model;

namespace CivOne.UnitTests
{
    public class MockedCityTile : ICityTile
    {
        public ITile[,] Tiles { get; }
        public ITile Tile { get => Tiles[2, 2]; }

        public MockedCityTile()
        {
            Tiles = new ITile[5, 5];
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    Tiles[x, y] = new MockedGrassland(x, y)
                    {
                        MockedMap = Tiles
                    };
                }
            }
        }
    }
}
