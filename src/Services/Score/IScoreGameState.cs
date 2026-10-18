namespace CivOne.Services.Score
{
	/// <summary>
	/// The global game state the civilization score reads.
	/// </summary>
	public interface IScoreGameState
	{
		/// <summary>
		/// Gets the number of polluted map squares. The value is global, not per player.
		/// </summary>
		int PollutedSquaresCount { get; }

		/// <summary>
		/// Gets the number of consecutive peaceful turns. The value is global, not per player.
		/// </summary>
		int PeaceTurns { get; }

		/// <summary>
		/// Gets the current year; negative values are BC.
		/// </summary>
		int Year { get; }

		/// <summary>
		/// Gets the current game turn.
		/// </summary>
		int GameTurn { get; }

		/// <summary>
		/// Gets the number of AI opponents chosen at game setup, barbarians not counted.
		/// </summary>
		int OpponentCount { get; }

		/// <summary>
		/// Gets whether the human player has eliminated every other civilization.
		/// Decided by the game's <see cref="WorldConquestDelegate"/>.
		/// </summary>
		bool IsWorldConquered { get; }
	}
}
