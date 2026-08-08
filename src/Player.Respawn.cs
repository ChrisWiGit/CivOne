// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Governments;
using CivOne.Graphics.Sprites;
using CivOne.Services.Civilizations;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Wonders;

using Gov = CivOne.Governments;

namespace CivOne
{
	public partial class Player : BaseInstance, ITurn
	{
		private IRespawnCivilizationService? _respawnCivilizationService;
		private IRespawnCivilizationService RespawnCivilizationService => _respawnCivilizationService ??= RespawnCivilizationServiceFactory.Create();

		/// <summary>
		/// Creates the player that takes over this player's slot after it was destroyed.
		/// The replacement gets a civilization that nobody else plays where possible, and a name that
		/// distinguishes it from the other players when it has to share one.
		/// </summary>
		/// <returns>The new player for this slot.</returns>
		public Player Respawn()
		{
			Debug.Assert(IsHuman == false, "Cannot respawn a human player!");
			Debug.Assert(Civilization.PreferredPlayerNumber != 0, "Cannot respawn barbarian player!");

			RespawnCivilizationResult replacement = RespawnCivilizationService.SelectReplacement(Civilization, CivilizationsInUse());
			CivilizationNames names = new CivilizationNameDelegate().Build(replacement.Civilization, replacement.Occurrence);

			return new Player(replacement.Civilization, names.LeaderName, names.TribeName, names.TribeNamePlural);
		}

		/// <summary>
		/// The civilizations of all other players that are still alive.
		/// Reads the raw <c>_destroyed</c> flag on purpose: the <see cref="IsDestroyed"/> property runs
		/// <see cref="HandleExtinction(bool)"/>, which disbands units and marks players as destroyed, and must
		/// not be triggered for every player just to build this list.
		/// </summary>
		/// <returns>One entry per living player, including duplicates when a civilization is shared.</returns>
		private ICivilization[] CivilizationsInUse()
			=> [.. Game.Players.Where(player => player != this && !player._destroyed).Select(player => player.Civilization)];

		public bool AllowedToRespawn(ReplayData.CivilizationDestroyed[] ReplayData)
		{
			// Use the player's slot index (not Civilization.PreferredPlayerNumber): with civilization reuse
			// beyond player 7, PreferredPlayerNumber no longer identifies the player slot, but Game.PlayerDestroyed
			// now records the real slot index in ReplayData.CivilizationDestroyed.DestroyedId.
			byte playerIndex = Game.PlayerNumber(this);
			bool atLeastOneCivBuddyAvailable = ReplayData.Count(x => x.DestroyedId == playerIndex) < 2;

			// CW: If atLeastOneCivBuddyAvailable is disabled, this may affect end screen and could
			// confuse BaseCivilization.Buddy.cs Algorithm.
			return playerIndex != 0 && !this.IsHuman && atLeastOneCivBuddyAvailable;
		}
	}
}