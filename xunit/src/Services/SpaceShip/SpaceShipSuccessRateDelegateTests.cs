using CivOne.Services.SpaceShip;
using Xunit;

namespace CivOne.UnitTests.Services.SpaceShip
{
	public class SpaceShipSuccessRateDelegateTests
	{
		private static readonly SpaceShipMissionStatus CompleteShip = new(1, 1, 1, 2, 2, 4, 100, 100, 100);

		[Fact]
		public void CompleteLaunchableShipReturns100()
		{
			Assert.Equal(100, new SpaceShipSuccessRateDelegate().Calculate(true, CompleteShip));
		}

		[Fact]
		public void CompleteShipThatCannotLaunchIsCappedAt99()
		{
			Assert.Equal(99, new SpaceShipSuccessRateDelegate().Calculate(false, CompleteShip));
		}

		[Fact]
		public void EmptyShipIsClampedToZero()
		{
			Assert.Equal(0, new SpaceShipSuccessRateDelegate().Calculate(true, default));
		}

		[Fact]
		public void MissingPartsAndPercentagesReduceRate()
		{
			// 100 - 45 (no command) - (100 - 70) / 3 - (100 - 40) / 4 = 100 - 45 - 10 - 15
			SpaceShipMissionStatus status = CompleteShip with { CommandCount = 0, Support = 70, FuelPercent = 40 };

			Assert.Equal(30, new SpaceShipSuccessRateDelegate().Calculate(true, status));
		}
	}
}
