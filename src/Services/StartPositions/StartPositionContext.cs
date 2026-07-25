// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using CivOne.Persistence.Model;
using CivOne.Services.Random;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Shared, read-only inputs an <see cref="IStartPositionService"/> needs to find starting positions for a batch of candidates.
	/// </summary>
	public sealed class StartPositionContext
	{
		/// <summary>
		/// The map to search for valid starting tiles. Injected instead of using the <c>Map</c> singleton directly.
		/// </summary>
		public required IMapEditor Map { get; init; }

		/// <summary>
		/// The random number generator to use, so placement stays reproducible for a given game seed.
		/// </summary>
		public required IRandomService RandomService { get; init; }

		/// <summary>
		/// Whether this is turn 0 of a new game. Custom and civilization-default starting positions only apply on the first turn.
		/// </summary>
		public bool IsFirstGameTurn { get; init; }

		/// <summary>
		/// Whether any player in the game has a custom <see cref="StartPositionCandidate.MapStartPosition"/> set.
		/// When true, civilization-default starting positions are not used for players without one.
		/// </summary>
		public bool AnyFixedMapStartPosition { get; init; }

		/// <summary>
		/// The current game turn, used to relax placement constraints over time.
		/// </summary>
		public int GameTurn { get; init; }

		/// <summary>
		/// The game difficulty (0 = Chieftain, the easiest). See <see cref="CivOne.Persistence.Model.DifficultyLevel"/>.
		/// </summary>
		public int Difficulty { get; init; }

		/// <summary>
		/// Tiles that are already occupied by a unit and cannot be used for a new starting position.
		/// </summary>
		public IReadOnlyList<MapLocation> OccupiedTiles { get; init; } = [];

		/// <summary>
		/// Locations of existing cities, kept at a minimum distance from new starting positions.
		/// </summary>
		public IReadOnlyList<MapLocation> CityLocations { get; init; } = [];

		/// <summary>
		/// Locations of existing Settlers units, kept at a minimum distance from new starting positions.
		/// </summary>
		public IReadOnlyList<MapLocation> SettlerLocations { get; init; } = [];

		/// <summary>
		/// Optional logger for diagnostic messages (e.g. when falling back to a relaxed placement strategy).
		/// </summary>
		public ILogger? Logger { get; init; }
	}
}
