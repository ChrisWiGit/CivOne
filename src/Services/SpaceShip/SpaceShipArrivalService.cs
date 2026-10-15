using System;
using CivOne.Enums;

namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// Default <see cref="ISpaceShipArrivalService"/>: like the original game, the arrival year is stored at launch
	/// as <c>LaunchYear + flightTenths / 10</c>, cut to whole years, and the ship arrives in the turn that reaches or
	/// steps over that year.
	/// </summary>
	/// <remarks>
	/// The calendar advances by 1 year between 1750 and 1850 while a ship is in flight, so the arrival year is always
	/// visited. See <c>docs/plan-calendar.impl.md</c>.
	/// </remarks>
	/// <param name="spaceShipServiceFactory">Creates the per-player spaceship service; <c>null</c> uses the production factory.</param>
	/// <param name="calendarService">Converts turns to years; <c>null</c> uses the current calendar.</param>
	public sealed class SpaceShipArrivalService(
		ISpaceShipServiceFactory? spaceShipServiceFactory = null,
		IGameCalendarService? calendarService = null) : ISpaceShipArrivalService
	{
		private const short NoArrival = -1;

		private readonly ISpaceShipServiceFactory? _spaceShipServiceFactory = spaceShipServiceFactory;
		private readonly IGameCalendarService? _calendarService = calendarService;

		private ISpaceShipServiceFactory SpaceShipServiceFactory => _spaceShipServiceFactory ?? SpaceShipServiceFactoryProvider.GetInstance();

		private IGameCalendarService CalendarService => _calendarService ?? GameCalendarServiceFactory.Current;

		/// <inheritdoc />
		public int? ArrivalYear(IPlayerSpaceRace player)
		{
			ArgumentNullException.ThrowIfNull(player);

			if (!IsInFlight(player))
			{
				return null;
			}

			return player.SpaceShipEtaYear;
		}

		/// <inheritdoc />
		public void Launch(IPlayerSpaceRace player, int year)
		{
			ArgumentNullException.ThrowIfNull(player);

			SpaceShipScreenData screenData = SpaceShipServiceFactory.Create(player).GetScreenData();
			int flightTenths = (int)Math.Round(screenData.FlightTimeYears * 10);

			player.SpaceShipLaunchYear = (short)year;
			player.SpaceShipEtaYear = (short)(year + (flightTenths / 10));
		}

		/// <inheritdoc />
		public void CancelLaunch(IPlayerSpaceRace player)
		{
			ArgumentNullException.ThrowIfNull(player);

			player.SpaceShipLaunchYear = 0;
			player.SpaceShipEtaYear = NoArrival;
		}

		/// <inheritdoc />
		public SpaceShipLoss LoseShip(IPlayerSpaceRace player)
		{
			ArgumentNullException.ThrowIfNull(player);

			SpaceShipLoss loss = SpaceShipLoss.None;
			if (IsInFlight(player))
			{
				loss = SpaceShipLoss.ReturnsToEarth;
			}
			else if (HasParts(player))
			{
				loss = SpaceShipLoss.ConstructionCancelled;
			}

			SpaceShipComponentType[,]? grid = player.SpaceShipGrid;
			if (grid != null)
			{
				player.SpaceShipGrid = new SpaceShipComponentType[grid.GetLength(0), grid.GetLength(1)];
			}

			player.SpaceShipPopulation = 0;
			CancelLaunch(player);
			return loss;
		}

		/// <inheritdoc />
		public bool IsInFlight(IPlayerSpaceRace player)
		{
			ArgumentNullException.ThrowIfNull(player);

			// -1 means no arrival year; a BC arrival year is negative but valid.
			return player.SpaceShipLaunchYear != 0 && player.SpaceShipEtaYear != NoArrival;
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

			// Deviation from the original, which tests ETA == Year: a ship also arrives when the turn steps over its
			// arrival year. With 1-year steps (1750-1850 during a flight, and from 1850) both tests are the same; the
			// original simply never had a ship in flight during larger steps. A ship launched earlier, e.g. through
			// the debug menu, would otherwise never arrive.
			int year = CalendarService.TurnToYear(turn);
			int previousYear = turn == 0 ? int.MinValue : CalendarService.TurnToYear((ushort)(turn - 1));
			return previousYear < arrivalYear.Value && arrivalYear.Value <= year;
		}

		private static bool HasParts(IPlayerSpaceRace player)
		{
			SpaceShipComponentType[,]? grid = player.SpaceShipGrid;
			if (grid == null)
			{
				return false;
			}

			foreach (SpaceShipComponentType part in grid)
			{
				if (part != SpaceShipComponentType.Empty)
				{
					return true;
				}
			}

			return false;
		}

		/// <inheritdoc />
		public int SuccessRate(IPlayerSpaceRace player)
		{
			ArgumentNullException.ThrowIfNull(player);

			return SpaceShipServiceFactory.Create(player).SuccessRate();
		}
	}
}
