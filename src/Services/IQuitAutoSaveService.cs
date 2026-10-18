namespace CivOne.Services
{
	/// <summary>
	/// Writes an automatic savegame while the game is shutting down.
	/// </summary>
	/// <remarks>
	/// This is separate from the turn based autosave.
	/// It only applies when a game was actually loaded or started, so quitting from the credits or
	/// the setup screens never produces a file.
	/// </remarks>
	public interface IQuitAutoSaveService
	{
		/// <summary>
		/// Saves the running game if there is one.
		/// </summary>
		/// <returns>
		/// The full path of the written savegame, or <c>null</c> when no game was running or the
		/// save failed.
		/// </returns>
		string? TrySaveOnQuit();
	}
}
