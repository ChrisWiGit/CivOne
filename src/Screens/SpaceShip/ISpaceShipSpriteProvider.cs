using System.Diagnostics.CodeAnalysis;
using CivOne.Enums;
using CivOne.Graphics;

namespace CivOne.Screens.SpaceShipAssets
{
	/// <summary>
	/// Provides sprites for concrete <see cref="SpaceShipComponentType"/> values used by <see cref="CivOne.Screens.SpaceShipView"/>.
	/// </summary>
	public interface ISpaceShipSpriteProvider
	{
		bool TryGetPartSprite(SpaceShipComponentType partType, [NotNullWhen(true)] out Picture? sprite);
	}
}