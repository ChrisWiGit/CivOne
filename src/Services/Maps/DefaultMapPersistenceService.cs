// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.IO;

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Default <see cref="IMapPersistenceService"/> that writes the byte stream
	/// to disk via <see cref="File.Open(string, FileMode)"/> + <see cref="BinaryWriter"/>,
	/// preserving the legacy behaviour of <see cref="CivOne.Map.SaveMap(string)"/>.
	/// </summary>
	internal sealed class DefaultMapPersistenceService : IMapPersistenceService
	{
		public void WriteAllBytes(string filename, byte[] bytes)
		{
			using BinaryWriter bw = new(File.Open(filename, FileMode.Create));
			bw.Write(bytes);
		}
	}
}
