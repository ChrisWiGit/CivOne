using System;
using CivOne.Enums;
using CivOne.Tiles;

namespace CivOne.Services
{
	public class LandValueCalculatorServiceImpl() : ILandValueCalculatorService
	{
		private readonly ILoggerService logger = LoggerProvider.GetLogger();
		
		public void CalculateLandValue(ITile[,] _tiles)
		{
			logger.Log("Map: Calculating land value");

			byte riverLandScore = new River().LandScore;
			byte grasslandLandScore = new Grassland().LandScore;

			// This code is a translation of Darkpanda's forum post here: http://forums.civfanatics.com/showthread.php?t=498532
			// Comments are pasted from the original forum thread to make the code more readable.

			// map squares for which the land value is calculated are in the range [2,2] - [77,47]
			for (int y = 2; y < Map.HEIGHT - 2; y++)
				for (int x = 2; x < Map.WIDTH - 2; x++)
				{
					// initial value is 0
					_tiles[x, y].LandValue = 0;

					// If the square's terrain type is not Plains, Grassland or River, then its land value is 0
					if (!Map.TileIsType(_tiles[x, y], Terrain.Plains, Terrain.Grassland1, Terrain.Grassland2, Terrain.River)) continue;

					// for each 'city square' neighbouring the map square (i.e. each square following the city area pattern,
					// including the map square itself, so totally 21 'neighbours'), compute the following neighbour value (initially 0):
					int landValue = 0;
					for (int yy = -2; yy <= 2; yy++)
					{
						for (int xx = -2; xx <= 2; xx++)
						{
							// Skip the corners of the square to create a city area pattern
							if (Math.Abs(xx) == 2 && Math.Abs(yy) == 2) continue;

							// initial value is 0
							int val = 0;

							ITile tile = _tiles[x + xx, y + yy];
							if (tile.Special && Map.TileIsType(tile, Terrain.Grassland1, Terrain.Grassland2, Terrain.River))
							{
								// If the neighbour square type is Grassland special or River special, add 2,
								// then add the non-special Grassland or River terrain type score to the neighbour value
								val += 2;
								if (tile.Type == Terrain.River)
									val += riverLandScore;
								else
									val += grasslandLandScore;
							}
							else
							{
								// Else add neighbour's terrain type score to the neighbour value
								val += tile.LandScore;
							}

							// If the neighbour square is in the map square inner circle, i.e. one of the 8 neighbours immediatly
							// surrounding the map square, then multiply the neighbour value by 2
							if (Math.Abs(xx) <= 1 && Math.Abs(yy) <= 1 && (xx != 0 || yy != 0)) val *= 2;

							// If the neighbour square is the North square (relative offset 0,-1), then multiply the neighbour value by 2 ;
							// note: I actually think that this is a bug, and that the intention was rather to multiply by 2 if the 'neighbour'
							// was the central map square itself... the actual CIV code for this is to check if the 'neighbour index' is '0';
							// the neighbour index is used to retrieve the neighbour's relative offset coordinates (x,y) from the central square,
							// and the central square itself is actually the last in the list (index 20), the first one (index 0) being
							// the North neighbour; another '7x7 neighbour pattern' table found in CIV code does indeed set the central square
							// at index 0, and this why I believe ths is a programmer's mistake...
							if (xx == 0 && yy == -1) val *= 2;

							// Add the neighbour's value to the map square total value and loop to next neighbour
							landValue += val;
						}
					}

					// After all neighbours are processed, if the central map square's terrain type is non-special Grassland or River,
					// subtract 16 from total land value
					if (!_tiles[x, y].Special && Map.TileIsType(_tiles[x, y], Terrain.Grassland1, Terrain.River)) landValue -= 16;

					landValue -= 120; // Substract 120 (0x78) from the total land value,
					bool negative = (landValue < 0); // and remember its sign
					landValue = Math.Abs(landValue); // Set the land value to the absolute land value (i.e. negate it if it is negative)
					landValue /= 8; // Divide the land value by 8
					if (negative) landValue = 1 - landValue; // If the land value was negative 3 steps before, then negate the land value and add 1

					// Adjust the land value to the range [1..15]
					if (landValue < 1) landValue = 1;
					if (landValue > 15) landValue = 15;

					landValue /= 2; // Divide the land value by 2
					landValue += 8; // And finally, add 8 to the land value
					_tiles[x, y].LandValue = (byte)landValue;
				}
		}
	}
}