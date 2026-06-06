// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Drawing;
using CivOne.Enums;

namespace CivOne.Events
{
	public class ResizeEventArgs(int width, int height) : EventArgs
	{
		public int Width { get; private set; } = width;
		public int Height { get; private set; } = height;

		public Size Size => new Size(Width, Height);
	}
}