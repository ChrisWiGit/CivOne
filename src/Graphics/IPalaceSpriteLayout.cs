using CivOne.Enums;

namespace CivOne.Graphics
{
	public interface IPalaceSpriteLayout
	{
		PalacePictureLayout GetLayout(int level);
		PalacePartSourceRect GetPartSourceRect(PalacePart part, PalaceStyle style, PalacePictureLayout layout);
		PalacePartSourceSelection GetSpriteCoordinatesForPart(PalaceStyle style, PalacePart part, PalacePictureLayout layout);
	}
}