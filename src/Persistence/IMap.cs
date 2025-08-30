using System.Collections;
using System.Collections.Generic;
using System.IO;
using CivOne.Tiles;

namespace CivOne.Persistence
{
	public interface IMap
	{
		ITile this[int x, int y] { get; }
		ITile[,] this[int x, int y, int width, int height] { get; }
		bool FixedStartPositions { get; }

		IEnumerable<ITile> ContinentTiles(int continentId);
		IEnumerable<City> ContentCities(int continentId);
		IEnumerable<ITile> AllTiles();
		IEnumerable<ITile> QueryMapPart(int x, int y, int width, int height);
	}
}