namespace CivOne
{
	public interface ISettings
	{
		/// <summary>
		/// Gets the CivOne storage root. This is the root folder where all CivOne data is stored
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOne</c>
		/// Linux and macOS: <c>~/.local/share/CivOne</c>
		/// </remarks>
		string StorageDirectory { get; }

		/// <summary>
		/// Gets the directory used for captured screenshots and recordings.
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOne\capture</c>
		/// Linux and macOS: <c>~/.local/share/CivOne/capture</c>
		/// </remarks>
		string CaptureDirectory { get; }

		/// <summary>
		/// Gets the directory that contains the game data files.
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOne\data</c>
		/// Linux and macOS: <c>~/.local/share/CivOne/data</c>
		/// </remarks>
		string DataDirectory { get; }

		/// <summary>
		/// Gets the directory used for plugins.
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOne\plugins</c>
		/// Linux and macOS: <c>~/.local/share/CivOne/plugins</c>
		/// </remarks>
		string PluginsDirectory { get; }

		/// <summary>
		/// Gets the directory used for savegames.
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOne\saves</c>
		/// Linux and macOS: <c>~/.local/share/CivOne/saves</c>
		/// </remarks>
		string SavesDirectory { get; }

		/// <summary>
		/// Gets the directory used for classic .cos savegames.
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOne\saves\cos</c>
		/// Linux and macOS: <c>~/.local/share/CivOne/saves/cos</c>
		/// </remarks>
		string CosSavesDirectory { get; }

		/// <summary>
		/// Gets the directory used for sound assets.
		/// </summary>
		/// <remarks>
		/// Windows: <c>%LOCALAPPDATA%\CivOne\sounds</c>
		/// Linux and macOS: <c>~/.local/share/CivOne/sounds</c>
		/// </remarks>
		string SoundsDirectory { get; }

		/// <summary>
		/// Gets a value indicating whether the entire world map is revealed.
		/// </summary>
		bool RevealWorld { get; }
	}
}