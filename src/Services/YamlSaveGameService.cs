using System;
using CivOne.Persistence;
using CivOne.Persistence.Factories;
using CivOne.Persistence.Model;

namespace CivOne.Services
{
	public sealed class YamlSaveGameServiceFactory(IAtomicFileReplacementService? atomicFileReplacementService = null) : IYamlSaveGameServiceFactory
	{
		private readonly IAtomicFileReplacementService? _atomicFileReplacementService = atomicFileReplacementService;

		public IYamlSaveGameService Create(Game game)
		{
			return new YamlSaveGameService(game, _atomicFileReplacementService);
		}
	}

	public class YamlSaveGameService(Game game, IAtomicFileReplacementService? atomicFileReplacementService = null) : IYamlSaveGameService
	{
		private readonly Game _game = game ?? throw new ArgumentNullException(nameof(game));
		private readonly IAtomicFileReplacementService _atomicFileReplacementService = atomicFileReplacementService ?? new AtomicFileReplacementService();

		public void SaveCos(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));

			_game.SaveMetaData.DisplayName = _game.SaveMetaDataService.BuildDisplayName(_game.Difficulty, _game.HumanPlayer, _game.GameTurn);

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
				stream => writer.Write(stream, GameStateHandler.Create(_game), _game.SaveMetaData));

			_game.MarkAsYamlSaveSource();
		}
	}
}
