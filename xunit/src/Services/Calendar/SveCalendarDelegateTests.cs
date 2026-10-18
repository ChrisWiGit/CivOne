using System.Linq;
using CivOne.Services;
using CivOne.Services.Calendar;
using Xunit;

namespace CivOne.UnitTests.Services.Calendar
{
	public class SveCalendarDelegateTests
	{
		private readonly SveCalendarDelegate _testee = new();

		[Theory]
		[InlineData(100, -2000)]
		[InlineData(375, 1800)]
		[InlineData(450, 1900)]
		public void PlainYearNeedsNoHistory(ushort turn, int year)
		{
			Assert.Empty(_testee.Rebuild(turn, year));
		}

		[Fact]
		public void ShipInFlightBefore1850UsesTheMostRecentTurns()
		{
			// Turn 380 is 1810 with the plain formula; 1805 means five 1-year steps.
			ushort[] turns = [.. _testee.Rebuild(380, 1805)];

			Assert.Equal([376, 377, 378, 379, 380], turns);
			Assert.Equal(1805, YearOf(380, turns));
		}

		[Theory]
		[InlineData(410, 1855)]
		[InlineData(410, 1856)]
		[InlineData(500, 1940)]
		public void After1850TheCurrentYearIsExact(ushort turn, int year)
		{
			ushort[] turns = [.. _testee.Rebuild(turn, year)];

			Assert.NotEmpty(turns);
			Assert.Equal(year, YearOf(turn, turns));
		}

		private static int YearOf(ushort turn, ushort[] acceleratedTurns)
		{
			CalendarState state = new();
			state.Restore(acceleratedTurns.AsEnumerable());
			return GameCalendarServiceFactory.Create(state).TurnToYear(turn);
		}
	}
}
