using System;
using CivOne.Units;

namespace CivOne.Services.Pathfinding
{
	internal sealed class PathfinderFactory : IPathfinderFactory
	{
		private readonly Func<bool> _isComputerPlayerPathfindingEnabled;
		private readonly IPathfinder _smartPathfinder;
		private readonly IPathfinder _disabledPathfinder;

		public PathfinderFactory(
			Func<bool> isComputerPlayerPathfindingEnabled = null,
			IPathfinder smartPathfinder = null,
			IPathfinder disabledPathfinder = null)
		{
			_isComputerPlayerPathfindingEnabled = isComputerPlayerPathfindingEnabled ?? (() => Settings.Instance.ComputerPlayerPathFinding);
			_smartPathfinder = smartPathfinder ?? new AStarPathfinderAdapter();
			_disabledPathfinder = disabledPathfinder ?? new DisabledPathfinder();
		}

		public IPathfinder CreateFor(IUnit unit)
		{
			if (_isComputerPlayerPathfindingEnabled())
			{
				return _smartPathfinder;
			}

			return _disabledPathfinder;
		}
	}
}