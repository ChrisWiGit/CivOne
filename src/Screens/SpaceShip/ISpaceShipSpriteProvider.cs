// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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