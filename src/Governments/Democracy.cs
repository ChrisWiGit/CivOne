// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Governments
{
	internal class Democracy : BaseGovernment
	{
		public Democracy() : base(5, new Advances.Democracy())
		{
			Name = "Democracy";
			TranslatedName = Translate("Democracy");
			NameAdjective = Translate("Democratic");
			CorruptionMultiplier = 0;
		}
	}
}