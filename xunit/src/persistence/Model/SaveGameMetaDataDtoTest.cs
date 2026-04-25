using System;
using Xunit;

namespace CivOne.Persistence.Model
{
	public class SaveGameMetaDataDtoTest
	{
		private readonly SaveGameMetaDataDto _testee;

		public SaveGameMetaDataDtoTest()
		{
			_testee = new SaveGameMetaDataDto();
		}

		[Fact]
		public void GetCreatedAtOr_WhenGameStartedAtIsValid_ReturnsParsedUtcValue()
		{
			// Arrange
			var expected = new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.FromHours(2)).ToUniversalTime();
			var fallback = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

			_testee.GameStartedAt = "2026-04-01T09:30:00+02:00";

			// Act
			var actual = _testee.GetCreatedAtOr(fallback);

			// Assert
			Assert.Equal(expected, actual);
		}
	}
}