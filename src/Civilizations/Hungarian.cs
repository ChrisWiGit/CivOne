using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Hungarian : BaseCivilization<Corvinus>
	{
		public Hungarian() : base(Civilization.Hungarians, TranslationServiceFactory.GetCurrent().Translate("Hungarian"), TranslationServiceFactory.GetCurrent().Translate("Hungarians"), "matt")
		{
			StartX = 38;
			StartY = 11;
			CityNames = TranslateArray(
				"Buda\n" +
				"Pest\n" +
				"Esztergom\n" +
				"Szeged\n" +
				"Debrecen\n" +
				"Pecs\n" +
				"Gyor\n" +
				"Miskolc\n" +
				"Kecskemet\n" +
				"Eger\n" +
				"Sopron\n" +
				"Veszprem\n" +
				"Szolnok\n" +
				"Szombathely\n" +
				"Nyiregyhaza\n" +
				"Kaposvar");
		}
	}
}
