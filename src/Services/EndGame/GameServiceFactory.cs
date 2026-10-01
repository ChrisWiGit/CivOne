namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Factory for creating game services.
	/// </summary>
	/// <remarks>
	/// Centralizes construction so service implementations can be swapped without touching callers.
	/// </remarks>
	internal static class GameServiceFactory
	{
		/// <summary>
		/// Creates a default game service.
		/// </summary>
		/// <returns>A service implementing IGameService.</returns>
		public static IGameService CreateDefault() => new GameServiceImpl();
	}
}