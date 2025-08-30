using CivOne.Persistence.Impl;
using CivOne.Persistence.Original.Impl;
using CivOne.Persistence.Original.Load;

namespace CivOne.Persistence
{
	public class MapLoaderService
	{
		private IFileMapLoader fileMapLoader;
		private IMapLoaderProvider mapLoaderProvider = new MapLoaderProvider();
		private IMapFactory mapFactory = new MapFactoryImpl();

		public IMap LoadWithOriginal(string filePath)
		{
			fileMapLoader ??= new FileMapLoaderImpl<OriginalMapLoaderImpl>(mapLoaderProvider);

			return fileMapLoader.Load(filePath);
		}
	}
}