using System.IO;

namespace CivOne.Persistence
{
	public interface IStreamGameLoader
	{
		IGameData Load(Stream stream);
	}
}