using System.Diagnostics.CodeAnalysis;

namespace CivOne.Services.Calendar
{
	/// <summary>
	/// The year steps of the original game:
	/// +20 before 1000 AD, +10 before 1500, +5 before 1750, +2 before 1850, then +1.
	/// While a spaceship is in flight, the 2-year phase also advances by 1, so every year of a flight is visited.
	/// </summary>
	internal sealed class OriginalCalendarStepRuleDelegate : ICalendarStepRule
	{
		/// <inheritdoc />
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public int YearStep(int currentYear, bool spaceshipInFlight)
		{
			if (currentYear < 1000)
			{
				return 20;
			}

			if (currentYear < 1500)
			{
				return 10;
			}

			if (currentYear < 1750)
			{
				return 5;
			}

			if (currentYear < 1850 && !spaceshipInFlight)
			{
				return 2;
			}

			return 1;
		}
	}
}
