using System;

namespace CivOne.Services.Palace
{
	/// <summary>
	/// Trigger that determines whether the palace upgrade screen should be shown based on the player's civilization score.
	/// The upgrade screen will be shown if the player's civilization score is greater than or equal to the threshold defined by the formula: 1 + (n * n) + n, where n is the number of times the player has already upgraded their palace.
	/// This trigger only applies to human players, as AI players do not need to see the upgrade screen.
	/// </summary>
	internal sealed class HumanCivScorePalaceTrigger : IPalaceUpgradeTrigger
	{
		public bool ShouldTrigger(IPlayerGameState player)
		{
			if (player == null || player.Palace == null || !player.Palace.CanUpgrade)
			{
				return false;
			}

			if (!player.IsHuman)
			{
				return false;
			}

			int n = player.Palace.UpgradeCount;
			long threshold = 1L + ((long)n * n) + n;
			return player.CivilizationScore >= threshold;
		}
	}
}