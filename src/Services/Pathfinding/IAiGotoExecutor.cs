using CivOne.Units;

namespace CivOne.Services.Pathfinding
{
	internal enum AiGotoExecutionResult
	{
		NotHandled,
		Continue,
		TurnComplete
	}

	internal interface IAiGotoExecutor
	{
		AiGotoExecutionResult TryExecute(IUnit unit);
	}
}
