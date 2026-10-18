namespace CivOne.Services.EndGame
{
	/// <summary>
	/// What the end-of-turn check found. Several reasons can hold in the same turn.
	/// </summary>
	/// <param name="RetirementWarning">The retirement warning is due this turn (20 years before the end).</param>
	/// <param name="Conquest">The world is conquered; the game ends without "keep playing?".</param>
	/// <param name="SpaceshipWinnerSlot">The slot of the player whose spaceship arrives this turn, or <see cref="NoWinner"/>.</param>
	/// <param name="YearEnd">The final year of the game is reached this turn.</param>
	internal sealed record GameEndCheckResult(bool RetirementWarning, bool Conquest, int SpaceshipWinnerSlot, bool YearEnd)
	{
		/// <summary>
		/// The value of <see cref="SpaceshipWinnerSlot"/> when no spaceship arrives.
		/// </summary>
		public const int NoWinner = -1;

		/// <summary>
		/// Gets whether a spaceship arrives this turn.
		/// </summary>
		public bool HasSpaceshipWinner => SpaceshipWinnerSlot != NoWinner;

		/// <summary>
		/// Gets whether the end-of-game block runs this turn.
		/// </summary>
		public bool EndsGame => Conquest || HasSpaceshipWinner || YearEnd;
	}
}
