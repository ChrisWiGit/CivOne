namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Root DTO for standalone map files (<c>*.comap</c>).
	/// </summary>
	/// <remarks>
	/// A <c>.comap</c> YAML file has a single top-level <c>Map:</c> key that contains
	/// the tile grid and terrain seed. This wrapper DTO matches that structure so the
	/// file can be deserialized independently of a full game save.
	/// </remarks>
	public class MapFileDto
	{
		/// <summary>Gets or sets the map data.</summary>
		public MapDto Map { get; set; } = new();
	}
}