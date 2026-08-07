using System.Drawing;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Services.Pathfinding
{
	internal interface IUnitGotoService
	{
		/// <summary>
		/// Calculates the full path for a unit to the specified destination.
		/// </summary>
		/// <param name="unit">The unit for which to calculate the path.</param>
		/// <param name="destination">The target destination tile coordinates.</param>
		/// <returns>
		/// A sequence of tiles starting with the first movement step and ending at the destination.
		/// Returns an empty array if no path exists or if the unit is already at the destination.
		/// </returns>
		ITile[] GetPath(IUnit unit, Point destination);

		/// <summary>
		/// Returns the next tile to move into on the path towards unit.Goto,
		/// or null if the goal is already reached or no path exists.
		/// </summary>
		ITile? GotoStep(IUnit unit);
	}
}
