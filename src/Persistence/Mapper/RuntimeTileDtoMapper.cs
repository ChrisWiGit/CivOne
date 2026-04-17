using System;
using CivOne.Enums;
using CivOne.Persistence.Factories;
using CivOne.Tiles;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Runtime implementation of <see cref="ITileDtoMapper"/> that writes tile data
	/// directly into <see cref="Map.Instance"/> during YAML load.
	/// Tile types are created via <see cref="ITerrainFactory"/> analogously to
	/// <c>Map.LoadSave.cs</c> and <c>Map.ChangeTileType</c>, using
	/// <see cref="Map.TileIsSpecial"/> for the special-resource flag (requires
	/// <c>_terrainMasterWord</c> to be set first via
	/// <see cref="Map.InitializeForYamlLoad"/>).
	/// </summary>
	public class RuntimeTileDtoMapper(Map _map, ITerrainFactory _terrainFactory) : ITileDtoMapper
	{

		public void SetTileFromDto(TileDto dto, int x, int y)
		{
			// dto.Special=true means explicitly set in YAML → honour it directly.
			// dto.Special=false means not stored (old saves) → derive from map seed for backwards compatibility.
			bool special = dto.Special || _map.TileIsSpecialInternal(x, y);

			ITile tile = _terrainFactory.CreateTile(dto.Terrain, x, y, special);

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
				Special    = domain.Special,
				LandValue  = domain.LandValue,
			};
		}

		public ITile FromDto(TileDto dto)
		{
			throw new NotSupportedException(
				$"{nameof(RuntimeTileDtoMapper)}.{nameof(FromDto)} is not supported. Use {nameof(SetTileFromDto)} instead.");
		}
	}
}

