using System;

namespace CivOne.Services.HallOfFame
{
	/// <summary>
	/// Represents one persisted Hall of Fame result row.
	/// </summary>
	/// <param name="LeaderName">
	/// Human leader name used in the run.
	/// </param>
	/// <param name="LeaderTitle">
	/// Difficulty title (for example Chief, King, Deity).
	/// </param>
	/// <param name="CivilizationNamePlural">
	/// Plural civilization name (for example Romans).
	/// </param>
	/// <param name="YearLabel">
	/// Formatted game year label.
	/// </param>
	/// <param name="Population">
	/// Final population value.
	/// </param>
	/// <param name="Score">
	/// Final score value.
	/// </param>
	/// <param name="RatingRankLabel">
	/// Historical personality label selected from leader rating thresholds.
	/// This value is independent from difficulty title.
	/// </param>
	/// <param name="RatingPercent">
	/// Computed civilization rating percentage.
	/// </param>
	/// <param name="CreatedAtUtc">
	/// UTC creation timestamp used for sorting and tie-breaking.
	/// </param>
	internal sealed record HallOfFameEntry(
		string LeaderName,
		string LeaderTitle,
		string CivilizationNamePlural,
		string YearLabel,
		int Population,
		int Score,
		string RatingRankLabel,
		int RatingPercent,
		DateTimeOffset CreatedAtUtc);
}
