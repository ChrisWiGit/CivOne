// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Enums;
using CivOne.Persistence.Model;
using CivOne.Tiles;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Last-resort search for any usable land tile on the map, used when the regular placement rules
	/// (land value, distance to other civilizations, continent size) cannot be satisfied anywhere.
	/// Shared by both starting-position services so a civilization never silently ends up without Settlers
	/// while a free land tile still exists.
	/// The context is passed once at construction time, because it is the same for every candidate of a batch.
	/// </summary>
	/// <param name="context">The shared inputs of the current starting-position batch.</param>
	internal sealed class FallbackTileScanDelegate(StartPositionContext context)
	{
		private readonly StartPositionContext _context = context ?? throw new ArgumentNullException(nameof(context));

		/// <summary>
		/// Scans the map for the first free, non-ocean tile.
		/// Tiles near the poles and Arctic tiles are only considered once the rest of the map turned out to be
		/// unusable: a polar start is playable but bad, while Arctic offers no growth at all (no food, no
		/// irrigation bonus) and is only accepted as an absolute last resort.
		/// </summary>
		/// <param name="occupiedTiles">Tiles that are already taken and must not be returned.</param>
		/// <returns>The first usable tile, or <see langword="null"/> if the map has no free land tile at all.</returns>
		public MapLocation? FindAnyUsableTile(IReadOnlyList<MapLocation> occupiedTiles)
		{
			ArgumentNullException.ThrowIfNull(occupiedTiles);

			const int poleMargin = 2;
			return Scan(poleMargin, _context.Map.Height - poleMargin, allowArctic: false, occupiedTiles)
				?? Scan(0, _context.Map.Height, allowArctic: false, occupiedTiles)
				?? Scan(poleMargin, _context.Map.Height - poleMargin, allowArctic: true, occupiedTiles)
				?? Scan(0, _context.Map.Height, allowArctic: true, occupiedTiles);
		}

		private MapLocation? Scan(int firstRow, int lastRowExclusive, bool allowArctic, IReadOnlyList<MapLocation> occupiedTiles)
		{
			for (int y = firstRow; y < lastRowExclusive; y++)
			{
				for (int x = 0; x < _context.Map.Width; x++)
				{
					ITile tile = _context.Map[x, y];
					if (tile == null)
					{
						continue;
					}

					if (!allowArctic && tile.OfTypes(Terrain.Arctic, Terrain.Tundra, Terrain.Ocean, Terrain.Mountains))
					{
						continue;
					}

					if (occupiedTiles.Any(occupied => occupied.X == (uint)x && occupied.Y == (uint)y))
					{
						continue;
					}

					return new MapLocation((uint)x, (uint)y);
				}
			}

			return null;
		}
	}
}
