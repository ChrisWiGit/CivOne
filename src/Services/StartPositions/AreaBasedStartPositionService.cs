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
using CivOne.Services.Random;
using CivOne.Tiles;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Divides the map into one roughly equally sized area per candidate and places each candidate's Settlers
	/// somewhere inside its own area, so civilizations start spread out across the map instead of wherever a
	/// global random search happens to land. Unlike <see cref="LegacyStartPositionService"/>, this algorithm can
	/// also place a caller-requested set of additional units near the Settlers.
	/// </summary>
	internal sealed class AreaBasedStartPositionService : IStartPositionService
	{
		private const int MaxAttemptsPerArea = 500;
		private const int AdditionalUnitSearchRadius = 3;
		private const int AdditionalUnitSearchAttempts = 20;

		private readonly FixedStartPositionResolverDelegate _fixedPositionResolver = new();

		public IReadOnlyList<StartPositionResult> FindStartPositions(IReadOnlyList<StartPositionCandidate> candidates, StartPositionContext context)
		{
			ArgumentNullException.ThrowIfNull(candidates);
			ArgumentNullException.ThrowIfNull(context);

			List<MapLocation> occupiedTiles = [.. context.OccupiedTiles];

			MapArea[] areas = BuildAreas(candidates.Count, context);
			int[] areaOrder = [.. Enumerable.Range(0, areas.Length)];
			Shuffle(areaOrder, context.RandomService);

			var results = new List<StartPositionResult>(candidates.Count);
			for (int i = 0; i < candidates.Count; i++)
			{
				StartPositionCandidate candidate = candidates[i];
				MapArea area = areas[areaOrder[i]];

				StartPositionResult result = FindStartPosition(candidate, area, context, occupiedTiles);
				if (result.Success)
				{
					occupiedTiles.Add(result.Position);
					occupiedTiles.AddRange(result.AdditionalUnitPositions);
				}
				results.Add(result);
			}

			return results;
		}

		private readonly record struct MapArea(int X0, int Y0, int X1, int Y1);

		private static MapArea[] BuildAreas(int count, StartPositionContext context)
		{
			int cols = (int)Math.Ceiling(Math.Sqrt(count));
			int rows = (int)Math.Ceiling(count / (double)cols);

			const int poleMargin = 2;
			int top = poleMargin;
			int bottom = context.Map.Height - poleMargin;

			int cellWidth = context.Map.Width / cols;
			int cellHeight = (bottom - top) / rows;

			var areas = new MapArea[count];
			for (int i = 0; i < count; i++)
			{
				int row = i / cols;
				int col = i % cols;

				int x0 = col * cellWidth;
				int x1 = (col == cols - 1) ? context.Map.Width : x0 + cellWidth;
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

		private StartPositionResult FindStartPosition(StartPositionCandidate candidate, MapArea area, StartPositionContext context, List<MapLocation> occupiedTiles)
		{
			MapLocation? fixedPosition = _fixedPositionResolver.TryResolve(candidate, context);
			if (fixedPosition != null)
			{
				return Success(candidate, fixedPosition, context, occupiedTiles);
			}

			MapLocation? position = FindTileInArea(area, context, occupiedTiles) ?? FindTileAnywhere(context, occupiedTiles);
			if (position == null)
			{
				context.Logger?.Log("AddStartingUnits: no valid area tile found for {0}.", candidate.Civilization.Name);
				return new StartPositionResult { Civilization = candidate.Civilization, Success = false };
			}

			return Success(candidate, position, context, occupiedTiles);
		}

		private static MapLocation? FindTileInArea(MapArea area, StartPositionContext context, List<MapLocation> occupiedTiles)
		{
			for (int attempt = 0; attempt < MaxAttemptsPerArea; attempt++)
			{
				int x = context.RandomService.NextInt(area.X0, area.X1);
				int y = context.RandomService.NextInt(area.Y0, area.Y1);

				if (IsValidTile(x, y, attempt, context, occupiedTiles))
				{
					return new MapLocation((uint)x, (uint)y);
				}
			}

			// Random search didn't find anything in the budget (e.g. a small or sparse area): scan it exhaustively.
			for (int y = area.Y0; y < area.Y1; y++)
			{
				for (int x = area.X0; x < area.X1; x++)
				{
					if (IsValidTile(x, y, 0, context, occupiedTiles))
					{
						return new MapLocation((uint)x, (uint)y);
					}
				}
			}

			return null;
		}

		private static MapLocation? FindTileAnywhere(StartPositionContext context, List<MapLocation> occupiedTiles)
		{
			// The assigned area was fully unusable (e.g. entirely ocean): fall back to the whole map.
			for (int y = 2; y < context.Map.Height - 2; y++)
			{
				for (int x = 0; x < context.Map.Width; x++)
				{
					if (IsValidTile(x, y, 0, context, occupiedTiles))
					{
						return new MapLocation((uint)x, (uint)y);
					}
				}
			}

			return null;
		}

		private static bool IsValidTile(int x, int y, int attempt, StartPositionContext context, List<MapLocation> occupiedTiles)
		{
			ITile tile = context.Map[x, y];
			if (tile == null || tile.IsOcean) return false;
			if (tile.Hut) return false;
			if (IsOccupied(x, y, occupiedTiles)) return false;
			if (tile.LandValue < (12 - (attempt / 32))) return false;

			return true;
		}

		private static bool IsOccupied(int x, int y, List<MapLocation> occupiedTiles) => occupiedTiles.Any(t => t.X == (uint)x && t.Y == (uint)y);

		private static StartPositionResult Success(StartPositionCandidate candidate, MapLocation position, StartPositionContext context, List<MapLocation> occupiedTiles)
		{
			List<MapLocation> localOccupied = [.. occupiedTiles, position];
			var additionalPositions = new List<MapLocation>(candidate.AdditionalUnitTypes.Count);
			foreach (UnitType _ in candidate.AdditionalUnitTypes)
			{
				MapLocation additionalPosition = FindAdditionalUnitPosition(position, context, localOccupied);
				additionalPositions.Add(additionalPosition);
				localOccupied.Add(additionalPosition);
			}

			return new StartPositionResult
			{
				Civilization = candidate.Civilization,
				Success = true,
				Position = position,
				PlaceSecondSettlerAtSamePosition = false,
				AdditionalUnitPositions = additionalPositions
			};
		}

		private static MapLocation FindAdditionalUnitPosition(MapLocation settlerPosition, StartPositionContext context, List<MapLocation> occupiedTiles)
		{
			for (int attempt = 0; attempt < AdditionalUnitSearchAttempts; attempt++)
			{
				int offsetX = context.RandomService.NextInt(-AdditionalUnitSearchRadius, AdditionalUnitSearchRadius + 1);
				int offsetY = context.RandomService.NextInt(-AdditionalUnitSearchRadius, AdditionalUnitSearchRadius + 1);
				int x = Math.Clamp((int)settlerPosition.X + offsetX, 0, context.Map.Width - 1);
				int y = Math.Clamp((int)settlerPosition.Y + offsetY, 2, context.Map.Height - 3);

				ITile tile = context.Map[x, y];
				if (tile == null || tile.IsOcean) continue;
				if (IsOccupied(x, y, occupiedTiles)) continue;

				return new MapLocation((uint)x, (uint)y);
			}

			// No distinct free tile found nearby: stack on the settler's tile rather than losing the unit.
			return settlerPosition;
		}
	}
}
