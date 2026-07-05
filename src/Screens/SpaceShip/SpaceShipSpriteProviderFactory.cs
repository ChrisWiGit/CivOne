// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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
