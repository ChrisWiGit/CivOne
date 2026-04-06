using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Governments;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Builds the mapper chain for YAML load (FromDto) using runtime-capable factories.
	/// Unlike <see cref="YamlMapperDependenciesFactory"/>, this factory is intended for
	/// deserialization and does not require an existing game instance.
	/// </summary>
	public static class YamlLoadMapperDependenciesFactory
	{
		public static YamlMapperDependencies Create(IYamlReadValueSanitizer sanitizer)
		{
			ArgumentNullException.ThrowIfNull(sanitizer);

			var map = Map.Instance;
			var unitMapper = new UnitDtoMapper(new RuntimeUnitFactory(), sanitizer);
			var mapMapper = new MapDtoMapper(
				new RuntimeMapFactory(map),
				new RuntimeTileDtoMapper(map, new RuntimeTerrainFactory()),
				0);
			var cityMapper = new CityDtoMapper(new ProductionDtoMapper(new GameReflect()), new CityDefinitionResolver(), sanitizer);
			// NullPlayerGame: no game instance exists yet during load; PlayerDtoMapper
			// requires IPlayerGame only for ToDto (save path), which is never called here.
			// NoopPlayerOwnerResolver: owner resolution is used exclusively in ToDto to
			// filter units by player; always returning false is safe and emits all units.
			var playerMapper = new PlayerDtoMapper(
				new NullPlayerGame(),
				new NoopPlayerOwnerResolver(),
				new RuntimePlayerFactory(),
				new CivilizationDtoMapper(Common.Civilizations),
				new PalaceDtoMapper(sanitizer),
				cityMapper,
				unitMapper,
				new RuntimeAdvanceResolver(),
				new RuntimeGovernmentResolver(),
				sanitizer);

			InitializeDocLists();
			return new YamlMapperDependencies(playerMapper, unitMapper, mapMapper, sanitizer);
		}

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

		private sealed class RuntimeAdvanceResolver : IAdvanceResolver
		{
			public IAdvance ResolveById(uint id)
			{
				return Common.Advances.FirstOrDefault(a => a.Id == id)
					?? throw new InvalidOperationException($"Advance with ID {id} was not found.");
			}
		}

		private sealed class RuntimeGovernmentResolver : IGovernmentResolver
		{
			public IGovernment ResolveById(byte id)
			{
				return Reflect.GetGovernments().FirstOrDefault(g => g.Id == id)
					?? throw new InvalidOperationException($"Government with ID {id} was not found.");
			}
		}

		private sealed class NoopPlayerOwnerResolver : IPlayerOwnerResolver
		{
			public bool TryResolveOwnerId(IPlayer player, out byte ownerId)
			{
				ownerId = 0;
				return false;
			}
		}

		private sealed class NullPlayerGame : IPlayerGame
		{
			public bool Started => false;
			public ushort GameTurn => 0;
			public int Difficulty => 0;

			public Player HumanPlayer => null;
			public Player CurrentPlayer => null;
			public IEnumerable<Player> Players => [];

			public byte PlayerNumber(Player player) => 0;
			public Player GetPlayer(byte number) => null;

			public City[] GetCities() => [];
			public Units.IUnit[] GetUnits() => [];
			public void DisbandUnit(Units.IUnit unit) { }

			public bool WonderObsolete<T>() where T : Wonders.IWonder, new() => false;
			public bool WonderBuilt<T>() where T : Wonders.IWonder => false;
			public Wonders.IWonder[] BuiltWonders => [];

			public void SetAdvanceOrigin(IAdvance advance, Player player) { }
		}
	}
}
