namespace CivOne.Services.Screen
{
	/// <summary>
	/// Creates the <see cref="IPowerGraphSelectionService"/> used by the power graph.
	/// </summary>
	/// <remarks>
	/// The service outlives the power graph screen, because the selection has to survive closing and
	/// reopening the graph. It is therefore shared, and bound to a game through
	/// <see cref="IPowerGraphSelectionService.UseGame"/> rather than by being recreated.
	/// </remarks>
	internal static class PowerGraphSelectionServiceFactory
	{
		private static IPowerGraphSelectionService? _current;

		/// <summary>
		/// The service instance shared by every power graph screen.
		/// </summary>
		/// <returns>The shared selection service.</returns>
		public static IPowerGraphSelectionService Create() => _current ??= new PowerGraphSelectionService();
	}
}
