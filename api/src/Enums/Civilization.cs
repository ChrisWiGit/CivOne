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
		Americans = 12, // CW: With original Civ SaveGame and CivOne Conquest Screen, Americans and Chines are swapped otherwise.
		Greeks = 6,
		Indians = 7,
		Russians = 8, //Romans is 8 - 7 = 1
		Zulus = 9, // Babylonians is 9 - 7 = 2
		French = 10, // Germans is 10 - 7 = 3
		Aztecs = 11, // Egyptians is 11 - 7 = 4
		Chinese = 5, // swapped with Americans
		English = 13, // Greeks is 13 - 7 = 6
		Mongols = 14, // Indians is 14 - 7 = 7
	}
}