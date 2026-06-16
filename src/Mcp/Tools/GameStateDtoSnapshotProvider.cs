using System;
using System.Diagnostics.CodeAnalysis;
using CivOne.Mcp.Automation;
using CivOne.Persistence.Factories;
using CivOne.Persistence.Mapper;
using CivOne.Persistence.Model;

namespace CivOne.Mcp.Tools
{
	public sealed class GameStateDtoSnapshotProvider : IGameStateDtoSnapshotProvider
	{
		private readonly IMcpGameTickProvider _gameTickProvider;
		private readonly GameStateHandler _gameStateHandler;
		private readonly IYamlMapperDependenciesFactory _mapperDependenciesFactory;

		private uint _cachedTick = uint.MaxValue;
		private GameStateDto? _cachedSnapshot;

		public bool TryGetSnapshot([NotNullWhen(true)] out GameStateDto? snapshot, [NotNullWhen(false)] out string? errorCode, [NotNullWhen(false)] out string? errorMessage)
		{
			snapshot = null;
			errorCode = null;
			errorMessage = null;

			Game gameInstance = Game.Instance;
			if (gameInstance == null)
			{
				errorCode = "NO_GAME";
				errorMessage = "No active game session.";
				return false;
			}

			uint tick = _gameTickProvider.CurrentTick;
			if (_cachedSnapshot != null && tick == _cachedTick)
			{
				snapshot = _cachedSnapshot;
				return true;
			}

			var gameState = _gameStateHandler.Create(gameInstance);
			var mapperDependencies = _mapperDependenciesFactory.Create(gameInstance);
			var mapper = new GameStateDtoMapper(
				mapperDependencies.PlayerMapper,
				mapperDependencies.UnitMapper,
				mapperDependencies.MapMapper,
				mapperDependencies.GlobalWarmingMapper,
				mapperDependencies.Sanitizer);

			snapshot = mapper.ToDto(gameState);
			_cachedSnapshot = snapshot;
			_cachedTick = tick;
			return true;
		}

		public GameStateDtoSnapshotProvider(
			IMcpGameTickProvider gameTickProvider,
			GameStateHandler gameStateHandler,
			IYamlMapperDependenciesFactory mapperDependenciesFactory)
		{
			_gameTickProvider = gameTickProvider ?? throw new ArgumentNullException(nameof(gameTickProvider));
			_gameStateHandler = gameStateHandler ?? throw new ArgumentNullException(nameof(gameStateHandler));
			_mapperDependenciesFactory = mapperDependenciesFactory ?? throw new ArgumentNullException(nameof(mapperDependenciesFactory));
		}
	}
}
