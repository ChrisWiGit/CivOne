namespace CivOne.Screens.Services
{
	/// <summary>
	/// Moves unhappiness that did not fit into the city back into the city.
	/// The original game parks the surplus outside the city and refills from it whenever a modifier lowers
	/// the visible unhappiness again, so an improvement is partly absorbed before it becomes visible.
	/// The refill must not stop at the city size on its own. The original clamps only afterwards, and a
	/// refill that stopped early would leave a unit of unhappiness parked that the original has already
	/// spent.
	/// </summary>
	/// <param name="unhappy">The visible unhappy citizen count. Raised by the refill.</param>
	/// <param name="pending">The parked surplus. Lowered by the refill.</param>
	internal delegate void PendingUnhappinessRefill(ref int unhappy, ref int pending);
}
