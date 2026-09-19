namespace CivOne.Services.Screen
{
	/// <summary>
	/// Factory for creating screen services.
	/// </summary>
	/// <remarks>
	/// Centralizes construction so service implementations can be swapped without touching callers.
	/// </remarks>
	internal static class ScreenServiceFactory
	{
		/// <summary>
		/// Creates a default screen service that combines query and command operations.
		/// </summary>
		/// <returns>A service implementing both IScreenQueryService and IScreenCommandService.</returns>
		public static IScreenCommandService CreateCommandService() => new ScreenServiceImpl();

		/// <summary>
		/// Creates a default screen query service.
		/// </summary>
		/// <returns>A service implementing IScreenQueryService.</returns>
		public static IScreenQueryService CreateQueryService() => new ScreenServiceImpl();
	}
}