namespace CivOne
{
	/// <summary>
	/// The entry point the host looks for: namespace CivOne, type name Plugin, implementing IPlugin.
	/// </summary>
	public class Plugin : IPlugin
	{
		/// <summary>
		/// The plugin name shown in the settings menu.
		/// </summary>
		public string Name => "CivOne Test Plugin";

		/// <summary>
		/// The plugin author shown in the settings menu.
		/// </summary>
		public string Author => "CivOne Tests";

		/// <summary>
		/// The plugin version shown in the settings menu.
		/// </summary>
		public string Version => "1.0.0";
	}
}
