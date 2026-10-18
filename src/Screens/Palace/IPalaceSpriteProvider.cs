using CivOne.Enums;
using CivOne.Graphics;

namespace CivOne.Screens.PalaceAssets
{
	internal interface IPalaceSpriteProvider
	{
		Picture GetBackground();
		Picture? GetGardenBackdrop(byte gardenLevel);
		Picture? GetGardenBrush(int gardenIndex, byte gardenLevel);
		/// <summary>
		/// Gets one single palace sprite as cut by the original game.
		/// </summary>
		/// <param name="level">The picture level, 0..4.</param>
		/// <param name="style">The style.</param>
		/// <param name="kind">The sprite.</param>
		/// <returns>The sprite, owned by the cache.</returns>
		Picture GetPalacePiece(int level, PalaceStyle style, PalacePieceKind kind);
	}
}