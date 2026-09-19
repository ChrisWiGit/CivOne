using CivOne.Enums;
using CivOne.Graphics;

namespace CivOne.Screens.PalaceAssets
{
	internal interface IPalaceSpriteProvider
	{
		Picture GetBackground();
		Picture? GetGardenBackdrop(byte gardenLevel);
		Picture? GetGardenBrush(int gardenIndex, byte gardenLevel);
		Picture GetPalacePart(PalaceStyle style, PalacePart part, int level);
	}
}