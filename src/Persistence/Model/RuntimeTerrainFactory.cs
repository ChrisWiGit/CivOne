using System;
using CivOne.Enums;
using CivOne.Tiles;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Runtime implementation of <see cref="ITerrainFactory"/> that creates concrete
	/// <see cref="ITile"/> instances analogously to <c>Map.LoadSave.cs</c> and
	/// <c>Map.ChangeTileType</c>.
	/// </summary>
	public class RuntimeTerrainFactory : ITerrainFactory
	{
		public ITile CreateTile(Terrain terrain, int x, int y, bool special)
		{
			return terrain switch
			{
				Terrain.Forest     => new Forest(x, y, special),
				Terrain.Swamp      => new Swamp(x, y, special),
				Terrain.Plains     => new Plains(x, y, special),
				Terrain.Tundra     => new Tundra(x, y, special),
				Terrain.River      => new River(x, y),
				Terrain.Grassland1 => new Grassland(x, y),
				Terrain.Grassland2 => new Grassland(x, y),
				Terrain.Jungle     => new Jungle(x, y, special),
				Terrain.Hills      => new Hills(x, y, special),
				Terrain.Mountains  => new Mountains(x, y, special),
				Terrain.Desert     => new Desert(x, y, special),
				Terrain.Arctic     => new Arctic(x, y, special),
				Terrain.Ocean      => new Ocean(x, y, special),
				_ => throw new ArgumentOutOfRangeException(nameof(terrain), terrain, $"Unknown terrain type: {terrain}"),
			};
		}
	}
}
