using System;
using System.IO;
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
		private readonly IYamlReadValueSanitizer? _sanitizer;

		/// <summary>
		/// Constructor for subclasses that provide their own DTO mapping strategy.
		/// </summary>
		protected YamlSaveGameStateWriter()
		{
		}

		/// <summary>
		/// Constructor for dependency injection of mapper chain components.
		/// Allows flexible instantiation from DI container or test setups.
		/// </summary>
		public YamlSaveGameStateWriter(
			PlayerDtoMapper playerMapper,
			UnitDtoMapper unitMapper,
			MapDtoMapper mapMapper,
			IYamlReadValueSanitizer sanitizer)
		{
			_playerMapper = playerMapper ?? throw new ArgumentNullException(nameof(playerMapper));
			_unitMapper = unitMapper ?? throw new ArgumentNullException(nameof(unitMapper));
			_mapMapper = mapMapper ?? throw new ArgumentNullException(nameof(mapMapper));
			_sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
		}

		public void Write(Stream stream, GameState snapshot)
		{
			ArgumentNullException.ThrowIfNull(stream);
			ArgumentNullException.ThrowIfNull(snapshot);

			var dto = CreateDto(snapshot);

			var yaml = YamlWriter
				.Of(dto)
				.WithStandard()
				.WithTypeConverter(new MapDtoTileDtoYamlConverter())
				.AsString();

			stream.Write(System.Text.Encoding.UTF8.GetBytes(yaml));
		}

		protected virtual GameStateDto CreateDto(GameState snapshot)
		{
			ArgumentNullException.ThrowIfNull(snapshot);

			var mapper = new GameStateDtoMapper(_playerMapper, _unitMapper, _mapMapper, _sanitizer);
			return mapper.ToDto(snapshot);
		}
	}
}

#nullable restore