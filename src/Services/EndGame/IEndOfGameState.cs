namespace CivOne.Services.EndGame
{
	/// <summary>
	/// The end-of-game state the end-game flow reads and writes.
	/// </summary>
	internal interface IEndOfGameState
	{
		/// <summary>
		/// Gets or sets whether the final score was recorded and the player chose to keep playing.
		/// While set, no further end-game flow runs and no Hall of Fame entry is made.
		/// </summary>
		bool ScoringCompleted { get; set; }
	}
}
