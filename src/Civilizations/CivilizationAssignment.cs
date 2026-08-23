using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Civilizations
{
	/// <summary>
	/// Deterministically assigns a starting civilization to each player index (0 = barbarians) for a new game.
	///
	/// For player indices 0-7, this reproduces the original "preferred player number" algorithm exactly, one
	/// draw per slot in increasing index order, so games created with the same seed and up to 7 non-barbarian
	/// players are unaffected by this class (bit-identical civilization assignment to the pre-existing code).
	///
	/// Player indices 8 and above (only reachable with more than 7 non-barbarian players) are assigned by
	/// drawing from a pool of all 14 non-barbarian civilizations, decoupled from the "preferred player number"
	/// slots. The pool starts with only the civilizations not already used in slots 1-7, guaranteeing that all
	/// 14 civilizations appear at least once before any of them repeats; once the pool is exhausted it refills
	/// with all 14 again, so civilizations are reused (with different leader/tribe names, see Game.NewGame.cs)
	/// once there are more non-barbarian players than civilizations.
	/// </summary>
	internal sealed class CivilizationAssignment
	{
		private const int ClassicCivilizationMaxId = 14;

		private readonly ICivilization[] _civilizationByIndex;

		private CivilizationAssignment(ICivilization[] civilizationByIndex)
		{
			_civilizationByIndex = civilizationByIndex;
		}

		/// <summary>
		/// The number of player slots covered by this assignment (competition + 1).
		/// </summary>
		public int Count => _civilizationByIndex.Length;

		/// <summary>
		/// Returns the starting civilization assigned to the given player index.
		/// </summary>
		public ICivilization this[int playerIndex] => _civilizationByIndex[playerIndex];

		/// <summary>
		/// Returns the "buddy" civilization for the given player index: the other civilization in the same
		/// buddy pair as the one currently assigned to that index (Id +/- 7), used when a civilization needs
		/// to be replaced (e.g. on respawn). See also <see cref="Player.Respawn"/>.
		/// </summary>
		public ICivilization Buddy(int playerIndex)
		{
			int buddyId = BuddyPair.BuddyId(this[playerIndex].Id);
			return Common.Civilizations.First(civ => civ.Id == buddyId);
		}

		/// <summary>
		/// Builds the civilization assignment for a new game.
		/// </summary>
		/// <param name="initialSeed">The random seed the game was created with.</param>
		/// <param name="competition">The number of non-barbarian player slots (the human player plus AI opponents).</param>
		/// <param name="humanPlayerIndex">The player index of the human player in the current game setup.</param>
		/// <param name="humanCivilization">The civilization chosen by the human player.</param>
		public static CivilizationAssignment Create(short initialSeed, int competition, byte humanPlayerIndex, ICivilization humanCivilization)
		{
			ArgumentNullException.ThrowIfNull(humanCivilization);

			// Read once: Common.Civilizations reflects over every loaded assembly and instantiates all
			// civilizations on each access, and this method would otherwise do that once per player slot.
			ICivilization[] allCivilizations = Common.Civilizations;

			ICivilization[] civilizationByIndex = new ICivilization[competition + 1];
			Random startRandom = new(initialSeed);

			int lowSlotLimit = Math.Min(competition, 7);
			for (int i = 0; i <= lowSlotLimit; i++)
			{
				if (i == humanPlayerIndex)
				{
					civilizationByIndex[i] = humanCivilization;
					continue;
				}

				ICivilization[] civs =
				[
					.. allCivilizations.Where(civ =>
						civ.PreferredPlayerNumber == i &&
						civ.Id <= ClassicCivilizationMaxId)
				];

				if (civs.Length == 0)
				{
					civs = [.. allCivilizations.Where(civ => civ.PreferredPlayerNumber == i)];
				}

				int r = startRandom.Next(civs.Length);
				civilizationByIndex[i] = civs[r];
			}

			if (competition > 7)
			{
				// PreferredPlayerNumber 0 identifies the barbarians, whose Id is 15 rather than 0, so they have
				// to be filtered out by that instead — otherwise they end up in the pool for a regular player.
				int[] selectableIds =
				[
					.. allCivilizations.Where(civ =>
						civ.PreferredPlayerNumber != 0 &&
						civ.Id <= ClassicCivilizationMaxId)
					.Select(civ => civ.Id)
				];
				HashSet<int> usedIds = [.. civilizationByIndex.Where(civ => civ != null).Select(civ => civ.Id)];
				List<int> reusePool = [.. selectableIds.Where(id => !usedIds.Contains(id))];

				for (int i = 8; i <= competition; i++)
				{
					if (reusePool.Count == 0)
					{
						reusePool.AddRange(selectableIds);
					}

					int poolIndex = startRandom.Next(reusePool.Count);
					int civId = reusePool[poolIndex];
					reusePool.RemoveAt(poolIndex);

					civilizationByIndex[i] = allCivilizations.First(civ => civ.Id == civId);
				}
			}

			return new CivilizationAssignment(civilizationByIndex);
		}
	}
}
