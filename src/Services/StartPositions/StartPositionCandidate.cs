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
using CivOne.Enums;
using CivOne.Persistence.Model;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Describes one civilization that needs a starting position.
	/// </summary>
	public sealed class StartPositionCandidate
	{
		/// <summary>
		/// The civilization to find a starting position for.
		/// </summary>
		public required ICivilization Civilization { get; init; }

		/// <summary>
		/// A custom starting position set for this civilization on the map (e.g. painted in the terrain editor), if any.
		/// When set and the request is for the first game turn, this position is used instead of a computed one.
		/// </summary>
		public MapLocation? MapStartPosition { get; init; }

		/// <summary>
		/// Additional unit types the caller wants a position for, alongside the starting Settlers.
		/// </summary>
		public IReadOnlyList<UnitType> AdditionalUnitTypes { get; init; } = [];
	}
}
