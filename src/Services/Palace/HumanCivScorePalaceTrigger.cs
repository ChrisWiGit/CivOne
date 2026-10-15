namespace CivOne.Services.Palace
{
	/// <summary>
	/// Offers the human player a palace upgrade once the civilization score reaches the threshold of the original game.
	/// </summary>
	/// <remarks>
	/// The original checks this once per turn, only with the game option "Palace" on and only while the human player
	/// owns a palace city, and no longer after the final score was recorded ("scoring completed").
	/// The caller runs the check once per turn; this trigger tests the remaining conditions.
	/// See <c>docs/plan-civilizationScore.impl.md</c>, §5.
	/// </remarks>
	internal sealed class HumanCivScorePalaceTrigger : IPalaceUpgradeTrigger
	{
		public bool ShouldTrigger(IPlayerGameState player)
		{
			if (player == null || player.Palace == null || !player.Palace.CanUpgrade)
			{
				return false;
			}

			if (!player.IsHuman || !player.PalaceOptionEnabled || !player.HasPalaceCity || player.ScoringCompleted)
			{
				return false;
			}

			return player.CivilizationScore >= Threshold(player.Palace.UpgradeCount, player.Difficulty);
		}

		/// <summary>
		/// Original formula, with <c>n = upgrades so far + 1</c> and <c>D</c> the difficulty:
		/// <c>threshold(n) = (7 - D) * n * (n + 1) / 2 / 2 + 1</c>, integer division.
		/// The original's difficulties run 0-4; CivOneX's Deity (5) continues the curve with coefficient 2.
		/// </summary>
		private static long Threshold(int upgradeCount, int difficulty)
		{
			long n = upgradeCount + 1L;
			return ((7L - difficulty) * n * (n + 1) / 2 / 2) + 1;
		}
	}
}
