using System.IO;
using CivOne.Persistence.Original.Impl;
using CivOne.Persistence.Original.Load;

namespace CivOne.Persistence.Impl
{
	public class OriginalFileGameLoaderImpl(IMapLoader mapLoader) : IFileGameLoader
	{
		public IGame Load(string filePath)
		{
			using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);

			var loader = new OriginalGameLoaderImpl();
			IGameData gameData = loader.Load(fs);

			var map = mapLoader.Load(gameData.RandomSeed);

			return new Game.GameBuilder(gameData, map)
				.SetupAll()
				.Build();
		}
	}
}