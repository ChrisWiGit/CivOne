using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Services.GlobalWarming.Impl
{
	// oben in delegate definieren
	public delegate void RemoveUnit(IUnit unit);

	/// <summary>
	/// Global Warming Scourge Service with modified flooding behavior: chance to turn land into ocean
	/// </summary>
	/// <param name="globalWarmingService"></param>
	/// <param name="mapTiles"></param>
	/// <param name="tileChangeRequestCallback"></param>
	/// <param name="MapWidth"></param>
	/// <param name="MapHeight"></param>
	public class GlobalWarmingScourgeWithFloodServiceImpl(
		IGlobalWarmingService globalWarmingService,
		ITile[,] mapTiles,
		TileChangeRequestCallback tileChangeRequestCallback,
		RemoveUnit removeUnit,
		int MapWidth,
		int MapHeight,
		Settings.GlobalWarmingFeatureFlag featureFlags
			) : GlobalWarmingScourgeServiceImpl(globalWarmingService, mapTiles, tileChangeRequestCallback, MapWidth, MapHeight)
	{
		private readonly TileChangeRequestCallback tileChangeRequestCallback = tileChangeRequestCallback;
		private readonly RemoveUnit removeUnit = removeUnit;
		private readonly int MapHeight = MapHeight;

		protected override void ProcessTerrain(int totalWarmings, ITile tile)
		{
			if (featureFlags.HasFlag(Settings.GlobalWarmingFeatureFlag.SeaLevelRise))
			{
				if (RiseSeaLevelOnRiverTile(tile, totalWarmings)) return;
			}

			base.ProcessTerrain(totalWarmings, tile);
		}

		private bool IsPoles(ITile tile)
		{
			return tile.Y < 3 || tile.Y > MapHeight - 3;
		}

		private bool RiseSeaLevelOnRiverTile(ITile tile, int totalWarmings)
		{
			bool isArctic = (tile.Type is Terrain.Arctic or Terrain.Tundra || IsPoles(tile)) &&
				Common.Random.Hit(Math.Min(totalWarmings * 20, 100));
			bool Otherwise = !isArctic &&
				Common.Random.Hit(Math.Min(totalWarmings * 10, 100));

			if (isArctic || Otherwise)
			{
				RemoveUnits(tile);
				RemoveImprovements(tile);
				tileChangeRequestCallback(tile, Terrain.Ocean);

				return true;
			}
			return false;
		}

		private static void RemoveImprovements(ITile tile)
		{
			tile.Road = false;
			tile.RailRoad = false;
			tile.Mine = false;
			tile.Fortress = false;
			tile.Irrigation = false;
		}

		private void RemoveUnits(ITile tile)
		{
			foreach (IUnit u in tile.Units.OfType<BaseUnitLand>())
			{
				removeUnit(u);
			}
		}

		protected override bool IsAffectedTerrain(ITile terrain)
		{
			if (terrain.HasCity) return false;

			return base.IsAffectedTerrain(terrain)
					|| IsPoles(terrain)
					|| terrain.Type == Terrain.Jungle
					|| terrain.Type == Terrain.Swamp
					|| terrain.Type == Terrain.Tundra
					|| terrain.Type == Terrain.Arctic;
		}
	}
}