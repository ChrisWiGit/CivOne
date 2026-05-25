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
	internal class German : BaseCivilization<Frederick>
	{
		public German() : base(Civilization.Germans, TranslationServiceFactory.GetCurrent().Translate("German"), TranslationServiceFactory.GetCurrent().Translate("Germans"), "fred")
		{
			StartX = 38;
			StartY = 15;
			CityNames = TranslateArray(
				"Berlin\n" +
				"Leipzig\n" +
				"Hamburg\n" +
				"Bremen\n" +
				"Frankfurt\n" +
				"Bonn\n" +
				"Nuremberg\n" +
				"Cologne\n" +
				"Hannover\n" +
				"Munich\n" +
				"Stuttgart\n" +
				"Heidelberg\n" +
				"Salzburg\n" +
				"Konigsberg\n" +
				"Dortmond\n" +
				"Brandenburg");
		}
	}
}