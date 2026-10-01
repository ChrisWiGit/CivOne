namespace CivOne.Services.Maps
{
	/// <summary>
	/// Saves the current map (terrain and, where set, per-civilization start positions)
	/// as a standalone <c>*.comap</c> file, independent of a full game save.
	/// </summary>
	public interface IMapSaveService
	{
		/// <summary>
		/// Writes the current map to <paramref name="filePath"/> as a <c>.comap</c> YAML file.
		/// </summary>
		/// <param name="filePath">Destination file path.</param>
		void SaveCivOneMap(string filePath);

		/// <summary>
		/// Writes the current map to <paramref name="filePath"/> as a legacy Civilization I <c>.map</c> file.
		/// </summary>
		/// <remarks>
		/// The legacy format stores less than <c>.comap</c>; callers should first confirm the map is
		/// compatible via <see cref="GetLegacyMapCompatibility"/> to avoid silently dropping data.
		/// </remarks>
		/// <param name="filePath">Destination file path.</param>
		void SaveLegacyMap(string filePath);

		/// <summary>
		/// Checks whether the current map can be written as a legacy <c>.map</c> file without losing data.
		/// </summary>
		/// <returns>A result describing whether the legacy save is available and, if not, why.</returns>
		LegacyMapSaveCompatibilityResult GetLegacyMapCompatibility();
	}
}