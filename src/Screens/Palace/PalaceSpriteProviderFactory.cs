// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Graphics;

namespace CivOne.Screens.PalaceAssets
{
	internal static class PalaceSpriteProviderFactory
	{
		private static IPalaceSpriteProvider? _instance;

		public static IPalaceSpriteProvider GetInstance()
		{
			if (_instance == null)
			{
				_instance = new ResourcesPalaceSpriteProvider(Resources.Instance);
			}

			return _instance;
		}
	}
}