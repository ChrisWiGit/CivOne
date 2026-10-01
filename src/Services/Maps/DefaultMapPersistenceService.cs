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