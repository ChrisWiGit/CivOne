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

		public static readonly PalaceStyle[] AllPalaceStyles = (PalaceStyle[])System.Enum.GetValues(typeof(PalaceStyle));
		
		
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



// namespace CivOne
// {
// 	public class PalaceData
// 	{
// 		protected byte[] PalaceStyle = new byte[7];
// 		protected byte[] PalaceLevel = new byte[7];
// 		protected byte[] GardenLevel = new byte[3];

// 		public int PalaceLeft
// 		{
// 			get
// 			{
// 				for (int i = 0; i < 3; i++)
// 				{
// 					if (PalaceLevel[i] > 0) return i;
// 				}
// 				return 2;
// 			}
// 		}

// 		public int PalaceRight
// 		{
// 			get
// 			{
// 				for (int i = 6; i > 3; i--)
// 				{
// 					if (PalaceLevel[i] > 0) return i;
// 				}
// 				return 4;
// 			}
// 		}

// 		public PalaceStyle GetPalaceStyle(int index)
// 		{
// 			if (index < 0 || index > 6) throw new Exception("Invalid palace index");
// 			return (PalaceStyle)PalaceStyle[index];
// 		}

// 		public byte GetPalaceLevel(int index)
// 		{
// 			if (index < 0 || index > 6) throw new Exception("Invalid palace index");
// 			return PalaceLevel[index];
// 		}

// 		public byte GetGardenLevel(int index)
// 		{
// 			if (index < 0 || index > 2) throw new Exception("Invalid garden index");
// 			return GardenLevel[index];
// 		}

// 		public void SetPalace(int index, byte style, byte level)
// 		{
// 			if (index < 0 || index > 6)
// 				throw new Exception("Invalid palace index");
// 			if (style < 0 || style > 3)
// 				throw new Exception("Invalid palace style");
// 			if (level < 0 || level > 4)
// 				throw new Exception("Invalid palace level");

// 			if (level == 0 || style == 0)
// 			{
// 				PalaceStyle[index] = 0;
// 				PalaceLevel[index] = 0;
// 				return;
// 			}
// 			PalaceStyle[index] = style;
// 			PalaceLevel[index] = level;
// 		}

// 		public void SetGarden(int index, byte level)
// 		{
// 			if (index < 0 || index > 2) throw new Exception("Invalid garden index");
// 			if (level < 0 || level > 3) throw new Exception("Invalid garden level");

// 			GardenLevel[index] = level;
// 		}
// 	}
// }