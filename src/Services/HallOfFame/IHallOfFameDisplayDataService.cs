using System.Collections.Generic;

namespace CivOne.Services.HallOfFame
{
	/// <summary>
	/// Builds presentation rows used by the Hall of Fame screen from persisted entries.
	/// </summary>
	internal interface IHallOfFameDisplayDataService
	{
		/// <summary>
		/// Builds a list of display rows up to <paramref name="maxRows"/>.
		/// </summary>
		IReadOnlyList<HallOfFameDisplayRow> BuildRows(IReadOnlyList<HallOfFameEntry> entries, int maxRows);

		/// <summary>
		/// Builds a single display row for a given entry and rank.
		/// </summary>
		HallOfFameDisplayRow BuildEntryRow(HallOfFameEntry entry, int rank);

		/// <summary>
		/// Builds a placeholder row used when there are fewer entries than rows.
		/// </summary>
		HallOfFameDisplayRow BuildPlaceholderRow();
	}
}
