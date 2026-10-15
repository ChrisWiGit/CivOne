using System;
using System.Collections.Generic;
using CivOne.Services.SpaceShip;

namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Default <see cref="IGameEndCheckService"/>, like the original game.
	/// </summary>
	/// <remarks>
	/// <list type="bullet">
	/// <item>The game ends in the year <c>2100 - 20 x D</c> and warns once in <c>2080 - 20 x D</c>; both are exact years.
	/// From 1850 the calendar advances by 1 year per turn, so both years are always reached.
	/// CivOneX's Deity (5) continues the formula: warning in 1980, end in 2000.</item>
	/// <item>A spaceship arrives in the turn of its arrival year. If several ships arrive in the same year,
	/// the player with the highest slot wins; the others are never counted.</item>
	/// <item>World conquest clears the spaceship winner. It does not stop the year end.</item>
	/// <item>The "scoring completed" latch does not stop the check; later events still end the turn.</item>
	/// </list>
	/// See <c>docs/plan-endOfGameContinue.impl.md</c>.
	/// </remarks>
	/// <param name="arrivalService">Decides when a spaceship arrives; <c>null</c> uses the production service.</param>
	/// <param name="calendarService">Converts turns to years; <c>null</c> uses the current calendar.</param>
	internal sealed class GameEndCheckService(
		ISpaceShipArrivalService? arrivalService = null,
		IGameCalendarService? calendarService = null) : IGameEndCheckService
	{
		private const int SlotCount = 8;

		private readonly ISpaceShipArrivalService? _arrivalService = arrivalService;
		private readonly IGameCalendarService? _calendarService = calendarService;

		private ISpaceShipArrivalService ArrivalService => _arrivalService ?? SpaceShipServiceFactoryProvider.CreateArrivalService();

		private IGameCalendarService CalendarService => _calendarService ?? GameCalendarServiceFactory.Current;

		/// <summary>
		/// Gets the final year of the game.
		/// </summary>
		/// <param name="difficulty">The difficulty, 0 (Chieftain) to 5 (Deity).</param>
		/// <returns><c>2100 - 20 x difficulty</c>.</returns>
		public static int EndYear(int difficulty) => 2100 - (20 * difficulty);

		/// <summary>
		/// Gets the year of the retirement warning, 20 years before the end.
		/// </summary>
		/// <param name="difficulty">The difficulty, 0 (Chieftain) to 5 (Deity).</param>
		/// <returns><c>2080 - 20 x difficulty</c>.</returns>
		public static int WarningYear(int difficulty) => EndYear(difficulty) - 20;

		/// <inheritdoc />
		public GameEndCheckResult Check(IReadOnlyList<IPlayerSpaceRace> playersBySlot, ushort turn, int difficulty, bool worldConquered)
		{
			ArgumentNullException.ThrowIfNull(playersBySlot);

			int year = CalendarService.TurnToYear(turn);
			int winner = worldConquered ? GameEndCheckResult.NoWinner : SpaceshipWinner(playersBySlot, turn);

			return new(
				RetirementWarning: year == WarningYear(difficulty),
				Conquest: worldConquered,
				SpaceshipWinnerSlot: winner,
				YearEnd: year == EndYear(difficulty));
		}

		private int SpaceshipWinner(IReadOnlyList<IPlayerSpaceRace> playersBySlot, ushort turn)
		{
			int winner = GameEndCheckResult.NoWinner;
			for (int slot = 1; slot < Math.Min(playersBySlot.Count, SlotCount); slot++)
			{
				if (ArrivalService.ArrivedThisTurn(playersBySlot[slot], turn))
				{
					winner = slot;
				}
			}

			return winner;
		}
	}
}
