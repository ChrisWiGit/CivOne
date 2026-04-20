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
		public int ReplayDataLengthBytes { get; }
		public bool HasInvalidTradeCityReferences { get; }
		public bool HasInvalidUnitHomeCityReferences { get; }
		public bool HasOutOfBoundsCityCoordinates { get; }
		public bool HasOutOfBoundsUnitCoordinates { get; }
		public bool HasOutOfBoundsUnitGotoCoordinates { get; }
		public IReadOnlyCollection<int> TradeCityCountsPerCity { get; }
		public IReadOnlyCollection<byte> CityOwners { get; }
		public IReadOnlyCollection<byte> UnitOwners { get; }

		internal SveSaveCompatibilitySnapshot(
			bool isLoadedFromYaml,
			int playerCount,
			int mapWidth,
			int mapHeight,
			int cityCount,
			int replayDataLengthBytes,
			bool hasInvalidTradeCityReferences,
			bool hasInvalidUnitHomeCityReferences,
			bool hasOutOfBoundsCityCoordinates,
			bool hasOutOfBoundsUnitCoordinates,
			bool hasOutOfBoundsUnitGotoCoordinates,
			IReadOnlyCollection<int> tradeCityCountsPerCity,
			IReadOnlyCollection<byte> cityOwners,
			IReadOnlyCollection<byte> unitOwners)
		{
			IsLoadedFromYaml = isLoadedFromYaml;
			PlayerCount = playerCount;
			MapWidth = mapWidth;
			MapHeight = mapHeight;
			CityCount = cityCount;
			ReplayDataLengthBytes = replayDataLengthBytes;
			HasInvalidTradeCityReferences = hasInvalidTradeCityReferences;
			HasInvalidUnitHomeCityReferences = hasInvalidUnitHomeCityReferences;
			HasOutOfBoundsCityCoordinates = hasOutOfBoundsCityCoordinates;
			HasOutOfBoundsUnitCoordinates = hasOutOfBoundsUnitCoordinates;
			HasOutOfBoundsUnitGotoCoordinates = hasOutOfBoundsUnitGotoCoordinates;
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
		private int _replayDataLengthBytes;
		private bool _hasInvalidTradeCityReferences;
		private bool _hasInvalidUnitHomeCityReferences;
		private bool _hasOutOfBoundsCityCoordinates;
		private bool _hasOutOfBoundsUnitCoordinates;
		private bool _hasOutOfBoundsUnitGotoCoordinates;
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

		public SveSaveCompatibilitySnapshotBuilder WithReplayDataLengthBytes(int replayDataLengthBytes)
		{
			_replayDataLengthBytes = replayDataLengthBytes;
			return this;
		}

		public SveSaveCompatibilitySnapshotBuilder WithInvalidTradeCityReferences(bool hasInvalidTradeCityReferences)
		{
			_hasInvalidTradeCityReferences = hasInvalidTradeCityReferences;
			return this;
		}

		public SveSaveCompatibilitySnapshotBuilder WithInvalidUnitHomeCityReferences(bool hasInvalidUnitHomeCityReferences)
		{
			_hasInvalidUnitHomeCityReferences = hasInvalidUnitHomeCityReferences;
			return this;
		}

		public SveSaveCompatibilitySnapshotBuilder WithOutOfBoundsCityCoordinates(bool hasOutOfBoundsCityCoordinates)
		{
			_hasOutOfBoundsCityCoordinates = hasOutOfBoundsCityCoordinates;
			return this;
		}

		public SveSaveCompatibilitySnapshotBuilder WithOutOfBoundsUnitCoordinates(bool hasOutOfBoundsUnitCoordinates)
		{
			_hasOutOfBoundsUnitCoordinates = hasOutOfBoundsUnitCoordinates;
			return this;
		}

		public SveSaveCompatibilitySnapshotBuilder WithOutOfBoundsUnitGotoCoordinates(bool hasOutOfBoundsUnitGotoCoordinates)
		{
			_hasOutOfBoundsUnitGotoCoordinates = hasOutOfBoundsUnitGotoCoordinates;
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
				_replayDataLengthBytes,
				_hasInvalidTradeCityReferences,
				_hasInvalidUnitHomeCityReferences,
				_hasOutOfBoundsCityCoordinates,
				_hasOutOfBoundsUnitCoordinates,
				_hasOutOfBoundsUnitGotoCoordinates,
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
		private const int SveMaxReplayDataLengthBytes = 4096;

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

			if (snapshot.ReplayDataLengthBytes > SveMaxReplayDataLengthBytes)
			{
				return new SveSaveCompatibilityResult(false, $"Replay data is {snapshot.ReplayDataLengthBytes} bytes, SVE supports at most {SveMaxReplayDataLengthBytes} bytes.");
			}

			if (snapshot.TradeCityCountsPerCity.Any(count => count > SveMaxTradeCitiesPerCity))
			{
				return new SveSaveCompatibilityResult(false, $"SVE supports at most {SveMaxTradeCitiesPerCity} trade cities per city.");
			}

			if (snapshot.HasInvalidTradeCityReferences)
			{
				return new SveSaveCompatibilityResult(false, "At least one trade-city reference does not point to an existing city.");
			}

			if (snapshot.HasInvalidUnitHomeCityReferences)
			{
				return new SveSaveCompatibilityResult(false, "At least one unit has a home-city reference that does not point to an existing city.");
			}

			if (snapshot.HasOutOfBoundsCityCoordinates)
			{
				return new SveSaveCompatibilityResult(false, "At least one city has coordinates outside the SVE map bounds.");
			}

			if (snapshot.HasOutOfBoundsUnitCoordinates)
			{
				return new SveSaveCompatibilityResult(false, "At least one unit has coordinates outside the SVE map bounds.");
			}

			if (snapshot.HasOutOfBoundsUnitGotoCoordinates)
			{
				return new SveSaveCompatibilityResult(false, "At least one unit has goto coordinates outside the SVE map bounds.");
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