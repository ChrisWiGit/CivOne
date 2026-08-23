using CivOne.Units;

namespace CivOne.Services.Pathfinding
{
	internal interface IAiGotoExecutorFactory
	{
		IAiGotoExecutor CreateFor(IUnit unit);

		static IAiGotoExecutorFactory Create() => new AiGotoExecutorFactory();
	}
}
