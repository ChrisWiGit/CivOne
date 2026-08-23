using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Maya : BaseCivilization<Pacal>
	{
		public Maya() : base(Civilization.Maya, TranslationServiceFactory.GetCurrent().Translate("Maya"), TranslationServiceFactory.GetCurrent().Translate("Maya"), "paca")
		{
			StartX = 8;
			StartY = 28;
			CityNames = TranslateArray(
				"Tikal\n" +
				"Palenque\n" +
				"Calakmul\n" +
				"Copan\n" +
				"Uxmal\n" +
				"Chichen Itza\n" +
				"Yaxchilan\n" +
				"Bonampak\n" +
				"Quirigua\n" +
				"Coba\n" +
				"Mayapan\n" +
				"Naranjo\n" +
				"Dos Pilas\n" +
				"Piedras Negras\n" +
				"Edzna\n" +
				"Dzibilchaltun");
		}
	}
}
