// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;

namespace CivOne.Civilizations
{
	public abstract partial class BaseCivilization : BaseInstance
	{
		public delegate ICivilization BuddyCivilization(int playerIndex);

		/// <summary>
		/// Returns a function that can be used to get the civilization for a given player index, for
		/// reconstructing (from the initial random seed) which civilization occupied a player slot over the
		/// course of a game, including after respawns.
		///
		/// The first call for a given player index returns that slot's starting civilization (see
		/// <see cref="CivilizationAssignment"/>). Each subsequent call for the same index alternates to that
		/// civilization's buddy (Id +/- 7) and back, mirroring the flip performed by <see cref="Player.Respawn"/>
		/// every time a slot's civilization is destroyed and replaced.
		/// </summary>
		public static BuddyCivilization GetBuddyCivilizationSupplier(short initialSeed, int competitorsCount, byte humanPlayerIndex, ICivilization humanCivilization)
		{
			CivilizationAssignment assignment = CivilizationAssignment.Create(initialSeed, competitorsCount, humanPlayerIndex, humanCivilization);
			Dictionary<int, bool> flipped = [];

			return playerIndex =>
			{
				bool isFlipped = flipped.TryGetValue(playerIndex, out bool value) && value;
				ICivilization result = isFlipped ? assignment.Buddy(playerIndex) : assignment[playerIndex];
				flipped[playerIndex] = !isFlipped;
				return result;
			};
		}
	}
}
