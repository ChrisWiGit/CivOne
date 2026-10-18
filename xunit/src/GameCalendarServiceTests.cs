using System.Linq;
using CivOne.Services;
using CivOne.Services.Calendar;
using Xunit;

namespace CivOne.UnitTests
{
	public class GameCalendarServiceTests
	{
		private readonly CalendarState _state = new();
		private readonly GameCalendarService _testee;

		public GameCalendarServiceTests()
		{
			_testee = new GameCalendarService(calendarState: _state);
		}

		[Theory]
		[InlineData(0, -4000)]
		[InlineData(1, -3980)]
		[InlineData(199, -20)]
		[InlineData(200, 1)]
		[InlineData(201, 20)]
		[InlineData(250, 1000)]
		[InlineData(300, 1500)]
		[InlineData(350, 1750)]
		[InlineData(400, 1850)]
		[InlineData(500, 1950)]
		public void TurnToYearReturnsExpectedYear(ushort turn, int expectedYear)
		{
			var actual = _testee.TurnToYear(turn);

			Assert.Equal(expectedYear, actual);
		}

		[Theory]
		[InlineData(-5000, (ushort)0)]   // below min clamps to 0
		[InlineData(-4000, (ushort)0)]
		[InlineData(-3980, (ushort)1)]
		[InlineData(-20, (ushort)199)]
		[InlineData(1, (ushort)200)]
		[InlineData(1000, (ushort)250)]
		[InlineData(1500, (ushort)300)]
		[InlineData(1750, (ushort)350)]
		[InlineData(1850, (ushort)400)]
		[InlineData(1950, (ushort)500)]
		public void YearToTurnReturnsExpectedTurn(int year, ushort expectedTurn)
		{
			var actual = _testee.YearToTurn(year);

			Assert.Equal(expectedTurn, actual);
		}

		[Theory]
		[InlineData(0, "4000 BC")]
		[InlineData(199, "20 BC")]
		[InlineData(200, "1 AD")]
		[InlineData(400, "1850 AD")]
		public void FormatYearWithIdentityTranslationReturnsExpectedString(ushort turn, string expected)
		{
			var actual = _testee.FormatYear(turn);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData(-4000, "4000 BC")]
		[InlineData(1, "1 AD")]
		[InlineData(1813, "1813 AD")]
		public void FormatCalendarYearShowsTheYearItself(int year, string expected)
		{
			// 1813 belongs to no turn without recorded 1-year steps; a stored arrival year still shows as itself.
			Assert.Equal(expected, _testee.FormatCalendarYear(year));
		}

		[Theory]
		[InlineData(0, "BC")]
		[InlineData(199, "BC")]
		[InlineData(200, "AD")]
		[InlineData(400, "AD")]
		public void FormatEraWithIdentityTranslationReturnsExpectedEra(ushort turn, string expectedEra)
		{
			var actual = _testee.FormatEra(turn);

			Assert.Equal(expectedEra, actual);
		}

		[Fact]
		public void TurnToYearAndYearToTurnAreConsistentRoundTrip()
		{
			ushort expectedTurn = 250;

			var year = _testee.TurnToYear(expectedTurn);
			var actual = _testee.YearToTurn(year);

			Assert.Equal(expectedTurn, actual);
		}

		[Fact]
		public void AcceleratedTurnsShiftEveryLaterYear()
		{
			GameCalendarService plain = new(calendarState: new CalendarState());
			_state.Restore([360, 361, 362, 363, 364, 365]);

			for (ushort turn = 0; turn < 360; turn++)
			{
				Assert.Equal(plain.TurnToYear(turn), _testee.TurnToYear(turn));
			}

			// Six 1-year steps instead of 2-year steps: 6 years less until 1850 is reached ...
			Assert.Equal(plain.TurnToYear(380) - 6, _testee.TurnToYear(380));
			// ... and 1850 comes 3 turns later, from where every turn is 1 year.
			Assert.Equal(1850, _testee.TurnToYear(403));
			Assert.Equal(1950, _testee.TurnToYear(503));
		}

		[Fact]
		public void YearToTurnIsTheInverseWithAcceleratedTurns()
		{
			_state.Restore(Enumerable.Range(370, 20).Select(t => (ushort)t));

			for (ushort turn = 0; turn < 600; turn++)
			{
				int year = _testee.TurnToYear(turn);
				Assert.Equal(turn, _testee.YearToTurn(year));
			}
		}

		[Fact]
		public void AdvanceTurnMarksOnlyTheTwoYearPhaseWhileAShipIsInFlight()
		{
			_testee.AdvanceTurn(340, spaceshipInFlight: true);
			_testee.AdvanceTurn(351, spaceshipInFlight: false);
			_testee.AdvanceTurn(352, spaceshipInFlight: true);
			_testee.AdvanceTurn(353, spaceshipInFlight: true);
			_testee.AdvanceTurn(450, spaceshipInFlight: true);

			Assert.Equal([352, 353], _state.AcceleratedTurns);
			Assert.Equal(1753, _testee.TurnToYear(352));
			Assert.Equal(1754, _testee.TurnToYear(353));
		}

		[Fact]
		public void WithAShipInFlightEveryYearUntil1850IsVisited()
		{
			for (ushort turn = 351; turn < 500; turn++)
			{
				_testee.AdvanceTurn(turn, spaceshipInFlight: true);
			}

			int[] years = [.. Enumerable.Range(350, 101).Select(t => _testee.TurnToYear((ushort)t))];
			Assert.Equal(Enumerable.Range(1750, 101), years);
		}

		[Theory]
		[InlineData(-4000, false, 20)]
		[InlineData(999, true, 20)]
		[InlineData(1000, false, 10)]
		[InlineData(1500, false, 5)]
		[InlineData(1750, false, 2)]
		[InlineData(1849, false, 2)]
		[InlineData(1750, true, 1)]
		[InlineData(1850, false, 1)]
		[InlineData(2000, true, 1)]
		public void OriginalStepRuleMatchesTheOriginalTable(int year, bool inFlight, int expectedStep)
		{
			Assert.Equal(expectedStep, new OriginalCalendarStepRuleDelegate().YearStep(year, inFlight));
		}
	}
}