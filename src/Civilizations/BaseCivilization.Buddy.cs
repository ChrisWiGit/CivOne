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
using CivOne.Enums;
using CivOne.Leaders;

namespace CivOne.Civilizations
{
	public abstract partial class BaseCivilization : BaseInstance
	{
		public delegate ICivilization BuddyCivilization(int preferredPlayerNumber);

		/// <summary>
		/// Returns a function that can be used to get a buddy civilization for a given player
		/// number. 
		/// As in Civilization.cs, civilizations are grouped by their preferred player id (1-7, 0 are barbarians, and one of seven is the human itself).
		/// Some (most) of civilizations have a buddy civilization (e.g. Babylonians and Zulus = 2)
		/// This method returns a delegate that returns a buddy civilization for a given player number (1-7).
		/// If you start a game with a specific seed, the buddy civilizations will be chosen randomly, but consistently for that seed.
		/// So you apply the InitialSeed to this method to get the same buddy civilizations every time.
		/// E.g.
		/// GetBuddyCivilizationSupplier(Common.Random.InitialSeed)(2) returns Babylonians
		/// GetBuddyCivilizationSupplier(Common.Random.InitialSeed)(2) returns Zulus
		/// or the other way around, depending on the InitialSeed.
		public static BuddyCivilization GetBuddyCivilizationSupplier(short InitialSeed, int competitorsCount, byte preferredPlayerNumber)
		{
			// CW: We cannot get the random situation from the original game.
			// This only works because Game.NewGame.cs sets the InitialSeed at the beginning of the creation of the civs.
			Random startRandom = new(InitialSeed);
			Dictionary<int, int> buddyCivIndexMap = GetStartCivMapping(competitorsCount, preferredPlayerNumber, startRandom);

			return preferredPlayerNumber =>
			{
				ICivilization[] civBuds = Common.Civilizations.Where(civ => civ.PreferredPlayerNumber == preferredPlayerNumber).ToArray();

				Debug.Assert(civBuds.Length > 0, $"No buddy civilization found for player number {preferredPlayerNumber}!");

				var result = civBuds[buddyCivIndexMap[preferredPlayerNumber]];

				buddyCivIndexMap[preferredPlayerNumber] = buddyCivIndexMap[preferredPlayerNumber] == 0 ? 1 : 0;

				return result;
			};
		}

		private static Dictionary<int, int> GetStartCivMapping(int competitorsCount, byte preferredPlayerNumber, Random startRandom)
		{
			Dictionary<int, int> civBuddyIndex = [];
			List<int> range = [.. Enumerable.Range(0, competitorsCount + 1).Where(x => x != preferredPlayerNumber)];

			foreach (int i in range)
			{
				ICivilization[] civs = Common.Civilizations.Where(civ => civ.PreferredPlayerNumber == i).ToArray();

				int r = startRandom.Next(civs.Length);
				civBuddyIndex[i] = r;
			}

			return civBuddyIndex;
		}
	}
}