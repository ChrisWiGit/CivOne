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
	internal class English : BaseCivilization<Elizabeth>
	{
		public English() : base(Civilization.English, TranslationServiceFactory.GetCurrent().Translate("English"), TranslationServiceFactory.GetCurrent().Translate("English"), "eliz")
		{
			StartX = 31;
			StartY = 14;
			CityNames = TranslateArray(
				"London\n" +
				"Coventry\n" +
				"Birmingham\n" +
				"Dover\n" +
				"Nottingham\n" +
				"York\n" +
				"Liverpool\n" +
				"Brighton\n" +
				"Oxford\n" +
				"Reading\n" +
				"Exeter\n" +
				"Cambridge\n" +
				"Hastings\n" +
				"Canterbury\n" +
				"Banbury\n" +
				"Newcastle");
		}
	}
}