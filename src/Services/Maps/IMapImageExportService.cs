namespace CivOne.Services.Maps
{
	/// <summary>
	/// Exports the world map, including cities and units, as an image file.
	/// </summary>
	public interface IMapImageExportService
	{
		/// <summary>
		/// Renders the complete map and writes it to the given file, replacing an existing file.
		/// </summary>
		/// <param name="map">The map to render.</param>
		/// <param name="visibilityPlayer">
		/// The player whose visibility limits the exported area.
		/// Pass <see langword="null"/> to export the entire world without fog of war.
		/// </param>
		/// <param name="filePath">The target file path.</param>
		void ExportToFile(IMap map, Player? visibilityPlayer, string filePath);
	}
}
