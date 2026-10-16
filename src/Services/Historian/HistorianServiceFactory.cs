namespace CivOne.Services.Historian
{
	/// <summary>
	/// Creates the service behind the Historian's palace pictures.
	/// </summary>
	internal static class HistorianServiceFactory
	{
		/// <summary>
		/// Creates the service that picks the palace shown per player.
		/// </summary>
		/// <returns>A new <see cref="IHistorianPalaceService"/> for the running game.</returns>
		public static IHistorianPalaceService CreatePalaceService() => new HistorianPalaceService();
	}
}
