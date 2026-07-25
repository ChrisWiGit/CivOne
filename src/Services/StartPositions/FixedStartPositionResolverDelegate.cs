// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Linq;
using CivOne.Persistence.Model;
using CivOne.Tiles;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Resolves a candidate's custom or civilization-default starting position, if one applies and is still usable.
	/// Shared by both <see cref="LegacyStartPositionService"/> and <see cref="AreaBasedStartPositionService"/> so
	/// neither one has to special-case custom map start positions on its own.
	/// </summary>
	internal sealed class FixedStartPositionResolverDelegate
	{
		/// <summary>
		/// Returns the candidate's fixed starting position if one applies to it and the tile is still usable, otherwise null.
		/// </summary>
		public MapLocation? TryResolve(StartPositionCandidate candidate, StartPositionContext context)
		{
			if (!context.IsFirstGameTurn)
			{
				return null;
			}

			if (candidate.MapStartPosition is MapLocation mapStartPosition)
			{
				return IsUsable(mapStartPosition, context) ? mapStartPosition : null;
			}

			if (!context.AnyFixedMapStartPosition && context.Map.FixedStartPositions)
			{
				MapLocation civilizationDefault = new(candidate.Civilization.StartX, candidate.Civilization.StartY);
				return IsUsable(civilizationDefault, context) ? civilizationDefault : null;
			}

			return null;
		}

		private static bool IsUsable(MapLocation location, StartPositionContext context)
		{
			ITile tile = context.Map[(int)location.X, (int)location.Y];
			if (tile == null || tile.IsOcean)
			{
				return false;
			}

			return !context.OccupiedTiles.Any(occupied => occupied == location);
		}
	}
}
