using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Services
{
	internal sealed class SveSaveCompatibilityResult
	{
		public static SveSaveCompatibilityResult Compatible { get; } = new(true, string.Empty);

		public bool CanSaveAsSve { get; }
		public string Reason { get; }

		public SveSaveCompatibilityResult(bool canSaveAsSve, string reason)
		{
			CanSaveAsSve = canSaveAsSve;
			Reason = reason ?? string.Empty;
		}
	}

	internal sealed class SveSaveCompatibilitySnapshot
	{
		public bool IsLoadedFromYaml { get; }
		public int PlayerCount { get; }
		public int MapWidth { get; }
		public int MapHeight { get; }
		public int CityCount { get; }
		public IReadOnlyCollection<int> TradeCityCountsPerCity { get; }
		public IReadOnlyCollection<byte> CityOwners { get; }
		public IReadOnlyCollection<byte> UnitOwners { get; }

		private SveSaveCompatibilitySnapshot(
			bool isLoadedFromYaml,
			int playerCount,
			int mapWidth,
			int mapHeight,
			int cityCount,
			IReadOnlyCollection<int> tradeCityCountsPerCity,
			IReadOnlyCollection<byte> cityOwners,
			IReadOnlyCollection<byte> unitOwners)
		{
			IsLoadedFromYaml = isLoadedFromYaml;
			PlayerCount = playerCount;
			MapWidth = mapWidth;
			MapHeight = mapHeight;
			CityCount = cityCount;
			TradeCityCountsPerCity = tradeCityCountsPerCity ?? Array.Empty<int>();
			CityOwners = cityOwners ?? Array.Empty<byte>();
			UnitOwners = unitOwners ?? Array.Empty<byte>();
		}

		public static SveSaveCompatibilitySnapshotBuilder Builder() => new();
	}

	internal sealed class SveSaveCompatibilitySnapshotBuilder
	{
		private bool _isLoadedFromYaml;
		private int _playerCount;
		private int _mapWidth;
		private int _mapHeight;
		private int _cityCount;
		private IReadOnlyCollection<int> _tradeCityCountsPerCity = Array.Empty<int>();
		private IReadOnlyCollection<byte> _cityOwners = Array.Empty<byte>();
		private IReadOnlyCollection<byte> _unitOwners = Array.Empty<byte>();

		public SveSaveCompatibilitySnapshotBuilder FromYamlSource(bool isLoadedFromYaml)
		{
			_isLoadedFromYaml = isLoadedFromYaml;
			return this;
		}

		public SveSaveCompatibilitySnapshotBuilder WithPlayerCount(int playerCount)
		{
			_playerCount = playerCount;
			return this;
		}

		public SveSaveCompatibilitySnapshotBuilder WithMapSize(int mapWidth, int mapHeight)
		{
			_mapWidth = mapWidth;
			_mapHeight = mapHeight;
			return this;
		}

		public SveSaveCompatibilitySnapshotBuilder WithCityCount(int cityCount)
		{
			_cityCount = cityCount;
			return this;
		}

		public SveSaveCompatibilitySnapshotBuilder WithTradeCityCountsPerCity(IReadOnlyCollection<int> tradeCityCountsPerCity)
		{
			_tradeCityCountsPerCity = tradeCityCountsPerCity ?? Array.Empty<int>();
			return this;
		}

		public SveSaveCompatibilitySnapshotBuilder WithCityOwners(IReadOnlyCollection<byte> cityOwners)
		{
			_cityOwners = cityOwners ?? Array.Empty<byte>();
			return this;
		}

		public SveSaveCompatibilitySnapshotBuilder WithUnitOwners(IReadOnlyCollection<byte> unitOwners)
		{
			_unitOwners = unitOwners ?? Array.Empty<byte>();
			return this;
		}

		public SveSaveCompatibilitySnapshot Build()
		{
			return new SveSaveCompatibilitySnapshot(
				_isLoadedFromYaml,
				_playerCount,
				_mapWidth,
				_mapHeight,
				_cityCount,
				_tradeCityCountsPerCity,
				_cityOwners,
				_unitOwners);
		}
	}

	internal interface ISveSaveCompatibilityService
	{
		SveSaveCompatibilityResult Evaluate(SveSaveCompatibilitySnapshot snapshot);
	}

	internal sealed class SveSaveCompatibilityService : ISveSaveCompatibilityService
	{
		private const int SveMaxPlayers = 8;
		private const int SveMapWidth = 80;
		private const int SveMapHeight = 50;
		private const int SveMaxCities = 128;
		private const int SveMaxTradeCitiesPerCity = 3;
		private const int SveMaxUnitsPerPlayer = 128;

		public SveSaveCompatibilityResult Evaluate(SveSaveCompatibilitySnapshot snapshot)
		{
			ArgumentNullException.ThrowIfNull(snapshot);

			if (snapshot.IsLoadedFromYaml)
			{
				return new SveSaveCompatibilityResult(false, "Game was loaded from YAML/COS and is locked to YAML/COS saves.");
			}

			if (snapshot.MapWidth != SveMapWidth || snapshot.MapHeight != SveMapHeight)
			{
				return new SveSaveCompatibilityResult(false, $"Map size {snapshot.MapWidth}x{snapshot.MapHeight} is not supported by SVE ({SveMapWidth}x{SveMapHeight} required).");
			}

			if (snapshot.PlayerCount > SveMaxPlayers)
			{
				return new SveSaveCompatibilityResult(false, $"SVE supports at most {SveMaxPlayers} players, current game has {snapshot.PlayerCount}.");
			}

			if (snapshot.CityCount > SveMaxCities)
			{
				return new SveSaveCompatibilityResult(false, $"SVE supports at most {SveMaxCities} cities, current game has {snapshot.CityCount}.");
			}

			if (snapshot.TradeCityCountsPerCity.Any(count => count > SveMaxTradeCitiesPerCity))
			{
				return new SveSaveCompatibilityResult(false, $"SVE supports at most {SveMaxTradeCitiesPerCity} trade cities per city.");
			}

			if (snapshot.CityOwners.Any(owner => owner >= SveMaxPlayers))
			{
				return new SveSaveCompatibilityResult(false, $"At least one city has an owner index outside SVE range 0-{SveMaxPlayers - 1}.");
			}

			if (snapshot.UnitOwners.Any(owner => owner >= SveMaxPlayers))
			{
				return new SveSaveCompatibilityResult(false, $"At least one unit has an owner index outside SVE range 0-{SveMaxPlayers - 1}.");
			}

			var unitsByOwner = snapshot.UnitOwners
				.GroupBy(owner => owner)
				.ToDictionary(group => group.Key, group => group.Count());

			if (unitsByOwner.Any(entry => entry.Value > SveMaxUnitsPerPlayer))
			{
				var firstOverflow = unitsByOwner.First(entry => entry.Value > SveMaxUnitsPerPlayer);
				return new SveSaveCompatibilityResult(false, $"Player {firstOverflow.Key} has {firstOverflow.Value} units, SVE supports at most {SveMaxUnitsPerPlayer} units per player.");
			}

			return SveSaveCompatibilityResult.Compatible;
		}
	}
}