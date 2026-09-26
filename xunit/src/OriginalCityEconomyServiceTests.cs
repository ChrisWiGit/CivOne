using Xunit;

namespace CivOne.UnitTests
{
    /// <summary>
    /// Covers the one thing <see cref="OriginalCityEconomyService"/> changes: the original adds the
    /// specialists before the marketplace and the bank raise the result, so a specialist is worth more in a
    /// city that has those buildings.
    /// The shipped order is pinned by <see cref="CityEconomyServiceImplTests"/> and stays untouched.
    /// </summary>
    public class OriginalCityEconomyServiceTests
    {
        // The methods under test are pure calculations, so no City/IGame setup is needed.
        private readonly OriginalCityEconomyService _original = new(null!, null!);
        private readonly CityEconomyServiceImpl _shipped = new(null!, null!);

        [Theory]
        // trade, entertainer luxuries, marketplace, bank, expected
        [InlineData(4, 3, false, false, 7)]     // without buildings both orders agree
        [InlineData(4, 3, true, false, 10)]     // (4 + 3) + 3 — the shipped order gives 6 + 3 = 9
        [InlineData(4, 3, true, true, 15)]      // 7 -> 10 -> 15, the shipped order gives 12
        [InlineData(0, 2, true, true, 4)]       // 2 -> 3 -> 4, entertainers alone still get raised
        public void EntertainersAreRaisedByTheMarketplaceAndTheBank(
            short tradeLuxuries,
            int entertainerLuxuries,
            bool hasMarketPlace,
            bool hasBank,
            short expected)
        {
            Assert.Equal(
                expected,
                _original.CalculateLuxuries(tradeLuxuries, hasMarketPlace, hasBank, entertainerLuxuries));
        }

        [Theory]
        // trade, taxmen, marketplace, bank, expected
        [InlineData(4, 2, false, false, 8)]     // without buildings both orders agree
        [InlineData(4, 2, true, false, 12)]     // (4 + 4) + 4 — the shipped order gives 6 + 4 = 10
        [InlineData(4, 2, true, true, 18)]      // 8 -> 12 -> 18, the shipped order gives 13
        public void TaxmenAreRaisedByTheMarketplaceAndTheBank(
            short tradeTaxes,
            int taxmen,
            bool hasMarketPlace,
            bool hasBank,
            short expected)
        {
            Assert.Equal(
                expected,
                _original.CalculateTaxes(tradeTaxes, hasMarketPlace, hasBank, taxmen));
        }

        [Fact]
        public void WithoutSpecialistsBothServicesAgree()
        {
            Assert.Equal(
                _shipped.CalculateLuxuries(8, hasMarketPlace: true, hasBank: true, entertainerLuxuries: 0),
                _original.CalculateLuxuries(8, hasMarketPlace: true, hasBank: true, entertainerLuxuries: 0));

            Assert.Equal(
                _shipped.CalculateTaxes(8, hasMarketPlace: true, hasBank: true, taxmen: 0),
                _original.CalculateTaxes(8, hasMarketPlace: true, hasBank: true, taxmen: 0));
        }

        [Fact]
        public void EntertainerPointsAreUnraisedSoTheMarketplaceIsNotCountedTwice()
        {
            // CivOne's City.EntertainerLuxuries is Entertainers * 3, the marketplace bonus already folded in.
            // The original counts two points per entertainer and raises them with the buildings, so this
            // service must start from two. Three would give 4.5 points per entertainer with a marketplace.
            Assert.Equal(
                6,
                _original.CalculateLuxuries(0, hasMarketPlace: true, hasBank: false, entertainerLuxuries: 4));
        }

        [Fact]
        public void TheShippedOrderIsUnchanged()
        {
            // The two tests that pin the shipped behaviour, repeated here so that a change to
            // CityEconomyServiceImpl shows up as a difference between the two services rather than silently.
            Assert.Equal(12, _shipped.CalculateLuxuries(4, hasMarketPlace: true, hasBank: true, entertainerLuxuries: 3));
            Assert.Equal(13, _shipped.CalculateTaxes(4, hasMarketPlace: true, hasBank: true, taxmen: 2));
        }
    }
}
