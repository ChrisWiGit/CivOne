using System.Collections.Generic;

namespace CivOne.Services.Sorting
{
	/// <summary>
	/// Compares strings using natural ("numeric-aware") ordering, so that embedded numbers
	/// sort by their numeric value instead of lexicographically.
	/// </summary>
	/// <example>
	/// With natural ordering, "map2" sorts before "map10".
	/// A plain ordinal sort would place "map10" before "map2" because '1' precedes '2'.
	/// </example>
	public interface INaturalSortService : IComparer<string>
	{
	}
}
