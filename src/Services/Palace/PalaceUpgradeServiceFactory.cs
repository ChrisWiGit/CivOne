// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Services.Palace
{
	internal static class PalaceUpgradeServiceFactory
	{
		private static IPalaceUpgradeService _instance;

		public static IPalaceUpgradeService GetInstance()
		{
			if (_instance == null)
			{
				_instance = new PalaceUpgradeService(
				[
					new HumanCivScorePalaceTrigger()
					// additional triggers can be added here in the future if desired
				]);
			}

			return _instance;
		}
	}
}