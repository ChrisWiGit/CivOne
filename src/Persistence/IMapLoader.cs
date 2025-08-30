using System.IO;

namespace CivOne.Persistence
{
	public interface IMapLoader
	{
		IMap Load(Stream stream);
	}
}