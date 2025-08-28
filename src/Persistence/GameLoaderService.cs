using CivOne.Persistence.Impl;

namespace CivOne.Persistence
{
	public class GameLoaderService
	{
		private IFileGameLoader fileGameLoader;
		private IGameLoaderProvider gameLoaderProvider = new GameLoaderProvider();
		private IGameFactory gameFactory = new GameFactoryImpl();

		public IGame LoadWithOriginal(string filePath)
		{
			fileGameLoader ??= new FileGameLoaderImpl(gameLoaderProvider, gameFactory);

			return fileGameLoader.Load<OriginalGameLoaderImpl>(filePath);
		}
	}
}