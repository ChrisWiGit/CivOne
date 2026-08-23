// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Services;

namespace CivOne.Civilizations
{
	internal class Mongol : BaseCivilization<Genghis>
	{
		public Mongol() : base(Civilization.Mongols, TranslationServiceFactory.GetCurrent().Translate("Mongol"), TranslationServiceFactory.GetCurrent().Translate("Mongols"), "geng")
		{
			StartX = 49;
			StartY = 19;
			CityNames = TranslateArray(
				"Samarkand\n" +
				"Bokhara\n" +
				"Nishapur\n" +
				"Karakorum\n" +
				"Kashgar\n" +
				"Tabriz\n" +
				"Aleppo\n" +
				"Kabul\n" +
				"Ormuz\n" +
				"Basra\n" +
				"Khanbaryk\n" +
				"Khorasan\n" +
				"Shangtu\n" +
				"Kazan\n" +
				"Qyinsay\n" +
				"Kerman");
		}
	}
}