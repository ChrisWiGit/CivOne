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
	/// On Chieftain difficulty, there is a 50% chance a second Settlers unit is placed on the same tile.
	/// </summary>
	internal sealed class LegacyStartPositionService : IStartPositionService
	{
		private const int MaxRandomAttempts = 2000;

		private readonly FixedStartPositionResolverDelegate _fixedPositionResolver = new();

		public IReadOnlyList<StartPositionResult> FindStartPositions(IReadOnlyList<StartPositionCandidate> candidates, StartPositionContext context)
		{
			ArgumentNullException.ThrowIfNull(candidates);
			ArgumentNullException.ThrowIfNull(context);

			// Tiles claimed by earlier candidates in this same batch must also block later ones,
			// the same way the original per-player loop saw previously placed units in `_units`.
			List<MapLocation> occupiedTiles = [.. context.OccupiedTiles];
			List<MapLocation> settlerLocations = [.. context.SettlerLocations];

			var results = new List<StartPositionResult>(candidates.Count);
			foreach (StartPositionCandidate candidate in candidates)
			{
				StartPositionResult result = FindStartPosition(candidate, context, occupiedTiles, settlerLocations);
				if (result.Success)
				{
					occupiedTiles.Add(result.Position);
					occupiedTiles.AddRange(result.AdditionalUnitPositions);
					settlerLocations.Add(result.Position);
				}
				results.Add(result);
			}

			return results;
		}

		private StartPositionResult FindStartPosition(StartPositionCandidate candidate, StartPositionContext context, List<MapLocation> occupiedTiles, List<MapLocation> settlerLocations)
		{
			MapLocation? fixedPosition = _fixedPositionResolver.TryResolve(candidate, context);
			if (fixedPosition != null)
			{
				return Success(candidate, fixedPosition, context);
			}

			int loopCounter = 0;
			while (loopCounter++ < MaxRandomAttempts)
			{
				int x = context.RandomService.NextInt(0, context.Map.Width);
				int y = context.RandomService.NextInt(2, context.Map.Height - 2);

				if (!IsValidRandomTile(x, y, loopCounter, context, occupiedTiles, settlerLocations))
				{
					continue;
				}

				return Success(candidate, new MapLocation((uint)x, (uint)y), context);
			}

			context.Logger?.Log("AddStartingUnits: strict placement failed for {0}; trying relaxed fallback placement.", candidate.Civilization.Name);

			MapLocation? fallback = FindFallbackTile(context, occupiedTiles);
			if (fallback != null)
			{
				context.Logger?.Log("AddStartingUnits: fallback placement succeeded for {0} at {1},{2}.", candidate.Civilization.Name, fallback.X, fallback.Y);
				return Success(candidate, fallback, context);
			}

			context.Logger?.Log("AddStartingUnits: no valid fallback tile found for {0}.", candidate.Civilization.Name);
			return new StartPositionResult { Civilization = candidate.Civilization, Success = false };
		}

		private static bool IsValidRandomTile(int x, int y, int loopCounter, StartPositionContext context, List<MapLocation> occupiedTiles, List<MapLocation> settlerLocations)
		{
			ITile tile = context.Map[x, y];
			if (tile == null) return false; // Outside the generated map.
			if (tile.IsOcean) return false;
			if (tile.Hut) return false;
			if (IsOccupied(x, y, occupiedTiles)) return false;
			if (tile.LandValue < (12 - (loopCounter / 32))) return false; // Is the land value high enough?
			if (context.CityLocations.Any(c => Common.DistanceToTile(x, y, (int)c.X, (int)c.Y) < (10 - (loopCounter / 64)))) return false; // Distance to other cities
			if (settlerLocations.Any(s => Common.DistanceToTile(x, y, (int)s.X, (int)s.Y) < (10 - (loopCounter / 64)))) return false; // Distance to other settlers
			if (context.Map.ContinentTiles(tile.ContinentId).Count(t => context.Map.TileIsType(t, Terrain.Plains, Terrain.Grassland1, Terrain.Grassland2, Terrain.River)) < (32 - (context.GameTurn / 16))) return false; // Check buildable tiles on continent

			// CW: Civs are only spawned until 0 AD. So what is the point of this?
			// After 0 AD, don't spawn a Civilization on a continent that already contains cities.
			if (Common.TurnToYear((ushort)context.GameTurn) >= 0 && context.Map.ContinentTiles(tile.ContinentId).Any(t => t.City != null)) return false;

			return true;
		}

		private static MapLocation? FindFallbackTile(StartPositionContext context, List<MapLocation> occupiedTiles)
		{
			for (int y = 2; y < context.Map.Height - 2; y++)
			{
				for (int x = 0; x < context.Map.Width; x++)
				{
					ITile tile = context.Map[x, y];
					if (tile == null || tile.IsOcean) continue;
					if (IsOccupied(x, y, occupiedTiles)) continue;

					return new MapLocation((uint)x, (uint)y);
				}
			}
			return null;
		}

		private static bool IsOccupied(int x, int y, List<MapLocation> occupiedTiles) => occupiedTiles.Any(t => t.X == (uint)x && t.Y == (uint)y);

		private static StartPositionResult Success(StartPositionCandidate candidate, MapLocation position, StartPositionContext context)
		{
			bool placeSecondSettler = context.Difficulty == (int)DifficultyLevel.Chieftain && context.RandomService.Hit(50);

			List<MapLocation> additionalPositions = [.. candidate.AdditionalUnitTypes.Select(_ => position)];

			return new StartPositionResult
			{
				Civilization = candidate.Civilization,
				Success = true,
				Position = position,
				PlaceSecondSettlerAtSamePosition = placeSecondSettler,
				AdditionalUnitPositions = additionalPositions
			};
		}
	}
}
