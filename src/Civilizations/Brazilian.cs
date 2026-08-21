using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Brazilian : BaseCivilization<PedroII>
	{
		public Brazilian() : base(Civilization.Brazilians, TranslationServiceFactory.GetCurrent().Translate("Brazilian"), TranslationServiceFactory.GetCurrent().Translate("Brazilians"), "pedr")
		{
			StartX = 15;
			StartY = 35;
			CityNames = TranslateArray(
				"Rio de Janeiro\n" +
				"Sao Paulo\n" +
				"Salvador\n" +
				"Recife\n" +
				"Brasilia\n" +
				"Fortaleza\n" +
				"Belo Horizonte\n" +
				"Curitiba\n" +
				"Porto Alegre\n" +
				"Manaus\n" +
				"Goiania\n" +
				"Natal\n" +
				"Belem\n" +
				"Florianopolis\n" +
				"Vitoria\n" +
				"Campinas");
		}
	}
}
