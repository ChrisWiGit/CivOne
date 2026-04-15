// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Enums
{
	/// <summary>
	/// Spaceship component grid cell type. Each cell in the 12×12 spaceship grid
	/// can be empty or contain one of the three structural components.
	/// </summary>
	public enum SpaceShipComponentType : byte
	{
		Empty = 0,
		Structural = 1,      // SSStructural (Cost 8)
		Component = 2,        // SSComponent (Cost 16)
		Module = 3            // SSModule (Cost 32)
	}
}
