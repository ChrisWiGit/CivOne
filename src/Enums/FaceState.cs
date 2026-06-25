// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Diagnostics.CodeAnalysis;

namespace CivOne.Enums
{
	[SuppressMessage("Design", "CA1027:Mark enums with FlagsAttribute", Justification = "The FaceState enum is not intended to be used as a bit field, and the values are mutually exclusive states for a character's facial expression. The Flags attribute is not appropriate for this enum, as it would allow for invalid combinations of states that do not make sense in the context of facial expressions.")]
	public enum FaceState
	{
		Neutral = 0,
		Smiling = 1,
		Angry = 2,
		EyesClosed = 4
	}
}