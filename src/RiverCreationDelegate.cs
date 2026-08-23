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
using CivOne.Enums;
using CivOne.Services.Random;
using CivOne.Tiles;

namespace CivOne
{
	internal sealed class RiverCreationDelegate
	{
		private const int RiverWalkMinimumSteps = 1024;
		private const int RiverWalkStepFactor = 2;
		private const int RiverMinimumSuccessfulLength = 3;

		/// <summary>
		/// Minimum number of candidate-start scans for one river attempt, indexed by climate.
		/// Higher values make river creation more persistent when start tiles are scarce.
		/// </summary>
		internal static readonly int[] RiverStartSearchMinimumAttemptsByClimate = [1024, 2048, 4096];

		/// <summary>
		/// Scales river-start scans with map size.
		/// Higher values increase search effort on larger maps.
		/// </summary>
		internal static readonly int[] RiverStartSearchFactorByClimate = [3, 4, 5];

		/// <summary>
		/// River target multiplier by climate, indexed by arid, normal, wet.
		/// Higher values create more rivers in wetter worlds.
		/// </summary>
		internal static readonly int[] RiverPercentByClimate = [500, 2000, 10000];

		/// <summary>
		/// Chance for a river to start on non-hill land by climate, indexed by arid, normal, wet.
		/// Higher values increase fallback river starts on plains or grassland.
		/// </summary>
		internal static readonly int[] RiverFallbackChanceByClimate = [0, 25, 50];

		internal static int ComputeClimateRiverPercent( Climate climate )
		{
			return RiverPercentByClimate[ Math.Clamp( (int)climate, 0, RiverPercentByClimate.Length - 1 ) ];
		}

		internal static int ComputeClimateRiverFallbackChancePercent( Climate climate )
		{
			return RiverFallbackChanceByClimate[ Math.Clamp( (int)climate, 0, RiverFallbackChanceByClimate.Length - 1 ) ];
		}

		internal static int ComputeClimateRiverStartSearchMinimumAttempts( Climate climate )
		{
			return RiverStartSearchMinimumAttemptsByClimate[ Math.Clamp( (int)climate, 0, RiverStartSearchMinimumAttemptsByClimate.Length - 1 ) ];
		}

		internal static int ComputeClimateRiverStartSearchFactor( Climate climate )
		{
			return RiverStartSearchFactorByClimate[ Math.Clamp( (int)climate, 0, RiverStartSearchFactorByClimate.Length - 1 ) ];
		}

		private readonly int _width;
		private readonly int _height;
		private readonly ITile[,] _tiles;
		private readonly IRandomService _randomService;
		private readonly Func<int, int, bool> _nearOcean;
		private readonly Func<int, int, bool> _tileIsSpecial;
		private readonly Action<string, object[]> _log;
		private readonly int _climateValue;
		private readonly int _landMass;
		private readonly int _climateRiverPercent;
		private readonly int _fallbackChance;
		private readonly int _minStartSearchAttempts;
		private readonly int _startSearchFactor;

		internal RiverCreationDelegate(
			int width,
			int height,
			ITile[,] tiles,
			IRandomService randomService,
			Func<int, int, bool> nearOcean,
			Func<int, int, bool> tileIsSpecial,
			Action<string, object[]> log,
			Climate climate,
			int landMass)
		{
			ArgumentNullException.ThrowIfNull( tiles );
			ArgumentNullException.ThrowIfNull( randomService );
			ArgumentNullException.ThrowIfNull( nearOcean );
			ArgumentNullException.ThrowIfNull( tileIsSpecial );
			ArgumentNullException.ThrowIfNull( log );

			_width = width;
			_height = height;
			_tiles = tiles;
			_randomService = randomService;
			_nearOcean = nearOcean;
			_tileIsSpecial = tileIsSpecial;
			_log = log;
			_climateValue = (int)climate;
			_landMass = landMass;
			_climateRiverPercent = ComputeClimateRiverPercent( climate );
			_fallbackChance = ComputeClimateRiverFallbackChancePercent( climate );
			_minStartSearchAttempts = ComputeClimateRiverStartSearchMinimumAttempts( climate );
			_startSearchFactor = ComputeClimateRiverStartSearchFactor( climate );
		}

		internal void CreateRivers()
		{
			CreateRiversFaster();
		}


		private ITile? GetTile( int x, int y )
		{
			if( y < 0 || y >= _height )
			{
				return null;
			}

			while( x < 0 )
			{
				x += _width;
			}

			x %= _width;
			return _tiles[ x, y ];
		}

		/// <summary>
		/// This method creates rivers by running bounded random walks from valid hill or mountain start tiles.
		/// It uses attempt and step limits so map generation stays predictable and fast.
		/// </summary>
		private void CreateRiversFaster()
		{
			Log("Map: Stage 6 - Create rivers");

			List<int> hillTileIndices = [];
			List<int> fallbackHillTileIndices = [];
			List<int> landTileIndices = [];

			InitializeTileIndices(hillTileIndices, fallbackHillTileIndices, landTileIndices);
			bool flowControl = HasValidRiverStartTiles(ref hillTileIndices, fallbackHillTileIndices, landTileIndices);
			if (!flowControl)
			{
				return;
			}

			int rivers = 0;
			int baseRiverCount = ((_climateValue + _landMass) * 2) + 6;
			int targetRiverCount = Math.Max(1, (int)Math.Round(baseRiverCount * (_climateRiverPercent / 100D), MidpointRounding.AwayFromZero));
			int maxRiverAttempts = Math.Max(768, targetRiverCount * 160);
			bool useLandFallback = hillTileIndices.Count == 0 && landTileIndices.Count > 0 && _fallbackChance > 0;
			int maxRiverWalkSteps = Math.Max(RiverWalkMinimumSteps, _width * _height * RiverWalkStepFactor);
			
			Log("Map: Stage 6 - River target base {0}, climate percent {1}%, final target {2}.", baseRiverCount, _climateRiverPercent, targetRiverCount);
			// The main loop attempts to create rivers until it reaches the target river count or exhausts the maximum number of attempts. 
			// In each attempt, it randomly selects a starting tile from the list of hills or mountains tiles (or land tiles if the fallback is being used), 
			// and performs a random walk to create a river. The walk continues until it reaches the ocean or another river, or exceeds the maximum number of steps. 
			// If a valid river is created, it updates the map accordingly; otherwise, it reverts any changes made during the attempt.
			for (int i = 0; i < maxRiverAttempts && rivers < targetRiverCount && (hillTileIndices.Count > 0 || useLandFallback); i++)
			{
				List<int> riverStartPool = hillTileIndices;
				bool usingLandFallbackThisAttempt = false;

				if (riverStartPool.Count == 0)
				{
					// If there are no valid hills or mountains tiles to start a river, 
					// and the fallback chance is greater than zero, use land tiles as potential river start points.
					if (!useLandFallback || _randomService.NextInt(100) >= _fallbackChance)
					{
						continue;
					}

					riverStartPool = landTileIndices;
					usingLandFallbackThisAttempt = true;
				}

				List<int> riverStartCandidates = [.. riverStartPool];
				int maxRiverStartSearchAttempts = Math.Min(Math.Max(_minStartSearchAttempts, _width * _height * _startSearchFactor), riverStartCandidates.Count);
				ITile? tile = null;
				int? selectedStartTileIndex = null;

				// To find a starting tile for the river, it randomly selects tiles from the candidate pool (which is either hills/mountains or land tiles depending on 
				// availability and fallback usage) and checks if they are valid starting points (hills or mountains). 
				// It limits the number of attempts to find a valid starting tile to avoid performance issues. 
				// If it cannot find a valid starting tile within the allowed attempts, it logs a message and skips the river creation attempt.
				for (int startSearchAttempts = 0; startSearchAttempts < maxRiverStartSearchAttempts && riverStartCandidates.Count > 0; startSearchAttempts++)
				{
					int hillIndex = _randomService.NextInt(riverStartCandidates.Count);
					int tileIndex = riverStartCandidates[hillIndex];
					riverStartCandidates.RemoveAt(hillIndex);
					int x = tileIndex % _width;
					int y = tileIndex / _width;
					ITile candidate = _tiles[x, y];
					if (usingLandFallbackThisAttempt || candidate.Type == Terrain.Hills || candidate.Type == Terrain.Mountains)
					{
						tile = candidate;
						selectedStartTileIndex = tileIndex;
						break;
					}
				}

				if (tile == null)
				{
					Log("Map: Stage 6 - Could not find hills or mountains tile after {0} attempts; stopping river creation.", maxRiverStartSearchAttempts);
					break;
				}

				int riverLength = 0;

				// directionIndex is used to determine the direction of the river walk. 
				// It is initialized to a random value and then updated in each step of the walk to create a somewhat natural river path.
				int directionIndex = _randomService.NextInt(4) * 2;
				bool nearOcean = false;
				bool riverWalkFailed = false;
				Dictionary<int, ITile> changedTiles = [];

				do
				{
					int currentIndex = (tile.Y * _width) + tile.X;
					if (!changedTiles.ContainsKey(currentIndex))
					{
						// if the tile has not already been changed during this river walk, add
						// it to the changedTiles dictionary so that it can be reverted if the river walk fails
						changedTiles.Add(currentIndex, _tiles[tile.X, tile.Y]);
					}

					_tiles[tile.X, tile.Y] = new River(tile.X, tile.Y);
					
					int riverWalkStepCount = _randomService.NextInt(2);
					directionIndex = ((riverWalkStepCount - riverLength % 2) * 2 + directionIndex) & 0x07;
					riverLength++;

					nearOcean = _nearOcean(tile.X, tile.Y);

					// The river walk continues by updating the current tile to the next tile in the direction determined by directionIndex. 
					// If the next tile is null (which can happen if it goes out of bounds
					// due to the map wrapping or reaching the edge of the map), or if the river walk exceeds the maximum allowed steps, 
					// it marks the river walk as failed and breaks out of the loop.
					switch (directionIndex)
					{
						case 0:
						case 1: tile = GetTile(tile.X, tile.Y - 1); break;
						case 2:
						case 3: tile = GetTile(tile.X + 1, tile.Y); break;
						case 4:
						case 5: tile = GetTile(tile.X, tile.Y + 1); break;
						case 6:
						case 7: tile = GetTile(tile.X - 1, tile.Y); break;
					}

					if (tile == null)
					{
						riverWalkFailed = true;
						break;
					}

					if (riverLength >= maxRiverWalkSteps)
					{
						Log("Map: Stage 6 - River walk exceeded {0} steps; skipping river attempt.", maxRiverWalkSteps);
						riverWalkFailed = true;
						break;
					}
				}
				// we want to continue the river walk until we reach the ocean or another river, 
				// but we also want to stop if we encounter a null tile (which can happen if we go out of bounds due to the map wrapping or reaching the edge of the map)
				while (!nearOcean && (tile.GetType() != typeof(Ocean) && tile.GetType() != typeof(River)));

				bool reachedValidRiverEndpoint = nearOcean || tile?.Type == Terrain.River || tile?.Type == Terrain.Ocean;

				// After the river walk, it checks if the river walk was successful (i.e., it reached a valid endpoint and is of sufficient length). 
				// If the river walk was successful, it increments the river count and updates the map to reflect the new river. 
				// It also updates nearby tiles to turn forests into jungles, which is a natural progression in the game's terrain generation.
				if (!riverWalkFailed && tile != null && reachedValidRiverEndpoint && riverLength >= RiverMinimumSuccessfulLength)
				{
					rivers++;

					// If the river walk was successful, it increments the river count and updates the map to reflect the new river. 
					// It also updates nearby tiles to turn forests into jungles, which is a natural progression in the game's terrain generation.
					if (selectedStartTileIndex.HasValue)
					{
						hillTileIndices.Remove(selectedStartTileIndex.Value);
						if (usingLandFallbackThisAttempt)
						{
							landTileIndices.Remove(selectedStartTileIndex.Value);
						}
					}

					// If the river walk was successful, it increments the river count and updates the map to reflect the new river. 
					// It also updates nearby tiles to turn forests into jungles, which is a natural progression in the game's terrain generation.
					// This is fast way to update nearby tiles without having to check every tile on the map. 
					// It iterates over a 7x7 grid centered around the end tile of the river walk, and for each tile in that grid, it checks if it is a forest.
					ITile endTile = tile;
					for (int localX = 0; localX < 7; localX++)
					{
						int mapX = endTile.X - 3 + localX;
						while (mapX < 0)
						{
							mapX += _width;
						}

						mapX %= _width;
						for (int localY = 0; localY < 7; localY++)
						{
							int mapY = endTile.Y - 3 + localY;
							if (mapY < 0 || mapY >= _height)
							{
								continue;
							}

							if (_tiles[mapX, mapY].Type == Terrain.Forest)
							{
								_tiles[mapX, mapY] = new Jungle(mapX, mapY, _tileIsSpecial(mapX, mapY));
							}
						}
					}
				}
				else
				{
					// if the river walk was not successful, we need to revert any changes made to the
					// map during the river walk by using the changedTiles dictionary to restore the original tiles
					// This is necessary to ensure that failed river attempts do not leave behind partial rivers or other unintended terrain changes on the map.
					foreach (KeyValuePair<int, ITile> changedTile in changedTiles)
					{
						int x = changedTile.Key % _width;
						int y = changedTile.Key / _width;
						_tiles[x, y] = changedTile.Value;
					}
				}
			}

			Log("Map: Stage 6 - Rivers created {0}/{1}.", rivers, targetRiverCount);
		}

		private bool HasValidRiverStartTiles(ref List<int> hillTileIndices, List<int> fallbackHillTileIndices, List<int> landTileIndices)
		{
			if (hillTileIndices.Count != 0)
			{
				return true;
			}
			hillTileIndices = fallbackHillTileIndices;
			if (hillTileIndices.Count == 0)
			{
				if (_fallbackChance <= 0 || landTileIndices.Count == 0)
				{
					Log("Map: Stage 6 - No hills tiles or mountains available; skipping river creation.");
					return false;
				}

				Log("Map: Stage 6 - No hills tiles or mountains available; using land fallback chance {0}% with {1} tiles.", _fallbackChance, landTileIndices.Count);
			}

			return true;
		}

		private void InitializeTileIndices(List<int> hillTileIndices, List<int> fallbackHillTileIndices, List<int> landTileIndices)
		{
			for (int y = 0; y < _height; y++)
			{
				for (int x = 0; x < _width; x++)
				{
					Terrain terrain = _tiles[x, y].Type;
					int tileIndex = (y * _width) + x;

					if (terrain != Terrain.Ocean && terrain != Terrain.River)
					{
						landTileIndices.Add(tileIndex);
					}

					if (terrain == Terrain.Hills || terrain == Terrain.Mountains)
					{
						fallbackHillTileIndices.Add(tileIndex);
						if (!_nearOcean(x, y))
						{
							hillTileIndices.Add(tileIndex);
						}
					}
				}
			}
		}

		private void Log( string text, params object[] parameters )
		{
			_log( text, parameters );
		}
	}
}