using System.Diagnostics.CodeAnalysis;
using CivOne.Graphics;

namespace CivOne.Screens.SpaceShipAssets
{
	/// <summary>
	/// Creates and caches the default <see cref="ISpaceShipSpriteProvider"/> for spaceship rendering.
	/// </summary>
	public static class SpaceShipSpriteProviderFactory
	{
		private static ISpaceShipSpriteProvider? _instance;

		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This method may perform initialization and is not a simple property getter.")]
		public static ISpaceShipSpriteProvider GetInstance()
		{
			_instance ??= new ResourcesSpaceShipSpriteProvider(Resources.Instance);

			return _instance;
		}
	}
}