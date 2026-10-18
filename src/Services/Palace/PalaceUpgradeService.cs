using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Services.Palace
{
	/// <summary>
	/// Service responsible for determining whether the palace upgrade screen should be shown to the player.
	/// This class allows multiple triggers to be defined, and if any trigger indicates that the upgrade screen should be shown, then it will be shown.
	/// Use the PalaceUpgradeServiceFactory to get an instance of this service with the default triggers registered.
	/// </summary>
	internal sealed class PalaceUpgradeService : IPalaceUpgradeService
	{
		private readonly IReadOnlyList<IPalaceUpgradeTrigger> _triggers;

		public PalaceUpgradeService(IReadOnlyList<IPalaceUpgradeTrigger> triggers)
		{
			ArgumentNullException.ThrowIfNull(triggers);
			_triggers = triggers;
		}

		public bool ShouldShowPalaceUpgrade(IPlayerGameState player)
		{
			if (player == null)
			{
				return false;
			}

			return _triggers.Any(t => t.ShouldTrigger(player));
		}
	}
}