using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CivOne.Persistence;
using CivOne.Persistence.Model;
using CivOne.Services.GlobalWarming;
using CivOne.Tiles;
using CivOne.Units;

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
		Player? HumanPlayer { get; }

		Player[] Players { get; }

		[SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "A list is suffice and changing it would require unnecessary changes to the GameState and related code.")]
		List<City> Cities { get; }
		[SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "A list is suffice and changing it would require unnecessary changes to the GameState and related code.")]
		List<IUnit> Units { get; }

		

		Dictionary<byte, byte> AdvanceOrigin { get; }

		ushort GameTurn { get; }
		ushort AnthologyTurn { get; }
		ushort PeaceTurns { get; }
		ushort PlayerFutureTech { get; }

		string[] CityNames { get; }

		[SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "A list is suffice and changing it would require unnecessary changes to the GameState and related code.")]
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

		uint? GameRandomSeed { get; }

		(short X, short Y)? GetHumanLastMapPosition();

		int TerrainMasterWord { get; }

		IGlobalWarmingService GlobalWarmingService { get; }
	}

	public static class GameStateHandler
	{
		public static GameState Create(IGameSnapshotSource game)
		{
			ArgumentNullException.ThrowIfNull(game);

			Debug.Assert(game.HumanPlayer == null || game.Players.Contains(game.HumanPlayer), "Human player must be in the list of players.");
			
			if (game.HumanPlayer != null && game.GetHumanLastMapPosition() is { } humanLastMapPosition)
			{
				game.HumanPlayer.LastMapPosition = humanLastMapPosition;
			}

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
				RandomSeed = game.GameRandomSeed ?? unchecked((uint)game.TerrainMasterWord),
				
				MapWidth = game.MapTiles.GetLength(0),
				MapHeight = game.MapTiles.GetLength(0) > 0 ? 
							game.MapTiles.GetLength(1) : 0,
				
				MapTiles = game.MapTiles,
				Units = game.Units,
				CityNames = game.CityNames,
				
				GameOptions = [.. options				
					.Select((option, index) => (option, index))
					.Where(x => x.option)
					.Select(x => (GameSetting)x.index)],

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
	}
}