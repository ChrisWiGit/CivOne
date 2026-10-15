using System;
using System.Linq;
using CivOne.Services.Calendar;
using CivOne.Services.SpaceShip;

namespace CivOne
{
	/// <summary>
	/// SVE persistence of the space race, the calendar history and the "scoring completed" latch.
	/// </summary>
	public partial class Game
	{
		private const int SveSlotCount = 8;

		private void StoreSpaceRace(SaveDataAdapter gameData)
		{
			SveSpaceRaceDelegate spaceRace = new(SpaceShipArrivalService);
			int humanSlot = PlayerNumber(HumanPlayer);

			gameData.SpaceshipFlags = spaceRace.BuildFlags(_players, ScoringCompleted);
			gameData.SpaceshipEtaYear = spaceRace.BuildArrivalYears(_players);
			gameData.SpaceshipSuccessRateHuman = spaceRace.SuccessRate(_players, humanSlot, human: true);
			gameData.SpaceshipSuccessRateAi = spaceRace.SuccessRate(_players, humanSlot, human: false);
			gameData.SpaceShipLaunchYear = Slots(slot => slot < _players.Length ? _players[slot].SpaceShipLaunchYear : (short)0);
			gameData.SpaceShipPopulation = Slots(slot => slot < _players.Length ? _players[slot].SpaceShipPopulation : (ushort)0);
		}

		private void RestoreSpaceRace(IGameData gameData)
		{
			_calendarState.Restore(new SveCalendarDelegate().Rebuild(gameData.GameTurn, gameData.GameYear));

			short[] launchYears = gameData.SpaceShipLaunchYear;
			ushort[] populations = gameData.SpaceShipPopulation;
			for (int slot = 0; slot < Math.Min(_players.Length, SveSlotCount); slot++)
			{
				_players[slot].SpaceShipLaunchYear = launchYears[slot];
				_players[slot].SpaceShipPopulation = populations[slot];
			}

			SveSpaceRaceDelegate spaceRace = new(SpaceShipArrivalService);
			spaceRace.RestoreArrivalYears(_players, gameData.SpaceshipFlags, gameData.SpaceshipEtaYear);
			ScoringCompleted = spaceRace.IsScoringCompleted(gameData.SpaceshipFlags);
		}

		private static T[] Slots<T>(Func<int, T> value) => [.. Enumerable.Range(0, SveSlotCount).Select(value)];
	}
}
