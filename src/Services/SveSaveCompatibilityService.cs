using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Services
{
	/// <summary>
	/// Represents the result of evaluating whether a game snapshot is compatible with SVE save format.
	/// </summary>
	internal sealed class SveSaveCompatibilityResult(bool canSaveAsSve, string reason)
	{
		/// <summary>
		/// Indicates that the game snapshot is compatible with SVE save format and can be saved as an SVE save.
		/// </summary>
		public static SveSaveCompatibilityResult Compatible { get; } = new(true, string.Empty);

		/// <summary>
		/// Indicates whether the game snapshot is compatible with SVE save format and can be saved as an SVE save. If false, the Reason property will contain an explanation of why it is not compatible.
		/// </summary>
		public bool CanSaveAsSve { get; } = canSaveAsSve;
		
		/// <summary>
		/// If CanSaveAsSve is false, this property contains a human-readable explanation of
		/// why the game snapshot is not compatible with SVE save format. If CanSaveAsSve is true, this property will be an empty string.
		/// </summary>
		public string Reason { get; } = reason ?? string.Empty;
	}

	/// <summary>
	/// Represents a snapshot of game state and metadata relevant for evaluating SVE save compatibility. 
	/// This is a plain data holder with no logic, used to pass information
	/// Use the Builder to create instances of this class with a fluent API. 
	/// 
	/// <example>
	/// var service = new SveSaveCompatibilityService();
	/// var snapshot = SveSaveCompatibilitySnapshot.Builder()
	///     .FromYamlSource(_loadedFromYamlSaveSource)
	///     .WithPlayerCount(_players.Length);
	/// //     // Set other properties as needed...
	///	var result = service.Evaluate(snapshot);
	/// </example>
	/// </summary>	
	internal sealed class SveSaveCompatibilitySnapshot
	{
		/// <summary>
		/// Indicates whether the game was loaded from a YAML/COS save source. SVE saves are only compatible with games that were not loaded from YAML/COS, as they rely on the original binary save format. If true, the game is locked to YAML/COS saves and cannot be saved in SVE format.
		/// </summary>
		public bool IsLoadedFromYaml { get; }

		/// <summary>
		/// The number of players in the game. SVE supports a maximum of 8 players, so if this value exceeds 8, the game is not compatible with SVE saves.
		/// </summary>
		public int PlayerCount { get; }

		/// <summary>
		/// The width of the game map. SVE supports a fixed map size of 80x50, so if the map width or height does not match these values, the game is not compatible with SVE saves.
		/// </summary>
		public int MapWidth { get; }

		/// <summary>
		/// The height of the game map. SVE supports a fixed map size of 80x50, so if the map width or height does not match these values, the game is not compatible with SVE saves.
		/// </summary>
		public int MapHeight { get; }

		/// <summary>
		/// The number of cities in the game. SVE supports a maximum of 128 cities, so if this value exceeds 128, the game is not compatible with SVE saves.
		/// </summary>
		public int CityCount { get; }

		/// <summary>
		/// The length of the replay data in bytes. SVE supports a maximum replay data length of 4096 bytes, so if this value exceeds 4096, the game is not compatible with SVE saves.
		/// </summary>
		public int ReplayDataLengthBytes { get; }

		/// <summary>
		/// Indicates whether at least one trade-city reference points to a non-existing city.
		/// </summary>
		public bool HasInvalidTradeCityReferences { get; }

		/// <summary>
		/// Indicates whether at least one unit has a home-city reference that points to a non-existing city.
		/// </summary>
		public bool HasInvalidUnitHomeCityReferences { get; }

		/// <summary>
		/// Indicates whether at least one city has coordinates outside the SVE map bounds.
		/// </summary>
		public bool HasOutOfBoundsCityCoordinates { get; }

		/// <summary>
		/// Indicates whether at least one unit has coordinates outside the SVE map bounds.
		/// </summary>
		public bool HasOutOfBoundsUnitCoordinates { get; }

		/// <summary>
		/// Indicates whether at least one unit has goto coordinates outside the SVE map bounds.
		/// </summary>
		public bool HasOutOfBoundsUnitGotoCoordinates { get; }

		/// <summary>
		/// The number of trade cities per city. Each city can have at most 3 trade city references in SVE.
		/// </summary>
		public IReadOnlyCollection<int> TradeCityCountsPerCity { get; }

		/// <summary>
		/// The owner index per city. Each owner must be in the SVE player range 0-7.
		/// </summary>
		public IReadOnlyCollection<byte> CityOwners { get; }

		/// <summary>
		/// The owner index per regular unit stored in the SVE unit list. Each owner must be in the SVE player range 0-7.
		/// </summary>
		public IReadOnlyCollection<byte> UnitOwners { get; }

		/// <summary>
		/// The number of regular units stored in the SVE unit list. This count excludes fortified units stored in city slots.
		/// </summary>
		public int UnitsCount { get; }

		/// <summary>
		/// The number of fortified units per city. Each city can have up to 2 fortified units.
		/// These units are stored separately in the SVE format and do not count towards the maximum units per player limit.
		/// </summary>
		public IReadOnlyCollection<int> FortifiedUnitCountsPerCity { get; }

		/// <summary>
		/// The total number of fortified units stored across all cities in SVE-specific city slots.
		/// </summary>
		public int FortifiedUnitsCount { get; }

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
			IReadOnlyCollection<byte> unitOwners,
			int unitsCount,
			IReadOnlyCollection<int> fortifiedUnitCountsPerCity,
			int fortifiedUnitsCount)
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
			UnitsCount = unitsCount;
			FortifiedUnitCountsPerCity = fortifiedUnitCountsPerCity ?? Array.Empty<int>();
			FortifiedUnitsCount = fortifiedUnitsCount;
		}

		public static SveSaveCompatibilitySnapshotBuilder Builder() => new();
	}

	/// <summary>
	/// Builder for creating instances of <see cref="SveSaveCompatibilitySnapshot"/>. Provides a fluent API for setting properties and building the snapshot.
	/// </summary>
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
		private int _unitsCount;
		private IReadOnlyCollection<int> _fortifiedUnitCountsPerCity = Array.Empty<int>();
		private int _fortifiedUnitsCount;

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

		public SveSaveCompatibilitySnapshotBuilder WithUnitsCount(int unitsCount)
		{
			_unitsCount = unitsCount;
			return this;
		}

		public SveSaveCompatibilitySnapshotBuilder WithFortifiedUnitCountsPerCity(IReadOnlyCollection<int> fortifiedUnitCountsPerCity)
		{
			_fortifiedUnitCountsPerCity = fortifiedUnitCountsPerCity ?? Array.Empty<int>();
			return this;
		}

		public SveSaveCompatibilitySnapshotBuilder WithFortifiedUnitsCount(int fortifiedUnitsCount)
		{
			_fortifiedUnitsCount = fortifiedUnitsCount;
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
				_unitOwners,
				_unitsCount,
				_fortifiedUnitCountsPerCity,
				_fortifiedUnitsCount);
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
		private const int SveMaxFortifiedUnitsPerCity = 2;
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

			if (snapshot.FortifiedUnitCountsPerCity.Any(count => count > SveMaxFortifiedUnitsPerCity))
			{
				return new SveSaveCompatibilityResult(false, $"SVE supports at most {SveMaxFortifiedUnitsPerCity} fortified units per city.");
			}

			if (snapshot.UnitOwners.Count != snapshot.UnitsCount)
			{
				return new SveSaveCompatibilityResult(false, $"Unit owner list count ({snapshot.UnitOwners.Count}) does not match UnitsCount ({snapshot.UnitsCount}).");
			}

			if (snapshot.CityOwners.Count != snapshot.FortifiedUnitCountsPerCity.Count)
			{
				return new SveSaveCompatibilityResult(false, "City owner count does not match fortified unit count entries per city.");
			}

			var fortifiedUnitsCountFromCities = snapshot.FortifiedUnitCountsPerCity.Sum();
			if (fortifiedUnitsCountFromCities != snapshot.FortifiedUnitsCount)
			{
				return new SveSaveCompatibilityResult(false, $"Fortified unit sum per city ({fortifiedUnitsCountFromCities}) does not match FortifiedUnitsCount ({snapshot.FortifiedUnitsCount}).");
			}

			var unitsByOwner = snapshot.UnitOwners
				.GroupBy(owner => owner)
				.ToDictionary(group => group.Key, group => group.Count());

			// Calculate fortified units per player by iterating through cities and their owners.
			var fortifiedUnitsByOwner = new Dictionary<byte, int>();
			var cityOwners = snapshot.CityOwners.ToArray();
			var fortifiedUnitCountsPerCity = snapshot.FortifiedUnitCountsPerCity.ToArray();
			for (int i = 0; i < cityOwners.Length; i++)
			{
				var cityOwner = cityOwners[i];
				var fortifiedCount = fortifiedUnitCountsPerCity[i];
				
				if (!fortifiedUnitsByOwner.ContainsKey(cityOwner))
				{
					fortifiedUnitsByOwner[cityOwner] = 0;
				}
				
				fortifiedUnitsByOwner[cityOwner] += fortifiedCount;
			}

			// Get the count of cities per player
			var citiesByOwner = snapshot.CityOwners
				.GroupBy(owner => owner)
				.ToDictionary(group => group.Key, group => group.Count());

			// Validate total units per player: normal units (max 128) + fortified units (max 2 per city).
			for (byte player = 0; player < SveMaxPlayers; player++)
			{
				var normalUnitsCount = unitsByOwner.ContainsKey(player) ? unitsByOwner[player] : 0;
				var fortifiedUnitsCount = fortifiedUnitsByOwner.ContainsKey(player) ? fortifiedUnitsByOwner[player] : 0;
				var citiesCount = citiesByOwner.ContainsKey(player) ? citiesByOwner[player] : 0;
				var maxTotalUnitsForPlayer = SveMaxUnitsPerPlayer + (citiesCount * SveMaxFortifiedUnitsPerCity);
				var totalUnitsCount = normalUnitsCount + fortifiedUnitsCount;

				if (normalUnitsCount > SveMaxUnitsPerPlayer)
				{
					return new SveSaveCompatibilityResult(false, $"Player {player} has {normalUnitsCount} regular units, SVE supports at most {SveMaxUnitsPerPlayer} regular units per player.");
				}

				if (totalUnitsCount > maxTotalUnitsForPlayer)
				{
					return new SveSaveCompatibilityResult(false, $"Player {player} has {totalUnitsCount} total units ({normalUnitsCount} regular + {fortifiedUnitsCount} fortified), SVE supports at most {maxTotalUnitsForPlayer} total units per player ({SveMaxUnitsPerPlayer} regular + {citiesCount} cities × {SveMaxFortifiedUnitsPerCity} fortified).");
				}
			}

			return SveSaveCompatibilityResult.Compatible;
		}
	}
}