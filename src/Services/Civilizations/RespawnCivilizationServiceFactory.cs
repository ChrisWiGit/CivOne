namespace CivOne.Services.Civilizations
{
	/// <summary>
	/// Creates the services around civilization assignment during a running game.
	/// </summary>
	internal static class RespawnCivilizationServiceFactory
	{
		/// <summary>
		/// Creates the service that picks the civilization for a respawning player slot.
		/// </summary>
		/// <returns>The respawn civilization service.</returns>
		public static IRespawnCivilizationService Create() => new RespawnCivilizationService();
	}
}
