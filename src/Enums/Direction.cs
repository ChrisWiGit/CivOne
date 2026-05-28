// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;

namespace CivOne.Enums
{
	[Flags]
	// S2342: The name of this enum is not ideal, but it is used in many places and changing it would require a large refactor.
	#pragma warning disable S2342
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