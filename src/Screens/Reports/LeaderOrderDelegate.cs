// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Linq;
using CivOne.Services;

namespace CivOne.Screens.Reports
{
		/// <summary>
		/// Represents a single leader and the rating threshold required to select them.
		/// </summary>
		/// <param name="Name">Localized leader display name.</param>
		/// <param name="RatingThreshold">Minimum civilization rating required for selection.</param>
		internal readonly record struct LeaderOrderEntry(string Name, int RatingThreshold);

		/// <summary>
		/// Result object returned by <see cref="LeaderOrderDelegate"/> calculations.
		/// </summary>
		/// <param name="RatingPercent">Computed rating percent for the civilization.</param>
		/// <param name="SelectedLeaderIndex">Index of the selected leader in the ordered list.</param>
		/// <param name="SelectedLeaderName">Name of the selected leader.</param>
		/// <param name="OrderedLeaderNames">Ordered list of leader names used for selection.</param>
		internal readonly record struct LeaderOrderResult(int RatingPercent, int SelectedLeaderIndex, string SelectedLeaderName, IReadOnlyList<string> OrderedLeaderNames);

		/// <summary>
		/// Determines the ordered list of historical leaders and maps a civilization rating
		/// percentage to the corresponding leader.
		/// </summary>
		/// <remarks>
		/// Use <see cref="Calculate(int)"/> to obtain a <see cref="LeaderOrderResult"/> for a rating percent.
		/// </remarks>
		internal sealed class LeaderOrderDelegate(ITranslationService? translationService = null)
	{
		private readonly ITranslationService _translationService = translationService ?? TranslationServiceFactory.CreateDefault();

		public LeaderOrderResult Calculate(int ratingPercent)
		{
			IReadOnlyList<LeaderOrderEntry> orderedEntries = GetLeaderOrder();
			int selectedLeaderIndex = GetSelectedLeaderIndex(ratingPercent, orderedEntries);
			string selectedLeaderName = orderedEntries[selectedLeaderIndex].Name;
			IReadOnlyList<string> orderedLeaderNames = [.. orderedEntries.Select(entry => entry.Name)];

			return new LeaderOrderResult(ratingPercent, selectedLeaderIndex, selectedLeaderName, orderedLeaderNames);
		}

		public IReadOnlyList<LeaderOrderEntry> GetLeaderOrder()
		{
			return
			[
				// source: https://civilization.fandom.com/wiki/Hall_of_Fame_(Civ1)
				new LeaderOrderEntry(_translationService.Translate("Solomon the Wise"), 137),
				new LeaderOrderEntry(_translationService.Translate("Emperor Augustus"), 129),
				new LeaderOrderEntry(_translationService.Translate("King Charlemagne"), 111),
				new LeaderOrderEntry(_translationService.Translate("Thomas Jefferson"), 100),
				new LeaderOrderEntry(_translationService.Translate("Shogun Tokugawa"), 88),
				new LeaderOrderEntry(_translationService.Translate("Franklin Delano Roosevelt"), 82),
				new LeaderOrderEntry(_translationService.Translate("Sulayman the Magnificent"), 66),
				new LeaderOrderEntry(_translationService.Translate("Winston Churchill"), 59),
				new LeaderOrderEntry(_translationService.Translate("Cleopatra"), 49),
				new LeaderOrderEntry(_translationService.Translate("Charles De Gaulle"), 41),
				new LeaderOrderEntry(_translationService.Translate("Vladimir Lenin"), 36),
				new LeaderOrderEntry(_translationService.Translate("Otto von Bismarck"), 23),
				new LeaderOrderEntry(_translationService.Translate("Eric the Red"), 14),
				new LeaderOrderEntry(_translationService.Translate("Kaiser Wilhelm"), 13),
				new LeaderOrderEntry(_translationService.Translate("Louis the XVI"), 10),
				new LeaderOrderEntry(_translationService.Translate("Neville Chamberlain"), 6),
				new LeaderOrderEntry(_translationService.Translate("Ferdinand Marcos"), 4),
				new LeaderOrderEntry(_translationService.Translate("Emperor Nero"), 2),
				new LeaderOrderEntry(_translationService.Translate("Dan Quayle"), 1)
			];
		}

		private static int GetSelectedLeaderIndex(int ratingPercent, IReadOnlyList<LeaderOrderEntry> orderedEntries)
		{
			for (int i = 0; i < orderedEntries.Count; i++)
			{
				if (ratingPercent >= orderedEntries[i].RatingThreshold)
				{
					return i;
				}
			}

			// Fallback to the last leader (Dan Quayle) if the rating percent is below all thresholds.
			return orderedEntries.Count - 1;
		}
	}
}