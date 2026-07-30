namespace CivOne.Services.Maps
{
	/// <summary>
	/// Creates <see cref="IMapDialogPathProvider"/> instances.
	/// </summary>
	public static class MapDialogPathProviderFactory
	{
		/// <summary>
		/// Creates a new <see cref="IMapDialogPathProvider"/> backed by the runtime and the runtime settings.
		/// </summary>
		/// <returns>A provider for the map file chooser start path.</returns>
		public static IMapDialogPathProvider Create() => new MapDialogPathProvider(RuntimeHandler.Runtime, Settings.Instance);
	}
}
