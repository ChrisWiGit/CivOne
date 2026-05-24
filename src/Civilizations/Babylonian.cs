// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Babylonian : BaseCivilization<Hammurabi>
	{
		public Babylonian() : base(Civilization.Babylonians, TranslationServiceFactory.GetCurrent().Translate("Babylonian"), TranslationServiceFactory.GetCurrent().Translate("Babylonians"), "hama")
		{
			StartX = 45;
			StartY = 22;
			CityNames = TranslateArray(
				"Babylon\n" +
				"Sumer\n" +
				"Uruk\n" +
				"Ninevah\n" +
				"Ashur\n" +
				"Ellipi\n" +
				"Akkad\n" +
				"Eridu\n" +
				"Kish\n" +
				"Nippur\n" +
				"Shuruppak\n" +
				"Zariqum\n" +
				"Izibia\n" +
				"Nimrud\n" +
				"Arbela\n" +
				"Zamua");
		}
	}
}