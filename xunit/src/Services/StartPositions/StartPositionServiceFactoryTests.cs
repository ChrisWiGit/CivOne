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