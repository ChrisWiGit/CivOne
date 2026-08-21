using System;
using System.Drawing;
using CivOne.Units;

namespace CivOne.Services.Pathfinding
{
	internal sealed class AStarPathfinderAdapter : IPathfinder
	{
		public PathStepResult GetNextStep(IUnit unit, Point destination)
		{
			if (unit == null)
			{
				return PathStepResult.InvalidRequest();
			}

			// Keep in sync with Game.Update() legacy pathfinding branch.
			// (0,0) is a valid map coordinate, not "no destination" (see UnitGotoDestinationExtensions),
			// so it must reach AStar rather than being rejected here.
			// X is wrapped to the map width because the map wraps east-west and callers (e.g. AI.cs)
			// may set an unnormalized GotoDestination.X; AStar.Neighbors() wraps X internally too,
			// so an unwrapped goal would never match a wrapped current node and the path would never complete.
			int normalizedX = destination.X % Map.WIDTH;
			if (normalizedX < 0)
			{
				normalizedX += Map.WIDTH;
			}

			if (destination.Y < 0 || destination.Y >= Map.HEIGHT)
			{
				return PathStepResult.InvalidRequest();
			}

			AStar.SPosition goal = new()
			{
				iX = normalizedX,
				iY = destination.Y
			};

			AStar astar = new();
			AStar.SPosition nextPosition = astar.FindPath(goal, unit);

			if (nextPosition.iX < 0 || nextPosition.iY < 0)
			{
				return PathStepResult.NoPath();
			}
			if (nextPosition.iX == unit.X && nextPosition.iY == unit.Y)
			{	
				// explicitly return success if the unit is already at the destination.
				return PathStepResult.Success(unit.X, unit.Y);
			}

			return PathStepResult.Success(nextPosition.iX, nextPosition.iY);
		}
	}

	internal sealed class DisabledPathfinder : IPathfinder
	{
		public PathStepResult GetNextStep(IUnit unit, Point destination) => PathStepResult.Disabled();
	}
}