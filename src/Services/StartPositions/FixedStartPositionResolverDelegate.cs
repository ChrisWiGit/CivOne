// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Persistence.Model;
using CivOne.Tiles;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Resolves a candidate's custom or civilization-default starting position, if one applies and is still usable.
	/// Shared by both <see cref="LegacyStartPositionService"/> and <see cref="AreaBasedStartPositionService"/> so
	/// neither one has to special-case custom map start positions on its own.
	/// The context is passed once at construction time, because it is the same for every candidate of a batch.
	/// </summary>
	/// <param name="context">The shared inputs of the current starting-position batch.</param>
	internal sealed class FixedStartPositionResolverDelegate(StartPositionContext context)
	{
		private readonly StartPositionContext _context = context ?? throw new ArgumentNullException(nameof(context));

		/// <summary>
		/// Returns the candidate's fixed starting position if one applies to it and the tile is still usable, otherwise null.
		/// </summary>
		/// <param name="candidate">The civilization to resolve a fixed starting position for.</param>
		/// <returns>The usable fixed position, or <see langword="null"/> if none applies.</returns>
		public MapLocation? TryResolve(StartPositionCandidate candidate)
		{
			ArgumentNullException.ThrowIfNull(candidate);

			if (!_context.IsFirstGameTurn)
			{
				return null;
			}

			if (candidate.MapStartPosition is MapLocation mapStartPosition)
			{
				return IsUsable(mapStartPosition) ? mapStartPosition : null;
			}

			if (!_context.AnyFixedMapStartPosition && _context.Map.FixedStartPositions)
			{
				MapLocation civilizationDefault = new(candidate.Civilization.StartX, candidate.Civilization.StartY);
				return IsUsable(civilizationDefault) ? civilizationDefault : null;
			}

			return null;
		}

		private bool IsUsable(MapLocation location)
		{
			ITile tile = _context.Map[(int)location.X, (int)location.Y];
			if (tile == null || tile.IsOcean)
			{
				return false;
			}

			return !_context.OccupiedTiles.Any(occupied => occupied == location);
		}
	}
}
