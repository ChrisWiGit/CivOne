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
	internal class Roman : BaseCivilization<Caesar>
	{
		public Roman() : base(Civilization.Romans, TranslationServiceFactory.GetCurrent().Translate("Roman"), TranslationServiceFactory.GetCurrent().Translate("Romans"), "ceas")
		{
			StartX = 36;
			StartY = 19;
			CityNames = TranslateArray(
				"Rome\n" +
				"Caesarea\n" +
				"Carthage\n" +
				"Nicopolis\n" +
				"Byzantium\n" +
				"Brundisium\n" +
				"Syracuse\n" +
				"Antioch\n" +
				"Palmyra\n" +
				"Cyrene\n" +
				"Gordion\n" +
				"Tyrus\n" +
				"Jerusalem\n" +
				"Seleucia\n" +
				"Ravenna\n" +
				"Artaxata");
		}
	}
}