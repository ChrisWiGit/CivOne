using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Polish : BaseCivilization<Casimir>
	{
		public Polish() : base(Civilization.Poles, TranslationServiceFactory.GetCurrent().Translate("Polish"), TranslationServiceFactory.GetCurrent().Translate("Poles"), "casi")
		{
			StartX = 40;
			StartY = 10;
			CityNames = TranslateArray(
				"Krakow\n" +
				"Warsaw\n" +
				"Gdansk\n" +
				"Wroclaw\n" +
				"Poznan\n" +
				"Lodz\n" +
				"Szczecin\n" +
				"Lublin\n" +
				"Bydgoszcz\n" +
				"Katowice\n" +
				"Torun\n" +
				"Opole\n" +
				"Bialystok\n" +
				"Gdynia\n" +
				"Rzeszow\n" +
				"Sandomierz");
		}
	}
}
