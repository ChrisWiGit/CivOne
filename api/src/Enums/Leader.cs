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
	[SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32", Justification = "The enum values are all between 0 and 15, so a byte is sufficient and more memory-efficient than an int.")]
	public enum Leader : byte
	{
		Atilla,
		Caesar,
		Hammurabi,
		Frederick,
		Ramesses,
		Lincoln,
		Alexander,
		Gandhi,
		Stalin,
		Shaka,
		Napoleon,
		Montezuma,
		Mao,
		Elizabeth,
		Genghis
	}
}