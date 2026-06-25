// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Diagnostics.CodeAnalysis;

namespace CivOne.Enums
{
	[Flags]
	[SuppressMessage("Design", "CA2217:Do not mark enums with FlagsAttribute", Justification = "The Direction enum is intended to be used as a bit field to allow for combinations of directions, and the Flags attribute is appropriate for this use case to enable bitwise operations and clear representation of combined values.")]
	public enum Direction
	{
		Alternating = -1,
		None = 0,
		North = 1,
		East = 2,
		South = 4,
		West = 8,
		NorthWest = 16,
		NorthEast = 32,
		SouthWest = 64,
		SouthEast = 128
	}
}