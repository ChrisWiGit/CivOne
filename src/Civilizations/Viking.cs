using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Viking : BaseCivilization<Harald>
	{
		public Viking() : base(Civilization.Vikings, TranslationServiceFactory.GetCurrent().Translate("Viking"), TranslationServiceFactory.GetCurrent().Translate("Vikings"), "hara")
		{
			StartX = 34;
			StartY = 8;
			CityNames = TranslateArray(
				"Nidaros\n" +
				"Birka\n" +
				"Ribe\n" +
				"Jorvik\n" +
				"Hedeby\n" +
				"Kaupang\n" +
				"Trondheim\n" +
				"Aarhus\n" +
				"Uppsala\n" +
				"Skalholt\n" +
				"Tonsberg\n" +
				"Lund\n" +
				"Roskilde\n" +
				"Bergen\n" +
				"Reykjavik\n" +
				"Oslo");
		}
	}
}
