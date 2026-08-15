using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Arab : BaseCivilization<Harun>
	{
		public Arab() : base(Civilization.Arabs, TranslationServiceFactory.GetCurrent().Translate("Arab"), TranslationServiceFactory.GetCurrent().Translate("Arabs"), "haru")
		{
			StartX = 46;
			StartY = 28;
			CityNames = TranslateArray(
				"Baghdad\n" +
				"Mecca\n" +
				"Medina\n" +
				"Damascus\n" +
				"Basra\n" +
				"Kufa\n" +
				"Jerusalem\n" +
				"Aden\n" +
				"Muscat\n" +
				"Sana\n" +
				"Aleppo\n" +
				"Cairo\n" +
				"Tripoli\n" +
				"Tunis\n" +
				"Fez\n" +
				"Cordoba");
		}
	}
}
