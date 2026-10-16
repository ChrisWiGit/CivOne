using System;
using System.Linq;
using CivOne.Services.Score;
using CivOne.Services.SpaceShip;

namespace CivOne.Services
{
	/// <summary>
	/// Calculates the civilization score with the formula of the original game.
	/// </summary>
	/// <remarks>
	/// <code>
	/// score = Σ cities (Size + Happy - (Unhappy + RedShirt))   // at 40 % luxuries
	///       + 20 × wonders
	///       + SuccessRate × Population / 2                     // only in the arrival turn
	///       - 10 × polluted squares
	///       + min(3 × PeaceTurns, 100)                         // only in AD years
	///       + 5 × future techs
	/// score = clamp(score, 0, short.MaxValue)
	/// if conquered: bonus = 400 + 100 × opponents + 2 × (550 - turn); if bonus &gt;= score: score = bonus
	/// </code>
	/// <para>
	/// Deliberate deviations from the original:
	/// </para>
	/// <list type="bullet">
	/// <item>The sum saturates at <see cref="short.MaxValue"/> instead of wrapping in 16 bit (deviation 12).</item>
	/// <item>
	/// The spaceship term applies in the turn the arrival is detected, not when a stored arrival year equals the
	/// current year (deviation 7). The success rate is read live, not from the last spaceship screen visit (deviation 2).
	/// </item>
	/// <item>Red shirts count as unhappy citizens; the original has no separate red-shirt count.</item>
	/// <item>Any player can be scored; the original scores only the human player (deviation 14).</item>
	/// </list>
	/// <para>
	/// See <c>docs/plan-civilizationScore.impl.md</c>, §1 and §8.
	/// </para>
	/// </remarks>
	/// <param name="game">The running game; <c>null</c> reads <see cref="Game.Instance"/>.</param>
	/// <param name="arrivalService">Decides spaceship arrival and success rate; <c>null</c> uses the default.</param>
	/// <param name="calendarService">Converts turns to years; <c>null</c> uses the current calendar.</param>
	internal sealed class CivilizationScoreService(
		Game? game = null,
		ISpaceShipArrivalService? arrivalService = null,
		IGameCalendarService? calendarService = null) : ICivilizationScoreService
	{
		/// <summary>
		/// The luxury rate, in tenths, at which the score measures happiness: always 40 %, whatever the player set.
		/// </summary>
		internal const int ScoringLuxuryRate = 4;

		private const int WonderWeight = 20;
		private const int PollutionWeight = 10;
		private const int PeaceWeight = 3;
		private const int MaxPeaceScore = 100;
		private const int FutureTechWeight = 5;
		private const int SpaceShipPopulationUnit = 10_000;
		private const int ConquestBase = 400;
		private const int ConquestPerOpponent = 100;
		private const int ConquestTurnLimit = 550;

		private readonly Game? _game = game;
		private ISpaceShipArrivalService? _arrivalService = arrivalService;
		private readonly IGameCalendarService? _calendarService = calendarService;

		private Game Game => _game ?? Game.Instance;

		private ISpaceShipArrivalService ArrivalService => _arrivalService ??= SpaceShipServiceFactoryProvider.CreateArrivalService();

		private IGameCalendarService CalendarService => _calendarService ?? GameCalendarServiceFactory.Current;

		/// <inheritdoc />
		public CivilizationScoreBreakdown Calculate(IScoredCivilization civilization, IScoreGameState game)
		{
			ArgumentNullException.ThrowIfNull(civilization);
			ArgumentNullException.ThrowIfNull(game);

			int citizenScore = civilization.Cities.Sum(city => city.Size + city.Happy - (city.Unhappy + city.RedShirt));
			int wonderScore = WonderWeight * civilization.WonderCount;
			int spaceShipValue = SpaceShipValue(civilization);
			bool spaceShipApplied = spaceShipValue > 0 && civilization.SpaceShipArrivedThisTurn;
			int pollutionScore = -PollutionWeight * game.PollutedSquaresCount;
			int peaceScore = game.Year > 0 ? Math.Min(PeaceWeight * game.PeaceTurns, MaxPeaceScore) : 0;
			int futureTechScore = FutureTechWeight * civilization.FutureTechCount;

			long sum = (long)citizenScore + wonderScore + (spaceShipApplied ? spaceShipValue : 0)
				+ pollutionScore + peaceScore + futureTechScore;
			int total = (int)Math.Clamp(sum, 0, short.MaxValue);

			bool isBonus = false;
			if (game.IsWorldConquered)
			{
				int bonus = ConquestBase + (ConquestPerOpponent * game.OpponentCount) + (2 * (ConquestTurnLimit - game.GameTurn));
				if (bonus >= total)
				{
					total = bonus;
					isBonus = true;
				}
			}

			return new CivilizationScoreBreakdown(
				citizenScore,
				wonderScore,
				spaceShipValue,
				spaceShipApplied,
				pollutionScore,
				peaceScore,
				futureTechScore,
				total,
				isBonus);
		}

		/// <inheritdoc />
		public CivilizationScoreBreakdown Calculate(Player player)
		{
			ArgumentNullException.ThrowIfNull(player);

			return Calculate(
				new PlayerScoreInput(player, ArrivalService, Game.GameTurn),
				new GameScoreState(Game, CalendarService));
		}

		/// <inheritdoc />
		public int TotalScore(Player player) => Calculate(player).Total;

		private static int SpaceShipValue(IScoredCivilization civilization)
		{
			int population = civilization.SpaceShipPopulation / SpaceShipPopulationUnit;
			int successRate = civilization.SpaceShipSuccessRate;
			if (population == 0 || successRate == 0)
			{
				return 0;
			}

			return Math.Max(0, successRate * population / 2);
		}
	}
}
