namespace CivOne.Services.Maps
{
	/// <summary>
	/// Creates <see cref="ICustomMapLoaderService"/> instances.
	/// </summary>
	public static class CustomMapLoaderServiceFactory
	{
		/// <summary>
		/// Creates a new <see cref="ICustomMapLoaderService"/> backed by the runtime settings.
		/// </summary>
		public static ICustomMapLoaderService Create() => new CustomMapLoaderService(Settings.Instance);
	}
}