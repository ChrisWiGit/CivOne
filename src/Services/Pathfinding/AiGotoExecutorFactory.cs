using System;
using CivOne.Services.Random;
using CivOne.Units;

namespace CivOne.Services.Pathfinding
{
	internal sealed class AiGotoExecutorFactory : IAiGotoExecutorFactory
	{
		private readonly Func<bool> _isComputerPlayerPathfindingEnabled;
		private readonly IAiGotoExecutor _smartGotoExecutor;
		private readonly IAiGotoExecutor _noOpGotoExecutor;

		public AiGotoExecutorFactory(
			Func<bool> isComputerPlayerPathfindingEnabled = null,
			IAiGotoExecutor smartGotoExecutor = null,
			IAiGotoExecutor noOpGotoExecutor = null,
			IPathfinderFactory pathfinderFactory = null,
			IRandomService randomService = null)
		{
			_isComputerPlayerPathfindingEnabled = isComputerPlayerPathfindingEnabled ?? (() => Settings.Instance.ComputerPlayerPathFinding);
			IPathfinderFactory effectivePathfinderFactory = pathfinderFactory ?? IPathfinderFactory.Create();
			IRandomService effectiveRandomService = randomService ?? RandomServiceFactory.Create();
			_smartGotoExecutor = smartGotoExecutor ?? new SmartAiGotoExecutor(effectivePathfinderFactory, effectiveRandomService);
			_noOpGotoExecutor = noOpGotoExecutor ?? new NoOpAiGotoExecutor();
		}

		public IAiGotoExecutor CreateFor(IUnit unit)
		{
			if (_isComputerPlayerPathfindingEnabled())
			{
				return _smartGotoExecutor;
			}

			return _noOpGotoExecutor;
		}
	}
}
