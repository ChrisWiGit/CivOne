// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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
