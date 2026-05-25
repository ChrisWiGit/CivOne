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
	internal class Greek : BaseCivilization<Alexander>
	{
		public Greek() : base(Civilization.Greeks, TranslationServiceFactory.GetCurrent().Translate("Greek"), TranslationServiceFactory.GetCurrent().Translate("Greeks"), "alex")
		{
			StartX = 39;
			StartY = 18;
			CityNames = TranslateArray(
				"Athens\n" +
				"Sparta\n" +
				"Corinth\n" +
				"Delphi\n" +
				"Eretria\n" +
				"Pharsalos\n" +
				"Argos\n" +
				"Mycenae\n" +
				"Herakleia\n" +
				"Antioch\n" +
				"Ephesos\n" +
				"Rhodes\n" +
				"Knossos\n" +
				"Troy\n" +
				"Pergamon\n" +
				"Miletos");
		}
	}
}