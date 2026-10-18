namespace CivOne.Services.Peace
{
	/// <summary>
	/// Counts the turns of peace that feed the peace bonus of the civilization score.
	/// The counter is global, not per player.
	/// </summary>
	public interface IPeaceTurnService
	{
		/// <summary>
		/// Gets the number of consecutive peaceful turns.
		/// The counter itself is not capped; the score caps its own term.
		/// </summary>
		ushort PeaceTurns { get; }

		/// <summary>
		/// Records a hostile action between two civilization slots.
		/// The streak is reset on the next <see cref="AdvanceTurn"/>.
		/// </summary>
		/// <param name="attackerPlayer">Slot index of the attacking civilization.</param>
		/// <param name="defenderPlayer">Slot index of the defending civilization, or <c>-1</c> if there is none.</param>
		void RegisterCombat(int attackerPlayer, int defenderPlayer);

		/// <summary>
		/// Applies a pending reset and then counts the new turn.
		/// Call once per turn wrap.
		/// </summary>
		/// <param name="year">The year of the new turn; negative values are BC.</param>
		void AdvanceTurn(int year);

		/// <summary>
		/// Replaces the counter with a loaded value and drops any pending reset.
		/// </summary>
		/// <param name="peaceTurns">The loaded counter value.</param>
		void Restore(ushort peaceTurns);
	}
}
