using CivOne.Enums;
using CivOne.Tiles;

namespace CivOne.Persistence.Factories
{
/// <summary>
/// Factory for creating <see cref="ITile"/> instances from terrain type and coordinates.
/// </summary>
public interface ITerrainFactory
{
ITile CreateTile(Terrain terrain, int x, int y, bool special);
}
}
