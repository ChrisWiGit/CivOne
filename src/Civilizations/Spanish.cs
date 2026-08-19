using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Spanish : BaseCivilization<Isabella>
	{
		public Spanish() : base(Civilization.Spanish, TranslationServiceFactory.GetCurrent().Translate("Spanish"), TranslationServiceFactory.GetCurrent().Translate("Spanish"), "isab")
		{
			StartX = 28;
			StartY = 20;
			CityNames = TranslateArray(
				"Madrid\n" +
				"Barcelona\n" +
				"Seville\n" +
				"Valencia\n" +
				"Toledo\n" +
				"Granada\n" +
				"Cordoba\n" +
				"Malaga\n" +
				"Zaragoza\n" +
				"Bilbao\n" +
				"Murcia\n" +
				"Valladolid\n" +
				"Salamanca\n" +
				"Cadiz\n" +
				"Pamplona\n" +
				"Leon");
		}
	}
}
