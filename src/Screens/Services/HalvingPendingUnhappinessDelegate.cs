using System.Diagnostics.CodeAnalysis;

namespace CivOne.Screens.Services
{
	/// <summary>
	/// Refills the visible unhappiness until it has caught up with the parked surplus.
	/// This is the reading the decompilation of the original states literally: roughly half of an
	/// improvement survives, the other half is absorbed by the surplus.
	/// </summary>
	internal sealed class HalvingPendingUnhappinessDelegate
	{
		/// <summary>
		/// Gets the refill behaviour of this class.
		/// </summary>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The behaviour is offered as an instance member so the class can be exchanged for another refill behaviour.")]
		public PendingUnhappinessRefill Refill => RefillCore;

		private static void RefillCore(ref int unhappy, ref int pending, int maxUnhappy)
		{
			while (pending > 0 && unhappy < pending && unhappy < maxUnhappy)
			{
				pending--;
				unhappy++;
			}
		}
	}
}
