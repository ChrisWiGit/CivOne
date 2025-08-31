using System.Data;
using System.Diagnostics;
using System.IO;
using CivOne.IO;
using CivOne.Persistence.Impl;
using CivOne.Persistence.Original.Load;
using CivOne.Services;
using CivOne.Services.Impl;

namespace CivOne.Persistence.Original.Impl
{
	public class OriginalGameLoaderImpl : IStreamGameLoader
	{
		protected IStreamToSaveDataService _streamToSaveDataService = new StreamToSaveDataService();
		protected IOriginalGameTime _gameTime = new GameTimeServiceImpl();


		protected SaveData saveData;

		public OriginalGameLoaderImpl(
			IStreamToSaveDataService streamToSaveDataService, IOriginalGameTime gameTime)
		{
			_streamToSaveDataService = streamToSaveDataService ?? _streamToSaveDataService;
			_gameTime = gameTime ?? _gameTime;
		}

		public OriginalGameLoaderImpl() : this(null, null) { }

		public IGameData Load(Stream stream)
		{
			// Civ original game loading code goes here
			// SaveAdapter.Get.cs: Load()
			saveData = _streamToSaveDataService.StreamToSaveData<SaveData>(stream);

			return new GameDataQueryAdapter(saveData, _gameTime);
		}
	}

}
