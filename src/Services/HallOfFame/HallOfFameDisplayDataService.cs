using System.Collections.Generic;

namespace CivOne.Services.HallOfFame
{
	internal sealed record HallOfFameDisplayRow(string Headline, string Details, string Rating, bool IsPlaceholder);

	internal sealed class HallOfFameDisplayDataService : IHallOfFameDisplayDataService
	{
		public IReadOnlyList<HallOfFameDisplayRow> BuildRows(IReadOnlyList<HallOfFameEntry> entries, int maxRows)
		{
			List<HallOfFameDisplayRow> rows = [];
			for (int i = 0; i < maxRows; i++)
			{
				int rank = i + 1;
				if (i < entries.Count)
				{
					rows.Add(BuildEntryRow(entries[i], rank));
					continue;
				}

				rows.Add(BuildPlaceholderRow());
			}

			return rows;
		}

		public HallOfFameDisplayRow BuildEntryRow(HallOfFameEntry entry, int rank)
		{
			return new HallOfFameDisplayRow(
				Headline: $"{rank}. {entry.LeaderName}, {entry.LeaderTitle} of the {entry.CivilizationNamePlural} to {entry.YearLabel}.",
				Details: $"Population: {entry.Population:N0}, Score: {entry.Score:N0} ({entry.RatingRankLabel})",
				Rating: $"--- CIVILIZATION RATING: {entry.RatingPercent}% ---",
				IsPlaceholder: false);
		}

		public HallOfFameDisplayRow BuildPlaceholderRow()
		{
			return new HallOfFameDisplayRow(
				Headline: "---",
				Details: string.Empty,
				Rating: string.Empty,
				IsPlaceholder: true);
		}
	}
}
