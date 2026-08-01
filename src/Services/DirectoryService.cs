// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.IO;

namespace CivOne.Services
{
	/// <summary>
	/// Default <see cref="IDirectoryService"/> implementation, backed by <see cref="Directory"/>.
	/// </summary>
	public class DirectoryService : IDirectoryService
	{
		/// <inheritdoc/>
		public void CreateDirectory(string path) => Directory.CreateDirectory(path);
	}
}
