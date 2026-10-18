using CivOne.Graphics;

namespace CivOne.Screens.PalaceAssets
{
	internal static class PreviewPalaceRendererFactory
	{
		private static IPreviewPalaceRenderer? _instance;

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