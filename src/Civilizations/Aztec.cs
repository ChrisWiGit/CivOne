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
	internal class Aztec : BaseCivilization<Montezuma>
	{
		public Aztec() : base(Civilization.Aztecs, TranslationServiceFactory.GetCurrent().Translate("Aztec"), TranslationServiceFactory.GetCurrent().Translate("Aztecs"), "mont")
		{
			StartX = 5;
			StartY = 23;
			CityNames = TranslateArray(
				"Tenochtitlan\n" +
				"Chiauhtia\n" +
				"Chapultapec\n" +
				"Coatepec\n" +
				"Ayontzinco\n" +
				"Itzapalapa\n" +
				"Itzapam\n" +
				"Mitxcoac\n" +
				"Tucubaya\n" +
				"Tecamac\n" +
				"Tepezinco\n" +
				"Ticoman\n" +
				"Tlaxcala\n" +
				"Xaltocan\n" +
				"Xicalango\n" +
				"Zumpanco");
		}
	}
}