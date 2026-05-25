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
using CivOne.Persistence.Mapper;
using CivOne.Persistence.Resolver;
using CivOne.Persistence.Game;

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
				new CivilizationDtoMapper(RuntimeFactory.Civilizations),
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

		private static class RuntimeFactory
		{
			public static IEnumerable<ICivilization> Civilizations => Reflect.GetCivilizations();
			public static IEnumerable<IAdvance> Advances => Reflect.GetAdvances();

			// units
			public static IEnumerable<IUnit> Units => Reflect.GetUnits();

			// governments
			public static IEnumerable<IGovernment> Governments => Reflect.GetGovernments();
		}

		/// <summary>
		/// Initializes the DTO documentation fields.
		/// This will provide all runtime information into the Doc-Attributes of these DTO to be shown within a YAML file as a comment for the user.
		/// The access to the RuntimeFactory class is always encapsulated because we do not want RuntimeFactory in classes that are tested in unit tests.
		/// </summary>
		private static void InitializeDocLists()
		{
			CivilizationDto.AllLeaderClassNames = [.. RuntimeFactory.Civilizations
				.Select(c => c.Leader.GetType().Name)
				.Distinct()
				.OrderBy(leaderName => leaderName.ToUpperInvariant())];

			UnitDto.AllUnitsClassNames = [.. RuntimeFactory.Units
				.Select(u => u.GetType().Name)
				.Distinct()
				.OrderBy(className => className.ToUpperInvariant())];

			PlayerDto.AllAdvances = [.. RuntimeFactory.Advances
				.OrderBy(a => a.Id)
				.Select(a => $"{a.Id}({a.TranslatedName})")];

			PlayerDto.AllAdvancesInfo = RuntimeFactory.Advances
				.ToDictionary(a => (uint)a.Id, a => a.TranslatedName);

			PlayerDto.AllGovernments = [.. RuntimeFactory.Governments
				.OrderBy(g => g.Id)
				.Select(g => $"{g.Id}({g.TranslatedName})")];
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
