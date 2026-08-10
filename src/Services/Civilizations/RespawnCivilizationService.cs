using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Services.Random;

namespace CivOne.Services.Civilizations
{
	/// <summary>
	/// Picks the replacement civilization for a respawning player slot, preferring one that nobody plays.
	///
	/// The candidates are tried in this order:
	/// <list type="number">
	/// <item>the "buddy" civilization (Id +/- 7), if <c>preferBuddyCivilization</c> is set and it is free;</item>
	/// <item>otherwise a random civilization that no living player uses;</item>
	/// <item>otherwise a random one among the least used, which the caller disambiguates by name.</item>
	/// </list>
	/// The caller decides whether the buddy is preferred: <see cref="Player.Respawn"/> does so for the classic
	/// setup of at most seven non-barbarian players, unless the game disables it.
	/// Note that this differs from the original game even in that classic setup: the original always handed
	/// out the buddy civilization, so two players could end up playing the same one. Here a taken buddy falls
	/// through to a free civilization instead.
	/// </summary>
	internal sealed class RespawnCivilizationService : IRespawnCivilizationService
	{
		/// <summary>
		/// Distance between the two civilizations of a buddy pair (Romans 1 and Russians 8, and so on).
		/// </summary>
		private const int BuddyIdOffset = 7;

		private readonly ICivilization[]? _civilizations;
		private readonly IRandomService? _randomService;

		private ICivilization[] Civilizations => _civilizations ?? Common.Civilizations;
		private IRandomService RandomService => _randomService ?? RandomServiceFactory.Create();

		/// <summary>
		/// Creates the service.
		/// </summary>
		/// <param name="civilizations">All selectable civilizations. Defaults to the ones known to the game.</param>
		/// <param name="randomService">The random source, so respawns stay reproducible for a given game seed.</param>
		public RespawnCivilizationService(ICivilization[]? civilizations = null, IRandomService? randomService = null)
		{
			_civilizations = civilizations;
			_randomService = randomService;
		}

		public RespawnCivilizationResult SelectReplacement(
			ICivilization destroyed,
			IReadOnlyCollection<ICivilization> civilizationsInUse,
			bool preferBuddyCivilization)
		{
			ArgumentNullException.ThrowIfNull(destroyed);
			ArgumentNullException.ThrowIfNull(civilizationsInUse);

			// PreferredPlayerNumber 0 identifies the barbarians, who are never handed to a regular player.
			ICivilization[] candidates = [.. Civilizations.Where(civ => civ.PreferredPlayerNumber != 0 && civ.Id != destroyed.Id)];
			if (candidates.Length == 0)
			{
				return new RespawnCivilizationResult { Civilization = destroyed, Occurrence = 0 };
			}

			Dictionary<int, int> usageById = [];
			foreach (ICivilization civilization in civilizationsInUse)
			{
				usageById[civilization.Id] = usageById.TryGetValue(civilization.Id, out int count) ? count + 1 : 1;
			}

			ICivilization? buddy = candidates.FirstOrDefault(civ => civ.Id == BuddyId(destroyed.Id));
			if (preferBuddyCivilization && buddy != null && !usageById.ContainsKey(buddy.Id))
			{
				return new RespawnCivilizationResult { Civilization = buddy, Occurrence = 0 };
			}

			ICivilization[] unused = [.. candidates.Where(civ => !usageById.ContainsKey(civ.Id))];
			if (unused.Length > 0)
			{
				return new RespawnCivilizationResult { Civilization = Pick(unused), Occurrence = 0 };
			}

			// Every civilization is taken (more living players than civilizations): share the least used one.
			int fewestUses = candidates.Min(civ => usageById[civ.Id]);
			ICivilization[] leastUsed = [.. candidates.Where(civ => usageById[civ.Id] == fewestUses)];
			return new RespawnCivilizationResult { Civilization = Pick(leastUsed), Occurrence = fewestUses };
		}

		/// <summary>
		/// Returns the Id of the other civilization in the same buddy pair.
		/// </summary>
		/// <param name="civilizationId">The Id to find the buddy for.</param>
		/// <returns>The buddy's Id.</returns>
		private static int BuddyId(int civilizationId)
			=> civilizationId > BuddyIdOffset ? civilizationId - BuddyIdOffset : civilizationId + BuddyIdOffset;

		private ICivilization Pick(ICivilization[] candidates)
			=> candidates.Length == 1 ? candidates[0] : candidates[RandomService.NextInt(candidates.Length)];
	}
}
