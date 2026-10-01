using System.Diagnostics.CodeAnalysis;

namespace CivOne.Screens.Services
{
	/// <summary>
	/// Balances the visible unhappiness against the surplus parked outside the city.
	/// The original does not hand the parked unhappiness back in one go, and it does not ignore it either.
	/// It moves one unit at a time from the surplus into the visible unhappiness until the visible side is
	/// no longer the smaller of the two, which keeps the sum of both untouched.
	///
	/// The effect on a city improvement depends on how much is parked. With little parked, the improvement
	/// works in full. With a lot parked, it is absorbed entirely and only shrinks the surplus. In between it
	/// works in part, which is where the odd-looking numbers come from: every pass closes the gap by two,
	/// because it takes from one side and gives to the other.
	/// </summary>
	internal sealed class EqualisingPendingUnhappinessDelegate
	{
		/// <summary>
		/// Gets the refill behaviour of this class.
		/// </summary>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The behaviour is offered as an instance member so the class can be exchanged for another refill behaviour.")]
		public PendingUnhappinessRefill Refill => RefillCore;

		private static void RefillCore(ref int unhappy, ref int pending)
		{
			while (pending > 0 && unhappy < pending)
			{
				pending--;
				unhappy++;
			}
		}
	}
}
