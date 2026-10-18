using CivOne.Services.Maps;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Locks down the constructor seam that lets tests supply a stub
	/// <see cref="IMapGenerationSettings"/> instead of reading from the
	/// global <see cref="Settings"/> singleton.
	/// </summary>
	public class MapGenerationSettingsInjectionTests : src.TestsBase
	{
		[Fact]
		public void ConstructorAcceptsInjectedGenerationSettings()
		{
			IMapGenerationSettings testee = new StubGenerationSettings();

			Map actual = new(null, null, testee);

			Assert.NotNull(actual);
		}

		[Fact]
		public void ConstructorFallsBackToDefaultWhenNoSettingsProvided()
		{
			Map actual = new(null, null, null);

			Assert.NotNull(actual);
		}

		private sealed class StubGenerationSettings : IMapGenerationSettings { }
	}
}