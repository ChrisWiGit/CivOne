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
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CivOne.Enums;
using CivOne.Screens;
using CivOne.Tasks;
using CivOne.Tiles;
using System.Collections;
using CivOne.Services;

namespace CivOne
{
    public partial class Map
    {
        private bool[,] GenerateLandChunk()
        {
            bool[,] stencil = new bool[ WIDTH, HEIGHT ];

            int x = Common.Random.Next( 4, WIDTH - 4 );
            int y = Common.Random.Next( 8, HEIGHT - 8 );
            int pathLength = Common.Random.Next( 1, 64 );

            for( int i = 0; i < pathLength; i++ )
            {
                stencil[ x, y ] = true;
                stencil[ x + 1, y ] = true;
                stencil[ x, y + 1 ] = true;
                switch( Common.Random.Next( 4 ) )
                {
                    case 0: y--; break;
                    case 1: x++; break;
                    case 2: y++; break;
                    default: x--; break;
                }

                if( x < 3 || y < 3 || x > ( WIDTH - 4 ) || y > ( HEIGHT - 5 ) ) break;
            }

            return stencil;
        }

        private int[,] GenerateLandMass()
        {
            Log( "Map: Stage 1 - Generate land mass" );

            int[,] elevation = new int[ WIDTH, HEIGHT ];
            int landMassSize = (int)( ( WIDTH * HEIGHT ) / 12.5 ) * ( _landMass + 2 );

            // Generate the landmass
            while( ( from int tile in elevation where tile > 0 select 1 ).Sum() < landMassSize )
            {
                bool[,] chunk = GenerateLandChunk();
                for( int y = 0; y < HEIGHT; y++ )
                    for( int x = 0; x < WIDTH; x++ )
                    {
                        if( chunk[ x, y ] ) elevation[ x, y ]++;
                    }
            }

            // remove narrow passages
            for( int y = 0; y < ( HEIGHT - 1 ); y++ )
                for( int x = 0; x < ( WIDTH - 1 ); x++ )
                {
                    if( ( elevation[ x, y ] > 0 && elevation[ x + 1, y + 1 ] > 0 ) && ( elevation[ x + 1, y ] == 0 && elevation[ x, y + 1 ] == 0 ) )
                    {
                        elevation[ x + 1, y ]++;
                        elevation[ x, y + 1 ]++;
                    }
                    else if( ( elevation[ x, y ] == 0 && elevation[ x + 1, y + 1 ] == 0 ) && ( elevation[ x + 1, y ] > 0 && elevation[ x, y + 1 ] > 0 ) )
                    {
                        elevation[ x + 1, y + 1 ]++;
                    }
                }

            return elevation;
        }

        private int[,] TemperatureAdjustments()
        {
            Log( "Map: Stage 2 - Temperature adjustments" );

            int[,] latitude = new int[ WIDTH, HEIGHT ];

            for( int y = 0; y < HEIGHT; y++ )
                for( int x = 0; x < WIDTH; x++ )
                {
                    int l = (int)( ( (float)y / HEIGHT ) * 50 ) - 29;
                    l += Common.Random.Next( 7 );
                    if( l < 0 ) l = -l;
                    l += 1 - _temperature;

                    l = ( l / 6 ) + 1;

                    switch( l )
                    {
                        case 0:
                        case 1: latitude[ x, y ] = 0; break;
                        case 2:
                        case 3: latitude[ x, y ] = 1; break;
                        case 4:
                        case 5: latitude[ x, y ] = 2; break;
                        case 6:
                        default: latitude[ x, y ] = 3; break;
                    }
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
                    bool special = TileConverterService.HasExtraResourceOnTile( x, y );
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
                        case 2: _tiles[ x, y ] = new Hills( x, y, special ); break;
                        default: _tiles[ x, y ] = new Mountains( x, y, special ); break;
                    }
                }
        }

        private void ClimateAdjustments()
        {
            Log( "Map: Stage 4 - Climate adjustments" );

            int wetness, latitude;

            for( int y = 0; y < HEIGHT; y++ )
            {
                int yy = (int)( ( (float)y / HEIGHT ) * 50 );

                wetness = 0;
                latitude = Math.Abs( 25 - yy );

                for( int x = 0; x < WIDTH; x++ )
                {
                    if( _tiles[ x, y ].Type == Terrain.Ocean )
                    {
                        // wetness yield
                        int wy = latitude - 12;
                        if( wy < 0 ) wy = -wy;
                        wy += ( _climate * 4 );

                        if( wy > wetness ) wetness++;
                    }
                    else if( wetness > 0 )
                    {
                        bool special = TileConverterService.HasExtraResourceOnTile( x, y );
                        int rainfall = Common.Random.Next( 7 - ( _climate * 2 ) );
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
                latitude = Math.Abs( 25 - yy );

                // reset row wetness to 0
                for( int x = WIDTH - 1; x >= 0; x-- )
                {
                    if( _tiles[ x, y ].Type == Terrain.Ocean )
                    {
                        // wetness yield
                        int wy = ( latitude / 2 ) + _climate;
                        if( wy > wetness ) wetness++;
                    }
                    else if( wetness > 0 )
                    {
                        bool special = TileConverterService.HasExtraResourceOnTile( x, y );
                        int rainfall = Common.Random.Next( 7 - ( _climate * 2 ) );
                        wetness -= rainfall;

                        switch( _tiles[ x, y ].Type )
                        {
                            case Terrain.Swamp: _tiles[ x, y ] = new Forest( x, y, special ); break;
                            case Terrain.Plains: new Grassland( x, y ); break;
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
            int ageRepeat = (int)( ( (float)800 * ( 1 + _age ) / ( 80 * 50 ) ) * ( WIDTH * HEIGHT ) );
            for( int i = 0; i < ageRepeat; i++ )
            {
                if( i % 2 == 0 )
                {
                    x = Common.Random.Next( WIDTH );
                    y = Common.Random.Next( HEIGHT );
                }
                else
                {
                    switch( Common.Random.Next( 8 ) )
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

                bool special = TileConverterService.HasExtraResourceOnTile( x, y );
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
                    case Terrain.Hills: _tiles[ x, y ] = new Mountains( x, y, special ); break;
                    case Terrain.Mountains:
                        if( ( x == 0 || _tiles[ x - 1, y - 1 ].Type != Terrain.Ocean ) &&
                            ( y == 0 || _tiles[ x + 1, y - 1 ].Type != Terrain.Ocean ) &&
                            ( x == ( WIDTH - 1 ) || _tiles[ x + 1, y + 1 ].Type != Terrain.Ocean ) &&
                            ( y == ( HEIGHT - 1 ) || _tiles[ x - 1, y + 1 ].Type != Terrain.Ocean ) )
                            _tiles[ x, y ] = new Ocean( x, y, special );
                        break;
                    case Terrain.Desert: _tiles[ x, y ] = new Plains( x, y, special ); break;
                    case Terrain.Arctic: _tiles[ x, y ] = new Mountains( x, y, special ); break;
                }
            }
        }

        private void CreateRivers()
        {
            Log( "Map: Stage 6 - Create rivers" );

            int rivers = 0;
            for( int i = 0; i < 256 && rivers < ( ( _climate + _landMass ) * 2 ) + 6; i++ )
            {
                ITile[,] tilesBackup = (ITile[,])_tiles.Clone();

                int riverLength = 0;
                int varA = Common.Random.Next( 4 ) * 2;
                bool nearOcean = false;

                ITile tile = null;
                while( tile == null )
                {
                    int x = Common.Random.Next( WIDTH );
                    int y = Common.Random.Next( HEIGHT );
                    if( _tiles[ x, y ].Type == Terrain.Hills ) tile = _tiles[ x, y ];
                }
                do
                {
                    _tiles[ tile.X, tile.Y ] = new River( tile.X, tile.Y );
                    int varB = varA;
                    int varC = Common.Random.Next( 2 );
                    varA = ( ( ( varC - riverLength % 2 ) * 2 + varA ) & 0x07 );
                    varB = 7 - varB;

                    riverLength++;

                    nearOcean = NearOcean( tile.X, tile.Y );
                    switch( varA )
                    {
                        case 0:
                        case 1: tile = _tiles[ tile.X, tile.Y - 1 ]; break;
                        case 2:
                        case 3: tile = _tiles[ tile.X + 1, tile.Y ]; break;
                        case 4:
                        case 5: tile = _tiles[ tile.X, tile.Y + 1 ]; break;
                        case 6:
                        case 7: tile = _tiles[ tile.X - 1, tile.Y ]; break;
                    }
                }
                while( !nearOcean && ( tile.GetType() != typeof( Ocean ) && tile.GetType() != typeof( River ) && tile.GetType() != typeof( Mountains ) ) );

                if( ( nearOcean || tile.Type == Terrain.River ) && riverLength > 5 )
                {
                    rivers++;
                    ITile[,] mapPart = this[ tile.X - 3, tile.Y - 3, 7, 7 ];
                    for( int x = 0; x < 7; x++ )
                        for( int y = 0; y < 7; y++ )
                        {
                            if( mapPart[ x, y ] == null ) continue;
                            int xx = mapPart[ x, y ].X, yy = mapPart[ x, y ].Y;
                            if( _tiles[ xx, yy ].Type == Terrain.Forest )
                                _tiles[ xx, yy ] = new Jungle( xx, yy, TileConverterService.HasExtraResourceOnTile( x, y ) );
                        }
                }
                else
                {
                    _tiles = (ITile[,])tilesBackup.Clone(); ;
                }
            }
        }

        // This is a recursive function used to mark all tiles in a continent with a continent/ocean number. That
        // number will then be corrected so continents/ocans are numberd in size order

        readonly int[,] aiRelPos = { { -1, 0 }, { 0, -1 }, { 0, 1 }, { 1, 0 } };  // Check "Manhattan" conections only

        private byte ContinentId;
        private ulong ContinetSize;

        private void CountContinent( int x, int y, bool oOcean )
        {
            for( int i = 0; i < 4; i++ )
            {
                int XX = x + aiRelPos[ i, 0 ];
                int YY = y + aiRelPos[ i, 1 ];
                if( XX < 0 || XX >= WIDTH ) continue;
                if( YY < 0 || YY >= HEIGHT ) continue;

                if( this[ XX, YY ].IsOcean != oOcean ) continue;
                if( this[ XX, YY ].ContinentId > 0 ) continue;    // Already counted
                this[ XX, YY ].ContinentId = ContinentId;
                ContinetSize++;
                CountContinent( XX, YY, oOcean );
            }
        }

        /* ***********************************************************************************************************/

        private struct Continent
        {
            public byte ContinentId;
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

            int nTiles = 0;
            bool oOcean = false;
            for( int j = 0; j < 2; j++ )
            {
                Continents.Clear();
                ContinentId = 0;
                for( int y = 0; y < HEIGHT; y++ )
                    for( int x = 0; x < WIDTH; x++ )
                        if( this[ x, y ].ContinentId == 0 && ( this[ x, y ].IsOcean == oOcean ))
                        {  // Found a "new" continent/ocean
                            ContinetSize = 1;
                            ContinentId++;
                            this[ x, y ].ContinentId = ContinentId;
                            CountContinent( x, y, oOcean );         // Here is where the counting is done 
                            Continent continent;
                            continent.ContinentId = ContinentId;
                            continent.ContinetSize = ContinetSize;
                            Continents.Add( continent );

                        }
                ContinentsSorted = Continents.OrderByDescending( x => x.ContinetSize ).ToList();
                int[] _iConvTbl = new int[ ContinentsSorted.LongCount() + 1 ];

                for( int i = 0; i < ContinentsSorted.LongCount(); i++ )
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
                        this[ x, y ].ContinentId = (byte)( Math.Min( _iConvTbl[ this[ x, y ].ContinentId ], 15 ) );
                        nTiles++;       // Just a check

                    }
                oOcean = true;
            }
            Log( "Map: Total number of tiles = {0}", nTiles );
        }

        /* ***********************************************************************************************/
        private void CreatePoles()
		{
			Log("Map: Creating poles");
			
			for (int x = 0; x < WIDTH; x++)
			foreach (int y in new[] { 0, (HEIGHT - 1) })
			{
				_tiles[x, y] = new Arctic(x, y, false);
			}
			
			for (int i = 0; i < (WIDTH / 4); i++)
			foreach (int y in new[] { 0, 1, (HEIGHT - 2), (HEIGHT - 1) })
			{
				int x = Common.Random.Next(WIDTH);
				_tiles[x, y] = new Tundra(x, y, false);
			}
		}
		
		protected ILandValueCalculatorService LandValueCalculator => MapServiceProvider.GetLandValueCalculator();
        
		protected void CalculateLandValue()
        {
            LandValueCalculator.CalculateLandValue(_tiles);
        }
		
		private void GenerateThread()
		{
			Log("Generating map (Land Mass: {0}, Temperature: {1}, Climate: {2}, Age: {3})", _landMass, _temperature, _climate, _age);
			
			_tiles = new ITile[WIDTH, HEIGHT];
			
			int[,] elevation = GenerateLandMass();
			int[,] latitude = TemperatureAdjustments();
			MergeElevationAndLatitude(elevation, latitude);
			ClimateAdjustments();
			AgeAdjustments();
			CreateRivers();
			
			CalculateContinentSize();
			CreatePoles();
			HutGeneratorService.PlaceHuts(_tiles);
			CalculateLandValue();
			
			Ready = true;
			Log("Map: Ready");
		}
		
		public void Generate(int landMass = 1, int temperature = 1, int climate = 1, int age = 1)
		{
			if (Ready || _tiles != null)
			{
				Log("ERROR: Map is already load{0}/generat{0}", (Ready ? "ed" : "ing"));
				return;
			}
			
			if (Settings.Instance.CustomMapSize)
			{
				CustomMapSize customMapSize = new CustomMapSize();
				customMapSize.Closed += (s, a) =>
				{
					Size mapSize = (s as CustomMapSize).MapSize;
					_width = mapSize.Width;
					_height = mapSize.Height;

					_landMass = landMass;
					_temperature = temperature;
					_climate = climate;
					_age = age;
					
					Task.Run(() => GenerateThread());
				};

				GameTask.Insert(Show.Screen(customMapSize));
				return;
			}
			
			_landMass = landMass;
			_temperature = temperature;
			_climate = climate;
			_age = age;
			
			Task.Run(() => GenerateThread());
		}
	}
}
