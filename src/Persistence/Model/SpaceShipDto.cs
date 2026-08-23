// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Data transfer object for a spaceship's state. Stores the 12×12 component grid,
	/// population capacity, and launch year.
	/// 
	/// The Grid uses SpaceShipGridMap2d which serializes to a compact YAML format:
	/// Grid:
	///   - ECMM000000000
	///   - S00000000000
	///   - ...
	/// 
	/// Population and LaunchYear use long type to support YAML compatibility;
	/// the mapper will clamp them to appropriate ranges (ushort/short).
	/// </summary>
	public class SpaceShipDto
	{
		[Doc("12×12 component grid for spaceship construction. Each cell stores component type: E=Empty, S=Structural, C=Component, M=Module.")]
		public SpaceShipGridMap2D Grid { get; set; } = SpaceShipGridMap2D.Uninitialized;

		[Doc("Population capacity of the spaceship. Clamped to [0, 65535] (ushort) during load.", 0, long.MaxValue)]
		public long Population { get; set; }

		[Doc("The game year the spaceship was launched. 0 = not launched. Clamped to [-32768, 32767] (short) during load.")]
		public long LaunchYear { get; set; }
	}
}
