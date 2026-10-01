namespace CivOne.Services.Maps
{
	/// <summary>
	/// Creates <see cref="IMapSaveService"/> instances that write standalone <c>*.comap</c> files.
	/// </summary>
	public static class MapSaveServiceFactory
	{
		/// <summary>
		/// Creates a new <see cref="IMapSaveService"/>.
		/// </summary>
		public static IMapSaveService Create() => new MapSaveService();
	}
}