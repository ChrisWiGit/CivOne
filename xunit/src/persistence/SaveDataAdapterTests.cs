using CivOne.IO;
using CivOne.Persistence.Factories;
using CivOne.Persistence.Model;
using CivOne.src;
using System;
using System.Linq;
using Xunit;

namespace CivOne.UnitTests.Persistence
{
	public class SaveDataAdapterTests : TestsBase
	{
		[Fact]
		public void CivilizationIdentitySetterAndGetterRoundtripFlagsWithoutBitShiftRegression()
		{
			// Arrange
			using var _testee = new SaveDataAdapter();
			byte[] expected = [1, 0, 1, 1, 0, 1, 0, 1];

			// Act
			_testee.CivilizationIdentity = expected;
			using var actualAdapter = SaveDataAdapter.Load(_testee.GetBytes());
			byte[] actual = actualAdapter.CivilizationIdentity;

			// Assert
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void CitiesSetterPreservesVisibleSizeInBinarySaveData()
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

		[Fact]
		public void ReplayDataSetterThrowsWhenSerializedDataExceedsFixedBuffer()
		{
			// Each CivilizationDestroyed entry serializes to 4 bytes.
			// 1025 entries exceed the 4096-byte fixed replay buffer.
			var replayData = Enumerable.Range(0, 1025)
				.Select(i => new ReplayData.CivilizationDestroyed(i % 4000, 1, 2))
				.Cast<ReplayData>()
				.ToArray();

			using var testee = new SaveDataAdapter();

			var ex = Assert.Throws<InvalidOperationException>(() => testee.ReplayData = replayData);

			Assert.Contains("Replay data exceeds 4096 bytes", ex.Message);
		}

		[Fact]
		public void CitiesSetterClampsCoordinatesAboveByteMaxValueWhenCheckedSanitizerIsActive()
		{
			using var sanitizerScope = ValueSanitizerFactory.UseCheckedValueSanitizer(new ValueSanitizer(new NoOpLogger()));
			var unit = Game.Instance.GetUnits().First(x => x.Owner == playa.Civilization.Id);
			City? city = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);
			Assert.NotNull(city);

			city.X = 300;
			city.Y = 20;

			using var testee = new SaveDataAdapter();

			testee.Cities = [.. new[] { city }.GetCityData()];
			using var actualAdapter = SaveDataAdapter.Load(testee.GetBytes());
			var actual = Assert.Single(actualAdapter.Cities);

			Assert.Equal(byte.MaxValue, actual.X);
			Assert.Equal((byte)20, actual.Y);
		}
	}
}
