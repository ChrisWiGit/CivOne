namespace CivOne.Screens
{
	public interface IButtonDrawer
	{
		void DrawButton(string text, byte fontId, byte colour, byte colourDark, int x, int y, int width, int height);
		void DrawButton(string text, byte colour, byte colourDark, int x, int y, int width);
	}
}