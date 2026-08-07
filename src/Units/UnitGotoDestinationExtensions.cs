using System.Drawing;

namespace CivOne.Units
{
	/// <summary>
	/// Provides helpers to read and reset a unit's goto destination state.
	/// </summary>
	/// <remarks>
	/// We intentionally do not use <see cref="Point.Empty"/> as "no goto destination".
	/// <see cref="Point.Empty"/> equals (0,0), and (0,0) is a valid map coordinate.
	/// Using a dedicated sentinel keeps "no destination" distinct from a real target tile.
	/// </remarks>
	internal static class UnitGotoDestinationExtensions
	{
		private static readonly Point NoGotoDestination = new(-1, -1);

		/// <summary>
		/// Determines whether the unit currently has a valid goto destination.
		/// </summary>
		/// <param name="unit">The unit whose goto destination state is checked.</param>
		/// <returns>
		/// <see langword="true"/> when both goto coordinates are non-negative.
		/// Otherwise, <see langword="false"/>.
		/// </returns>
		public static bool HasGotoDestination(this IUnit unit)
		{
			return unit.GotoDestination.X >= 0 && unit.GotoDestination.Y >= 0;
		}

		/// <summary>
		/// Clears the unit's goto destination by assigning the dedicated "no destination" sentinel.
		/// </summary>
		/// <param name="unit">The unit whose goto destination should be cleared.</param>
		public static void ClearGotoDestination(this IUnit unit)
		{
			unit.GotoDestination = NoGotoDestination;
		}
	}
}