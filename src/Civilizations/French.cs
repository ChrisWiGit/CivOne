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
	internal class French : BaseCivilization<Napoleon>
	{
		public French() : base(Civilization.French, TranslationServiceFactory.GetCurrent().Translate("French"), TranslationServiceFactory.GetCurrent().Translate("French"), "napo")
		{
			StartX = 33;
			StartY = 16;
			CityNames = TranslateArray(
				"Paris\n" +
				"Orleans\n" +
				"Lyons\n" +
				"Tours\n" +
				"Chartres\n" +
				"Bordeaux\n" +
				"Rouen\n" +
				"Avignon\n" +
				"Marseilles\n" +
				"Grenoble\n" +
				"Dijon\n" +
				"Amiens\n" +
				"Cherbourg\n" +
				"Poitiers\n" +
				"Toulouse\n" +
				"Bayonne");
		}
	}
}