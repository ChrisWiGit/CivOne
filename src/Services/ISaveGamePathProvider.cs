namespace CivOne.Services
{
    /// <summary>
    /// Provides methods to determine and manage file paths for saving game data, including tracking the last used save game path and providing an initial file path for save dialogs.
    /// This abstraction allows for flexibility in how save game paths are determined and stored, and can be implemented to use different storage mechanisms (e.g., user profile, configuration files, etc.) without affecting the rest of the application.
    /// Implementations of this interface should ensure that the directory for save games exists when providing paths.
    /// </summary>
    public interface ISaveGamePathProvider
    {
        /// <summary>
        /// Returns the directory for .cos savegames, ensuring it exists.
        /// If the directory does not exist, it will be created. The returned path is guaranteed to exist after this method is called.
        /// </summary>
        string EnsureCurrentSaveDirectory();

        /// <summary>
        /// Returns the initial file path for the save dialog.
        /// </summary>
        string GetInitialSaveFilePath();

        /// <summary>
        /// Returns the last used savegame path, or null if not set.
        /// </summary>
        string GetLastUsedSaveGamePath();

        /// <summary>
        /// Persists the last used savegame path in the profile.
        /// </summary>
        void SetLastUsedSaveGamePath(string path);
    }
}
