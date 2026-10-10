using CivOne.Graphics;
using Xunit;

namespace CivOne.UnitTests.Graphics
{
	/// <summary>
	/// Verifies that <see cref="ResourceGenerationDelegate"/> reports each resource reload exactly once.
	/// </summary>
	public sealed class ResourceGenerationDelegateTests
	{
		[Fact]
		public void HasChangedIsFalseOnFirstCall()
		{
			ResourceGenerationDelegate testee = new(() => 3);

			Assert.False(testee.HasChanged());
		}

		[Fact]
		public void HasChangedIsFalseWhileGenerationStaysTheSame()
		{
			ResourceGenerationDelegate testee = new(() => 3);
			testee.HasChanged();

			Assert.False(testee.HasChanged());
		}

		[Fact]
		public void HasChangedReportsANewGenerationOnce()
		{
			int generation = 3;
			ResourceGenerationDelegate testee = new(() => generation);
			testee.HasChanged();

			generation++;

			Assert.True(testee.HasChanged());
			Assert.False(testee.HasChanged());
		}
	}
}
