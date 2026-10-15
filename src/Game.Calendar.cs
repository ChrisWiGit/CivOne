using System.Linq;
using CivOne.Civilizations;
using CivOne.Services;
using CivOne.Services.Calendar;

namespace CivOne
{
	/// <summary>
	/// Calendar state of the game: the year of every turn follows from the turn number and the turns in which a
	/// spaceship flight slowed the calendar down to 1-year steps.
	/// </summary>
	public partial class Game
	{
		private readonly CalendarState _calendarState;

		/// <summary>
		/// Gets the calendar history of this game (turns with a 1-year step during a spaceship flight).
		/// </summary>
		internal ICalendarState CalendarState => _calendarState;

		private static IGameCalendarService CalendarService => GameCalendarServiceFactory.Current;

		private bool AnySpaceShipInFlight => _players.Any(x => x.Civilization is not Barbarian && SpaceShipArrivalService.IsInFlight(x));

		/// <summary>
		/// Like the original game, the research progress of every civilization doubles once, in the turn the calendar
		/// changes from BC to 1 AD. Only the progress is doubled, not the cost of the advance.
		/// </summary>
		private void DoubleResearchAtAnnoDomini()
		{
			if (GameTurn == 0 || CalendarService.TurnToYear(GameTurn) != 1 || CalendarService.TurnToYear((ushort)(GameTurn - 1)) >= 0)
			{
				return;
			}

			foreach (Player player in _players)
			{
				player.Science = (short)(player.Science << 1);
			}
		}

		/// <summary>
		/// Savegames from before the arrival year was stored have a launched ship without one.
		/// Such a ship never arrives; the warning makes the case visible.
		/// </summary>
		private void WarnAboutShipsWithoutArrivalYear()
		{
			foreach (Player player in _players.Where(x => x.SpaceShipLaunchYear != 0 && x.SpaceShipEtaYear == -1))
			{
				Log("Spaceship of {0} was launched in {1} but has no arrival year; it will not arrive. Launch it again to fix this savegame.", player.TribeName, player.SpaceShipLaunchYear);
			}
		}
	}
}
