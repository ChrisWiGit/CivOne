// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using CivOne.Civilizations;
using CivOne.Persistence.Model;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// The outcome of trying to find a starting position for one <see cref="StartPositionCandidate"/>.
	/// </summary>
	public sealed class StartPositionResult
	{
		/// <summary>
		/// The civilization this result belongs to. Matches the corresponding <see cref="StartPositionCandidate.Civilization"/>.
		/// </summary>
		public required ICivilization Civilization { get; init; }

		/// <summary>
		/// Whether a valid starting position was found. When false, <see cref="Position"/> and the other members are meaningless.
		/// </summary>
		public bool Success { get; init; }

		/// <summary>
		/// The tile where the Settlers unit should be placed.
		/// </summary>
		public MapLocation Position { get; init; }

		/// <summary>
		/// Whether a second Settlers unit should be placed on the same tile (Chieftain difficulty rule, legacy algorithm only).
		/// </summary>
		public bool PlaceSecondSettlerAtSamePosition { get; init; }

		/// <summary>
		/// Positions found for the requested <see cref="StartPositionCandidate.AdditionalUnitTypes"/>, in the same order.
		/// </summary>
		public IReadOnlyList<MapLocation> AdditionalUnitPositions { get; init; } = [];
	}
}
