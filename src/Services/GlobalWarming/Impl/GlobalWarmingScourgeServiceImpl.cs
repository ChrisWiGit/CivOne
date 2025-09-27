using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;

namespace CivOne.Services.GlobalWarming.Impl
{
	public delegate void TileChangeRequestCallback(ITile tile, Terrain newTerrainType);

	public class GlobalWarmingScourgeServiceImpl(
		IGlobalWarmingService globalWarmingService,
		ITile[,] mapTiles,
		TileChangeRequestCallback tileChangeRequestCallback,
		int MapWidth,
		int MapHeight
		) : IGlobalWarmingScourgeService
	{
		public void UnleashScourgeOfPollution()
		{
			int totalWarmings = globalWarmingService.GlobalWarmingCount;

			foreach (ITile tile in mapTiles)
			{
				RemovePollutionFromTile(tile);

				if (!IsAffectedTerrain(tile)) continue;

				ProcessTerrain(totalWarmings, tile);
			}
		}

		protected virtual void RemovePollutionFromTile(ITile tile)
		{
			tile.Pollution = false;
		}

		protected virtual void ProcessTerrain(int totalWarmings, ITile tile)
		{
			int adjacentOcean = GetOceanAdjacentCount(tile);

			// CW: Fix to original: limit to 0..7
			if (adjacentOcean >= Math.Max(7 - totalWarmings, 0))
			{
				FloodTile(tile);
			}
			else
			{
				DryOutTile(tile, totalWarmings);
			}
		}

		protected int GetOceanAdjacentCount(ITile tile)
		{
			return Neighbours(tile).Count(n => n.Type == Terrain.Ocean);
		}

		protected void FloodTile(ITile tile)
		{
			if (tile.Type == Terrain.Forest)
			{
				tileChangeRequestCallback(tile, Terrain.Jungle);
			}
			else
			{
				tileChangeRequestCallback(tile, Terrain.Swamp);
			}

			tile.Irrigation = false;
			tile.Mine = false;
		}

		protected void DryOutTile(ITile tile, int totalWarmings)
		{
			int mesh = (11 * tile.X + 13 * tile.Y) & 7;
			// if remainder of division (11*x+13*y) by 8 is equal to global warming counter, (i.e. if ((11*x+13*y)&7 == total_warmings))
			if (mesh != totalWarmings)
			{
				return;
			}
			if (tile.Type is Terrain.Desert or Terrain.Plains)
			{
				tileChangeRequestCallback(tile, Terrain.Desert);
			}
			else
			{
				tileChangeRequestCallback(tile, Terrain.Plains);
			}

			// CW: Bugfix of original algorithm: Irrigation is removed on drying out, too.
			tile.Irrigation = false;
		}

		// count number of adjacent ocean squares (including diagonals)
		protected ITile[] Neighbours(ITile tile)
		{
			List<ITile> neighbours = [];

			for (int relY = -1; relY <= 1; relY++)
				for (int relX = -1; relX <= 1; relX++)
				{
					if (relX == 0 && relY == 0) continue;
					int checkX = tile.X + relX;
					int checkY = tile.Y + relY;

					//CW: possibly not in original game, but still: map wraps horizontally
					if (checkX < 0) checkX = MapWidth + checkX;
					else if (checkX >= MapWidth) checkX %= MapWidth;

					if (checkY < 0 || checkY >= MapHeight) continue;

					neighbours.Add(mapTiles[checkX, checkY]);
				}
			return [.. neighbours];
		}

		protected virtual bool IsAffectedTerrain(ITile terrain)
		{
			if (terrain.HasCity) return false;

			return terrain.Type == Terrain.Desert
				|| terrain.Type == Terrain.River // CW: Rivers also dry out
				|| terrain.Type == Terrain.Plains
				|| terrain.Type == Terrain.Grassland1
				|| terrain.Type == Terrain.Grassland2
				|| terrain.Type == Terrain.Forest;
		}
	}
}