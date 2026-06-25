// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Diagnostics;
using CivOne.Services.Random;

namespace CivOne
{
	internal sealed class LandElevationGeneratorDelegate
	{
		/// <summary>
		/// Minimum no-growth attempts allowed before Stage-1 land generation may stop.
		/// </summary>
		private const int LandGenerationAdaptiveBaseAttemptsMinimum = 20000;

		/// <summary>
		/// Safety floor for the no-growth hard cap to avoid endless loops.
		/// </summary>
		private const int LandGenerationAdaptiveSafetyCapMinimum = 500000;

		/// <summary>
		/// Per-tile cap factor used to compute the adaptive no-growth hard cap.
		/// </summary>
		private const int LandGenerationAdaptiveSafetyCapFactor = 64;

		/// <summary>
		/// Minimum total attempts allowed for Stage-1 land generation before hard stop.
		/// </summary>
		private const int LandGenerationTotalAttemptsMinimum = 2 * 250000;

		/// <summary>
		/// Per-tile factor used to compute Stage-1 total-attempt hard cap.
		/// </summary>
		private const int LandGenerationTotalAttemptsFactor = 12;

		/// <summary>
		/// No-growth multiplier used while less than 50% of target land is reached.
		/// </summary>
		private const int LandGenerationProgressMultiplierEarly = 80;

		/// <summary>
		/// No-growth multiplier used while 50%-80% of target land is reached.
		/// </summary>
		private const int LandGenerationProgressMultiplierMid = 160;

		/// <summary>
		/// No-growth multiplier used while at least 80% of target land is reached.
		/// </summary>
		private const int LandGenerationProgressMultiplierLate = 400;

		/// <summary>
		/// Base path length used for land chunk generation.
		/// Higher values create longer land chunks and usually more connected land.
		/// </summary>
		private const int LandChunkPathLengthBase = 48;

		/// <summary>
		/// Minimum land chunk path length after scaling.
		/// Lower values allow shorter land chunks on small maps.
		/// </summary>
		private const int LandChunkPathLengthMinimum = 64;

		/// <summary>
		/// Maximum land chunk path length after scaling.
		/// Higher values allow longer land chunks before the generator clamps them.
		/// </summary>
		private const int LandChunkPathLengthMaximum = 384;

		private const int DefaultMapArea = Map.DefaultMapWidth * Map.DefaultMapHeight;

		private readonly int _width;
		private readonly int _height;
		private readonly int _landMassValue;
		private readonly IRandomService _randomService;
		private readonly Action<string, object[]> _log;

		internal LandElevationGeneratorDelegate(
			int width,
			int height,
			int landMassValue,
			IRandomService randomService,
			Action<string, object[]> log )
		{
			ArgumentNullException.ThrowIfNull( randomService );
			ArgumentNullException.ThrowIfNull( log );

			_width = width;
			_height = height;
			_landMassValue = landMassValue;
			_randomService = randomService;
			_log = log;
		}

		private void Log( string text, params object[] parameters )
		{
			_log( text, parameters );
		}

		private int GenerateLandChunk( bool[] touchedTiles, int[] chunkTiles, int landChunkMaxPathLength )
		{
			int chunkTileCount = 0;

			int x = _randomService.NextInt( 4, _width - 4 );
			int y = _randomService.NextInt( 8, _height - 8 );
			int pathLength = _randomService.NextInt( 1, landChunkMaxPathLength + 1 );

			for( int i = 0; i < pathLength; i++ )
			{
				int index = y * _width + x;
				if( !touchedTiles[ index ] )
				{
					touchedTiles[ index ] = true;
					chunkTiles[ chunkTileCount++ ] = index;
				}

				index = y * _width + x + 1;
				if( !touchedTiles[ index ] )
				{
					touchedTiles[ index ] = true;
					chunkTiles[ chunkTileCount++ ] = index;
				}

				index = ( y + 1 ) * _width + x;
				if( !touchedTiles[ index ] )
				{
					touchedTiles[ index ] = true;
					chunkTiles[ chunkTileCount++ ] = index;
				}

				switch( _randomService.NextInt( 4 ) )
				{
					case 0: y--; break;
					case 1: x++; break;
					case 2: y++; break;
					default: x--; break;
				}

				if( x < 3 || y < 3 || x > ( _width - 4 ) || y > ( _height - 5 ) ) break;
			}

			for( int i = 0; i < chunkTileCount; i++ )
			{
				touchedTiles[ chunkTiles[ i ] ] = false;
			}

			return chunkTileCount;
		}

		private int ComputeTargetLandTiles( int landMassSize )
		{
			int maxReachableLandTiles = Math.Max( 1, ( _width - 5 ) * ( _height - 6 ) );
			int targetLandTiles = Math.Min( landMassSize, maxReachableLandTiles );

			if( targetLandTiles < landMassSize )
			{
				Log( "Map: Stage 1 - Requested land tiles ({0}) exceed generator reach ({1}); clamping target.", landMassSize, targetLandTiles );
			}

			return targetLandTiles;
		}

		private int ComputeAdaptiveNoGrowthLimit( int targetLandTiles, int landTiles )
		{
			int adaptiveBaseAttempts = Math.Max( LandGenerationAdaptiveBaseAttemptsMinimum, targetLandTiles );
			int adaptiveSafetyCap = Math.Max( LandGenerationAdaptiveSafetyCapMinimum, _width * _height * LandGenerationAdaptiveSafetyCapFactor );
			int remainingLandTiles = Math.Max( 1, targetLandTiles - landTiles );
			float progress = landTiles / (float)Math.Max( 1, targetLandTiles );
			int progressMultiplier = progress switch
			{
				< 0.50F => LandGenerationProgressMultiplierEarly,
				< 0.80F => LandGenerationProgressMultiplierMid,
				_ => LandGenerationProgressMultiplierLate,
			};

			int adaptiveLimit = Math.Max( adaptiveBaseAttempts, remainingLandTiles * progressMultiplier );
			return Math.Min( adaptiveLimit, adaptiveSafetyCap );
		}

		private int ComputeLandChunkMaxPathLength()
		{
			double areaScale = Math.Sqrt( ( _width * (double)_height ) / DefaultMapArea );
			double landMassScale = 1D + ( ( _landMassValue - 1 ) * 0.10D );
			int scaledPathLength = (int)Math.Round( LandChunkPathLengthBase * areaScale * landMassScale );
			return Math.Clamp( scaledPathLength, LandChunkPathLengthMinimum, LandChunkPathLengthMaximum );
		}

		private void GenerateLandElevation( int[] elevation, bool[] touchedTiles, int[] chunkTiles, int targetLandTiles, int landChunkMaxPathLength )
		{
			int landTiles = 0;
			int attemptsWithoutGrowth = 0;
			int totalAttempts = 0;
			int maxTotalAttempts = Math.Max( LandGenerationTotalAttemptsMinimum, _width * _height * LandGenerationTotalAttemptsFactor );

			while( landTiles < targetLandTiles )
			{
				totalAttempts++;
				int previousLandTiles = landTiles;
				int chunkTileCount = GenerateLandChunk( touchedTiles, chunkTiles, landChunkMaxPathLength );
				for( int i = 0; i < chunkTileCount; i++ )
				{
					int index = chunkTiles[ i ];
					if( elevation[ index ] == 0 ) landTiles++;
					elevation[ index ]++;
				}

				if( landTiles == previousLandTiles )
				{
					attemptsWithoutGrowth++;
					int adaptiveLimit = ComputeAdaptiveNoGrowthLimit( targetLandTiles, landTiles );
					if( attemptsWithoutGrowth >= adaptiveLimit )
					{
						Log( "Map: Stage 1 - No progress after {0} attempts (limit {1}); stopping at {2}/{3} land tiles.", attemptsWithoutGrowth, adaptiveLimit, landTiles, targetLandTiles );
						break;
					}
				}
				else
				{
					attemptsWithoutGrowth = 0;
				}

				if( totalAttempts >= maxTotalAttempts )
				{
					Log( "Map: Stage 1 - Reached total attempt cap ({0}); stopping at {1}/{2} land tiles.", maxTotalAttempts, landTiles, targetLandTiles );
					break;
				}
			}
		}

		private int[,] CopyElevationGrid( int[] elevation )
		{
			int[,] elevationGrid = new int[ _width, _height ];
			for( int y = 0; y < _height; y++ )
			{
				for( int x = 0; x < _width; x++ )
				{
					int index = y * _width + x;
					elevationGrid[ x, y ] = elevation[ index ];
				}
			}

			return elevationGrid;
		}

		private void RemoveNarrowPassages( int[,] elevationGrid )
		{
			for( int y = 0; y < ( _height - 1 ); y++ )
			{
				for( int x = 0; x < ( _width - 1 ); x++ )
				{
					if( ( elevationGrid[ x, y ] > 0 && elevationGrid[ x + 1, y + 1 ] > 0 ) && ( elevationGrid[ x + 1, y ] == 0 && elevationGrid[ x, y + 1 ] == 0 ) )
					{
						elevationGrid[ x + 1, y ]++;
						elevationGrid[ x, y + 1 ]++;
					}
					else if( ( elevationGrid[ x, y ] == 0 && elevationGrid[ x + 1, y + 1 ] == 0 ) && ( elevationGrid[ x + 1, y ] > 0 && elevationGrid[ x, y + 1 ] > 0 ) )
					{
						elevationGrid[ x + 1, y + 1 ]++;
					}
				}
			}
		}

		/// <summary>
		/// Generates the land mass elevation grid.
		/// </summary>
		/// <returns>A 2D elevation grid of size width × height.</returns>
		internal int[,] GenerateLandMass()
		{
			Log( "Map: Stage 1 - Generate land mass" );

			int[] elevation = new int[ _width * _height ];
			int landMassSize = (int)( ( _width * _height ) / 12.5 ) * ( _landMassValue + 2 );
			int landChunkMaxPathLength = ComputeLandChunkMaxPathLength();
			bool[] touchedTiles = new bool[ _width * _height ];
			int[] chunkTiles = new int[ landChunkMaxPathLength * 3 ];
			int targetLandTiles = ComputeTargetLandTiles( landMassSize );
			Log( "Map: Stage 1 - Land chunk max path length: {0}", landChunkMaxPathLength );

			// Generate the landmass
			Stopwatch stage1Timer = Stopwatch.StartNew();
			GenerateLandElevation( elevation, touchedTiles, chunkTiles, targetLandTiles, landChunkMaxPathLength );
			stage1Timer.Stop();
			TimeSpan stage1Elapsed = stage1Timer.Elapsed;
			Log( $"Map: Stage 1 - Landmass generation took {stage1Elapsed.TotalMinutes:0.00} min {stage1Elapsed.TotalSeconds:0.00} s ({stage1Timer.ElapsedMilliseconds} ms)" );

			Stopwatch copyTimer = Stopwatch.StartNew();
			int[,] elevationGrid = CopyElevationGrid( elevation );
			copyTimer.Stop();
			TimeSpan copyElapsed = copyTimer.Elapsed;
			Log( "Map: Stage 1 - Elevation copy loop took {0:0.00} min {1:0.00} s ({2} ms)", copyElapsed.TotalMinutes, copyElapsed.TotalSeconds, copyTimer.ElapsedMilliseconds );

			// remove narrow passages
			Stopwatch narrowPassageTimer = Stopwatch.StartNew();
			RemoveNarrowPassages( elevationGrid );
			narrowPassageTimer.Stop();
			TimeSpan narrowPassageElapsed = narrowPassageTimer.Elapsed;
			Log( "Map: Stage 1 - Narrow passage loop took {0:0.00} min {1:0.00} s ({2} ms)", narrowPassageElapsed.TotalMinutes, narrowPassageElapsed.TotalSeconds, narrowPassageTimer.ElapsedMilliseconds );

			return elevationGrid;
		}
	}
}
