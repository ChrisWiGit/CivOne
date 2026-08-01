// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using Xunit;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Tests that <see cref="StartPositionServiceFactory"/> maps the configured algorithm to the right implementation.
	/// The algorithm is passed explicitly so the test does not touch the settings singleton.
	/// </summary>
	public class StartPositionServiceFactoryTests
	{
		[Fact]
		public void CreatesTheLegacyServiceForTheLegacyAlgorithm()
		{
			IStartPositionService service = StartPositionServiceFactory.Create(Settings.StartPositionAlgorithmType.Legacy);

			Assert.IsType<LegacyStartPositionService>(service);
		}

		[Fact]
		public void CreatesTheAreaBasedServiceForTheAreaBasedAlgorithm()
		{
			IStartPositionService service = StartPositionServiceFactory.Create(Settings.StartPositionAlgorithmType.AreaBased);

			Assert.IsType<AreaBasedStartPositionService>(service);
		}
	}
}
