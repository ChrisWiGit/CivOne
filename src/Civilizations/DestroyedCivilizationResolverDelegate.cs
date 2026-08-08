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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CivOne.Civilizations
{
	/// <summary>
	/// The civilization that held a player slot, together with the number of other players that used the same
	/// civilization when the slot was filled.
	///
	/// The occurrence is what <see cref="CivilizationNameDelegate"/> turns into the Roman numeral that keeps
	/// shared civilizations apart ("Roman II"), so a screen can rebuild the name the player was shown.
	/// </summary>
	/// <param name="Civilization">The civilization on the slot.</param>
	/// <param name="Occurrence">0 when the civilization was used by this player only.</param>
	internal readonly record struct DestroyedCivilization(ICivilization Civilization, int Occurrence);

	/// <summary>
	/// Works out which civilization occupied a player slot at the moment it was destroyed, for screens that
	/// look back over a finished game.
	///
	/// Games record a <see cref="ReplayData.CivilizationRespawned"/> entry whenever a slot is taken over, so
	/// the answer is read from the replay by walking it in order. Saves written before those entries existed
	/// fall back to <see cref="BaseCivilization.GetBuddyCivilizationSupplier"/>, which reconstructs the same
	/// information from the initial random seed — correct as long as respawns only ever flipped between the
	/// two civilizations of a buddy pair, which is what those games did.
	/// </summary>
	internal sealed class DestroyedCivilizationResolverDelegate
	{
		/// <summary>
		/// Maps every <see cref="ReplayData.CivilizationDestroyed"/> entry to the civilization that was
		/// destroyed by it.
		/// </summary>
		/// <param name="replayData">The complete replay, in the order the events happened.</param>
		/// <param name="initialSeed">The seed the game was created with.</param>
		/// <param name="competition">The number of non-barbarian player slots.</param>
		/// <param name="humanPlayerIndex">The player slot of the human player.</param>
		/// <param name="humanCivilization">The civilization the human player started with.</param>
		/// <returns>One entry per destruction, keyed by the replay entry itself.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public IReadOnlyDictionary<ReplayData.CivilizationDestroyed, DestroyedCivilization> Resolve(
			IReadOnlyList<ReplayData> replayData,
			short initialSeed,
			int competition,
			byte humanPlayerIndex,
			ICivilization humanCivilization)
		{
			ArgumentNullException.ThrowIfNull(replayData);
			ArgumentNullException.ThrowIfNull(humanCivilization);

			return replayData.OfType<ReplayData.CivilizationRespawned>().Any()
				? ResolveFromRespawnHistory(replayData, initialSeed, competition, humanPlayerIndex, humanCivilization)
				: ResolveFromBuddySupplier(replayData, initialSeed, competition, humanPlayerIndex, humanCivilization);
		}

		private static Dictionary<ReplayData.CivilizationDestroyed, DestroyedCivilization> ResolveFromRespawnHistory(
			IReadOnlyList<ReplayData> replayData,
			short initialSeed,
			int competition,
			byte humanPlayerIndex,
			ICivilization humanCivilization)
		{
			CivilizationAssignment assignment = CivilizationAssignment.Create(initialSeed, competition, humanPlayerIndex, humanCivilization);

			Dictionary<int, DestroyedCivilization> currentBySlot = InitialSlots(assignment);
			HashSet<int> aliveSlots = [.. currentBySlot.Keys];

			// The respawn entry for a slot is always written after the destruction that caused it, so reading
			// the slot's current civilization when the destruction is reached gives the one that died.
			Dictionary<ReplayData.CivilizationDestroyed, DestroyedCivilization> result = [];
			foreach (ReplayData entry in replayData)
			{
				switch (entry)
				{
					case ReplayData.CivilizationDestroyed destroyed
						when currentBySlot.TryGetValue(destroyed.DestroyedId, out DestroyedCivilization civilization):
						result[destroyed] = civilization;
						aliveSlots.Remove(destroyed.DestroyedId);
						break;

					case ReplayData.CivilizationRespawned respawned:
						ICivilization? replacement = Common.Civilizations.FirstOrDefault(civ => civ.Id == respawned.CivilizationId);
						if (replacement != null)
						{
							// Player.Respawn names the new player after how many living players already use the
							// civilization it is given, so counting them here reproduces that name.
							int occurrence = aliveSlots.Count(slot =>
								slot != respawned.PlayerId && currentBySlot[slot].Civilization.Id == replacement.Id);
							currentBySlot[respawned.PlayerId] = new DestroyedCivilization(replacement, occurrence);
							aliveSlots.Add(respawned.PlayerId);
						}
						break;
				}
			}

			return result;
		}

		/// <summary>
		/// Builds the starting civilization of every player slot, numbered the same way
		/// <see cref="Game.NewGame"/> numbers them: a slot counts how many lower slots already took its
		/// civilization.
		/// </summary>
		/// <param name="assignment">The civilization assignment the game was created with.</param>
		/// <returns>One entry per player slot.</returns>
		private static Dictionary<int, DestroyedCivilization> InitialSlots(CivilizationAssignment assignment)
		{
			Dictionary<int, DestroyedCivilization> slots = [];
			Dictionary<int, int> occurrenceByCivilizationId = [];
			for (int slot = 0; slot < assignment.Count; slot++)
			{
				ICivilization civilization = assignment[slot];
				int occurrence = occurrenceByCivilizationId.TryGetValue(civilization.Id, out int count) ? count : 0;
				occurrenceByCivilizationId[civilization.Id] = occurrence + 1;
				slots[slot] = new DestroyedCivilization(civilization, occurrence);
			}
			return slots;
		}

		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]

		private Dictionary<ReplayData.CivilizationDestroyed, DestroyedCivilization> ResolveFromBuddySupplier(
			IReadOnlyList<ReplayData> replayData,
			short initialSeed,
			int competition,
			byte humanPlayerIndex,
			ICivilization humanCivilization)
		{
			BaseCivilization.BuddyCivilization getBuddyCivilization =
				BaseCivilization.GetBuddyCivilizationSupplier(initialSeed, competition, humanPlayerIndex, humanCivilization);
			CivilizationAssignment assignment = CivilizationAssignment.Create(initialSeed, competition, humanPlayerIndex, humanCivilization);
			Dictionary<int, DestroyedCivilization> initialSlots = InitialSlots(assignment);

			Dictionary<ReplayData.CivilizationDestroyed, DestroyedCivilization> result = [];
			foreach (ReplayData.CivilizationDestroyed destroyed in replayData.OfType<ReplayData.CivilizationDestroyed>())
			{
				ICivilization civilization = getBuddyCivilization(destroyed.DestroyedId);

				// Only the civilization a slot started with can be numbered from the assignment; the buddy
				// civilizations handed out later are not recorded in these saves, so they stay unnumbered.
				int occurrence =
					initialSlots.TryGetValue(destroyed.DestroyedId, out DestroyedCivilization initial) && initial.Civilization.Id == civilization.Id
						? initial.Occurrence
						: 0;
				result[destroyed] = new DestroyedCivilization(civilization, occurrence);
			}

			return result;
		}
	}
}
