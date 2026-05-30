// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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
