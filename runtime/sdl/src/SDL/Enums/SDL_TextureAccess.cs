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
		public enum SDL_TextureAccess
		{
			SDL_TEXTUREACCESS_STATIC,
			SDL_TEXTUREACCESS_STREAMING,
			SDL_TEXTUREACCESS_TARGET,
		}
	}
}