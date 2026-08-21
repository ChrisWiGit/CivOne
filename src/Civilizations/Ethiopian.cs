using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Ethiopian : BaseCivilization<Selassie>
	{
		public Ethiopian() : base(Civilization.Ethiopians, TranslationServiceFactory.GetCurrent().Translate("Ethiopian"), TranslationServiceFactory.GetCurrent().Translate("Ethiopians"), "hail")
		{
			StartX = 43;
			StartY = 32;
			CityNames = TranslateArray(
				"Aksum\n" +
				"Gondar\n" +
				"Lalibela\n" +
				"Harar\n" +
				"Addis Ababa\n" +
				"Adwa\n" +
				"Mekele\n" +
				"Dire Dawa\n" +
				"Bahir Dar\n" +
				"Jimma\n" +
				"Dessie\n" +
				"Arba Minch\n" +
				"Debre Markos\n" +
				"Axum\n" +
				"Shire\n" +
				"Goba");
		}
	}
}
