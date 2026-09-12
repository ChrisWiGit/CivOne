using System.Diagnostics.CodeAnalysis;

namespace CivOne.Screens.Services
{
	/// <summary>
	/// Refills the visible unhappiness until the parked surplus is empty.
	/// This is the alternative reading of the original: an improvement has no visible effect at all as long
	/// as anything is parked outside the city.
	/// </summary>
	internal sealed class FullDrainPendingUnhappinessDelegate
	{
		/// <summary>
		/// Gets the refill behaviour of this class.
		/// </summary>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The behaviour is offered as an instance member so the class can be exchanged for another refill behaviour.")]
		public PendingUnhappinessRefill Refill => RefillCore;

		private static void RefillCore(ref int unhappy, ref int pending, int maxUnhappy)
		{
			while (pending > 0 && unhappy < maxUnhappy)
			{
				pending--;
				unhappy++;
			}
		}
	}
}
