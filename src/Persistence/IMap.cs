using System.Collections;
using System.Collections.Generic;
using System.IO;
using CivOne.Enums;
using CivOne.Tiles;

namespace CivOne.Persistence
{
	/// <summary>
	/// Read-only interface for querying map data as defined by CQRS pattern.
	/// </summary>
	public interface IMapQuery
	{
		int Width { get; }
		int Height { get; }

		ITile this[int x, int y] { get; }
		ITile[,] this[int x, int y, int width, int height] { get; }

		ITile[,] Tiles { get; }
		bool FixedStartPositions { get; }

		int Randomness { get; set; }

		IEnumerable<ITile> ContinentTiles(int continentId);
		IEnumerable<City> ContentCities(int continentId);
		IEnumerable<ITile> AllTiles();
		IEnumerable<ITile> QueryMapPart(int x, int y, int width, int height);
	}


	/// <summary>
	/// Write interface for modifying map data as defined by CQRS pattern.
	/// </summary>
	public interface IMapCommand
	{
		void ChangeTileType(int x, int y, Terrain type);
	}

	/// <summary>
	/// Compatibility interface for map data.
	/// </summary>
	public interface IMap : IMapQuery, IMapCommand
	{
	}
}