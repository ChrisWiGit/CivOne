using System.Collections.Generic;
using CivOne.Tiles;

namespace CivOne
{
	public interface ICityOnContinent : ICityBasic, ICityBuildings
	{
	}
	
	public interface IMap : IMapContinents, IMapTerrain, IMapTiles
	{
	
	}

	public interface IMapContinents
	{
		IEnumerable<ICityOnContinent> ContinentCities(int continentId);

		int TerrainMasterWord { get; }
	}

	public interface IMapTerrain
	{
		int TerrainMasterWord { get; }
	}

	public interface IMapTiles
	{
		ITile this[int x, int y] { get; }

		int Width { get; }
		int Height { get; }
	}
}