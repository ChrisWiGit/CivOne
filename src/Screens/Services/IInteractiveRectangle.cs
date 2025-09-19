using System.Drawing;

namespace CivOne.Screens.Services
{
	public interface IInteractiveRectangle
	{
		bool Contains(Point p);
		void DrawRectangle(byte color);
	}
}