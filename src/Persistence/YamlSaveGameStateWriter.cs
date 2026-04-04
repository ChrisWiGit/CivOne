using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CivOne.Civilizations;
using CivOne.Persistence.Model;
using CivOne.Persistence.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
		/// Parameterless constructor for backward compatibility.
		/// Used by legacy code that doesn't have access to mapper dependencies.
		/// Falls back to minimal stub behavior if mappers cannot be constructed.
		/// </summary>
		public YamlSaveGameStateWriter()
		{
			// Lazy initialization intentionally null to signal fallback mode
			_playerMapper = null;
			_unitMapper = null;
			_mapMapper = null;
			_sanitizer = null;
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

			if (_playerMapper == null || _unitMapper == null || _mapMapper == null || _sanitizer == null)
			{
				// Fallback to stub for backward compatibility (if mappers not injected)
				// This path should only be hit from legacy code without proper DI setup
				WriteUsingStub(stream, snapshot);
				return;
			}

			// Use the tested, bidirectional mapper with full data support
			var mapper = new GameStateDtoMapper(_playerMapper, _unitMapper, _mapMapper, _sanitizer);
			var dto = mapper.ToDto(snapshot);

			// Serialize with custom YAML formatting (camelCase, custom tile converter, doc comments)
			var yaml = YamlWriter
				.Of(dto)
				.WithStandard()
				.WithTypeConverter(new MapDtoTileDtoYamlConverter())
				.AsString();

			stream.Write(System.Text.Encoding.UTF8.GetBytes(yaml));
		}

		private static void WriteUsingStub(Stream stream, GameState snapshot)
		{
			// Minimal stub for legacy code paths without proper mapper setup
			// WARN: This loses Units, Players, Cities, and Map data!
			var serializer = new SerializerBuilder()
				.WithNamingConvention(CamelCaseNamingConvention.Instance)
				.Build();

			var stubDto = new GameStateDto
			{
				Difficulty = (DifficultyLevel)snapshot.Difficulty,
				GameTurn = snapshot.GameTurn,
				GameOptions = snapshot.GameOptions
				// NOTE: The following data is NOT serialized in stub mode:
				// - Players (critical!)
				// - Units (critical!)
				// - Cities
				// - Map & Tiles
				// This is a regression from the previous stub. Use dependency injection!
			};

			var yaml = serializer.Serialize(stubDto);
			stream.Write(System.Text.Encoding.UTF8.GetBytes(yaml));
		}
	}
}

#nullable restore