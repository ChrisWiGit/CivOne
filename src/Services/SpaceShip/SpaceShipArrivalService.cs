using System;

namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// Default <see cref="ISpaceShipArrivalService"/>: a ship arrives in <c>LaunchYear + Ceiling(FlightTimeYears)</c>.
	/// </summary>
	/// <remarks>
	/// The original game stores a truncated arrival year at launch and advances the calendar in 1-year steps
	/// between 1750 and 1850 while a ship is in flight, so it tests <c>ETA == Year</c>.
	/// CivOneX derives the arrival year and keeps 2-year steps, so an equality test could be skipped over.
	/// <see cref="ArrivedThisTurn"/> therefore checks that the arrival year falls into the span of the turn.
	/// See <c>docs/plan-civilizationScore.impl.md</c>, deviation 7 in §8, and <c>docs/plan-calendar.impl.md</c>.
	/// </remarks>
	/// <param name="spaceShipServiceFactory">Creates the per-player spaceship service; <c>null</c> uses the production factory.</param>
	/// <param name="calendarService">Converts turns to years; <c>null</c> uses the current calendar.</param>
	public sealed class SpaceShipArrivalService(
		ISpaceShipServiceFactory? spaceShipServiceFactory = null,
		IGameCalendarService? calendarService = null) : ISpaceShipArrivalService
	{
		private readonly ISpaceShipServiceFactory? _spaceShipServiceFactory = spaceShipServiceFactory;
		private readonly IGameCalendarService? _calendarService = calendarService;

		private ISpaceShipServiceFactory SpaceShipServiceFactory => _spaceShipServiceFactory ?? SpaceShipServiceFactoryProvider.GetInstance();

		private IGameCalendarService CalendarService => _calendarService ?? GameCalendarServiceFactory.Current;

		/// <inheritdoc />
		public int? ArrivalYear(IPlayerSpaceRace player)
		{
			ArgumentNullException.ThrowIfNull(player);

			if (player.SpaceShipLaunchYear == 0)
			{
				return null;
			}

			SpaceShipScreenData screenData = SpaceShipServiceFactory.Create(player).GetScreenData();
			return player.SpaceShipLaunchYear + (int)Math.Ceiling(screenData.FlightTimeYears);
		}

		/// <inheritdoc />
		public bool HasArrived(IPlayerSpaceRace player, int year)
		{
			int? arrivalYear = ArrivalYear(player);
			return arrivalYear.HasValue && year >= arrivalYear.Value;
		}

		/// <inheritdoc />
		public bool ArrivedThisTurn(IPlayerSpaceRace player, ushort turn)
		{
			int? arrivalYear = ArrivalYear(player);
			if (!arrivalYear.HasValue)
			{
				return false;
			}

			int year = CalendarService.TurnToYear(turn);
			int previousYear = turn == 0 ? int.MinValue : CalendarService.TurnToYear((ushort)(turn - 1));
			return previousYear < arrivalYear.Value && arrivalYear.Value <= year;
		}

		/// <inheritdoc />
		public int SuccessRate(IPlayerSpaceRace player)
		{
			ArgumentNullException.ThrowIfNull(player);

			return SpaceShipServiceFactory.Create(player).SuccessRate();
		}
	}
}
