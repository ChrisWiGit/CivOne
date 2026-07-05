using System.Linq;
using CivOne.Enums;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Represents the style and build level of a single palace section.
	/// </summary>
	public class PalaceSectionDto
	{
		[Doc("The architectural style of this palace section.", nameof(AllPalaceStyles))]
		public PalaceStyle Style { get; set; }

		public static readonly PalaceStyle[] AllPalaceStyles = System.Enum.GetValues<PalaceStyle>();
		
		
		[Doc("The build level of this palace section.", 0, 4)]
		public byte Level { get; set; }

		public PalaceSectionDto() { }

		public PalaceSectionDto(PalaceStyle style, byte level)
		{
			Style = style;
			Level = level;
		}
	}

	/// <summary>
	/// Data Transfer Object representing the player's palace.
	/// Seven named sections from left to right, plus three named garden levels.
	/// </summary>
	public class PalaceDto
	{
		// Palace sections, left to right
		[Doc("The leftmost tower section of the palace.")]
		public PalaceSectionDto LeftTower   { get; set; } = new();
		
		[Doc("The left wing section of the palace.")]
		public PalaceSectionDto LeftWing    { get; set; } = new();
		[Doc("The left annex section of the palace.")]
		public PalaceSectionDto LeftAnnex   { get; set; } = new();
		[Doc("The center section of the palace.")]
		public PalaceSectionDto Center      { get; set; } = new();
		[Doc("The right annex section of the palace.")]
		public PalaceSectionDto RightAnnex  { get; set; } = new();
		[Doc("The right wing section of the palace.")]
		public PalaceSectionDto RightWing   { get; set; } = new();
		[Doc("The rightmost tower section of the palace.")]
		public PalaceSectionDto RightTower  { get; set; } = new();

		[Doc("The build level of the left garden.", 0, 3)]
		public byte GardenLeftLevel   { get; set; }
		[Doc("The build level of the center garden.", 0, 3)]
		public byte GardenCenterLevel { get; set; }
		[Doc("The build level of the right garden.", 0, 3)]
		public byte GardenRightLevel  { get; set; }
	}
}