using System;
using System.Drawing;
using System.Linq;
using CivOne.Enums;
using CivOne.Services.Random;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Services.Pathfinding
{
	internal sealed class SmartAiGotoExecutor : IAiGotoExecutor
	{
		private readonly IPathfinderFactory _pathfinderFactory;
		private readonly IRandomService _randomService;

		public SmartAiGotoExecutor(IPathfinderFactory pathfinderFactory, IRandomService randomService)
		{
			_pathfinderFactory = pathfinderFactory ?? throw new ArgumentNullException(nameof(pathfinderFactory));
			_randomService = randomService ?? throw new ArgumentNullException(nameof(randomService));
		}

		public AiGotoExecutionResult TryExecute(IUnit unit)
		{
			if (!CanHandle(unit))
			{
				return AiGotoExecutionResult.NotHandled;
			}

			PathStepResult pathStep = GetPathStep(unit);
			if (pathStep.Status == PathStepStatus.Disabled)
			{
				return AiGotoExecutionResult.NotHandled;
			}

			if (!pathStep.HasStep)
			{
				return ResetGotoAndContinue(unit);
			}

			ITile? nextTile = ResolveNextTile(unit, pathStep);
			if (nextTile == null)
			{
				return ResetGotoAndContinue(unit);
			}

			if (ShouldCancelAttack(unit, nextTile))
			{
				return ResetGotoAndContinue(unit);
			}

			return TryMoveToNextStep(unit, pathStep);
		}

		private PathStepResult GetPathStep(IUnit unit)
		{
			IPathfinder pathfinder = _pathfinderFactory.CreateFor(unit);
			return pathfinder.GetNextStep(unit, unit.Goto);
		}

		private static ITile? ResolveNextTile(IUnit unit, PathStepResult pathStep) =>
			unit.MoveTargets.FirstOrDefault(x => x.X == pathStep.NextX && x.Y == pathStep.NextY);

		private static bool CanHandle(IUnit unit) => unit != null && !unit.Goto.IsEmpty;

		private bool ShouldCancelAttack(IUnit unit, ITile nextTile)
		{
			if (!nextTile.Units.Any(x => x.Owner != unit.Owner))
			{
				return false;
			}

			if (unit.Role == UnitRole.Civilian || unit.Role == UnitRole.Settler || unit is Carrier)
			{
				return true;
			}

			if (unit.Role == UnitRole.Transport && _randomService.Hit(67))
			{
				return true;
			}

			if (unit.Attack < nextTile.Units.Max(x => x.Defense) && _randomService.Hit(50))
			{
				return true;
			}

			return false;
		}

		private AiGotoExecutionResult TryMoveToNextStep(IUnit unit, PathStepResult pathStep)
		{
			if (unit.MoveTo(pathStep.NextX - unit.X, pathStep.NextY - unit.Y))
			{
				return AiGotoExecutionResult.TurnComplete;
			}

			return HandleMoveFailure(unit);
		}

		private AiGotoExecutionResult HandleMoveFailure(IUnit unit)
		{
			if (_randomService.Hit(67))
			{
				return ResetGotoAndContinue(unit);
			}

			if (_randomService.Hit(67))
			{
				unit.SkipTurn();
				return AiGotoExecutionResult.TurnComplete;
			}

			Game.Instance.DisbandUnit(unit);
			return AiGotoExecutionResult.TurnComplete;
		}

		private static AiGotoExecutionResult ResetGotoAndContinue(IUnit unit)
		{
			unit.Goto = Point.Empty;
			return AiGotoExecutionResult.Continue;
		}
	}
}
