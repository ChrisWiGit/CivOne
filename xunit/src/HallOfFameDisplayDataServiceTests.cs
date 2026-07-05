using System;
using CivOne.Services;
using CivOne.Services.HallOfFame;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Unit tests for <see cref="HallOfFameDisplayDataService"/> presentation logic.
	/// </summary>
	public sealed class HallOfFameDisplayDataServiceTests
	{
		private readonly HallOfFameDisplayDataService _testee;

		public HallOfFameDisplayDataServiceTests()
		{
			_testee = new HallOfFameDisplayDataService();
		}

		[Fact]
		public void BuildRowsWhenEntriesEmptyReturnsOnlyPlaceholderRows()
		{
			// Arrange
			
			// Act
			var actual = _testee.BuildRows([], maxRows: 5);

			// Assert
			Assert.Equal(5, actual.Count);
			Assert.All(actual, row => Assert.True(row.IsPlaceholder));
			Assert.All(actual, row => Assert.Equal("---", row.Headline));
			Assert.All(actual, row => Assert.Equal(string.Empty, row.Details));
			Assert.All(actual, row => Assert.Equal(string.Empty, row.Rating));
		}

		[Fact]
		public void BuildRowsWhenEntryProvidedFillsRemainingWithPlaceholders()
		{
			// Arrange
			HallOfFameEntry input = new(
				LeaderName: "OpenCivOne",
				LeaderTitle: "King",
				CivilizationNamePlural: "Germans",
				YearLabel: "3780 BC",
				Population: 360000,
				Score: 6,
				RatingRankLabel: "Winston Churchill",
				RatingPercent: 0,
				CreatedAtUtc: DateTimeOffset.UtcNow);

			// Act
			var actual = _testee.BuildRows([input], maxRows: 5);

			// Assert
			Assert.Equal(5, actual.Count);
			Assert.False(actual[0].IsPlaceholder);
			Assert.Contains("OpenCivOne", actual[0].Headline, StringComparison.Ordinal);
			Assert.Contains("(Winston Churchill)", actual[0].Details, StringComparison.Ordinal);
			Assert.Equal("--- CIVILIZATION RATING: 0% ---", actual[0].Rating);
			Assert.True(actual[1].IsPlaceholder);
		}
	}
}
