// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Services;
using CivOne.Services.GlobalWarming;
using CivOne.Services.GlobalWarming.Impl;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne
{
	public partial class Game
    {
		// Dependency Injection
		// Todo: Replace with DI framework
		internal readonly CityLoadGame _cityLoadGame = new();
		public static void LoadGame(string sveFile, string mapFile)
		{
			// Allow loading a game in-game.

			using (IGameData adapter = SaveDataAdapter.Load(File.ReadAllBytes(sveFile)))
			{
				if (!adapter.ValidData)
				{
					BaseInstance.Log("SaveDataAdapter failed to load game");
					return;
				}

				// Always use the save game's seed
				Common.SetRandomSeed(adapter.RandomSeed);

				Map.Instance.LoadMap(mapFile, adapter.RandomSeed);
				_instance = new Game(adapter);
				BaseInstance.Log($"Game instance loaded (difficulty: {_instance._difficulty}, competition: {_instance._competition}");
			}
		}

		public void Save(string sveFile, string mapFile)
		{
			using (IGameData gameData = new SaveDataAdapter())
			{
				gameData.GameTurn = _gameTurn;
				gameData.HumanPlayer = (ushort)PlayerNumber(HumanPlayer);
				gameData.RandomSeed = Map.Instance.SaveMap(mapFile);
				gameData.Difficulty = (ushort)_difficulty;
				gameData.ActiveCivilizations = _players.Select(x => (x.Civilization is Barbarian) || (x.Cities.Any(c => c.Size > 0) || GetUnits().Any(u => x == u.Owner))).ToArray();

                gameData.CivilizationIdentity = _players.Select(x => (byte)(x.Civilization.Id > 7 ? 1 : 0)).ToArray();

				gameData.CurrentResearch = HumanPlayer.CurrentResearch?.Id ?? 0;
				byte[][] discoveredAdvanceIDs = new byte[_players.Length][];
				for (int p = 0; p < _players.Length; p++)
					discoveredAdvanceIDs[p] = _players[p].Advances.Select(x => x.Id).ToArray();
				gameData.DiscoveredAdvanceIDs = discoveredAdvanceIDs;
				gameData.LeaderNames = _players.Select(x => x.LeaderName).ToArray();
				gameData.CivilizationNames = _players.Select(x => x.TribeNamePlural).ToArray();
				gameData.CitizenNames = _players.Select(x => x.TribeName).ToArray();
				gameData.CityNames = CityNames;
				gameData.PlayerGold = _players.Select(x => x.Gold).ToArray();
				gameData.ResearchProgress = _players.Select(x => x.Science).ToArray();
				gameData.TaxRate = _players.Select(x => (ushort)x.TaxesRate).ToArray();
				gameData.ScienceRate = _players.Select(p => (ushort)p.ScienceRate).ToArray();
				gameData.StartingPositionX = _players.Select(x => (ushort)x.StartX).ToArray();
				gameData.Government = _players.Select(x => (ushort)x.Government.Id).ToArray();
				gameData.Cities = _cities.GetCityData().ToArray();
				gameData.Units = _players.Select(p => _units.Where(u => p == u.Owner).GetUnitData().ToArray()).ToArray();
				ushort[] wonders = Enumerable.Repeat(ushort.MaxValue, 22).ToArray();
				for (byte i = 0; i < _cities.Count(); i++)
				foreach (IWonder wonder in _cities[i].Wonders)
				{
					wonders[wonder.Id] = i;
				}
				gameData.Wonders = wonders;
				bool[][,] visibility = new bool[_players.Length][,];
				for (int p = 0; p < visibility.Length; p++)
				{
					visibility[p] = new bool[80, 50];
					for (int xx = 0; xx < 80; xx++)
					for (int yy = 0; yy < 50; yy++)
					{
						if (!_players[p].Visible(xx, yy)) continue;
						visibility[p][xx, yy] = true;
					}
				}
				gameData.TileVisibility = visibility;
				ushort[] firstDiscovery = new ushort[72];
				foreach (byte key in _advanceOrigin.Keys)
					firstDiscovery[key] = _advanceOrigin[key];
				gameData.AdvanceFirstDiscovery = firstDiscovery;
				gameData.GameOptions = new bool[]
				{
					InstantAdvice,
					AutoSave,
					EndOfTurn,
					Animations,
					Sound,
					EnemyMoves,
					CivilopediaText,
					Palace
				};
				gameData.NextAnthologyTurn = _anthologyTurn;
				gameData.OpponentCount = (ushort)(_players.Length - 2);
				gameData.ReplayData = _replayData.ToArray();
				File.WriteAllBytes(sveFile, gameData.GetBytes());
			}
		}

		private Game(IGameData gameData)
		{
			_difficulty = gameData.Difficulty;
			_competition = (gameData.OpponentCount + 1);

			// CW: Dependency Injection. Otherwise this would be handled after this constructor (too late to call HandleExtinction).
			Player.Game = this;

			_players = new Player[_competition + 1];
			_cities = new List<City>();
			_units = new List<IUnit>();

			ushort[] advanceFirst = gameData.AdvanceFirstDiscovery;
			bool[][,] visibility = gameData.TileVisibility;
			for (int i = 0; i < _players.Length; i++)
			{
				ICivilization[] civs = Common.Civilizations.Where(c => c.PreferredPlayerNumber == i).ToArray();
				ICivilization civ = civs[gameData.CivilizationIdentity[i] % civs.Length];
				Player player = (_players[i] = new Player(civ, gameData.LeaderNames[i], gameData.CitizenNames[i], gameData.CivilizationNames[i]));
                if (i != 0) // don't need for barbarians (?)
				    player.Destroyed += PlayerDestroyed;
				player.Gold = gameData.PlayerGold[i];
				player.Science = gameData.ResearchProgress[i];
				player.Government = Reflect.GetGovernments().FirstOrDefault(x => x.Id == gameData.Government[i]);

				player.TaxesRate = gameData.TaxRate[i];
				player.LuxuriesRate = 10 - gameData.ScienceRate[i] - player.TaxesRate;
				player.StartX = (short)gameData.StartingPositionX[i];
				
				// Set map visibility
				for (int xx = 0; xx < 80; xx++)
				for (int yy = 0; yy < 50; yy++)
				{
					if (!visibility[i][xx, yy]) continue;
					if (i == 0 && Map[xx, yy].Hut) Map[xx, yy].Hut = false;
					player.Explore(xx, yy, 0);
				}

				byte[] advanceIds = gameData.DiscoveredAdvanceIDs[i];
				Common.Advances.Where(x => advanceIds.Any(id => x.Id == id)).ToList().ForEach(x =>
				{
					player.AddAdvance(x, false);
					if (advanceFirst[x.Id] != player.Civilization.Id) return;
					SetAdvanceOrigin(x, player);
				});
			}

			GameTurn = gameData.GameTurn;
			CityNames = gameData.CityNames;
			HumanPlayer = _players[gameData.HumanPlayer];
			HumanPlayer.CurrentResearch = Common.Advances.FirstOrDefault(a => a.Id == gameData.CurrentResearch);
		
			_anthologyTurn = gameData.NextAnthologyTurn;

			// City.Game = this; // Dependency Injection (for GetSpecialistsFromGameData)

			Dictionary<byte, City> cityList = new Dictionary<byte, City>();
			foreach (CityData cityData in gameData.Cities)
			{
				City city = new City(cityData.Owner)
				{
					X = cityData.X,
					Y = cityData.Y,
					NameId = cityData.NameId,
					Size = cityData.ActualSize,
					Food = cityData.Food,
					Shields = cityData.Shields
				};
				city.SetProduction(cityData.CurrentProduction);

				// CW:
				// Converting should be more of an extra class (Adapter pattern)
				// private CityAdapter cityAdapter = new CityAdapter(); //on top of class definition - later defined through DI
				// then...
				// city = cityAdapter.createCity(gameData);
				// but this required CityAdapter has a DI reference to Game. 
				// Today DI would be: CityAdapter.Game = this; // HACK
				city.SetupStatus(cityData.Status);
				city.SetupResourceTiles = _cityLoadGame.GetResourceTilesFromGameData(city, cityData.ResourceTiles);
				city.SetupSpecialists = _cityLoadGame.GetSpecialistsFromGameData(cityData.ResourceTiles);

				// Set city buildings
				foreach (byte buildingId in cityData.Buildings)
				{
					city.AddBuilding(Common.Buildings.First(b => b.Id == buildingId));
				}

				// Set city wonders
				foreach (IWonder wonder in Common.Wonders)
				{
					if (gameData.Wonders[wonder.Id] != cityData.Id) continue;
					city.AddWonder(wonder);
				}
				
				_cities.Add(city);

				//CW: not sure why this happens to AI cities, 
				// but a city tile may have a hut which reappears after a city is destroyed (funny).
				if (city.Tile != null)
				{
					city.Tile.Hut = false;
				}

				foreach (byte fortifiedUnit in cityData.FortifiedUnits)
                {
                    // fire-eggs 20190622 corrected restore of "fortified" units
                    // Unit id is actually in lower 6 bits
                    // see https://forums.civfanatics.com/threads/sve-file-format.493581/page-4
                    int unitId = fortifiedUnit & 0x3F;
                    bool fortified = (fortifiedUnit & 0x40) != 0;
                    bool veteran = (fortifiedUnit & 0x80) != 0;

                    IUnit unit = CreateUnit((UnitType)unitId, city.X, city.Y);
                    if (unit == null)
                    {
                        Log("Unknown fortified unit found: {0}", fortifiedUnit);
                        continue;
                    }

                    unit.Status = (byte)(fortified ? 8 : 0 + (veteran ? 32 : 0));

					unit.Owner = city.Owner;
					unit.SetHome(city);
					_units.Add(unit);
				}

				cityList.Add(cityData.Id, city);

				const byte NO_CITY = 0xFF;
				city.SetTradingCitiesIndexes([.. cityData.TradingCities.Select(index => (int)index).Where(index => index != NO_CITY)]);
			}

            // TODO fire-eggs: wrong when playing with fewer than 7?
			UnitData[][] unitData = gameData.Units;
			for (byte p = 0; p < 8; p++)
			{
				if (!gameData.ActiveCivilizations[p]) continue;
				foreach (UnitData data in unitData[p])
				{
					IUnit unit = CreateUnit((UnitType)data.TypeId, data.X, data.Y);
					if (unit == null) continue;
					unit.Status = data.Status;
					unit.Owner = p;
					unit.PartMoves = (byte)(data.RemainingMoves % 3);
					unit.MovesLeft = (byte)((data.RemainingMoves - unit.PartMoves) / 3);
					if (data.GotoX != 0xFF) unit.Goto = new Point(data.GotoX, data.GotoY);
					if (cityList.ContainsKey(data.HomeCityId))
					{
						unit.SetHome(cityList[data.HomeCityId]);
					}
					_units.Add(unit);
				}
			}

			_replayData.AddRange(gameData.ReplayData);

			globalWarmingService = GlobalWarmingServiceFactory.CreateGlobalWarmingService(gameData, _cities.AsReadOnly(), Map.AllTiles());
			globalWarmingScourgeService = GlobalWarmingServiceFactory.CreateGlobalWarmingScourgeService(
				globalWarmingService,
				Map.Tiles,
				(tile, newTerrainType) => Map.ChangeTileType(tile.X, tile.Y, newTerrainType),
				DisbandUnit,
				Map.WIDTH,
				Map.HEIGHT
			);

			// Game Settings
			InstantAdvice = (Settings.InstantAdvice == GameOption.On);
			AutoSave = (Settings.AutoSave != GameOption.Off);
			EndOfTurn = (Settings.EndOfTurn == GameOption.On);
			Animations = (Settings.Animations != GameOption.Off);
			Sound = (Settings.Sound != GameOption.Off);
			EnemyMoves = (Settings.EnemyMoves != GameOption.Off);
			CivilopediaText = (Settings.CivilopediaText != GameOption.Off);
			Palace = (Settings.Palace != GameOption.Off);

			bool[] options = gameData.GameOptions;
			if (Settings.InstantAdvice == GameOption.Default) InstantAdvice = options[0];
			if (Settings.AutoSave == GameOption.Default) AutoSave = options[1];
			if (Settings.EndOfTurn == GameOption.Default) EndOfTurn = options[2];
			if (Settings.Animations == GameOption.Default) Animations = options[3];
			if (Settings.Sound == GameOption.Default) Sound = options[4];
			if (Settings.EnemyMoves == GameOption.Default) EnemyMoves = options[5];
			if (Settings.CivilopediaText == GameOption.Default) CivilopediaText = options[6];
			if (Settings.Palace == GameOption.Default) Palace = options[7];

			_currentPlayer = gameData.HumanPlayer;
			for (int i = 0; i < _units.Count(); i++)
			{
				if (_units[i].Owner != gameData.HumanPlayer || _units[i].Busy) continue;
				_activeUnit = i;
				if (_units[i].MovesLeft > 0) break;
			}

			// CW: Calculate civilization extinction here to avoid showing the "civ destruction" message.
			_players.ToList().ForEach(player => player.HandleExtinction(false));
		}
	}
}