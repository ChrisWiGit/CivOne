using CivOne.IO;
using CivOne.src;
using Xunit;

namespace CivOne.UnitTests.Persistence
{
	public class SaveDataAdapterTests : TestsBase
	{
		[Fact]
		public void Cities_Setter_PreservesVisibleSizeInBinarySaveData()
		{
			// Arrange
			using var _testee = new SaveDataAdapter();
			var expected = new CityData
			{
				Id = 0,
				NameId = 1,
				Status = 1,
				Buildings = [],
				X = 10,
				Y = 20,
				ActualSize = 7,
				VisibleSize = 3,
				CurrentProduction = 4,
				BaseTrade = 2,
				Owner = 1,
				Food = 12,
				Shields = 8,
				ResourceTiles = [0, 1, 2, 3, 4, 5],
				FortifiedUnits = [],
				TradingCities = []
			};

			// Act
			_testee.Cities = [expected];
			using var actualAdapter = SaveDataAdapter.Load(_testee.GetBytes());
			var actual = Assert.Single(actualAdapter.Cities);

			// Assert
			Assert.Equal(expected.ActualSize, actual.ActualSize);
			Assert.Equal(expected.VisibleSize, actual.VisibleSize);
			Assert.Equal(expected.BaseTrade, actual.BaseTrade);
		}
	}
}
