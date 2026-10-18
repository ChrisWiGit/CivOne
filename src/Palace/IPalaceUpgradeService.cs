namespace CivOne.Services.Palace
{
	/// <summary>
	/// Service for evaluating palace upgrade triggers.
	/// Enables extensible trigger system where multiple conditions can fire independently.
	/// </summary>
	public interface IPalaceUpgradeService
	{
		/// <summary>
		/// Determines whether the palace upgrade screen should be shown for the given player.
		/// Evaluates all registered triggers and returns true if any trigger fires.
		/// </summary>
		/// <param name="player">The player to evaluate</param>
		/// <returns>True if at least one trigger fires and the player has upgradeable slots</returns>
		bool ShouldShowPalaceUpgrade(IPlayerGameState player);
	}
}