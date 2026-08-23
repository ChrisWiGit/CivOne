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
	internal class Zulu : BaseCivilization<Shaka>
	{
		public Zulu() : base(Civilization.Zulus, TranslationServiceFactory.GetCurrent().Translate("Zulu"), TranslationServiceFactory.GetCurrent().Translate("Zulus"), "shak")
		{
			StartX = 42;
			StartY = 42;
			CityNames = TranslateArray(
				"Zimbabwe\n" +
				"Ulundi\n" +
				"Bapedi\n" +
				"Hlobane\n" +
				"Isandhlwala\n" +
				"Intombe\n" +
				"Mpondo\n" +
				"Ngome\n" +
				"Swazi\n" +
				"Tugela\n" +
				"Umtata\n" +
				"Umfolozi\n" +
				"Ibabanago\n" +
				"Isipezi\n" +
				"Amatikulu\n" +
				"Zunquin");
		}
	}
}