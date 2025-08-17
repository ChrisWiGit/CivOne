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
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Wonders;

using Gov = CivOne.Governments;

namespace CivOne
{
	public partial class Player : BaseInstance, ITurn
	{
		public Player Respawn()
		{
			Debug.Assert(IsHuman == false, "Cannot respawn a human player!");
			Debug.Assert(Civilization.Id != 0, "Cannot respawn barbarian player!");

			var destroyed = this.Civilization;

			var civId = destroyed.Id >= 8 ? destroyed.Id - 7 : destroyed.Id + 7;

			ICivilization[] civs = [.. Common.Civilizations.Where(civ => civ.Id == civId)];

			int playerIndex = destroyed.PreferredPlayerNumber;

			return new Player(civs.First());
			// CW: schism could be done the same but with a different civ slot
			// But. Make sure the civ is not used.
		}

		public bool AllowedToRespawn(ReplayData.CivilizationDestroyed[] ReplayData)
		{
			bool atLeastOneCivBuddyAvailable = ReplayData.Count(x => x.DestroyedId == this.Civilization.PreferredPlayerNumber) < 2;

			// CW: If atLeastOneCivBuddyAvailable is disabled, this may affect end screen and could
			// confuse BaseCivilization.Buddy.cs Algorithm.
			return this.Civilization.PreferredPlayerNumber != 0 && !this.IsHuman && atLeastOneCivBuddyAvailable;
		}
	}
}