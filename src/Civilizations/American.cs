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
	internal class American : BaseCivilization<Lincoln>
	{
		public American() : base(Civilization.Americans, TranslationServiceFactory.GetCurrent().Translate("American"), TranslationServiceFactory.GetCurrent().Translate("Americans"), "linc")
		{
			StartX = 12;
			StartY = 18;
			CityNames = TranslateArray(
				"Washington\n" +
				"New York\n" +
				"Boston\n" +
				"Philadelphia\n" +
				"Atlanta\n" +
				"Chicago\n" +
				"Buffalo\n" +
				"St. Louis\n" +
				"Detroit\n" +
				"New Orleans\n" +
				"Baltimore\n" +
				"Denver\n" +
				"Cincinnati\n" +
				"Dallas\n" +
				"Los Angeles\n" +
				"Las Vegas");
		}
	}
}