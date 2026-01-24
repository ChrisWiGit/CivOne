using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Persistence;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne
{
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

		string[] CityNames { get; }

		List<ReplayData> ReplayData { get; }

		byte PlayerNumber(Player player);

		public bool Animations { get; }
		public bool Sound { get; }
		public bool CivilopediaText { get; }
		public bool EndOfTurn { get; }
		public bool InstantAdvice { get; }
		public bool AutoSave { get; }
		public bool EnemyMoves { get; }
		public bool Palace { get; }

		int TerrainMasterWord { get; }
	}

	public class GameStateHandler
	{
		public GameState Create(IGameSnapshotSource game)
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
			ushort[] firstDiscovery = new ushort[72];
			foreach (byte key in game.AdvanceOrigin.Keys)
				firstDiscovery[key] = game.AdvanceOrigin[key];

			return new GameState
			{
				GameTurn = game.GameTurn,
				HumanPlayer = game.PlayerNumber(game.HumanPlayer),
				RandomSeed = (ushort)game.TerrainMasterWord,
				Difficulty = (ushort)game.Difficulty,

				ActiveCivilizations = [.. game.Players
					.Select(player =>
						player.Civilization is Barbarian ||
						player.Cities.Any(c => c.Size > 0) ||
						game.Units.Any(u => player == u.Owner))],

				CivilizationIdentity = [.. game.Players.Select(x => (byte)(x.Civilization.Id > 7 ? 1 : 0))],

				CurrentResearch = game.HumanPlayer.CurrentResearch?.Id ?? 0,
				DiscoveredAdvanceIDs = discoveredAdvanceIDs,

				LeaderNames = [.. game.Players.Select(x => x.LeaderName)],
				CivilizationNames = [.. game.Players.Select(x => x.TribeNamePlural)],
				CitizenNames = [.. game.Players.Select(x => x.TribeName)],
				CityNames = game.CityNames,

				PlayerGold = [.. game.Players.Select(x => x.Gold)],
				ResearchProgress = [.. game.Players.Select(x => x.Science)],
				TaxRate = [.. game.Players.Select(x => (ushort)x.TaxesRate)],
				ScienceRate = [.. game.Players.Select(x => (ushort)x.ScienceRate)],
				StartingPositionX = [.. game.Players.Select(x => (ushort)x.StartX)],
				Government = [.. game.Players.Select(x => (ushort)x.Government.Id)],

				Cities = [.. game.Cities.GetCityData()],
				Units = [.. game.Players.Select(player => game.Units.Where(u => player == u.Owner).GetUnitData().ToArray())],

				Wonders = wonders,
				TileVisibility = visibility,
				AdvanceFirstDiscovery = firstDiscovery,

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
				OpponentCount = (ushort)(game.Players.Length - 2),
				ReplayData = [.. game.ReplayData]
			};
		}
	}
}