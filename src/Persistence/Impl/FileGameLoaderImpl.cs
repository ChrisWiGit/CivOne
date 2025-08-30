using System.IO;

namespace CivOne.Persistence.Impl
{
	public class FileGameLoaderImpl<T>(
		IGameLoaderProvider provider,
		IGameFactory factory
		) : IFileGameLoader where T : IGameLoader
	{

		public IGame Load(string filePath)
		{
			using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);

			IGameLoader loader = provider.GetLoader<T>();
			IGameData data = loader.Load(fs);

			return factory.Create(data);
		}
	}
}