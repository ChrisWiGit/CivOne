using System.IO;

namespace CivOne.Persistence.Impl
{
	public class FileGameLoaderImpl(
		IGameLoaderProvider provider,
		IGameFactory factory
		) : IFileGameLoader
	{

		public IGame Load<TLoader>(string filePath) where TLoader : IGameLoader
		{
			using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);

			IGameLoader loader = provider.GetLoader<TLoader>();
			IGameData data = loader.Load(fs);

			return factory.Create(data);
		}
	}
}