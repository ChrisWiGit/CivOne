using System.IO;
using CivOne.Persistence.Original.Load;
using CivOne.Services;

namespace CivOne.Persistence.Impl
{
	public class OriginalFileMapLoaderImpl(string mapFilePath) : IMapLoader
	{
		public IMap Load(int randomSeed)
		{
			using FileStream fs = new(mapFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

			var loader = new OriginalStreamMapLoaderImpl(mapFilePath, randomSeed);

			var map = loader.Load(fs);

			return map;
		}
	}
}