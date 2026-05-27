// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne
{
	#pragma warning disable S101 // Types should be named in PascalCase - but these are named to match SDL as a name.
	internal static partial class SDL
	{
		#pragma warning disable S2342 // Keep case sensitive to match SDL as a name.
		private enum SDL_EventType : uint
		{
			SDL_MIN = 0,

			// Application
			SDL_QUIT = 0x100,

			// Window
			SDL_WINDOWEVENT = 0x200,
			SDL_SYSWMEVENT,

			// Keyboard
			SDL_KEYDOWN = 0x300,
			SDL_KEYUP,
			SDL_TEXTEDITING,
			SDL_TEXTINPUT,
			SDL_KEYMAPCHANGED,

			// Mouse events
			SDL_MOUSEMOTION = 0x400,
			SDL_MOUSEBUTTONDOWN,
			SDL_MOUSEBUTTONUP,
			SDL_MOUSEWHEEL,

			SDL_MAX = 0xFFFF
		}
	}
}