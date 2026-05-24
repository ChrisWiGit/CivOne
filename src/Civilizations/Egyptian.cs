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
	internal class Egyptian : BaseCivilization<Ramesses>
	{
		public Egyptian() : base(Civilization.Egyptians, TranslationServiceFactory.GetCurrent().Translate("Egyptian"), TranslationServiceFactory.GetCurrent().Translate("Egyptians"), "rams")
		{
			StartX = 41;
			StartY = 24;
			CityNames = TranslateArray(
				"Thebes\n" +
				"Memphis\n" +
				"Oryx\n" +
				"Heliopolis\n" +
				"Gaza\n" +
				"Alexandria\n" +
				"Byblos\n" +
				"Cairo\n" +
				"Coptos\n" +
				"Edfu\n" +
				"Pithom\n" +
				"Busirus\n" +
				"Athribus\n" +
				"Mendes\n" +
				"Tanis\n" +
				"Abydos");
		}
	}
}