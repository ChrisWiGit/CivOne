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
	[SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags", Justification = "This enum is not intended to be used as a bit field.")]
	[SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32", Justification = "The enum values are all between 0 and 32, so a byte is sufficient and more memory-efficient than an int.")]
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
		Japanese = 16,
		Persians = 17,
		Ottomans = 18,
		Spanish = 19,
		Portuguese = 20,
		Vikings = 21,
		Koreans = 22,
		Maya = 23,
		Inca = 24,
		Carthaginians = 25,
		Byzantines = 26,
		Arabs = 27,
		Mali = 28,
		Ethiopians = 29,
		Poles = 30,
		Hungarians = 31,
		Brazilians = 32,
	}
}