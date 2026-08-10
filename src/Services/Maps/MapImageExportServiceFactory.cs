namespace CivOne.Services.Maps
{
	/// <summary>
	/// Creates <see cref="IMapImageExportService"/> instances that write <c>*.bmp</c> map images.
	/// </summary>
	public static class MapImageExportServiceFactory
	{
		/// <summary>
		/// Creates a new <see cref="IMapImageExportService"/>.
		/// </summary>
		public static IMapImageExportService Create() => new MapImageExportService();
	}
}
