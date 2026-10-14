using System;

namespace CivOne.Services.Score
{
	/// <summary>
	/// Live <see cref="IScoreGameState"/> view of a <see cref="Game"/>.
	/// </summary>
	internal sealed class GameScoreState : IScoreGameState
	{
		private readonly Game _game;
		private readonly IGameCalendarService _calendarService;

		/// <summary>
		/// Creates a view of <paramref name="game"/>.
		/// </summary>
		/// <param name="game">The game to read.</param>
		/// <param name="calendarService">Converts the game turn to a year.</param>
		public GameScoreState(Game game, IGameCalendarService calendarService)
		{
			ArgumentNullException.ThrowIfNull(game);
			ArgumentNullException.ThrowIfNull(calendarService);
			_game = game;
			_calendarService = calendarService;
		}

		/// <inheritdoc />
		public int PollutedSquaresCount => _game.GlobalWarmingService?.PollutedSquaresCount ?? 0;

		/// <inheritdoc />
		public int PeaceTurns => _game.PeaceTurns;

		/// <inheritdoc />
		public int Year => _calendarService.TurnToYear(_game.GameTurn);

		/// <inheritdoc />
		public int GameTurn => _game.GameTurn;

		/// <inheritdoc />
		public int OpponentCount => _game.Competition - 1;

		/// <inheritdoc />
		public bool IsWorldConquered => _game.IsWorldConquered();
	}
}
