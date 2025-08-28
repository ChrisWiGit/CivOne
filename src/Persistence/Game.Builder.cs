using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne
{
	public partial class Game
	{
		public class GameBuilder
		{
			private readonly IGameData _data;
			private readonly Game _game;

			internal readonly CityLoadGame _cityLoadGame = new();

			protected ILogger Logger => _game;

			public GameBuilder(IGameData data)
			{
				_data = data;
				_game = new Game();
			}

			protected byte BARBARIAN_PLAYER = 1;
			protected byte HUMAN_PLAYER = 1;

			protected byte PlayerCount => (byte)(_data.OpponentCount + BARBARIAN_PLAYER + HUMAN_PLAYER);


			public GameBuilder SetupMeta()
			{
				_game._difficulty = _data.Difficulty;
				_game._competition = _data.OpponentCount + BARBARIAN_PLAYER;
				_game.GameTurn = _data.GameTurn;
				_game.CityNames = _data.CityNames;
				_game._anthologyTurn = _data.NextAnthologyTurn;

				_game._replayData.AddRange(_data.ReplayData);

				return this;
			}

			public GameBuilder SetupPlayers()
			{
				byte BarbarianId = 0;

				_game._players = new Player[PlayerCount];

				ushort[] advanceFirst = _data.AdvanceFirstDiscovery;
				bool[][,] visibility = _data.TileVisibility;
				for (int i = 0; i < _game._players.Length; i++)
				{
					ICivilization[] tribes = [.. Common.Civilizations.Where(c => c.PreferredPlayerNumber == i)];
					ICivilization civ = tribes[_data.CivilizationIdentity[i] % tribes.Length];

					Player player = _game._players[i] = new Player(civ, _data.LeaderNames[i], _data.CitizenNames[i], _data.CivilizationNames[i])
					{
						GameDI = _game,
						Gold = _data.PlayerGold[i],
						Science = _data.ResearchProgress[i],
						Government = Reflect.GetGovernments().FirstOrDefault(x => x.Id == _data.Government[i]),
						TaxesRate = _data.TaxRate[i],
						LuxuriesRate = 10 - _data.ScienceRate[i] - _data.TaxRate[i],
						StartX = (short)_data.StartingPositionX[i]
					};

					if (i != BarbarianId)
					{
						player.Destroyed += _game.PlayerDestroyed;
					}

					// Set map visibility
					for (int xx = 0; xx < 80; xx++)
						for (int yy = 0; yy < 50; yy++)
						{
							if (!visibility[i][xx, yy]) continue;
							if (i == 0 && Map[xx, yy].Hut) Map[xx, yy].Hut = false;
							player.Explore(xx, yy, 0);
						}

					byte[] advanceIds = _data.DiscoveredAdvanceIDs[i];
					Common.Advances.Where(x => advanceIds.Any(id => x.Id == id)).ToList().ForEach(x =>
					{
						player.AddAdvance(x, false);
						if (advanceFirst[x.Id] != player.Civilization.Id) return;
						_game.SetAdvanceOrigin(x, player);
					});
				}

				return this;
			}

			protected Dictionary<byte, City> cityList = null;
			public GameBuilder SetupCities()
			{
				_game._cities = [];

				cityList = [];

				foreach (CityData cityData in _data.Cities)
				{
					City city = new(cityData.Owner)
					{
						GameDI = _game,
						X = cityData.X,
						Y = cityData.Y,
						NameId = cityData.NameId,
						Size = cityData.ActualSize,
						Food = cityData.Food,
						Shields = cityData.Shields
					};

					city.SetProduction(cityData.CurrentProduction);
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
						if (_data.Wonders[wonder.Id] != cityData.Id) continue;
						city.AddWonder(wonder);
					}

					_game._cities.Add(city);

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
							Logger.Log("Unknown fortified unit found: {0}", fortifiedUnit);
							continue;
						}

						unit.Status = (byte)(fortified ? 8 : 0 + (veteran ? 32 : 0));

						unit.Owner = city.Owner;
						unit.SetHome(city);
						_game._units.Add(unit);
					}

					cityList.Add(cityData.Id, city);
				}

				return this;
			}

			public GameBuilder SetupUnits()
			{
				Debug.Assert(cityList != null, "cityList was null in SetupUnits. Call SetupCities first.");

				// TODO fire-eggs: wrong when playing with fewer than 7?
				UnitData[][] unitData = _data.Units;
				for (byte p = 0; p < 8; p++)
				{
					if (!_data.ActiveCivilizations[p] || unitData[p] == null)
					{
						continue;
					}
					foreach (UnitData data in unitData[p])
					{
						IUnit unit = CreateUnit((UnitType)data.TypeId, data.X, data.Y);
						if (unit == null)
						{
							continue;
						}

						unit.Status = data.Status;
						unit.Owner = p;
						unit.PartMoves = (byte)(data.RemainingMoves % 3);
						unit.MovesLeft = (byte)((data.RemainingMoves - unit.PartMoves) / 3);
						if (data.GotoX != 0xFF)
						{
							unit.Goto = new Point(data.GotoX, data.GotoY);
						}

						if (cityList.TryGetValue(data.HomeCityId, out City value))
						{
							unit.SetHome(value);
						}
						_game._units.Add(unit);
					}
				}

				_game._units.ForEach(u => u.Explore());

				return this;
			}

			public GameBuilder SetupCurrentPlayer()
			{
				_game._currentPlayer = _data.HumanPlayer;
				for (int i = 0; i < _game._units.Count; i++)
				{
					if (_game._units[i].Owner != _data.HumanPlayer || _game._units[i].Busy)
					{
						continue;
					}
					_game._activeUnit = i;

					if (_game._units[i].MovesLeft > 0)
					{
						break;
					}
				}

				_game.HumanPlayer = _game._players[_data.HumanPlayer];
				_game.HumanPlayer.CurrentResearch = Common.Advances.FirstOrDefault(a => a.Id == _data.CurrentResearch);


				// CW: Calculate civilization extinction here to avoid showing the "civ destruction" message.
				_game._players.ToList().ForEach(player => player.HandleExtinction(false));

				return this;
			}

			public GameBuilder SetupOptions()
			{
				bool[] options = _data.GameOptions;

				SetGameOptionsWithDefault(() => Settings.InstantAdvice, (game, value) => game.InstantAdvice = value, options[0]);
				SetGameOptionsWithDefault(() => Settings.AutoSave, (game, value) => game.AutoSave = value, options[1]);
				SetGameOptionsWithDefault(() => Settings.EndOfTurn, (game, value) => game.EndOfTurn = value, options[2]);
				SetGameOptionsWithDefault(() => Settings.Animations, (game, value) => game.Animations = value, options[3]);
				SetGameOptionsWithDefault(() => Settings.Sound, (game, value) => game.Sound = value, options[4]);
				SetGameOptionsWithDefault(() => Settings.EnemyMoves, (game, value) => game.EnemyMoves = value, options[5]);
				SetGameOptionsWithDefault(() => Settings.CivilopediaText, (game, value) => game.CivilopediaText = value, options[6]);
				SetGameOptionsWithDefault(() => Settings.Palace, (game, value) => game.Palace = value, options[7]);

				return this;
			}

			private void SetGameOptionsWithDefault(
				Func<GameOption> setting,
				Action<Game, bool> applySetting,
				bool valueIfDefault)
			{
				if (setting() == GameOption.Default)
				{
					applySetting(_game, valueIfDefault);
					return;
				}

				applySetting(_game, setting() == GameOption.On);
			}

			public GameBuilder SetupAll()
			{
				return SetupMeta()
					.SetupPlayers()
					.SetupCities()
					.SetupUnits()
					.SetupCurrentPlayer()
					.SetupOptions();

			}

			public Game Build()
			{
				// evtl. letzte Konsistenzprüfungen
				return _game;
			}
		}
	}
}