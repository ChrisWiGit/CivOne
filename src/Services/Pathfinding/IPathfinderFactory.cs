using CivOne.Units;

namespace CivOne.Services.Pathfinding
{
	internal interface IPathfinderFactory
	{
		IPathfinder CreateFor(IUnit unit);

		static IPathfinderFactory Create() => new PathfinderFactory();
	}
}