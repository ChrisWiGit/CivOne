using CivOne.Persistence.Impl;
using CivOne.Persistence.Original.Impl;
using CivOne.Services;

namespace CivOne.Persistence
{
	public class GameLoaderService
	{
		protected static ILoggerService Logger => LoggerProvider.GetLogger();

		public IGame LoadWithOriginal(string filePath, string mapFilePath)
		{
			IMapLoader mapLoader = new OriginalFileMapLoaderImpl(mapFilePath);

			var fileGameLoader = new OriginalFileGameLoaderImpl(mapLoader);

			return fileGameLoader.Load(filePath);
		}
	
	}
}