using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Byzantine : BaseCivilization<Justinian>
	{
		public Byzantine() : base(Civilization.Byzantines, TranslationServiceFactory.GetCurrent().Translate("Byzantine"), TranslationServiceFactory.GetCurrent().Translate("Byzantines"), "just")
		{
			StartX = 42;
			StartY = 17;
			CityNames = TranslateArray(
				"Constantinople\n" +
				"Nicaea\n" +
				"Antioch\n" +
				"Trebizond\n" +
				"Thessalonica\n" +
				"Adrianople\n" +
				"Ephesus\n" +
				"Smyrna\n" +
				"Corinth\n" +
				"Athens\n" +
				"Ancyra\n" +
				"Caesarea\n" +
				"Heraclea\n" +
				"Chalcedon\n" +
				"Iconium\n" +
				"Philippopolis");
		}
	}
}
