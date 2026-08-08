using System.Collections.Generic;
using CivOne.Civilizations;

namespace CivOne.Services.Civilizations
{
	/// <summary>
	/// Picks the civilization a destroyed player is replaced with when its slot respawns.
	/// </summary>
	internal interface IRespawnCivilizationService
	{
		/// <summary>
		/// Selects the replacement civilization for a destroyed player.
		/// </summary>
		/// <param name="destroyed">The civilization of the player that was destroyed. Never selected again.</param>
		/// <param name="civilizationsInUse">The civilizations of all players that are still alive.</param>
		/// <param name="preferBuddyCivilization">Whether to prefer the destroyed civilization's buddy when it is free.</param>
		/// <returns>The replacement civilization and how many living players already use it.</returns>
		RespawnCivilizationResult SelectReplacement(
			ICivilization destroyed,
			IReadOnlyCollection<ICivilization> civilizationsInUse,
			bool preferBuddyCivilization);
	}
}
