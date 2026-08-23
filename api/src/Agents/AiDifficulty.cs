namespace CivOne.Agents
{
	/// <summary>
	/// Describes the intended strength or maturity level of one AI definition.
	/// The values match the game difficulty indices, so a value can be cast to and from the index
	/// used by the rest of the game.
	/// </summary>
	public enum AiDifficulty
	{
		/// <summary>
		/// No explicit difficulty classification is assigned.
		/// </summary>
		Unspecified = -1,

		/// <summary>
		/// Chieftain game difficulty.
		/// </summary>
		Chieftain = 0,

		/// <summary>
		/// Warlord game difficulty.
		/// </summary>
		Warlord = 1,

		/// <summary>
		/// Prince game difficulty.
		/// </summary>
		Prince = 2,

		/// <summary>
		/// King game difficulty.
		/// </summary>
		King = 3,

		/// <summary>
		/// Emperor game difficulty.
		/// </summary>
		Emperor = 4,

		/// <summary>
		/// Deity game difficulty.
		/// </summary>
		Deity = 5
	}
}
