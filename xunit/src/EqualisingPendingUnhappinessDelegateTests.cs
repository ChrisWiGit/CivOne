using CivOne.Screens.Services;
using Xunit;

namespace CivOne.UnitTests
{
    /// <summary>
    /// Pins the refill rule on its own, because the three cases it distinguishes are hard to reach through a
    /// whole city calculation. The rule balances the two sides without changing their sum, so how much of a
    /// city improvement survives depends entirely on how much unhappiness is parked.
    /// </summary>
    public class EqualisingPendingUnhappinessDelegateTests
    {
        private readonly PendingUnhappinessRefill _refill = new EqualisingPendingUnhappinessDelegate().Refill;

        [Theory]
        // visible after the improvement, parked, expected visible, expected parked
        [InlineData(7, 4, 7, 4)]      // little parked: the improvement works in full, nothing flows back
        [InlineData(7, 9, 8, 8)]      // in between: two of three points survive
        [InlineData(7, 15, 11, 11)]   // much parked: the improvement is absorbed, only the surplus shrinks
        [InlineData(0, 0, 0, 0)]
        [InlineData(5, 5, 5, 5)]      // already level, nothing moves
        public void TheTwoSidesAreBalancedWithoutChangingTheirSum(
            int unhappy,
            int pending,
            int expectedUnhappy,
            int expectedPending)
        {
            int sumBefore = unhappy + pending;

            _refill(ref unhappy, ref pending);

            Assert.Equal(expectedUnhappy, unhappy);
            Assert.Equal(expectedPending, pending);
            Assert.Equal(sumBefore, unhappy + pending);
        }

        [Fact]
        public void TheRefillDoesNotStopAtAnyCitySize()
        {
            // The original clamps the visible unhappiness to the city size only after this step. Stopping
            // here instead would leave one unit parked that the original has already spent, which shows up
            // several stages later as unhappiness that never goes away.
            int unhappy = 7;
            int pending = 15;

            _refill(ref unhappy, ref pending);

            Assert.Equal(11, unhappy);      // past a city size of ten
            Assert.Equal(11, pending);      // not twelve, which is where a size bound would stop
        }
    }
}
