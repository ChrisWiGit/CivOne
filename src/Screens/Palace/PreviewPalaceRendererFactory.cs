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
	internal static class PreviewPalaceRendererFactory
	{
		private static IPreviewPalaceRenderer _instance;

		public static IPreviewPalaceRenderer GetInstance()
		{
			if (_instance != null) return _instance;

			var resourcesDelegate = new PreviewPalaceResourcesWrapper(name => Resources.Instance[name]);
			_instance = new PreviewPalaceRenderer(resourcesDelegate);
			return _instance;
		}

		internal static void ClearInstance() => _instance = null;
	}
}
