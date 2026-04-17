using Xunit;

namespace CivOne.UnitTests
{
	public class CityEconomyServiceImplTests
	{
		// The public helper methods are pure calculations (value-in, value-out),
		// so no City/IGame setup is needed; null is safe here.
		private readonly CityEconomyServiceImpl _service = new(null, null);

		[Fact]
		public void CalculateTradeTaxes_TruncatesTowardsZero()
		{
			short taxes = _service.CalculateTradeTaxes(1, 5);
			Assert.Equal(0, taxes);
		}

		[Fact]
		public void CalculateTradeLuxuries_ReturnsZero_WhenTaxesRateIsMax()
		{
			short luxuries = _service.CalculateTradeLuxuries(20, 10, 10, 5);
			Assert.Equal(0, luxuries);
		}

		[Fact]
		public void CalculateTradeScience_DoesNotGoNegative()
		{
			short science = _service.CalculateTradeScience(3, 2, 3);
			Assert.Equal(0, science);
		}

		[Fact]
		public void CalculateLuxuries_AppliesBuildingAndEntertainerBonuses()
		{
			short luxuries = _service.CalculateLuxuries(4, hasMarketPlace: true, hasBank: true, entertainerLuxuries: 3);
			Assert.Equal(12, luxuries);
		}

		[Fact]
		public void CalculateTaxes_AppliesBuildingAndTaxmanBonuses()
		{
			short taxes = _service.CalculateTaxes(4, hasMarketPlace: true, hasBank: true, taxmen: 2);
			Assert.Equal(13, taxes);
		}

		[Fact]
		public void CalculateScience_AppliesScienceMultipliersInExpectedOrder()
		{
			short science = _service.CalculateScience(
			tradeScience: 10,
			hasLibrary: true,
			hasUniversity: true,
			hasSeti: false,
			hasNewton: true,
			hasCopernicus: true,
			scientists: 1);

			Assert.Equal(59, science);
		}

		[Fact]
		public void CalculateScience_CapsAtShortMaxValue()
		{
			short science = _service.CalculateScience(
			tradeScience: 30000,
			hasLibrary: true,
			hasUniversity: true,
			hasSeti: true,
			hasNewton: true,
			hasCopernicus: true,
			scientists: 1000);

			Assert.Equal(short.MaxValue, science);
		}
	}
}
