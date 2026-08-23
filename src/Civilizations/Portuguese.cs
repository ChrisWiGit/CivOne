using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Portuguese : BaseCivilization<Henrique>
	{
		public Portuguese() : base(Civilization.Portuguese, TranslationServiceFactory.GetCurrent().Translate("Portuguese"), TranslationServiceFactory.GetCurrent().Translate("Portuguese"), "henr")
		{
			StartX = 25;
			StartY = 22;
			CityNames = TranslateArray(
				"Lisbon\n" +
				"Porto\n" +
				"Coimbra\n" +
				"Braga\n" +
				"Evora\n" +
				"Faro\n" +
				"Aveiro\n" +
				"Guimaraes\n" +
				"Setubal\n" +
				"Leiria\n" +
				"Viseu\n" +
				"Lagos\n" +
				"Beja\n" +
				"Sintra\n" +
				"Tomar\n" +
				"Santarem");
		}
	}
}
