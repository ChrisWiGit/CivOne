using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Advances;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Governments;
using CivOne.Persistence.Model;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Persistence.Factories;

namespace CivOne.Persistence.Factories
{
	public sealed record YamlMapperDependencies(
		PlayerDtoMapper PlayerMapper,
		UnitDtoMapper UnitMapper,
		MapDtoMapper MapMapper,
		DtoMapper<GlobalWarmingDto, GameState> GlobalWarmingMapper,
		IValueSanitizer Sanitizer);

	public interface IYamlMapperDependenciesFactory
	{
		YamlMapperDependencies Create(IPlayerGame gameInstance);
	}

	public sealed class YamlMapperDependenciesFactory(
		IReflect reflect,
		IValueSanitizer sanitizer) : IYamlMapperDependenciesFactory
	{
		private readonly IReflect _reflect = reflect ?? throw new ArgumentNullException(nameof(reflect));
		private readonly IValueSanitizer _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));

		public YamlMapperDependencies Create(IPlayerGame gameInstance)
		{
			ArgumentNullException.ThrowIfNull(gameInstance);

			var unitMapper = new UnitDtoMapper(new NotSupportedUnitFactory(), _sanitizer);
			var mapMapper = new MapDtoMapper(new NotSupportedMapFactory(), new DefaultTileDtoMapper(new NotSupportedTileFactory()));
			var globalWarmingMapper = new GlobalWarmingDtoMapper(_sanitizer);
			var cityMapper = new CityDtoMapper(new ProductionDtoMapper(_reflect), new CityDefinitionResolver(), _sanitizer);
			var playerMapper = new PlayerDtoMapper(
				gameInstance,
				new GamePlayerOwnerResolver(gameInstance),
				new NotSupportedPlayerFactory(),
				new CivilizationDtoMapper(Common.Civilizations),
				new PalaceDtoMapper(_sanitizer),
				cityMapper,
				unitMapper,
				new RuntimeAdvanceResolver(),
				new RuntimeGovernmentResolver(),
				_sanitizer
			);

			InitializeDocLists();

			return new YamlMapperDependencies(playerMapper, unitMapper, mapMapper, globalWarmingMapper, _sanitizer);
		}

		public static IYamlMapperDependenciesFactory CreateDefault() => new YamlMapperDependenciesFactory(new GameReflect(), CreateValueSanitizer());

		private static IValueSanitizer CreateValueSanitizer()
		{
			return new ValueSanitizer(new RuntimeLogger());
		}

		/// <summary>
		/// Initializes the DTO documentation fields.
		/// This will provide all runtime information into the Doc-Attributes of these DTO to be shown within a YAML file as a comment for the user.
		/// The access to the Common class is always encapsulated because we do not want Common in classes that are tested in unit tests.
		/// </summary>
		private static void InitializeDocLists()
		{
			CivilizationDto.AllLeaderClassNames = [.. Common.Civilizations
				.Select(c => c.Leader.GetType().Name)
				.Distinct()
				.OrderBy(leaderName => leaderName.ToUpperInvariant())];

			UnitDto.AllUnitsClassNames = [.. Reflect.GetUnits()
				.Select(u => u.GetType().Name)
				.Distinct()
				.OrderBy(className => className.ToUpperInvariant())];

			PlayerDto.AllAdvances = [.. Common.Advances
				.OrderBy(a => a.Id)
				.Select(a => $"{a.Id}({a.Name})")];

			PlayerDto.AllAdvancesInfo = Common.Advances
				.ToDictionary(a => (uint)a.Id, a => a.Name);

			PlayerDto.AllGovernments = [.. Reflect.GetGovernments()
				.OrderBy(g => g.Id)
				.Select(g => $"{g.Id}({g.Name})")];
		}

		private sealed class NotSupportedPlayerFactory : IPlayerFactory
		{
			public IPlayerRestorable Create(ICivilization civilization, PlayerDto dto)
				=> throw new NotSupportedException("Not used when writing YAML (ToDto only).");
		}

		private sealed class NotSupportedUnitFactory : IUnitFactory
		{
			public IUnitRestorable Create(string className, byte player, Guid? homeCityGuid)
				=> throw new NotSupportedException("Not used when writing YAML (ToDto only).");
		}

		private sealed class NotSupportedMapFactory : IMapFactory
		{
			public IMapTiles CreateMap(int width, int height, uint terrainSeed)
				=> throw new NotSupportedException("Not used when writing YAML (ToDto only).");
		}

		private sealed class NotSupportedTileFactory : ITileFactory
		{
			public ITile CreateTile(int x, int y, Terrain terrain)
				=> throw new NotSupportedException("Not used when writing YAML (ToDto only).");
		}
	}
}
