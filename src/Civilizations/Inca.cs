using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Inca : BaseCivilization<Pachacuti>
	{
		public Inca() : base(Civilization.Inca, TranslationServiceFactory.GetCurrent().Translate("Inca"), TranslationServiceFactory.GetCurrent().Translate("Inca"), "pach")
		{
			StartX = 9;
			StartY = 34;
			CityNames = TranslateArray(
				"Cusco\n" +
				"Quito\n" +
				"Machu Picchu\n" +
				"Ollantaytambo\n" +
				"Pisac\n" +
				"Chinchero\n" +
				"Cajamarca\n" +
				"Huanuco\n" +
				"Arequipa\n" +
				"Nazca\n" +
				"Chan Chan\n" +
				"Vilcabamba\n" +
				"Ayacucho\n" +
				"Puno\n" +
				"Tiahuanaco\n" +
				"Abancay");
		}
	}
}
