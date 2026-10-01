namespace CivOne.Services
{
	/// <summary>
	/// Creates the service that writes a savegame while the game shuts down.
	/// </summary>
	public interface IQuitAutoSaveServiceFactory
	{
		/// <summary>
		/// Creates the quit autosave service.
		/// </summary>
		/// <param name="runtime">Runtime used for save paths and logging.</param>
		/// <param name="settings">Settings providing the save directories.</param>
		/// <returns>The service writing the savegame on quit.</returns>
		IQuitAutoSaveService Create(IRuntime runtime, ISettings settings);
	}
}
