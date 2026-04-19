using System;
using CivOne.Persistence;
using CivOne.Persistence.Factories;
using CivOne.Persistence.Model;

namespace CivOne.Services
{
	public class YamlSaveGameService : IYamlSaveGameService
	{
		private readonly Game _game;
		private readonly IAtomicFileReplacementService _atomicFileReplacementService;

		public YamlSaveGameService(Game game, IAtomicFileReplacementService atomicFileReplacementService = null)
		{
			_game = game ?? throw new ArgumentNullException(nameof(game));
			_atomicFileReplacementService = atomicFileReplacementService ?? new AtomicFileReplacementService();
		}

		public void SaveCos(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));

			_game.SaveMetaData.DisplayName = _game.SaveMetaDataService.BuildDisplayName(_game.Difficulty, _game.HumanPlayer, _game.GameTurn);

			GameStateHandler gameState = new();
			var mapperDependencies = YamlMapperDependenciesFactory
				.CreateDefault()
				.Create(_game);
			YamlSaveGameStateWriter writer = new(
				mapperDependencies.PlayerMapper,
				mapperDependencies.UnitMapper,
				mapperDependencies.MapMapper,
				mapperDependencies.GlobalWarmingMapper,
				mapperDependencies.Sanitizer);

			_atomicFileReplacementService.ReplaceFile(
				filePath,
				stream => writer.Write(stream, gameState.Create(_game), _game.SaveMetaData));
		}
	}
}
