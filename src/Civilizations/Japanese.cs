using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Japanese : BaseCivilization<Tokugawa>
	{
		public Japanese() : base(Civilization.Japanese, TranslationServiceFactory.GetCurrent().Translate("Japanese"), TranslationServiceFactory.GetCurrent().Translate("Japanese"), "toku")
		{
			StartX = 70;
			StartY = 20;
			CityNames = TranslateArray(
				"Kyoto\n" +
				"Edo\n" +
				"Osaka\n" +
				"Nara\n" +
				"Sapporo\n" +
				"Nagasaki\n" +
				"Yokohama\n" +
				"Kobe\n" +
				"Sendai\n" +
				"Kanazawa\n" +
				"Himeji\n" +
				"Kagoshima\n" +
				"Niigata\n" +
				"Okayama\n" +
				"Matsuyama\n" +
				"Fukuoka");
		}
	}
}
