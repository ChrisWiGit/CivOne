using System.Collections.Generic;

namespace CivOne.Services.HallOfFame
{
	internal interface IHallOfFameDisplayDataService
	{
		IReadOnlyList<HallOfFameDisplayRow> BuildRows(IReadOnlyList<HallOfFameEntry> entries, int maxRows);

		HallOfFameDisplayRow BuildEntryRow(HallOfFameEntry entry, int rank);

		HallOfFameDisplayRow BuildPlaceholderRow();
	}
}
