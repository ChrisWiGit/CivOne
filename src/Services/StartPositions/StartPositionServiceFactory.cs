namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Creates the <see cref="IStartPositionService"/> implementation selected by <see cref="Settings.StartPositionAlgorithm"/>.
	/// </summary>
	internal static class StartPositionServiceFactory
	{
		/// <summary>
		/// Creates the service for the algorithm currently configured in the settings.
		/// </summary>
		/// <returns>The starting-position service to use.</returns>
		public static IStartPositionService Create() => Create(Settings.Instance.StartPositionAlgorithm);

		/// <summary>
		/// Creates the service for a specific algorithm, without reading the settings singleton.
		/// </summary>
		/// <param name="algorithm">The algorithm to create a service for.</param>
		/// <returns>The starting-position service for that algorithm.</returns>
		public static IStartPositionService Create(Settings.StartPositionAlgorithmType algorithm) => algorithm switch
		{
			Settings.StartPositionAlgorithmType.AreaBased => new AreaBasedStartPositionService(),
			_ => new LegacyStartPositionService(),
		};
	}
}