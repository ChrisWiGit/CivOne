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
	/// This implementation is the refactored version of <see cref="UnitGotoServiceImpl"/> with improved readability and maintainability, while preserving the same pathfinding logic and behaviour. The original implementation is kept for reference and to allow for comparison and testing of the new implementation.
	/// 
	/// </summary>
	/// <param name="_mapTiles">Required map tiles service to access tile information for pathfinding.</param>
	/// <param name="_riverFastMovement">Configuration flag to determine if river tiles should be treated as roads for movement cost calculation.</param>
	internal sealed class UnitGotoService2(IMapTiles _mapTiles, bool _riverFastMovement) : IUnitGotoService
	{
		private sealed record AStarState(
			Dictionary<int, int> GCostMap,
			Dictionary<int, int> CameFromMap,
			List<(int fScore, int position)> OpenSet
		);

		// A* pathfinder for GoTo orders. Returns the next tile to move into, or null if unreachable.
		// Cost units: railroad=1, road=3, terrain=Movement*9 (max 18 for hills/forest).
		public ITile? GotoStep(IUnit unit)
		{
			int goalX = unit.GotoDestination.X, goalY = unit.GotoDestination.Y;
			int startX = unit.X, startY = unit.Y;

			bool isAlreadyAtGoal = startX == goalX && startY == goalY;
			if (isAlreadyAtGoal)
			{
				return null;
			}

			int mapWidth = _mapTiles.Width, mapHeight = _mapTiles.Height;

			var state = new AStarState(
				GCostMap: [],
				CameFromMap: [],
				OpenSet: []
			);

			int startPosition = EncodeCoordinatesToPosition(startX, startY, mapWidth);
			state.GCostMap[startPosition] = 0;
			state.OpenSet.Add((DistanceToTile(startX, startY, goalX, goalY), startPosition));

			HashSet<int> closedPositions = [];
			int maxClosedNodes = mapWidth * mapHeight;
			while (state.OpenSet.Count > 0 && closedPositions.Count < maxClosedNodes)
			{
				int nextNodeIndex = FindNextOpenNodeWithLowestFScore(state.OpenSet);
				int currentPosition = state.OpenSet[nextNodeIndex].position;
				state.OpenSet.RemoveAt(nextNodeIndex);

				if (!closedPositions.Add(currentPosition))
				{
					continue;
				}

				int currentX = currentPosition % mapWidth, currentY = currentPosition / mapWidth;
				bool isCurrentPositionGoal = currentX == goalX && currentY == goalY;
				if (isCurrentPositionGoal)
				{
					return ReconstructPath(currentPosition, startPosition, state.CameFromMap, mapWidth);
				}

				ExpandNeighbors(unit, currentX, currentY, goalX, goalY, currentPosition, state);
			}

			return null;
		}

		private static int FindNextOpenNodeWithLowestFScore(List<(int fScore, int position)> openSet)
		{
			int nodeIndexWithLowestFScore = 0;
			for (int i = 1; i < openSet.Count; i++)
			{
				if (openSet[i].fScore < openSet[nodeIndexWithLowestFScore].fScore)
				{
					nodeIndexWithLowestFScore = i;
				}
			}
			return nodeIndexWithLowestFScore;
		}

		private ITile ReconstructPath(int currentPosition, int startPosition, Dictionary<int, int> cameFromMap, int mapWidth)
		{
			int curPos = currentPosition;
			int previousPosition = cameFromMap.TryGetValue(curPos, out int p) ? p : startPosition;
			while (previousPosition != startPosition)
			{
				curPos = previousPosition;
				previousPosition = cameFromMap[curPos];
			}
			return _mapTiles[curPos % mapWidth, curPos / mapWidth];
		}

		private void ExpandNeighbors(
			IUnit unit,
			int currentX, int currentY,
			int goalX, int goalY,
			int currentPosition,
			AStarState state)
		{
			for (int deltaY = -1; deltaY <= 1; deltaY++)
			{
				for (int deltaX = -1; deltaX <= 1; deltaX++)
				{
					bool isOriginCell = deltaX == 0 && deltaY == 0;
					if (isOriginCell)
					{
						continue;
					}

					int neighborX = (currentX + deltaX + _mapTiles.Width) % _mapTiles.Width;
					int neighborY = currentY + deltaY;
					bool isNeighborOutOfBounds = neighborY < 0 || neighborY >= _mapTiles.Height;
					if (isNeighborOutOfBounds)
					{
						continue;
					}

					ITile neighbor = _mapTiles[neighborX, neighborY];
					bool isNeighborGoal = neighborX == goalX && neighborY == goalY;
					bool canUnitPassTile = IsUnitCanPassTile(unit, neighbor, isNeighborGoal);
					if (!canUnitPassTile)
					{
						continue;
					}

					TryRelaxNeighborCost(neighborX, neighborY, goalX, goalY, currentPosition, neighbor, state);
				}
			}
		}

		private void TryRelaxNeighborCost(
			int neighborX, int neighborY,
			int goalX, int goalY,
			int currentPosition,
			ITile neighbor,
			AStarState state)
		{
			int movementCost = CalculateMovementCost(neighbor);
			int neighborPosition = EncodeCoordinatesToPosition(neighborX, neighborY, _mapTiles.Width);
			int tentativeGCost = state.GCostMap[currentPosition] + movementCost;

			bool isNeighborNotVisited = !state.GCostMap.TryGetValue(neighborPosition, out int existingGCost);
			bool isNewPathCheaper = tentativeGCost < existingGCost;
			if (isNeighborNotVisited || isNewPathCheaper)
			{
				state.GCostMap[neighborPosition] = tentativeGCost;
				state.CameFromMap[neighborPosition] = currentPosition;
				int fScore = tentativeGCost + DistanceToTile(neighborX, neighborY, goalX, goalY);
				state.OpenSet.Add((fScore, neighborPosition));
			}
		}

		private static bool IsUnitCanPassTile(IUnit unit, ITile tile, bool isGoalTile)
		{
			if (unit.UnitCategory == UnitClass.Water)
			{
				return tile.IsOcean || isGoalTile;
			}
			return !tile.IsOcean && tile.Type != Terrain.Arctic;
		}

		private int CalculateMovementCost(ITile tile)
		{
			if (tile.RailRoad)
			{
				return 1;
			}

			if (tile.Road ||
				(_riverFastMovement && tile.Type == Terrain.River))
			{
				return 3;
			}

			return tile.Movement * 9;
		}

		private static int EncodeCoordinatesToPosition(int x, int y, int mapWidth)
			=> y * mapWidth + x;

		private int DistanceToTile(int x1, int y1, int x2, int y2)
			=> Math.Max(Math.Min(Math.Abs(x2 - x1), _mapTiles.Width - Math.Abs(x2 - x1)), Math.Abs(y2 - y1));
	}
}
