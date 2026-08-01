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
	/// Reproduces the original starting-position algorithm: a bounded random search with progressively relaxed
	/// constraints, falling back to an exhaustive map scan if the search doesn't find a tile in time.
	/// </summary>
	internal sealed class LegacyStartPositionService : IStartPositionService
	{
		private const int MaxRandomAttempts = 2000;

		public IReadOnlyList<StartPositionResult> FindStartPositions(IReadOnlyList<StartPositionCandidate> candidates, StartPositionContext context)
		{
			ArgumentNullException.ThrowIfNull(candidates);
			ArgumentNullException.ThrowIfNull(context);

			// Tiles claimed by earlier candidates in this same batch must also block later ones,
			// the same way the original per-player loop saw previously placed units in `_units`.
			List<MapLocation> occupiedTiles = [.. context.OccupiedTiles];
			List<MapLocation> settlerLocations = [.. context.SettlerLocations];

			ushort gameTurn = ClampGameTurn(context);
			var fixedPositionResolver = new FixedStartPositionResolverDelegate(context);
			var fallbackTileScan = new FallbackTileScanDelegate(context);

			var results = new List<StartPositionResult>(candidates.Count);
			foreach (StartPositionCandidate candidate in candidates)
			{
				StartPositionResult result = FindStartPosition(candidate, context, occupiedTiles, settlerLocations, gameTurn, fixedPositionResolver, fallbackTileScan);
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
		/// Converts the context's game turn to the <see cref="ushort"/> the turn-to-year conversion expects.
		/// Out-of-range values are clamped and logged instead of silently wrapping around, which would flip the
		/// "after 0 AD" rule below into the wrong branch.
		/// </summary>
		private static ushort ClampGameTurn(StartPositionContext context)
		{
			if (context.GameTurn < 0)
			{
				context.Logger?.Log("PlaceStartingUnits: game turn {0} is negative; clamped to 0.", context.GameTurn);
				return 0;
			}

			if (context.GameTurn > ushort.MaxValue)
			{
				context.Logger?.Log("PlaceStartingUnits: game turn {0} exceeds {1}; clamped.", context.GameTurn, ushort.MaxValue);
				return ushort.MaxValue;
			}

			return (ushort)context.GameTurn;
		}

		private static StartPositionResult FindStartPosition(
			StartPositionCandidate candidate,
			StartPositionContext context,
			List<MapLocation> occupiedTiles,
			List<MapLocation> settlerLocations,
			ushort gameTurn,
			FixedStartPositionResolverDelegate fixedPositionResolver,
			FallbackTileScanDelegate fallbackTileScan)
		{
			MapLocation? fixedPosition = fixedPositionResolver.TryResolve(candidate);
			if (fixedPosition != null)
			{
				return Success(candidate, fixedPosition);
			}

			int loopCounter = 0;
			while (loopCounter++ < MaxRandomAttempts)
			{
				int x = context.RandomService.NextInt(0, context.Map.Width);
				int y = context.RandomService.NextInt(2, context.Map.Height - 2);

				if (!IsValidRandomTile(x, y, loopCounter, context, occupiedTiles, settlerLocations, gameTurn))
				{
					continue;
				}

				return Success(candidate, new MapLocation((uint)x, (uint)y));
			}

			context.Logger?.Log("PlaceStartingUnits: strict placement failed for {0}; trying relaxed fallback placement.", candidate.Civilization.Name);

			MapLocation? fallback = fallbackTileScan.FindAnyUsableTile(occupiedTiles);
			if (fallback != null)
			{
				context.Logger?.Log("PlaceStartingUnits: fallback placement succeeded for {0} at {1},{2}.", candidate.Civilization.Name, fallback.X, fallback.Y);
				return Success(candidate, fallback);
			}

			context.Logger?.Log("PlaceStartingUnits: no valid fallback tile found for {0}.", candidate.Civilization.Name);
			return new StartPositionResult { Civilization = candidate.Civilization, Success = false };
		}

		private static bool IsValidRandomTile(int x, int y, int loopCounter, StartPositionContext context, List<MapLocation> occupiedTiles, List<MapLocation> settlerLocations, ushort gameTurn)
		{
			ITile tile = context.Map[x, y];
			if (tile == null) return false; // Outside the generated map.
			if (tile.IsOcean) return false;
			// Cities cannot be founded on Mountains; Arctic and Tundra offer no realistic growth.
			// Never valid starting positions, no matter how much the other constraints below are relaxed.
			if (tile.OfTypes(Terrain.Mountains, Terrain.Arctic, Terrain.Tundra)) return false;
			if (tile.Hut) return false;
			if (IsOccupied(x, y, occupiedTiles)) return false;
			if (tile.LandValue < (12 - (loopCounter / 32))) return false; // Is the land value high enough?
			if (context.CityLocations.Any(c => Common.DistanceToTile(x, y, (int)c.X, (int)c.Y) < (10 - (loopCounter / 64)))) return false; // Distance to other cities
			if (settlerLocations.Any(s => Common.DistanceToTile(x, y, (int)s.X, (int)s.Y) < (10 - (loopCounter / 64)))) return false; // Distance to other settlers
			if (context.Map.ContinentTiles(tile.ContinentId).Count(t => context.Map.TileIsType(t, Terrain.Plains, Terrain.Grassland1, Terrain.Grassland2, Terrain.River)) < (32 - (context.GameTurn / 16))) return false; // Check buildable tiles on continent

			// CW: Civs are only spawned until 0 AD. So what is the point of this?
			// After 0 AD, don't spawn a Civilization on a continent that already contains cities.
			if (Common.TurnToYear(gameTurn) >= 0 && context.Map.ContinentTiles(tile.ContinentId).Any(t => t.City != null)) return false;

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
