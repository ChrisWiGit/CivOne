namespace CivOne.Services.Maps
{
	/// <summary>
	/// Persists the byte stream produced by <see cref="CivOne.Map.SaveMap(string)"/>
	/// to a backing store. Abstracted away from <see cref="System.IO.File"/> so tests
	/// can capture the written bytes in memory instead of touching the file system.
	/// </summary>
	public interface IMapPersistenceService
	{
		/// <summary>
		/// Writes <paramref name="bytes"/> to the given target identifier, replacing
		/// any existing content. The semantics must match
		/// <c>new BinaryWriter(File.Open(filename, FileMode.Create)).Write(bytes)</c>
		/// (raw bytes, no length prefix, file truncated on open).
		/// </summary>
		void WriteAllBytes(string filename, byte[] bytes);
	}
}