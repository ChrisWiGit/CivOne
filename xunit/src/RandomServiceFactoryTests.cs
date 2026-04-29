using CivOne.Services.Random;
using Xunit;

namespace CivOne.UnitTests
{
	public class RandomServiceFactoryTests
	{
		[Fact]
		public void Create_WhenServiceWasOverridden_ReturnsSameCachedInstance()
		{
			// Arrange
			IRandomService expected = new StubRandomService();
			RandomServiceFactory.Override(expected);

			// Act
			IRandomService actual = RandomServiceFactory.Create();

			// Assert
			Assert.Same(expected, actual);
		}

		[Fact]
		public void Override_WhenCalledTwice_ReplacesCachedInstance()
		{
			// Arrange
			IRandomService oldService = new StubRandomService();
			IRandomService expected = new StubRandomService();
			RandomServiceFactory.Override(oldService);

			// Act
			RandomServiceFactory.Override(expected);
			IRandomService actual = RandomServiceFactory.Create();

			// Assert
			Assert.Same(expected, actual);
			Assert.NotSame(oldService, actual);
		}

		[Fact]
		public void Next_WhenCalled_ReturnsValueFromWrappedRandomInstance()
		{
			// Arrange
			var random = new Random(1234);
			var testee = new CommonRandomService(() => random);

			// Act
			int expected = random.Next(0, 100);
			random = new Random(1234);
			testee = new CommonRandomService(() => random);
			int actual = testee.Next(0, 100);

			// Assert
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void Hit_WhenPercentIsZero_ReturnsFalse()
		{
			// Arrange
			var random = new Random(23905);
			var testee = new CommonRandomService(() => random);

			// Act
			bool actual = testee.Hit(0);

			// Assert
			Assert.False(actual);
		}

		private sealed class StubRandomService : IRandomService
		{
			public int Next(int max) => 0;

			public int Next(int min, int max) => min;

			public bool Hit(int percent) => percent > 0;
		}
	}
}
