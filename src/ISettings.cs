namespace CivOne
{
	public interface ISettings
	{
		/// <summary>
		/// Gets the CivOne storage root. This is the root folder where all CivOne data is stored
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOneX</c>
		/// Linux and macOS: <c>~/.local/share/CivOneX</c>
		/// </remarks>
		string StorageDirectory { get; }

		/// <summary>
		/// Gets the directory used for captured screenshots and recordings.
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOneX\capture</c>
		/// Linux and macOS: <c>~/.local/share/CivOneX/capture</c>
		/// </remarks>
		string CaptureDirectory { get; }

		/// <summary>
		/// Gets the directory that contains the game data files.
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOneX\data</c>
		/// Linux and macOS: <c>~/.local/share/CivOneX/data</c>
		/// </remarks>
		string DataDirectory { get; }

		/// <summary>
		/// Gets the directory used for plugins.
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOneX\plugins</c>
		/// Linux and macOS: <c>~/.local/share/CivOneX/plugins</c>
		/// </remarks>
		string PluginsDirectory { get; }

		/// <summary>
		/// Gets the directory used for savegames.
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOneX\saves</c>
		/// Linux and macOS: <c>~/.local/share/CivOneX/saves</c>
		/// </remarks>
		string SavesDirectory { get; }

		/// <summary>
		/// Gets the directory used for classic .cos savegames.
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOneX\saves\cos</c>
		/// Linux and macOS: <c>~/.local/share/CivOneX/saves/cos</c>
		/// </remarks>
		string CosSavesDirectory { get; }

		/// <summary>
		/// Gets the directory used for custom map files (<c>*.comap</c>, <c>*.map</c>).
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOneX\maps</c>
		/// Linux and macOS: <c>~/.local/share/CivOneX/maps</c>
		/// </remarks>
		string MapsDirectory { get; }

		/// <summary>
		/// Gets the directory used for exported map images.
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOneX\pictures</c>
		/// Linux and macOS: <c>~/.local/share/CivOneX/pictures</c>
		/// </remarks>
		string PicturesDirectory { get; }

		/// <summary>
		/// Gets the directory used for sound assets.
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOneX\sounds</c>
		/// Linux and macOS: <c>~/.local/share/CivOneX/sounds</c>
		/// </remarks>
		string SoundsDirectory { get; }

		/// <summary>
		/// Gets a value indicating whether the entire world map is revealed.
		/// </summary>
		bool RevealWorld { get; }

		/// <summary>
		/// Gets a value indicating whether debug mode is enabled.
		/// </summary>
		bool DebugMenu { get; }

		/// <summary>
		/// Gets a value indicating whether the running game is saved automatically when the game is closed.
		/// </summary>
		bool AutoSaveOnQuit { get; }

		/// <summary>
		/// Gets a value indicating whether closing a running game asks for confirmation first.
		/// </summary>
		bool ConfirmExit { get; }
	}
}