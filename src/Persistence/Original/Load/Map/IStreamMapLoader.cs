using System.IO;

namespace CivOne.Persistence
{
	public interface IStreamMapLoader
	{
		IMap Load(Stream stream);
	}
}