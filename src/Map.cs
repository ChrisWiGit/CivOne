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
using System.Diagnostics;
using System.Linq;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Services.Maps;
using CivOne.Services.Random;
using CivOne.Tiles;

namespace CivOne
{
	// CA1708: the static SHOUTCASE accessors (WIDTH/HEIGHT) intentionally coexist with the
	// PascalCase instance properties (Width/Height) from IMap. The static form is the
	// project-wide constant-style accessor used in ~40 call sites; the instance form
	// fulfils the IMap contract used by persistence/pathfinding. Renaming either side
	// would either break the interface or churn every call site, so the casing-only
	// collision is accepted here.
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1708:Identifiers should differ by more than case", Justification = "Static SHOUTCASE accessors WIDTH/HEIGHT coexist with IMap PascalCase Width/Height; see comment.")]
	public partial class Map : IMap
	{
		protected static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		/// <summary>
		/// Random source for map construction and generation. Injected via constructor for tests;
		/// production code falls back to the cached <see cref="RandomServiceFactory"/> instance,
		/// which delegates to <c>Common.Random</c> and therefore preserves the legacy RNG
		/// consumption order required for save-file compatibility.
		/// </summary>
		private readonly IRandomService _randomService;

		/// <summary>
		/// Picture-resource source for LoadMap/SaveMap/RunEarthMapThread. Injected via
		/// constructor for tests; production code uses <see cref="DefaultMapResourceProvider"/>
		/// which forwards to the global <see cref="Resources"/> singleton.
		/// </summary>
		private readonly IMapResourceProvider _mapResourceProvider;

		/// <summary>
		/// Generation-settings source for <see cref="Generate(int, int, int, int)"/>. Injected via
		/// constructor for tests; production code uses <see cref="DefaultMapGenerationSettings"/>
		/// which forwards to the global <see cref="Settings"/> singleton.
		/// </summary>
		private readonly IMapGenerationSettings _mapGenerationSettings;

		/// <summary>
		/// Persistence sink for <see cref="SaveMap(string)"/>. Injected via constructor for tests;
		/// production code uses <see cref="DefaultMapPersistenceService"/> which writes to disk via
		/// <see cref="System.IO.File"/> + <see cref="System.IO.BinaryWriter"/>.
		/// </summary>
		private readonly IMapPersistenceService _mapPersistenceService;

		private static int _width = 80, _height = 50;
		public static int WIDTH => _width;
		public static int HEIGHT => _height;

		public int Width => WIDTH;
		public int Height => HEIGHT;
		
		private int _terrainMasterWord;
		public int TerrainMasterWord { get { return _terrainMasterWord; } }
		private int _landMass, _temperature, _climate, _age;
		
		
		#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional - but performance impact may be too low
		/// <summary>
		/// Lazily populated by LoadMap / InitializeForYamlLoad / generation pipeline.
		/// Marked null-forgiving so the constructor doesn't need to allocate an unusable placeholder grid;
		/// callers must guard via <see cref="Ready"/> or the explicit null check in <see cref="LoadEarthMapInThread"/>.
		/// </summary>
		private ITile[,] _tiles = null!;

		public ITile[,] Tiles { get { return _tiles; } }

		public bool Ready { get; private set; }
		public bool FixedStartPositions { get; private set; }

		public IEnumerable<ITile> QueryMapPart(int x, int y, int width, int height)
		{
			ITile[,] area = this[x, y, width, height];
			for (int yy = 0; yy < height; yy++)
			{
				for (int xx = 0; xx < width; xx++)
				{
					yield return area[xx, yy];
				}
			}
		}
		
		public IEnumerable<ITile> AllTiles()
		{
			for (int y = 0; y < HEIGHT; y++)
			{
				for (int x = 0; x < WIDTH; x++)
				{
					yield return this[x, y];
				}
			}
		}
		
		private bool NearOcean(int x, int y)
		{
			for (int relY = -1; relY <= 1; relY++)
			{
				for (int relX = -1; relX <= 1; relX++)
				{
					if (Math.Abs(relX) == Math.Abs(relY)) continue;
					if (_tiles[x + relX, y + relY] is Ocean) return true;
				}
			}
			return false;
		}
		
		internal static bool TileIsType(ITile tile, params Terrain[] terrain) => terrain.Any(x => tile.Type == x);

		public void ChangeTileType(int x, int y, Terrain type)
		{
			bool special = TileIsSpecial(x, y);
			bool road = _tiles[x, y].Road;
			bool railRoad = _tiles[x, y].RailRoad;
			switch(type)
			{
				case Terrain.Forest: _tiles[x, y] = new Forest(x, y, special); break;
				case Terrain.Swamp: _tiles[x, y] = new Swamp(x, y, special); break;
				case Terrain.Plains: _tiles[x, y] = new Plains(x, y, special); break;
				case Terrain.Tundra: _tiles[x, y] = new Tundra(x, y, special); break;
				case Terrain.River: _tiles[x, y] = new River(x, y); break;
				case Terrain.Grassland1:
				case Terrain.Grassland2: _tiles[x, y] = new Grassland(x, y); break;
				case Terrain.Jungle: _tiles[x, y] = new Jungle(x, y, special); break;
				case Terrain.Hills: _tiles[x, y] = new Hills(x, y, special); break;
				case Terrain.Mountains: _tiles[x, y] = new Mountains(x, y, special); break;
				case Terrain.Desert: _tiles[x, y] = new Desert(x, y, special); break;
				case Terrain.Arctic: _tiles[x, y] = new Arctic(x, y, special); break;
				case Terrain.Ocean: _tiles[x, y] = new Ocean(x, y, special); break;
				default: throw new ArgumentException($"Invalid terrain type: {type}");
			}
			_tiles[x, y].Road = road;
			_tiles[x, y].RailRoad = railRoad;
		}
		
		private int ModGrid(int x, int y) => (x % 4) * 4 + (y % 4);
		
		private bool TileIsSpecial(int x, int y)
		{
			if (y < 2 || y > (HEIGHT - 3)) return false;
			return ModGrid(x, y) == ((x / 4) * 13 + (y / 4) * 11 + _terrainMasterWord) % 16;
		}
		
		public IEnumerable<ITile> ContinentTiles(int continentId) => AllTiles().Where(t => t.ContinentId == continentId);
		
		public IEnumerable<ICityOnContinent> ContinentCities(int continentId) =>
			[.. ContinentTiles(continentId)
				.Where(x => x.City != null)
				.Select(x => x.City)
				.Cast<ICityOnContinent>()];
		
		public ITile this[int x, int y]
		{
			get
			{
				// CW: this if-case happens a lot! So a lot of code is dealing with null, althouth property is not nullable. 
				// Possible code smell but to large to refactor right now. 
				if (y < 0 || y >= HEIGHT) return null; 
				
				while (x < 0) x += WIDTH;
				x %= WIDTH;
				
				return _tiles[x, y];
			}
		}
		
		public ITile[,] this[int x, int y, int width, int height]
		{
			get
			{
				if (width < 0)
				{
					width = Math.Abs(width);
					x -= width;
				}
				if (height < 0)
				{
					height = Math.Abs(height);
					y -= height;
				}

				ITile[,] output = new ITile[width, height];
				
				for (int yy = y; yy < y + height; yy++)
				{
					for (int xx = x; xx < x + width; xx++)
					{
						output[xx - x, yy - y] = this[xx, yy];
					}
				}
				
				return output;
			}
		}
		
		private static Map _instance;
		public static Map Instance
		{
			get
			{
				if (_instance == null)
					_instance = new Map();
				return _instance;
			}
		}
		
		internal Map() : this(null, null, null, null)
		{
		}

		/// <summary>
		/// Test-friendly constructor that allows injecting a custom <see cref="IRandomService"/>.
		/// Pass <c>null</c> to use the shared instance from <see cref="RandomServiceFactory"/>.
		/// </summary>
		internal Map(IRandomService? randomService) : this(randomService, null, null, null)
		{
		}

		/// <summary>
		/// Test-friendly constructor that allows injecting both a custom <see cref="IRandomService"/>
		/// and a custom <see cref="IMapResourceProvider"/>. Pass <c>null</c> for either parameter
		/// to fall back to the production default.
		/// </summary>
		internal Map(IRandomService? randomService, IMapResourceProvider? mapResourceProvider) : this(randomService, mapResourceProvider, null, null)
		{
		}

		/// <summary>
		/// Test-friendly constructor that allows injecting every external collaborator
		/// (<see cref="IRandomService"/>, <see cref="IMapResourceProvider"/>,
		/// <see cref="IMapGenerationSettings"/>). Pass <c>null</c> for any parameter to
		/// fall back to the production default.
		/// </summary>
		internal Map(IRandomService? randomService, IMapResourceProvider? mapResourceProvider, IMapGenerationSettings? mapGenerationSettings) : this(randomService, mapResourceProvider, mapGenerationSettings, null)
		{
		}

		/// <summary>
		/// Test-friendly constructor that allows injecting every external collaborator
		/// (<see cref="IRandomService"/>, <see cref="IMapResourceProvider"/>,
		/// <see cref="IMapGenerationSettings"/>, <see cref="IMapPersistenceService"/>).
		/// Pass <c>null</c> for any parameter to fall back to the production default.
		/// </summary>
		internal Map(IRandomService? randomService, IMapResourceProvider? mapResourceProvider, IMapGenerationSettings? mapGenerationSettings, IMapPersistenceService? mapPersistenceService)
		{
			_randomService = randomService ?? RandomServiceFactory.Create();
			_mapResourceProvider = mapResourceProvider ?? new DefaultMapResourceProvider();
			_mapGenerationSettings = mapGenerationSettings ?? new DefaultMapGenerationSettings();
			_mapPersistenceService = mapPersistenceService ?? new DefaultMapPersistenceService();
			_terrainMasterWord = _randomService.Next(16);
			Ready = false;

			Log("Map instance created");
		}

        /// <summary>
        /// Fire-eggs 20190704: for unit testing, reset
        /// </summary>
        internal static void Reset(Map newInstance = null)
        {
            _instance = newInstance;
        }
	}
}