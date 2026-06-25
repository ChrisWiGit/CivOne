//NOSONAR
using System;
using System.Collections.Generic;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Services.Pathfinding
{
	/// <summary>
	/// A* pathfinding implementation for unit GoTo orders. Returns the next tile to move into, or null if unreachable.
	/// Cost units: railroad=1, road=3, terrain=Movement*9 (max 18 for hills/forest). 
	/// Optionally, river tiles can be treated as roads (cost 3) if the "River Fast Movement" setting is enabled.
	/// 
	/// This version is the original implementation of the A* pathfinding from 
	/// <a href="https://github.com/mwerneburg/CivOne/commit/eec2410b583cd3c119cd3889fecc579bcffa4374">mwerneburg/CivOne</a>, 
	/// kept for reference and to allow for comparison and testing of the new refactored implementation in <see cref="UnitGotoService2"/>. 
	/// The new implementation is available in the same factory and can be switched by changing the factory method to return the desired implementation.
	/// 
	/// A refactored version of this implementation with improved readability and maintainability is available in <see cref="UnitGotoService2"/>, while preserving the same pathfinding logic and behaviour.
	/// </summary>
	/// <param name="_mapTiles">Required map tiles service to access tile information for pathfinding.</param>
	internal sealed class UnitGotoServiceImpl(IMapTiles _mapTiles) : IUnitGotoService
	{
		// A* pathfinder for GoTo orders. Returns the next tile to move into, or null if unreachable.
		// Cost units: railroad=1, road=3, terrain=Movement*9 (max 18 for hills/forest).
		public ITile? GotoStep(IUnit unit)
		{
			int gx = unit.GotoDestination.X, gy = unit.GotoDestination.Y;
			int sx = unit.X, sy = unit.Y;
			if (sx == gx && sy == gy) return null;

			int w = _mapTiles.Width, h = _mapTiles.Height;

			var gScore = new Dictionary<int, int>();
			var cameFrom = new Dictionary<int, int>();
			var open = new List<(int f, int pos)>();

			int Encode(int x, int y) => y * w + x;
			int startPos = Encode(sx, sy);
			gScore[startPos] = 0;
			open.Add((DistanceToTile(sx, sy, gx, gy), startPos));

			int maxIterations = w * h;
			while (open.Count > 0 && maxIterations-- > 0)
			{
				// Pop node with lowest f (linear scan — fine for Civ map sizes)
				int minIdx = 0;
				for (int i = 1; i < open.Count; i++)
					if (open[i].f < open[minIdx].f) minIdx = i;
				int curPos = open[minIdx].pos;
				open.RemoveAt(minIdx);

				int cx = curPos % w, cy = curPos / w;
				if (cx == gx && cy == gy)
				{
					// Reconstruct path and return the first step
					int cur = curPos;
					int prev = cameFrom.TryGetValue(cur, out int p) ? p : startPos;
					while (prev != startPos)
					{
						cur = prev;
						prev = cameFrom[cur];
					}
					return _mapTiles[cur % w, cur / w];
				}

				for (int dy = -1; dy <= 1; dy++)
				{
					for (int dx = -1; dx <= 1; dx++)
					{
						if (dx == 0 && dy == 0) continue;
						int nx = (cx + dx + w) % w;
						int ny = cy + dy;
						if (ny < 0 || ny >= h) continue;

						ITile neighbor = _mapTiles[nx, ny];
						bool isGoal = nx == gx && ny == gy;

						// Determine passability based on unit class
						bool passable;
						if (unit.UnitCategory == UnitClass.Water)
							passable = neighbor.IsOcean || isGoal;
						else
							passable = !neighbor.IsOcean && neighbor.Type != Terrain.Arctic;

						if (!passable) continue;

						// Cost: railroad=1, road=3, terrain=Movement*9
						int cost;
						if (neighbor.RailRoad) cost = 1;
						else if (neighbor.Road) cost = 3;
						else cost = neighbor.Movement * 9;

						int neighborPos = Encode(nx, ny);
						int tentativeG = gScore[curPos] + cost;

						if (!gScore.TryGetValue(neighborPos, out int existingG) || tentativeG < existingG)
						{
							gScore[neighborPos] = tentativeG;
							cameFrom[neighborPos] = curPos;
							int fScore = tentativeG + DistanceToTile(nx, ny, gx, gy);
							open.Add((fScore, neighborPos));
						}
					}
				}
			}

			return null;
		}

		private int DistanceToTile(int x1, int y1, int x2, int y2)
			=> Math.Max(Math.Min(Math.Abs(x2 - x1), _mapTiles.Width - Math.Abs(x2 - x1)), Math.Abs(y2 - y1));
	}
}
