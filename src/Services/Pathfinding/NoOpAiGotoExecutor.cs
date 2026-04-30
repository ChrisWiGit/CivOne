using CivOne.Units;

namespace CivOne.Services.Pathfinding
{
	internal sealed class NoOpAiGotoExecutor : IAiGotoExecutor
	{
		public AiGotoExecutionResult TryExecute(IUnit unit) => AiGotoExecutionResult.NotHandled;
	}
}
