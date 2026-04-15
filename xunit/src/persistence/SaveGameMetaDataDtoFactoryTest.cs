using System;
using System.Globalization;
using System.Reflection;
using CivOne.Persistence.Factories;
using Xunit;

namespace CivOne.UnitTests.Persistence
{
	public class SaveGameMetaDataDtoFactoryTest
	{
		private readonly SaveGameMetaDataDtoFactory _testee;

		public SaveGameMetaDataDtoFactoryTest()
		{
			_testee = new SaveGameMetaDataDtoFactory();
		}

		[Fact]
		public void CreateFromRuntime_MapsMetaDataToDto()
		{
			// Arrange
			var gameStartedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));
			var metaData = new SaveFileMetaData
			{
				DisplayName = "Auto Save"
			};

			SetPrivateProperty(metaData, nameof(SaveFileMetaData.GameStartedAt), gameStartedAt);
			SetPrivateProperty(metaData, nameof(SaveFileMetaData.GameVersion), "1.2.3");
			SetPrivateProperty(metaData, nameof(SaveFileMetaData.PlayDuration), TimeSpan.FromMinutes(90));

			// Act
			var actual = _testee.CreateFromRuntime(metaData);

			// Assert
			Assert.Equal(gameStartedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture), actual.GameStartedAt);
			Assert.Equal("1.2.3", actual.GameVersion);
			Assert.Equal(90L, actual.PlayDurationMinutes);
			Assert.Equal("Auto Save", actual.DisplayName);
		}

		private static void SetPrivateProperty<T>(SaveFileMetaData target, string propertyName, T value)
		{
			var property = typeof(SaveFileMetaData).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			Assert.NotNull(property);
			property.SetValue(target, value);
		}
	}
}