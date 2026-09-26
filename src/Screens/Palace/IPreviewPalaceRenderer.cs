using CivOne.Graphics;

namespace CivOne.Screens.PalaceAssets
{
	public interface IPreviewPalaceRenderer
	{
		IBitmap RenderPalace(IPalaceData palace);

		int GetMaxPalaceHeight(IPalaceData palace);
	}
}