using CivOne.Enums;
using CivOne.Tiles;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Runtime implementation of <see cref="ITileDtoMapper"/> that writes tile data
	/// directly into <see cref="Map.Instance"/> during YAML load.
	/// Tile types are created analogously to <c>Map.LoadSave.cs</c> and
	/// <c>Map.ChangeTileType</c>, using <see cref="Map.TileIsSpecial"/> for the
	/// special-resource flag (requires <c>_terrainMasterWord</c> to be set first via
	/// <see cref="Map.InitializeForYamlLoad"/>).
	/// </summary>
	public class RuntimeTileDtoMapper(Map _map) : ITileDtoMapper
	{

		public void SetTileFromDto(TileDto dto, int x, int y)
		{
			bool special = _map.TileIsSpecialInternal(x, y);

			ITile tile = dto.Terrain switch
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
				_                  => new Ocean(x, y, special),
			};

			tile.Road       = dto.Road;
			tile.RailRoad   = dto.RailRoad;
			tile.Irrigation = dto.Irrigation;
			tile.Mine       = dto.Mine;
			tile.Fortress   = dto.Fortress;
			tile.Pollution  = dto.Pollution;
			tile.Hut        = dto.Hut;
			tile.LandValue  = dto.LandValue;

			_map.SetTileInternal(x, y, tile);
		}

		public TileDto ToDto(ITile domain)
		{
			return new TileDto
			{
				Terrain    = domain.Type,
				Road       = domain.Road,
				RailRoad   = domain.RailRoad,
				Irrigation = domain.Irrigation,
				Pollution  = domain.Pollution,
				Fortress   = domain.Fortress,
				Mine       = domain.Mine,
				Hut        = domain.Hut,
				LandValue  = domain.LandValue,
			};
		}

		public ITile FromDto(TileDto dto)
		{
			throw new System.NotSupportedException(
				$"{nameof(RuntimeTileDtoMapper)}.{nameof(FromDto)} is not supported. Use {nameof(SetTileFromDto)} instead.");
		}
	}
}
