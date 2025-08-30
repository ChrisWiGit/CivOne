using System.IO;

namespace CivOne.Persistence.Impl
{
	public class FileMapLoaderImpl<T>(
		IMapLoaderProvider provider
		) : IFileMapLoader where T : IMapLoader
	{

		public IMap Load(string filePath)
		{
			using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);

			var loader = provider.GetLoader<T>();
			var map = loader.Load(fs);

			return map;
		}
	}
}