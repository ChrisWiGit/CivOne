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
	internal class Chinese : BaseCivilization<Mao>
	{
		public Chinese() : base(Civilization.Chinese, TranslationServiceFactory.GetCurrent().Translate("Chinese"), TranslationServiceFactory.GetCurrent().Translate("Chinese"), "mao")
		{
			StartX = 66;
			StartY = 19;
			CityNames = TranslateArray(
				"Peking\n" +
				"Shanghai\n" +
				"Canton\n" +
				"Nanking\n" +
				"Tsingtao\n" +
				"Hangchow\n" +
				"Tientsin\n" +
				"Tatung\n" +
				"Macao\n" +
				"Anyang\n" +
				"Shantung\n" +
				"Chinan\n" +
				"Kaifeng\n" +
				"Ningpo\n" +
				"Paoting\n" +
				"Yangchow");
		}
	}
}