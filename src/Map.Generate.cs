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
using System.Threading.Tasks;
using System.Diagnostics;
using CivOne.Enums;
using CivOne.Tiles;
using System.Collections;
using System.Threading;

namespace CivOne
{
    public partial class Map
    {
        private bool TileHasHut( int x, int y )
        {
            if( y < 2 || y > ( HEIGHT - 3 ) ) return false;
            return ModGrid( x, y ) == ( ( x / 4 ) * 13 + ( y / 4 ) * 11 + _terrainMasterWord + 8 ) % 32;
        }

        /// <summary>
        /// Number of generation stages shown in the intro progress UI.
        /// </summary>
        private const int IntroVisibleGenerationStageCount = 8;

        private enum Stages
        {
            MergeElevationAndLatitude = 1,
            ClimateAdjustments = 2,
            AgeAdjustments = 3,
            CreateRivers = 4,
            CalculateContinentSize = 5,
            CreatePoles = 6,
            PlaceHuts = 7,
            CalculateLandValue = 8
        }

		private Climate ClimateValue => _climate ?? Climate.Normal;

        /// <summary>
        /// Reduces mountain generation so more hills remain available for rivers, indexed by age.
        /// Higher values produce fewer mountains and more hills.
        /// </summary>
        private static readonly int[] MountainGenerationReductionPercentByAge = [50, 70, 85];

        internal static int ComputeMountainGenerationReductionPercent( EarthAge age )
        {
            return ComputeMountainGenerationReductionPercent( (int)age );
        }

        internal static int ComputeMountainGenerationReductionPercent( int age )
        {
            int ageIndex = Math.Clamp( age, 0, MountainGenerationReductionPercentByAge.Length - 1 );
            return MountainGenerationReductionPercentByAge[ ageIndex ];
        }

        private int ComputeMountainGenerationReductionPercent()
        {
            return ComputeMountainGenerationReductionPercent( _ageValue );
        }

        private int ComputeMountainUpliftChancePercent()
        {
            int baseChance = Math.Clamp( 65 - ( _ageValue * 20 ), 10, 75 );
            int reducedChance = ( baseChance * ( 100 - ComputeMountainGenerationReductionPercent() ) ) / 100;
            return Math.Clamp( reducedChance, 5, 75 );
        }

        private int ComputeMountainErosionChancePercent()
        {
            int baseChance = Math.Clamp( ( _ageValue * 12 ) - 4, 0, 40 );
            int increasedChance = ( baseChance * ( 100 + ComputeMountainGenerationReductionPercent() ) ) / 100;
            return Math.Clamp( increasedChance, 0, 60 );
        }

        private int[,] TemperatureAdjustments()
        {
            // Normalized distance from equator: 0 = equator, 1 = pole.
            const float DesertLimit = 0.20F;
            // Up to 20% from equator stays in the hottest band.
            const float PlainsLimit = 0.78F;
            // Up to 78% from equator stays temperate/plains.
            const float TundraLimit = 0.94F;
            // Beyond 94% from equator becomes arctic.
            Log( "Map: Stage 2 - Temperature adjustments" );

            int[,] latitude = new int[ WIDTH, HEIGHT ];
            float equator = ( HEIGHT - 1 ) / 2F;
            float maxDistanceFromEquator = Math.Max( 1F, equator );
            float randomDrift = Math.Max( 0.01F, 1F / maxDistanceFromEquator );
            float temperatureShift = ( 1 - _temperatureValue ) * 0.04F;

            for( int y = 0; y < HEIGHT; y++ )
                for( int x = 0; x < WIDTH; x++ )
                {
                    float latitudePosition = Math.Abs( y - equator ) / maxDistanceFromEquator;
                    latitudePosition += ( _randomService.Next( 3 ) - 1 ) * randomDrift;
                    latitudePosition += temperatureShift;
                    latitudePosition = Math.Clamp( latitudePosition, 0F, 1F );

                    latitude[ x, y ] = latitudePosition switch
                    {
                        <= DesertLimit => 0,
                        <= PlainsLimit => 1,
                        <= TundraLimit => 2,
                        _ => 3,
                    };
                }

            return latitude;
        }

        private void MergeElevationAndLatitude( int[,] elevation, int[,] latitude )
        {
            Log( "Map: Stage 3 - Merge elevation and latitude into the map" );

            // merge elevation and latitude into the map
            for( int y = 0; y < HEIGHT; y++ )
                for( int x = 0; x < WIDTH; x++ )
                {
                    bool special = TileIsSpecial( x, y );
                    switch( elevation[ x, y ] )
                    {
                        case 0: _tiles[ x, y ] = new Ocean( x, y, special ); break;
                        case 1:
                            {
                                switch( latitude[ x, y ] )
                                {
                                    case 0: _tiles[ x, y ] = new Desert( x, y, special ); break;
                                    case 1: _tiles[ x, y ] = new Plains( x, y, special ); break;
                                    case 2: _tiles[ x, y ] = new Tundra( x, y, special ); break;
                                    case 3: _tiles[ x, y ] = new Arctic( x, y, special ); break;
                                }
                            }
                            break;
                        case 2:
                        case 3: _tiles[ x, y ] = new Hills( x, y, special ); break;
                        default:
                            _tiles[ x, y ] = _randomService.Next( 100 ) < ComputeMountainGenerationReductionPercent()
                                ? new Hills( x, y, special )
                                : new Mountains( x, y, special );
                            break;
                    }
                }
        }

        private void ClimateAdjustments()
        {
            Log( "Map: Stage 4 - Climate adjustments" );

            int wetness, latitude;
            int equator = HEIGHT / 2;
            int wetnessLatitudeOffset = (int)Math.Round((HEIGHT * 12F) / 50F);

            for( int y = 0; y < HEIGHT; y++ )
            {
                int yy = y;

                wetness = 0;
                latitude = Math.Abs( equator - yy );

                for( int x = 0; x < WIDTH; x++ )
                {
                    if( _tiles[ x, y ].Type == Terrain.Ocean )
                    {
                        // wetness yield
                        int wy = latitude - wetnessLatitudeOffset;
                        if( wy < 0 ) wy = -wy;
                        wy += ( (int)ClimateValue * 4 );

                        if( wy > wetness ) wetness++;
                    }
                    else if( wetness > 0 )
                    {
                        bool special = TileIsSpecial( x, y );
                        int rainfall = _randomService.Next( 7 - ( (int)ClimateValue * 2 ) );
                        wetness -= rainfall;

                        switch( _tiles[ x, y ].Type )
                        {
                            case Terrain.Plains: _tiles[ x, y ] = new Grassland( x, y ); break;
                            case Terrain.Tundra: _tiles[ x, y ] = new Arctic( x, y, special ); break;
                            case Terrain.Hills: _tiles[ x, y ] = new Forest( x, y, special ); break;
                            case Terrain.Desert: _tiles[ x, y ] = new Plains( x, y, special ); break;
                            case Terrain.Mountains: wetness -= 3; break;
                        }
                    }
                }

                wetness = 0;
                latitude = Math.Abs( equator - yy );

                // reset row wetness to 0
                for( int x = WIDTH - 1; x >= 0; x-- )
                {
                    if( _tiles[ x, y ].Type == Terrain.Ocean )
                    {
                        // wetness yield
                        int wy = ( latitude / 2 ) + (int)ClimateValue;
                        if( wy > wetness ) wetness++;
                    }
                    else if( wetness > 0 )
                    {
                        bool special = TileIsSpecial( x, y );
                        int rainfall = _randomService.Next( 7 - ( (int)ClimateValue * 2 ) );
                        wetness -= rainfall;

                        switch( _tiles[ x, y ].Type )
                        {
                            case Terrain.Swamp: _tiles[ x, y ] = new Forest( x, y, special ); break;
                            case Terrain.Plains: _tiles[ x, y ] = new Grassland( x, y ); break;
                            case Terrain.Grassland1:
                            case Terrain.Grassland2: _tiles[ x, y ] = new Jungle( x, y, special ); break;
                            case Terrain.Hills: _tiles[ x, y ] = new Forest( x, y, special ); break;
                            case Terrain.Mountains: _tiles[ x, y ] = new Forest( x, y, special ); wetness -= 3; break;
                            case Terrain.Desert: _tiles[ x, y ] = new Plains( x, y, special ); break;
                        }
                    }
                }
            }
        }

        private void AgeAdjustments()
        {
            Log( "Map: Stage 5 - Age adjustments" );

            int x = 0;
            int y = 0;
            int mountainUpliftChancePercent = ComputeMountainUpliftChancePercent();
            int mountainErosionChancePercent = ComputeMountainErosionChancePercent();
            int ageRepeat = (int)( ( (float)800 * ( 1 + _ageValue ) / DefaultMapArea ) * ( WIDTH * HEIGHT ) );
            for( int i = 0; i < ageRepeat; i++ )
            {
                if( i % 2 == 0 )
                {
                    x = _randomService.Next( WIDTH );
                    y = _randomService.Next( HEIGHT );
                }
                else
                {
                    switch( _randomService.Next( 8 ) )
                    {
                        case 0: { x--; y--; break; }
                        case 1: { y--; break; }
                        case 2: { x++; y--; break; }
                        case 3: { x--; break; }
                        case 4: { x++; break; }
                        case 5: { x--; y++; break; }
                        case 6: { y++; break; }
                        default: { x++; y++; break; }
                    }
                    if( x < 0 ) x = 1;
                    if( y < 0 ) y = 1;
                    if( x >= WIDTH ) x = WIDTH - 2;
                    if( y >= HEIGHT ) y = HEIGHT - 2;
                }

                bool special = TileIsSpecial( x, y );
                switch( _tiles[ x, y ].Type )
                {
                    case Terrain.Forest: _tiles[ x, y ] = new Jungle( x, y, special ); break;
                    case Terrain.Swamp: _tiles[ x, y ] = new Grassland( x, y ); break;
                    case Terrain.Plains: _tiles[ x, y ] = new Hills( x, y, special ); break;
                    case Terrain.Tundra: _tiles[ x, y ] = new Hills( x, y, special ); break;
                    case Terrain.River: _tiles[ x, y ] = new Forest( x, y, special ); break;
                    case Terrain.Grassland1:
                    case Terrain.Grassland2: _tiles[ x, y ] = new Forest( x, y, special ); break;
                    case Terrain.Jungle: _tiles[ x, y ] = new Swamp( x, y, special ); break;
                    case Terrain.Hills:
                        if( _randomService.Next( 100 ) < mountainUpliftChancePercent )
                        {
                            _tiles[ x, y ] = new Mountains( x, y, special );
                        }
                        break;
                    case Terrain.Mountains:
                        if( ( x == 0 || _tiles[ x - 1, y - 1 ].Type != Terrain.Ocean ) &&
                            ( y == 0 || _tiles[ x + 1, y - 1 ].Type != Terrain.Ocean ) &&
                            ( x == ( WIDTH - 1 ) || _tiles[ x + 1, y + 1 ].Type != Terrain.Ocean ) &&
                            ( y == ( HEIGHT - 1 ) || _tiles[ x - 1, y + 1 ].Type != Terrain.Ocean ) )
                            _tiles[ x, y ] = new Ocean( x, y, special );
                        else if( _randomService.Next( 100 ) < mountainErosionChancePercent )
                            _tiles[ x, y ] = new Hills( x, y, special );
                        break;
                    case Terrain.Desert: _tiles[ x, y ] = new Plains( x, y, special ); break;
                    case Terrain.Arctic:
                        if( _randomService.Next( 100 ) < ( mountainUpliftChancePercent / 2 ) )
                        {
                            _tiles[ x, y ] = new Mountains( x, y, special );
                        }
                        break;
                }
            }
        }

        private void CreateRivers()
        {
            new RiverCreationDelegate(
                WIDTH,
                HEIGHT,
                _tiles,
                _randomService,
                ( x, y ) => NearOcean( x, y ),
                ( x, y ) => TileIsSpecial( x, y ),
                ( format, parameters ) => Log( format, parameters ),
                ClimateValue,
                _landMassValue ).CreateRivers();
        }

        // This is a recursive function used to mark all tiles in a continent with a continent/ocean number. That
        // number will then be corrected so continents/ocans are numberd in size order

        readonly int[,] aiRelPos = { { -1, 0 }, { 0, -1 }, { 0, 1 }, { 1, 0 } };  // Check "Manhattan" conections only

        private int ContinentId;
        private ulong ContinetSize;

        /* ***********************************************************************************************************/

        private struct Continent
        {
            public int ContinentId;
            public ulong ContinetSize;
        };

        private readonly List<Continent> Continents = new List<Continent>();
        private List<Continent> ContinentsSorted = new List<Continent>();

        private void CalculateContinentSize()
        {
            Log( "Map: Calculate continent/ocean sizes and give continents a number in size order" );

            for( int y = 0; y < HEIGHT; y++ )       // todo  remove JR
                for( int x = 0; x < WIDTH; x++ )
                    this[ x, y ].ContinentId = 0;

            int[] traversalContinentIds = new int[ WIDTH * HEIGHT ];

            int nTiles = 0;
            bool oOcean = false;
            for( int j = 0; j < 2; j++ )
            {
                Continents.Clear();
                ContinentId = 0;

                Array.Clear( traversalContinentIds, 0, traversalContinentIds.Length );

                ContinentTraversalDelegate continentTraversal = new ContinentTraversalDelegate(
                    WIDTH,
                    HEIGHT,
                    aiRelPos,
                    (x, y) => this[ x, y ].IsOcean,
                    (x, y) => traversalContinentIds[ ( y * WIDTH ) + x ],
                    (x, y, continentId) => traversalContinentIds[ ( y * WIDTH ) + x ] = continentId);

                for( int y = 0; y < HEIGHT; y++ )
                    for( int x = 0; x < WIDTH; x++ )
                        if( traversalContinentIds[ ( y * WIDTH ) + x ] == 0 && ( this[ x, y ].IsOcean == oOcean ))
                        {  // Found a "new" continent/ocean
                            ContinentId++;
                            ContinetSize = continentTraversal.CountContinent( x, y, oOcean, ContinentId );
                            Continent continent;
                            continent.ContinentId = ContinentId;
                            continent.ContinetSize = ContinetSize;
                            Continents.Add( continent );

                        }
                ContinentsSorted = Continents.OrderByDescending( x => x.ContinetSize ).ToList();
                int[] _iConvTbl = new int[ ContinentsSorted.Count + 1 ];

                for( int i = 0; i < ContinentsSorted.Count; i++ )
                {
                    if( oOcean ) 
                        Log( "Map: ocean Nr = {0}, Size {1}", 
                            ContinentsSorted[ i ].ContinentId, ContinentsSorted[ i ].ContinetSize );
                    else 
                        Log( "Map: Continent Nr = {0}, Size {1}", 
                            ContinentsSorted[ i ].ContinentId, ContinentsSorted[ i ].ContinetSize );
                    _iConvTbl[ ContinentsSorted[ i ].ContinentId ] = i + 1;
                }

                // Give all ITiles their correct continent/ocean number
                for( int y = 0; y < HEIGHT; y++ )
                    for( int x = 0; x < WIDTH; x++ )
                    {
                        if( this[ x, y ].IsOcean != oOcean ) 
                            continue;
                        int continentId = traversalContinentIds[ ( y * WIDTH ) + x ];
                        this[ x, y ].ContinentId = _iConvTbl[ continentId ];
                        nTiles++;       // Just a check

                    }
                oOcean = true;
            }
            Log( "Map: Total number of tiles = {0}", nTiles );
        }

        private void CreatePoles()
		{
			Log("Map: Creating poles");
			
			for (int x = 0; x < WIDTH; x++)
            {
                foreach (int y in new[] { 0, (HEIGHT - 1) })
                {
                    _tiles[x, y] = new Arctic(x, y, false);
                }
            }
			
			for (int i = 0; i < (WIDTH / 4); i++)
            {
                foreach (int y in new[] { 0, 1, (HEIGHT - 2), (HEIGHT - 1) })
                {
                    int x = _randomService.Next(WIDTH);
                    _tiles[x, y] = new Tundra(x, y, false);
                }
            }
		}
		
		private void PlaceHuts()
		{
			Log("Map: Placing goody huts");
			
			for (int y = 0; y < HEIGHT; y++)
			for (int x = 0; x < WIDTH; x++)
			{
				if (_tiles[x, y].Type == Terrain.Ocean) continue;
				_tiles[x, y].Hut = TileHasHut(x, y);
			}
		}

		private void CalculateLandValue()
		{
			Log("Map: Calculating land value");
			
			// This code is a translation of Darkpanda's forum post here: http://forums.civfanatics.com/showthread.php?t=498532
			// Comments are pasted from the original forum thread to make the code more readable.
			
			// map squares for which the land value is calculated are in the range [2,2] - [77,47]
			for (int y = 2; y < HEIGHT - 2; y++)
			for (int x = 2; x < WIDTH - 2; x++)
			{
				// initial value is 0
				_tiles[x, y].LandValue = 0;
				
				// If the square's terrain type is not Plains, Grassland or River, then its land value is 0
				if (!TileIsType(_tiles[x, y], Terrain.Plains, Terrain.Grassland1, Terrain.Grassland2, Terrain.River)) continue;
				
				// for each 'city square' neighbouring the map square (i.e. each square following the city area pattern,
				// including the map square itself, so totally 21 'neighbours'), compute the following neighbour value (initially 0):
				int landValue = 0;
				for (int yy = -2; yy <= 2; yy++)
				for (int xx = -2; xx <= 2; xx++)
				{
					// Skip the corners of the square to create a city area pattern
					if (Math.Abs(xx) == 2 && Math.Abs(yy) == 2) continue;
					
					// initial value is 0
					int val = 0;
					
					ITile tile = _tiles[x + xx, y + yy];
					if (tile.Special && TileIsType(tile, Terrain.Grassland1, Terrain.Grassland2, Terrain.River))
					{
						// If the neighbour square type is Grassland special or River special, add 2,
						// then add the non-special Grassland or River terrain type score to the neighbour value
						val += 2;
						if (tile.Type == Terrain.River)
							val += (new River()).LandScore;
						else
							val += (new Grassland()).LandScore;
					}
					else
					{
						// Else add neighbour's terrain type score to the neighbour value
						val += tile.LandScore;
					}
					
					// If the neighbour square is in the map square inner circle, i.e. one of the 8 neighbours immediatly
					// surrounding the map square, then multiply the neighbour value by 2
					if (Math.Abs(xx) <= 1 && Math.Abs(yy) <= 1 && (xx != 0 || yy != 0)) val *= 2;
					
					// If the neighbour square is the North square (relative offset 0,-1), then multiply the neighbour value by 2 ;
					// note: I actually think that this is a bug, and that the intention was rather to multiply by 2 if the 'neighbour'
					// was the central map square itself... the actual CIV code for this is to check if the 'neighbour index' is '0';
					// the neighbour index is used to retrieve the neighbour's relative offset coordinates (x,y) from the central square,
					// and the central square itself is actually the last in the list (index 20), the first one (index 0) being
					// the North neighbour; another '7x7 neighbour pattern' table found in CIV code does indeed set the central square
					// at index 0, and this why I believe ths is a programmer's mistake...
					if (xx == 0 && yy == -1) val *= 2;
					
					// Add the neighbour's value to the map square total value and loop to next neighbour
					landValue += val;
				}
				
				// After all neighbours are processed, if the central map square's terrain type is non-special Grassland or River,
				// subtract 16 from total land value
				if (!_tiles[x, y].Special && TileIsType(_tiles[x, y], Terrain.Grassland1, Terrain.River)) landValue -= 16;
				
				landValue -= 120; // Substract 120 (0x78) from the total land value,
				bool negative = (landValue < 0); // and remember its sign
				landValue = Math.Abs(landValue); // Set the land value to the absolute land value (i.e. negate it if it is negative)
				landValue /= 8; // Divide the land value by 8
				if (negative) landValue = 1 - landValue; // If the land value was negative 3 steps before, then negate the land value and add 1
				
				// Adjust the land value to the range [1..15]
				if (landValue < 1) landValue = 1;
				if (landValue > 15) landValue = 15;
				
				landValue /= 2; // Divide the land value by 2
				landValue += 8; // And finally, add 8 to the land value
				_tiles[x, y].LandValue = (byte)landValue;
			}
		}
		
		private void GenerateThread()
		{
            Stopwatch totalGenerationTimer = Stopwatch.StartNew();
            try
			{
				Log("Generating map (Land Mass: {0}, Temperature: {1}, Climate: {2}, Age: {3})", _landMassValue, _temperatureValue, _climateValue, _ageValue);

				SetGenerationProgress(0, IntroVisibleGenerationStageCount, 0);

				_tiles = new ITile[WIDTH, HEIGHT];

				int[,] elevation = GenerateLandMass();
				int[,] latitude = TemperatureAdjustments();
				SetGenerationProgress((int)Stages.MergeElevationAndLatitude, IntroVisibleGenerationStageCount, (int)Stages.MergeElevationAndLatitude);
				MergeElevationAndLatitude(elevation, latitude);
				SetGenerationProgress((int)Stages.ClimateAdjustments, IntroVisibleGenerationStageCount, (int)Stages.ClimateAdjustments);
				ClimateAdjustments();
				SetGenerationProgress((int)Stages.AgeAdjustments, IntroVisibleGenerationStageCount, (int)Stages.AgeAdjustments);
				AgeAdjustments();
				SetGenerationProgress((int)Stages.CreateRivers, IntroVisibleGenerationStageCount, (int)Stages.CreateRivers);
				CreateRivers();

				SetGenerationProgress((int)Stages.CalculateContinentSize, IntroVisibleGenerationStageCount, (int)Stages.CalculateContinentSize);
				CalculateContinentSize();
				SetGenerationProgress((int)Stages.CreatePoles, IntroVisibleGenerationStageCount, (int)Stages.CreatePoles);
				CreatePoles();
				SetGenerationProgress((int)Stages.PlaceHuts, IntroVisibleGenerationStageCount, (int)Stages.PlaceHuts);
				PlaceHuts();
				SetGenerationProgress((int)Stages.CalculateLandValue, IntroVisibleGenerationStageCount, (int)Stages.CalculateLandValue);
				CalculateLandValue();

				SetReady(true);
				SetGenerationProgress(IntroVisibleGenerationStageCount, IntroVisibleGenerationStageCount, (int)Stages.CalculateLandValue);
				Log("Map: Ready");
			}
			catch (Exception ex) 
            {
                Log("Map generation failed: {0}", ex);
                SetError(true, "Error generating map. See logs for more information.");
            }
            finally
            {
                totalGenerationTimer.Stop();
                TimeSpan totalElapsed = totalGenerationTimer.Elapsed;
                int elapsedMinutes = (int)totalElapsed.TotalMinutes;
                int elapsedSeconds = totalElapsed.Seconds;
                Log( "Map: Total generation duration {0}:{1:00} min ({2:0.000} s, {3} ms)", elapsedMinutes, elapsedSeconds, totalElapsed.TotalSeconds, totalGenerationTimer.ElapsedMilliseconds );
            }
		}

		private int[,] GenerateLandMass()
		{
			return new LandElevationGeneratorDelegate(
								WIDTH,
								HEIGHT,
								_landMassValue,
								_randomService,
								Log).GenerateLandMass();
		}


		/// <summary>
		/// Generates a map using typed world-generation presets.
		/// </summary>
		/// <param name="landMass">Land mass preset.</param>
		/// <param name="temperature">Temperature preset.</param>
		/// <param name="climate">Climate preset.</param>
		/// <param name="age">Earth age preset.</param>
		public void Generate( LandMass landMass = LandMass.Normal, Temperature temperature = Temperature.Temperate, Climate climate = Climate.Normal, EarthAge age = EarthAge.FourBillionYears )
        {
            if (Ready || _tiles != null)
            {
                Log("ERROR: Map is already load{0}/generat{0}", (Ready ? "ed" : "ing"));
                return;
            }
			
            _landMass = landMass;
            _landMassValue = (int)landMass;
            _temperature = temperature;
            _temperatureValue = (int)temperature;
            _climate = climate;
            _climateValue = (int)climate;
            _age = age;
            _ageValue = (int)age;
			
            Task.Run(() => GenerateThread());
        }
	}
}
