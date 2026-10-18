using CivOne.Graphics;

namespace CivOne.Screens.Reports
{
	internal class TopLeaderScreenEnvironment
	{
		public virtual Palette GetDefaultPalette() => Common.DefaultPalette;

		public virtual int GetFontHeight(byte fontId) => Resources.Instance.GetFontHeight(fontId);
	}
}