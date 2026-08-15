using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Persian : BaseCivilization<Darius>
	{
		public Persian() : base(Civilization.Persians, TranslationServiceFactory.GetCurrent().Translate("Persian"), TranslationServiceFactory.GetCurrent().Translate("Persians"), "dari")
		{
			StartX = 47;
			StartY = 26;
			CityNames = TranslateArray(
				"Persepolis\n" +
				"Susa\n" +
				"Ecbatana\n" +
				"Pasargadae\n" +
				"Bactra\n" +
				"Rhagae\n" +
				"Gordium\n" +
				"Tarsus\n" +
				"Arbela\n" +
				"Sardis\n" +
				"Tyre\n" +
				"Damascus\n" +
				"Nisa\n" +
				"Merv\n" +
				"Hecatompylos\n" +
				"Istakhr");
		}
	}
}
