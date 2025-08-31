using System.IO;

namespace CivOne.Persistence
{
	public interface IMapLoader
	{
		IMap Load(int randomSeed);
	}
}