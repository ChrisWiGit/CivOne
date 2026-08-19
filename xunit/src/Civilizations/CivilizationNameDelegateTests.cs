using CivOne.Civilizations;
using Xunit;

namespace CivOne.UnitTests.Civilizations
{
	/// <summary>
	/// Covers the display names for players that share a civilization.
	/// </summary>
	public class CivilizationNameDelegateTests
	{
		[Theory]
		[InlineData(0, "")]
		[InlineData(1, "I")]
		[InlineData(2, "II")]
		[InlineData(4, "IV")]
		[InlineData(9, "IX")]
		[InlineData(14, "XIV")]
		[InlineData(40, "XL")]
		public void ConvertsNumbersToRomanNumerals(int number, string expected)
		{
			Assert.Equal(expected, new CivilizationNameDelegate().ToRomanNumeral(number));
		}

		[Fact]
		public void TheFirstUserOfACivilizationKeepsItsOwnNames()
		{
			MockedICivilization civilization = new(1, 1);

			CivilizationNames names = new CivilizationNameDelegate().Build(civilization, 0);

			Assert.Null(names.LeaderName);
			Assert.Null(names.TribeName);
			Assert.Null(names.TribeNamePlural);
		}

		[Fact]
		public void RepeatUsersGetANumberedName()
		{
			MockedICivilization civilization = new(1, 1);
			civilization.Leader.Name = "Caesar";
			civilization.Name = "Roman";
			civilization.NamePlural = "Romans";

			CivilizationNames names = new CivilizationNameDelegate().Build(civilization, 1);

			Assert.Equal("Caesar II", names.LeaderName);
			Assert.Equal("Roman II", names.TribeName);
			Assert.Equal("Romans II", names.TribeNamePlural);
		}

		[Fact]
		public void TheNumberKeepsCountingForFurtherUsers()
		{
			MockedICivilization civilization = new(1, 1);
			civilization.Leader.Name = "Caesar";

			CivilizationNames names = new CivilizationNameDelegate().Build(civilization, 2);

			Assert.Equal("Caesar III", names.LeaderName);
		}
	}
}
