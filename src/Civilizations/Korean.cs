using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Korean : BaseCivilization<Sejong>
	{
		public Korean() : base(Civilization.Koreans, TranslationServiceFactory.GetCurrent().Translate("Korean"), TranslationServiceFactory.GetCurrent().Translate("Koreans"), "sejo")
		{
			StartX = 68;
			StartY = 16;
			CityNames = TranslateArray(
				"Seoul\n" +
				"Busan\n" +
				"Gyeongju\n" +
				"Pyongyang\n" +
				"Incheon\n" +
				"Daegu\n" +
				"Daejeon\n" +
				"Gwangju\n" +
				"Suwon\n" +
				"Jeonju\n" +
				"Cheongju\n" +
				"Andong\n" +
				"Kaesong\n" +
				"Jeju\n" +
				"Ulsan\n" +
				"Mokpo");
		}
	}
}
