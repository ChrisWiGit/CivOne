// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;

namespace CivOne
{
	internal static partial class SDL
	{
		[Flags]
		private enum SDL_INIT : uint
		{
			TIMER = 0x00000001,
			AUDIO = 0x00000010,
			VIDEO = 0x00000020,  // Implies EVENTS
			JOYSTICK = 0x00000200,  // Implies EVENTS
			HAPTIC = 0x00001000,
			GAMECONTROLLER = 0x00002000,  // Implies JOYSTICK
			EVENTS = 0x00004000,
			SENSOR = 0x00008000,
			NOPARACHUTE = 0x00100000,  // Ignored (legacy)
			EVERYTHING = TIMER | AUDIO | VIDEO | EVENTS | JOYSTICK | HAPTIC | GAMECONTROLLER | SENSOR
		}
	}
}