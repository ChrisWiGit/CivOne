// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;

// ReSharper disable InconsistentNaming

namespace CivOne
{
	#pragma warning disable S101 // Types should be named in PascalCase - but these are named to match SDL as a name.
	internal static partial class SDL
	{
		[Flags]
		#pragma warning disable S2342 // Keep case sensitive to match SDL as a name.
        private enum SDL_WINDOW : uint
		{
			FULLSCREEN = 0x001,
			OPENGL = 0x002,
			SHOWN = 0x004,
			HIDDEN = 0x008,
			BORDERLESS = 0x010,
			RESIZABLE = 0x020,
			MINIMIZED = 0x040,
			MAXIMIZED = 0x080,
			INPUT_GRABBED = 0x100,
			INPUT_FOCUS = 0x200,
			MOUSE_FOCUS = 0x400,
			FOREIGN = 0x800,
			FULLSCREEN_DESKTOP = (0x1000 | FULLSCREEN),
			ALLOW_HIGHDPI = 0x2000
		}
	}
}