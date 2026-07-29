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
using CivOne.Persistence.Model;
using CivOne.Services.Random;
using CivOne.Tiles;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Divides the map into one roughly equally sized area per candidate and places each candidate's Settlers
	/// somewhere inside its own area, so civilizations start spread out across the map instead of wherever a
	/// global random search happens to land.
	/// </summary>
	internal sealed class AreaBasedStartPositionService : IStartPositionService
	{
		private const int MaxAttemptsPerArea = 500;

		/// <summary>
		/// The distance a new starting position keeps from existing cities and Settlers at the first attempt.
		/// It is relaxed as attempts run out, so a crowded or small map still gets a placement.
		/// </summary>
		private const int InitialMinDistance = 10;

		/// <summary>
		/// Build more grid cells than there are candidates, so a chunk of the map always stays unassigned.
		/// Those extra areas act as buffer space between civs and as additional fallback room for
		/// <see cref="FindTileInOtherAreas"/>, on top of the other candidates' own areas.
		/// Note that the fallback does not reserve areas: if every other area fails, two civilizations can
		/// still end up in the same one.
		/// </summary>
		internal const int AreaOversampleFactor = 2;

		public IReadOnlyList<StartPositionResult> FindStartPositions(IReadOnlyList<StartPositionCandidate> candidates, StartPositionContext context)
		{
			ArgumentNullException.ThrowIfNull(candidates);
			ArgumentNullException.ThrowIfNull(context);

			List<MapLocation> occupiedTiles = [.. context.OccupiedTiles];
			List<MapLocation> settlerLocations = [.. context.SettlerLocations];

			MapArea[] areas = BuildAreas(candidates.Count * AreaOversampleFactor, context.Map.Width, context.Map.Height);
			int[] areaOrder = [.. Enumerable.Range(0, areas.Length)];
			Shuffle(areaOrder, context.RandomService);

			var fixedPositionResolver = new FixedStartPositionResolverDelegate(context);
			var fallbackTileScan = new FallbackTileScanDelegate(context);

			var results = new List<StartPositionResult>(candidates.Count);
			for (int i = 0; i < candidates.Count; i++)
			{
				StartPositionCandidate candidate = candidates[i];
				int areaIndex = areaOrder[i];

				StartPositionResult result = FindStartPosition(candidate, areaIndex, areas, context, occupiedTiles, settlerLocations, fixedPositionResolver, fallbackTileScan);
				if (result.Success)
				{
					occupiedTiles.Add(result.Position);
					settlerLocations.Add(result.Position);
				}
				results.Add(result);
			}

			return results;
		}

		/// <summary>
		/// One grid cell of the area partition. Internal (rather than <see langword="private"/>) so debug tooling
		/// (e.g. a start-position overlay) can draw the same grid <see cref="BuildAreas"/> computes.
		/// </summary>
		internal readonly record struct MapArea(int X0, int Y0, int X1, int Y1);

		/// <summary>
		/// Splits the map into a <c>cols x rows</c> grid large enough to hold at least <paramref name="count"/> areas.
		/// All <c>cols * rows</c> cells are returned, so the grid covers the whole map (minus the pole margin) without
		/// gaps, even when the last row would not be filled by <paramref name="count"/> alone.
		/// Deterministic given the same inputs; only the assignment of a candidate to a specific area is randomized.
		/// </summary>
		/// <param name="count">The minimum number of areas the grid must provide.</param>
		/// <param name="mapWidth">The map width in tiles.</param>
		/// <param name="mapHeight">The map height in tiles.</param>
		/// <returns>The grid cells, ordered row by row.</returns>
		internal static MapArea[] BuildAreas(int count, int mapWidth, int mapHeight)
		{
			int cols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(count)));
			int rows = Math.Max(1, (int)Math.Ceiling(count / (double)cols));

			const int poleMargin = 2;
			int top = poleMargin;
			int bottom = mapHeight - poleMargin;

			int cellWidth = mapWidth / cols;
			int cellHeight = (bottom - top) / rows;

			var areas = new MapArea[cols * rows];
			for (int i = 0; i < areas.Length; i++)
			{
				int row = i / cols;
				int col = i % cols;

				int x0 = col * cellWidth;
				int x1 = (col == cols - 1) ? mapWidth : x0 + cellWidth;
				int y0 = top + (row * cellHeight);
				int y1 = (row == rows - 1) ? bottom : y0 + cellHeight;

				areas[i] = new MapArea(x0, y0, x1, y1);
			}
			return areas;
		}

		private static void Shuffle(int[] values, IRandomService random)
		{
			for (int i = values.Length - 1; i > 0; i--)
			{
				int j = random.NextInt(i + 1);
				(values[i], values[j]) = (values[j], values[i]);
			}
		}

		private static StartPositionResult FindStartPosition(
			StartPositionCandidate candidate,
			int areaIndex,
			MapArea[] areas,
			StartPositionContext context,
			List<MapLocation> occupiedTiles,
			List<MapLocation> settlerLocations,
			FixedStartPositionResolverDelegate fixedPositionResolver,
			FallbackTileScanDelegate fallbackTileScan)
		{
			MapLocation? fixedPosition = fixedPositionResolver.TryResolve(candidate);
			if (fixedPosition != null)
			{
				return Success(candidate, fixedPosition);
			}

			// Prefer the assigned area first (keeps civs spread out); if it's fully unusable (e.g. entirely ocean),
			// try the other areas in random order. Since the areas tile the whole map between them, this covers
			// every tile outside the pole margin without needing a separate "scan the whole map" pass.
			MapLocation? position = FindTileInArea(areas[areaIndex], context, occupiedTiles, settlerLocations)
				?? FindTileInOtherAreas(areaIndex, areas, context, occupiedTiles, settlerLocations);
			if (position != null)
			{
				return Success(candidate, position);
			}

			// Nothing satisfied the regular rules anywhere: take any free land tile rather than
			// letting the civilization start without a Settlers unit.
			MapLocation? fallback = fallbackTileScan.FindAnyUsableTile(occupiedTiles);
			if (fallback != null)
			{
				context.Logger?.Log("PlaceStartingUnits: no valid area tile for {0}; using last-resort placement at {1},{2}.", candidate.Civilization.Name, fallback.X, fallback.Y);
				return Success(candidate, fallback);
			}

			context.Logger?.Log("PlaceStartingUnits: no valid area tile found for {0}.", candidate.Civilization.Name);
			return new StartPositionResult { Civilization = candidate.Civilization, Success = false };
		}

		private static MapLocation? FindTileInOtherAreas(int excludedAreaIndex, MapArea[] areas, StartPositionContext context, List<MapLocation> occupiedTiles, List<MapLocation> settlerLocations)
		{
			// Randomize the order of the other areas so we don't always try
			// the same one first (which would bias placement toward the top-left).
			// All civs will therefore be placed in a random area.
			int[] order = [.. Enumerable.Range(0, areas.Length)];
			Shuffle(order, context.RandomService);

			foreach (int index in order)
			{
				if (index == excludedAreaIndex) continue;

				MapLocation? position = FindTileInArea(areas[index], context, occupiedTiles, settlerLocations);
				if (position != null)
				{
					return position;
				}
			}

			return null;
		}

		private static MapLocation? FindTileInArea(MapArea area, StartPositionContext context, List<MapLocation> occupiedTiles, List<MapLocation> settlerLocations)
		{
			for (int attempt = 0; attempt < MaxAttemptsPerArea; attempt++)
			{
				int x = context.RandomService.NextInt(area.X0, area.X1);
				int y = context.RandomService.NextInt(area.Y0, area.Y1);

				if (IsValidTile(x, y, attempt, context, occupiedTiles, settlerLocations))
				{
					return new MapLocation((uint)x, (uint)y);
				}
			}

			// Random search didn't find anything in the budget (e.g. a small or sparse area): scan it exhaustively.
			// The scan uses the fully relaxed constraints of the last random attempt, so it never rejects a tile
			// the random search would have accepted.
			for (int y = area.Y0; y < area.Y1; y++)
			{
				for (int x = area.X0; x < area.X1; x++)
				{
					if (IsValidTile(x, y, MaxAttemptsPerArea, context, occupiedTiles, settlerLocations))
					{
						return new MapLocation((uint)x, (uint)y);
					}
				}
			}

			return null;
		}

		private static bool IsValidTile(int x, int y, int attempt, StartPositionContext context, List<MapLocation> occupiedTiles, List<MapLocation> settlerLocations)
		{
			ITile tile = context.Map[x, y];
			if (tile == null || tile.IsOcean) return false;
			if (tile.Hut) return false;
			if (IsOccupied(x, y, occupiedTiles)) return false;
			if (tile.LandValue < (12 - (attempt / 32))) return false;

			// Areas alone only guarantee that civs start in different parts of the map; two civs in neighbouring
			// areas could still start right next to each other at the shared border. The distance rules of the
			// original algorithm therefore apply here too, relaxed the same way as the land value above so a
			// crowded map still produces a placement.
			int minDistance = Math.Max(0, InitialMinDistance - (attempt / 32));
			if (minDistance > 0)
			{
				if (context.CityLocations.Any(c => Common.DistanceToTile(x, y, (int)c.X, (int)c.Y) < minDistance)) return false;
				if (settlerLocations.Any(s => Common.DistanceToTile(x, y, (int)s.X, (int)s.Y) < minDistance)) return false;
			}

			return true;
		}

		private static bool IsOccupied(int x, int y, List<MapLocation> occupiedTiles) => occupiedTiles.Any(t => t.X == (uint)x && t.Y == (uint)y);

		private static StartPositionResult Success(StartPositionCandidate candidate, MapLocation position) => new()
		{
			Civilization = candidate.Civilization,
			Success = true,
			Position = position
		};
	}
}
