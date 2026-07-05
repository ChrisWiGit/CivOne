using System;
using System.Drawing;
using CivOne.Units;

namespace CivOne.Services.Pathfinding
{
	internal sealed class AStarPathfinderAdapter : IPathfinder
	{
		public PathStepResult GetNextStep(IUnit unit, Point destination)
		{
			if (unit == null || destination.IsEmpty)
			{
				return PathStepResult.InvalidRequest();
			}

			AStar.SPosition goal = new()
			{
				iX = destination.X,
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