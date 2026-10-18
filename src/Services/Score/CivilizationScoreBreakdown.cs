namespace CivOne.Services.Score
{
	/// <summary>
	/// The civilization score with its parts, as shown on the score screen.
	/// </summary>
	/// <param name="CitizenScore">Sum of <c>Size + Happy - (Unhappy + RedShirt)</c> over all cities.</param>
	/// <param name="WonderScore">20 points per wonder held.</param>
	/// <param name="SpaceShipValue">
	/// <c>SuccessRate * Population / 2</c> with the population in units of 10,000, never negative.
	/// Shown as "possible" while the ship has not arrived.
	/// </param>
	/// <param name="SpaceShipApplied">Whether <paramref name="SpaceShipValue"/> is part of the score, i.e. the ship arrives this turn.</param>
	/// <param name="PollutionScore">-10 points per polluted square; zero or negative.</param>
	/// <param name="PeaceScore"><c>min(3 * PeaceTurns, 100)</c>, only in AD years.</param>
	/// <param name="FutureTechScore">5 points per future technology.</param>
	/// <param name="Total">The final score, or the world-conquest bonus when it replaced the score.</param>
	/// <param name="IsBonus">Whether <paramref name="Total"/> is the world-conquest bonus ("Bonus Score").</param>
	public sealed record CivilizationScoreBreakdown(
		int CitizenScore,
		int WonderScore,
		int SpaceShipValue,
		bool SpaceShipApplied,
		int PollutionScore,
		int PeaceScore,
		int FutureTechScore,
		int Total,
		bool IsBonus);
}
