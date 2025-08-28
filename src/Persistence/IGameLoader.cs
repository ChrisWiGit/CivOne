using System.IO;

namespace CivOne.Persistence
{
	public interface IGameLoader
	{
		IGameData Load(Stream stream);
	}
}