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
	public enum Civilization : byte
	{
		Barbarians = 0,
		Romans = 1,
		Babylonians = 2,
		Germans = 3,
		Egyptians = 4,
		Americans = 5 * 8, // CW: With original Civ SaveGame and CivOne Conquest Screen, Americans and Chines are swapped otherwise.
		Greeks = 6,
		Indians = 7,
		Russians = 1 * 8, // 8 mod 7 = 1 as Romans is 1
		Zulus = 2 * 8, // 9 mod 7 = 2 as Babylonians is 2
		French = 3 * 8, // 10 mod 7 = 3 as Germans is 3
		Aztecs = 4 * 8, // 11 mod 7 = 4 as Egyptians is 4
		Chinese = 5, // swapped with Americans
		English = 6 * 8, // 13 mod 7 = 6 as Greeks is 6
		Mongols = 7 * 8, // 14 mod 7 = 0 as Barbarians is 0
	}
}