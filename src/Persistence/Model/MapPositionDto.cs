using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
	public class MapPositionDto
	{
		[Doc("The X coordinate of the stored map camera position. Use -1 when slot is empty.")]
		public long X { get; set; }

		[Doc("The Y coordinate of the stored map camera position. Use -1 when slot is empty.")]
		public long Y { get; set; }

		[Doc("Optional user-defined slot name for this map position. Empty or null means unnamed.")]
		public string? Name { get; set; }
	}
}