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