// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Services;
using Xunit;

namespace CivOne.UnitTests
{
	public class GameCalendarServiceTests
	{
		private readonly GameCalendarService _testee;

		public GameCalendarServiceTests()
		{
			_testee = new GameCalendarService();
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
		public void TurnToYear_ReturnsExpectedYear(ushort turn, int expectedYear)
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
		public void YearToTurn_ReturnsExpectedTurn(int year, ushort expectedTurn)
		{
			var actual = _testee.YearToTurn(year);

			Assert.Equal(expectedTurn, actual);
		}

		[Theory]
		[InlineData(0, "4000 BC")]
		[InlineData(199, "20 BC")]
		[InlineData(200, "1 AD")]
		[InlineData(400, "1850 AD")]
		public void FormatYear_WithIdentityTranslation_ReturnsExpectedString(ushort turn, string expected)
		{
			var actual = _testee.FormatYear(turn);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData(0, "BC")]
		[InlineData(199, "BC")]
		[InlineData(200, "AD")]
		[InlineData(400, "AD")]
		public void FormatEra_WithIdentityTranslation_ReturnsExpectedEra(ushort turn, string expectedEra)
		{
			var actual = _testee.FormatEra(turn);

			Assert.Equal(expectedEra, actual);
		}

		[Fact]
		public void TurnToYear_AndYearToTurn_AreConsistentRoundTrip()
		{
			ushort expectedTurn = 250;

			var year = _testee.TurnToYear(expectedTurn);
			var actual = _testee.YearToTurn(year);

			Assert.Equal(expectedTurn, actual);
		}
	}
}
