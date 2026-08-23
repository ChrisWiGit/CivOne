// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Graphics;

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Default <see cref="IMapResourceProvider"/> that delegates to the global
	/// <see cref="Resources"/> singleton. Used by <see cref="CivOne.Map"/> when no
	/// test provider is injected.
	/// </summary>
	internal sealed class DefaultMapResourceProvider : IMapResourceProvider
	{
		public Picture GetPicture(string filename) => Resources.Instance[filename];
	}
}
