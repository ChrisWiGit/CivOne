namespace CivOne.Screens.Services
{
	/// <summary>
	/// Moves unhappiness that did not fit into the city back into the city.
	/// The original game parks the surplus outside the city and refills from it whenever a modifier lowers
	/// the visible unhappiness again, so an improvement is partly absorbed before it becomes visible.
	/// </summary>
	/// <param name="unhappy">The visible unhappy citizen count. Raised by the refill.</param>
	/// <param name="pending">The parked surplus. Lowered by the refill.</param>
	/// <param name="maxUnhappy">
	/// The highest unhappy count the city can show. Refilling past it would throw the surplus away at the
	/// clamp that follows, which would make the parked unhappiness vanish instead of being felt later.
	/// </param>
	internal delegate void PendingUnhappinessRefill(ref int unhappy, ref int pending, int maxUnhappy);
}
