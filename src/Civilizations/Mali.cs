using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Mali : BaseCivilization<MansaMusa>
	{
		public Mali() : base(Civilization.Mali, TranslationServiceFactory.GetCurrent().Translate("Mali"), TranslationServiceFactory.GetCurrent().Translate("Mali"), "mans")
		{
			StartX = 30;
			StartY = 28;
			CityNames = TranslateArray(
				"Niani\n" +
				"Timbuktu\n" +
				"Gao\n" +
				"Djenne\n" +
				"Koumbi Saleh\n" +
				"Walata\n" +
				"Kangaba\n" +
				"Kita\n" +
				"Sikasso\n" +
				"Bamako\n" +
				"Segou\n" +
				"Mopti\n" +
				"Kidal\n" +
				"Kayes\n" +
				"Sokolo\n" +
				"Jenne");
		}
	}
}
