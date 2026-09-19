using CivOne.Graphics;

namespace CivOne.Screens.GovernmentPortraits
{
	internal static class AdvisorPortraitSpriteProviderFactory
	{
		private static IAdvisorPortraitSpriteProvider? _instance;

		public static IAdvisorPortraitSpriteProvider GetInstance()
		{
			_instance ??= new ResourcesAdvisorSpriteProvider(Resources.Instance);

			return _instance;
		}
	}
}