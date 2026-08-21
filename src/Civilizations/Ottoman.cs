using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Ottoman : BaseCivilization<Suleiman>
	{
		public Ottoman() : base(Civilization.Ottomans, TranslationServiceFactory.GetCurrent().Translate("Ottoman"), TranslationServiceFactory.GetCurrent().Translate("Ottomans"), "sule")
		{
			StartX = 43;
			StartY = 20;
			CityNames = TranslateArray(
				"Istanbul\n" +
				"Bursa\n" +
				"Edirne\n" +
				"Ankara\n" +
				"Konya\n" +
				"Izmir\n" +
				"Trabzon\n" +
				"Antalya\n" +
				"Adana\n" +
				"Sivas\n" +
				"Erzurum\n" +
				"Baghdad\n" +
				"Damietta\n" +
				"Aleppo\n" +
				"Mosul\n" +
				"Smyrna");
		}
	}
}
