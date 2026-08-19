using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Carthaginian : BaseCivilization<Hannibal>
	{
		public Carthaginian() : base(Civilization.Carthaginians, TranslationServiceFactory.GetCurrent().Translate("Carthaginian"), TranslationServiceFactory.GetCurrent().Translate("Carthaginians"), "hann")
		{
			StartX = 37;
			StartY = 24;
			CityNames = TranslateArray(
				"Carthage\n" +
				"Utica\n" +
				"Hippo Regius\n" +
				"Hadrumetum\n" +
				"Leptis Magna\n" +
				"Sabratha\n" +
				"Thapsus\n" +
				"Cirta\n" +
				"Bulla Regia\n" +
				"Oea\n" +
				"Lilybaeum\n" +
				"Panormus\n" +
				"Motya\n" +
				"Gades\n" +
				"Tingis\n" +
				"Rusadir");
		}
	}
}
