using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Verifies that <see cref="WindowTitleDelegate"/> replaces a stale default title but keeps a
	/// title the player chose.
	/// </summary>
	public class WindowTitleDelegateTests
	{
		[Fact]
		public void ResolveReplacesTheNameOfTheOlderVersion()
		{
			WindowTitleDelegate testee = new();

			Assert.Equal(ProductInfo.Name, testee.Resolve(ProductInfo.LegacyName));
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void ResolveReplacesAnEmptyTitle(string? storedTitle)
		{
			WindowTitleDelegate testee = new();

			Assert.Equal(ProductInfo.Name, testee.Resolve(storedTitle));
		}

		[Fact]
		public void ResolveKeepsATitleThePlayerChose()
		{
			WindowTitleDelegate testee = new();

			Assert.Equal("My own game", testee.Resolve("My own game"));
		}

		[Fact]
		public void ResolveKeepsTheCurrentName()
		{
			WindowTitleDelegate testee = new();

			Assert.Equal(ProductInfo.Name, testee.Resolve(ProductInfo.Name));
		}
	}
}
