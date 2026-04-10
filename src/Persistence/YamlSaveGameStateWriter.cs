using System;
using System.IO;
using CivOne.Persistence.Factories;
using CivOne.Persistence.Model;
using CivOne.Persistence.Yaml;

#nullable enable

namespace CivOne.Persistence
{
	/// <summary>
	/// Serializes GameState snapshots to YAML format using the tested GameStateDtoMapper.
	/// Replaces previous stub implementation with full bidirectional mapping support.
	/// </summary>
	public class YamlSaveGameStateWriter : IGameStateWriter
	{
		private readonly PlayerDtoMapper? _playerMapper;
		private readonly UnitDtoMapper? _unitMapper;
		private readonly MapDtoMapper? _mapMapper;
		private readonly DtoMapper<GlobalWarmingDto, GameState>? _globalWarmingMapper;
		private readonly IYamlReadValueSanitizer? _sanitizer;

		private readonly SaveGameMetaDataDtoFactory _metaDataFactory;

		/// <summary>
		/// Constructor for subclasses that provide their own DTO mapping strategy.
		/// </summary>
		protected YamlSaveGameStateWriter()
		{
			_metaDataFactory = new SaveGameMetaDataDtoFactory();
		}

		/// <summary>
		/// Constructor for dependency injection of mapper chain components.
		/// Allows flexible instantiation from DI container or test setups.
		/// </summary>
		public YamlSaveGameStateWriter(
			PlayerDtoMapper playerMapper,
			UnitDtoMapper unitMapper,
			MapDtoMapper mapMapper,
			DtoMapper<GlobalWarmingDto, GameState> globalWarmingMapper,
			IYamlReadValueSanitizer sanitizer,
			SaveGameMetaDataDtoFactory? metaDataFactory = null
			)
		{
			_playerMapper = playerMapper ?? throw new ArgumentNullException(nameof(playerMapper));
			_unitMapper = unitMapper ?? throw new ArgumentNullException(nameof(unitMapper));
			_mapMapper = mapMapper ?? throw new ArgumentNullException(nameof(mapMapper));
			_globalWarmingMapper = globalWarmingMapper ?? throw new ArgumentNullException(nameof(globalWarmingMapper));
			_sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
			_metaDataFactory = metaDataFactory ?? new SaveGameMetaDataDtoFactory();
		}

		public void Write(Stream stream, GameState snapshot)
		{
			Write(stream, snapshot, null);
		}

		public void Write(Stream stream, GameState snapshot, SaveFileMetaData? saveMetaData)
		{
			ArgumentNullException.ThrowIfNull(stream);
			ArgumentNullException.ThrowIfNull(snapshot);

			var fileDto = new SaveGameFileRootDto
			{
				FormatVersion = SaveGameFileRootDto.CurrentFormatVersion,
				Meta = saveMetaData is null ? null : _metaDataFactory.CreateFromRuntime(saveMetaData),
				GameState = CreateDto(snapshot)
			};

			var yaml = YamlWriter
				.Of(fileDto)
				.WithStandard()
				.WithTypeConverter(new MapDtoTileDtoYamlConverter())
				.AsString();

			stream.Write(System.Text.Encoding.UTF8.GetBytes(yaml));
		}

		protected virtual GameStateDto CreateDto(GameState snapshot)
		{
			ArgumentNullException.ThrowIfNull(snapshot);

			var mapper = new GameStateDtoMapper(
				_playerMapper,
				_unitMapper,
				_mapMapper,
				_globalWarmingMapper,
				_sanitizer);
			return mapper.ToDto(snapshot);
		}
	}
}

#nullable restore