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
	internal class Russian : BaseCivilization<Stalin>
	{
		public Russian() : base(Civilization.Russians, TranslationServiceFactory.GetCurrent().Translate("Russian"), TranslationServiceFactory.GetCurrent().Translate("Russians"), "stal")
		{
			StartX = 44;
			StartY = 12;
			CityNames = TranslateArray(
				"Moscow\n" +
				"Leningrad\n" +
				"Kiev\n" +
				"Minsk\n" +
				"Smolensk\n" +
				"Odessa\n" +
				"Sevastopol\n" +
				"Tblisi\n" +
				"Sverdlovsk\n" +
				"Yakutsk\n" +
				"Vladivostok\n" +
				"Novograd\n" +
				"Krasnoyarsk\n" +
				"Riga\n" +
				"Rostov\n" +
				"Atrakhan");
		}
	}
}