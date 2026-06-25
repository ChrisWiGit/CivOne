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
	[SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "The HutResult enum is used to represent the possible outcomes of exploring a hut, and the values are defined based on the original game mechanics. The Random value is included to represent the possibility of a random outcome, and it is not intended to be a default or uninitialized state. The other values represent specific outcomes that can occur when exploring a hut, and they are all valid states for this enum.")]
	public enum HutResult
	{
		Random,
		MetalDeposits,
		FriendlyTribe,
		AdvancedTribe,
		AncientScrolls,
		Barbarians
	}
}