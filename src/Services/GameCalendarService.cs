using CivOne.Services.Calendar;

namespace CivOne.Services
{
	/// <summary>
	/// Converts between game turns and calendar years, and formats years for display.
	/// BC/AD labels are resolved through the translation service to support localization.
	/// </summary>
	/// <remarks>
	/// Like the original game, the year advances by 1 instead of 2 between 1750 and 1850 while a spaceship is in
	/// flight. Those turns are recorded in <see cref="ICalendarState"/>, so the year of every turn, also of past
	/// turns, follows from the turn number and that record. Without any record the plain formula applies.
	/// </remarks>
	/// <param name="translationService">Translates the era labels; <c>null</c> uses the current translation.</param>
	/// <param name="calendarState">The game's calendar history; <c>null</c> uses the state of the running game.</param>
	/// <param name="stepRule">The year step rule; <c>null</c> uses the original rule.</param>
	public class GameCalendarService(
		ITranslationService? translationService = null,
		ICalendarState? calendarState = null,
		ICalendarStepRule? stepRule = null) : IGameCalendarService
	{
		private const ushort FirstTwoYearTurn = 350;
		private const int FirstTwoYearYear = 1750;
		private const int FirstOneYearYear = 1850;

		private readonly ITranslationService? _translationService = translationService;
		private readonly ICalendarState? _calendarState = calendarState;
		private readonly ICalendarStepRule? _stepRule = stepRule;

		private ITranslationService Translation => _translationService ?? TranslationServiceFactory.GetCurrent();

		private ICalendarState State => _calendarState ?? GameCalendarServiceFactory.CurrentState;

		private ICalendarStepRule StepRule => _stepRule ?? GameCalendarServiceFactory.CreateStepRule();

		/// <summary>
		/// Converts a calendar year to the last game turn whose year is not after it.
		/// Years before -4000 clamp to turn 0.
		/// </summary>
		public ushort YearToTurn(int year)
		{
			if (year < FirstTwoYearYear || State.AcceleratedTurns.Count == 0)
			{
				return PlainYearToTurn(year);
			}

			ushort turn = FirstTwoYearTurn;
			int turnYear = FirstTwoYearYear;
			while (turn < ushort.MaxValue)
			{
				int nextYear = turnYear + TwoYearPhaseStep(turnYear, (ushort)(turn + 1));
				if (nextYear > year)
				{
					break;
				}

				turn++;
				turnYear = nextYear;
			}

			return turn;
		}

		/// <summary>
		/// Converts a game turn to the corresponding calendar year.
		/// Returns negative values for BC years.
		/// </summary>
		public int TurnToYear(ushort turn)
		{
			if (turn <= FirstTwoYearTurn || State.AcceleratedTurns.Count == 0)
			{
				return PlainTurnToYear(turn);
			}

			int year = FirstTwoYearYear;
			for (int nextTurn = FirstTwoYearTurn + 1; nextTurn <= turn; nextTurn++)
			{
				if (year >= FirstOneYearYear)
				{
					return year + (turn - nextTurn + 1);
				}

				year += TwoYearPhaseStep(year, (ushort)nextTurn);
			}

			return year;
		}

		/// <inheritdoc />
		public void AdvanceTurn(ushort newTurn, bool spaceshipInFlight)
		{
			if (newTurn <= FirstTwoYearTurn)
			{
				return;
			}

			int previousYear = TurnToYear((ushort)(newTurn - 1));
			if (previousYear >= FirstOneYearYear)
			{
				return;
			}

			if (StepRule.YearStep(previousYear, spaceshipInFlight) == 1)
			{
				State.MarkAccelerated(newTurn);
			}
		}

		/// <summary>
		/// Formats a game turn as a human-readable year string, e.g. "4000 BC" or "1 AD".
		/// The era labels "BC" and "AD" are passed through the translation service.
		/// </summary>
		public string FormatYear(ushort turn, bool zeroAd = false)
		{
			int year = TurnToYear(turn);
			if (zeroAd && year == 1) year = 0;

			if (year < 0)
			{
				string bc = Translation.Translate("BC");
				return $"{-year} {bc}";
			}

			string ad = Translation.Translate("AD");
			return $"{year} {ad}";
		}

		public string FormatEra(ushort turn)
		{
			int year = TurnToYear(turn);
			if (year < 0)
			{
				return Translation.Translate("BC");
			}

			return Translation.Translate("AD");
		}

		private int TwoYearPhaseStep(int year, ushort nextTurn)
		{
			if (year >= FirstOneYearYear || State.IsAccelerated(nextTurn))
			{
				return 1;
			}

			return 2;
		}

		private static ushort PlainYearToTurn(int year)
		{
			if (year < -4000) return 0;
			if (year < 1000) return (ushort)System.Math.Floor(((double)year + 4000) / 20);
			if (year < 1500) return (ushort)System.Math.Floor(((double)year + 1500) / 10);
			if (year < 1750) return (ushort)System.Math.Floor(((double)year) / 5);
			if (year < 1850) return (ushort)System.Math.Floor(((double)year - 1050) / 2);
			return (ushort)(year - 1450);
		}

		private static int PlainTurnToYear(ushort turn)
		{
			if (turn < 200) return -(200 - turn) * 20;
			if (turn == 200) return 1;
			if (turn < 250) return (turn - 200) * 20;
			if (turn < 300) return ((turn - 250) * 10) + 1000;
			if (turn < 350) return ((turn - 300) * 5) + 1500;
			if (turn < 400) return ((turn - 350) * 2) + 1750;
			return turn - 400 + 1850;
		}
	}

	public static class GameCalendarServiceFactory
	{
		private static IGameCalendarService? _current;
		private static ICalendarState? _currentState;

		public static IGameCalendarService Current { get => _current ??= new GameCalendarService(); }

		/// <summary>
		/// Gets the calendar history of the running game.
		/// An empty history is created on first use when no game registered one.
		/// </summary>
		public static ICalendarState CurrentState => _currentState ??= new CalendarState();

		public static void SetCurrent(IGameCalendarService service)
		{
			_current = service;
		}

		/// <summary>
		/// Makes <paramref name="state"/> the calendar history used by every calendar service without an own state.
		/// Called by the game when it is created or loaded.
		/// </summary>
		/// <param name="state">The calendar history of the game.</param>
		public static void UseState(ICalendarState state)
		{
			_currentState = state;
		}

		/// <summary>
		/// Creates the year step rule of the original game.
		/// </summary>
		/// <returns>A new step rule.</returns>
		public static ICalendarStepRule CreateStepRule() => new OriginalCalendarStepRuleDelegate();

		/// <summary>
		/// Creates a calendar service that reads <paramref name="state"/> instead of the running game's history.
		/// </summary>
		/// <param name="state">The calendar history to read.</param>
		/// <returns>A new calendar service.</returns>
		public static IGameCalendarService Create(ICalendarState state) => new GameCalendarService(calendarState: state);
	}
}
