using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Persistence;
using CivOne.Persistence.Factories;
using CivOne.Persistence.Model;
using CivOne.Services.GlobalWarming;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne
{
	/**
	* Provides a way to create a GameState from the current game. 
	* The game implements these and adapters 
	* are used to convert the internal types to the DTO types.
	*/
	public interface IGameSnapshotSource
	{
		int Difficulty { get; }
		Player CurrentPlayer { get; }
		Player HumanPlayer { get; }

		Player[] Players { get; }

		List<City> Cities { get; }
		List<IUnit> Units { get; }

		

		Dictionary<byte, byte> AdvanceOrigin { get; }

		ushort GameTurn { get; }
		ushort AnthologyTurn { get; }
		ushort PeaceTurns { get; }
		ushort PlayerFutureTech { get; }

		string[] CityNames { get; }

		List<ReplayData> ReplayData { get; }

		byte PlayerNumber(Player player);

		ITile[,] MapTiles { get; }

		public bool Animations { get; }
		public bool Sound { get; }
		public bool CivilopediaText { get; }
		public bool EndOfTurn { get; }
		public bool InstantAdvice { get; }
		public bool AutoSave { get; }
		public bool EnemyMoves { get; }
		public bool Palace { get; }

		int? GameRandomSeed { get; }

		int TerrainMasterWord { get; }

		IGlobalWarmingService GlobalWarmingService { get; }
	}

	public class GameStateHandler
	{
		private static ICheckedValueSanitizer CVS => ValueSanitizerFactory.GetCheckedValueSanitizer();

		public GameState Create(IGameSnapshotSource game)
		{
			List<bool> options =
				[
					// order must be same as in GameOptionEnum
					game.Animations,
					game.Sound,
					game.CivilopediaText,
					game.EndOfTurn,
					game.InstantAdvice,
					game.AutoSave,
					game.EnemyMoves,
					game.Palace
				];
			return new GameState
			{
				GameTurn = game.GameTurn,
				HumanPlayer = game.HumanPlayer,

				Difficulty = game.Difficulty,
				CurrentPlayer = game.CurrentPlayer,

				Players = game.Players,

				AnthologyTurn = game.AnthologyTurn,
				// Seed semantics:
				// - TerrainSeed is always sourced from map context (TerrainMasterWord)
				// - RandomSeed prefers explicit GameRandomSeed, with legacy fallback to TerrainMasterWord
				//   when no dedicated RNG seed source is exposed by the snapshot source.
				TerrainSeed = game.TerrainMasterWord,
				RandomSeed = game.GameRandomSeed ?? game.TerrainMasterWord,
				
				MapWidth = game.MapTiles.GetLength(0),
				MapHeight = game.MapTiles.GetLength(0) > 0 ? 
							game.MapTiles.GetLength(1) : 0,
				
				MapTiles = game.MapTiles,
				Units = game.Units,  // Critical: Units were missing from snapshot!
				CityNames = game.CityNames,
				
				GameOptions = [.. options				
					.Select((option, index) => (option, index))
					.Where(x => x.option)
					.Select(x => (GameOptionEnum)x.index)],

				Cities = game.Cities,
				AdvanceOrigin = game.AdvanceOrigin,
				ReplayData = [.. game.ReplayData],
				PeaceTurns = game.PeaceTurns,
				PlayerFutureTech = game.PlayerFutureTech,
				GlobalWarmingCount = game.GlobalWarmingService?.GlobalWarmingCount ?? 0,
				PollutedSquaresCount = game.GlobalWarmingService?.PollutedSquaresCount ?? 0,
				WarmingIndicator = game.GlobalWarmingService?.WarmingIndicator ?? WarmingIndicator.None,
			};
		}

		public GameState2 CreateOld(IGameSnapshotSource game)
		{
			// Discovered advances
			byte[][] discoveredAdvanceIDs = new byte[game.Players.Length][];
			for (int p = 0; p < game.Players.Length; p++)
			{
				discoveredAdvanceIDs[p] = [.. game.Players[p].Advances.Select(x => x.Id)];
			}

			// Wonders
			ushort[] wonders = [.. Enumerable.Repeat(ushort.MaxValue, 22)];
			for (byte i = 0; i < game.Cities.Count; i++)
			{
				foreach (IWonder wonder in game.Cities[i].Wonders)
				{
					wonders[wonder.Id] = i;
				}
			}

			// Tile visibility
			bool[][,] visibility = new bool[game.Players.Length][,];
			for (int p = 0; p < visibility.Length; p++)
			{
				visibility[p] = new bool[80, 50];
				for (int xx = 0; xx < 80; xx++)
					for (int yy = 0; yy < 50; yy++)
					{
						if (game.Players[p].Visible(xx, yy))
							visibility[p][xx, yy] = true;
					}
			}

			// Advance first discovery
			byte maxKey = CVS.CheckedByte(game.AdvanceOrigin.Keys.Max(), nameof(GameStateHandler), "CreateOld.AdvanceOriginMaxKey", max: 72);
			ushort[] firstDiscovery = new ushort[maxKey + 1];
			foreach (byte key in game.AdvanceOrigin.Keys)
				firstDiscovery[key] = game.AdvanceOrigin[key];

			return new GameState2
			{
				GameTurn = game.GameTurn,
				HumanPlayer = game.PlayerNumber(game.HumanPlayer),
				RandomSeed = CVS.CheckedUInt16(game.TerrainMasterWord, nameof(GameStateHandler), "CreateOld.RandomSeed"),
				Difficulty = CVS.CheckedUInt16(game.Difficulty, nameof(GameStateHandler), "CreateOld.Difficulty"),

				ActiveCivilizations = [.. game.Players
					.Select(player =>
						player.Civilization is Barbarian ||
						player.Cities.Any(c => c.Size > 0) ||
						game.Units.Any(u => player == u.Owner))],

				CivilizationIdentity = [.. game.Players.Select(x => CVS.CheckedByte(x.Civilization.Id > 7 ? 1 : 0, nameof(GameStateHandler), "CreateOld.CivilizationIdentity"))],

				CurrentResearch = game.HumanPlayer.CurrentResearch?.Id ?? 0,
				DiscoveredAdvanceIDs = discoveredAdvanceIDs,

				LeaderNames = [.. game.Players.Select(x => x.LeaderName)],
				CivilizationNames = [.. game.Players.Select(x => x.TribeNamePlural)],
				CitizenNames = [.. game.Players.Select(x => x.TribeName)],
				CityNames = game.CityNames,

				PlayerGold = [.. game.Players.Select(x => x.Gold)],
				ResearchProgress = [.. game.Players.Select(x => x.Science)],
				TaxRate = [.. game.Players.Select(x => CVS.CheckedUInt16(x.TaxesRate, nameof(GameStateHandler), "CreateOld.TaxRate"))],
				ScienceRate = [.. game.Players.Select(x => CVS.CheckedUInt16(x.ScienceRate, nameof(GameStateHandler), "CreateOld.ScienceRate"))],
				StartingPositionX = [.. game.Players.Select(x => CVS.CheckedUInt16(x.StartX, nameof(GameStateHandler), "CreateOld.StartingPositionX"))],
				Government = [.. game.Players.Select(x => CVS.CheckedUInt16(x.Government.Id, nameof(GameStateHandler), "CreateOld.Government"))],

				Cities = [.. game.Cities.GetCityData()],
				Units = [.. game.Players.Select(player => game.Units.Where(u => player == u.Owner).GetUnitData().ToArray())],

				Wonders = wonders,
				TileVisibility = visibility,
				// AdvanceFirstDiscovery = game.AdvanceOrigin.ToDictionary(entry => entry.Key, entry => entry.Value),
				AdvanceFirstDiscovery = game.AdvanceOrigin,

				GameOptions =
				[
					game.InstantAdvice,
					game.AutoSave,
					game.EndOfTurn,
					game.Animations,
					game.Sound,
					game.EnemyMoves,
					game.CivilopediaText,
					game.Palace
				],

				NextAnthologyTurn = game.AnthologyTurn,
				OpponentCount = CVS.CheckedUInt16(game.Players.Length - 2, nameof(GameStateHandler), "CreateOld.OpponentCount"),
				// ReplayData = [.. game.ReplayData]  // TODO: CW: Produces Heap Corruption. App stops 0xc0000374 with "A heap has been corrupted" error.
			};
		}
	}
}