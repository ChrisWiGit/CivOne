using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Services.Pathfinding
{
	internal interface IUnitGotoService
	{
		/// <summary>
		/// Returns the next tile to move into on the path towards unit.Goto,
		/// or null if the goal is already reached or no path exists.
		/// </summary>
		ITile? GotoStep(IUnit unit);
	}
}
