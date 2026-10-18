using System;
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
		/// Holds the Famous Persons of the original game and maps a civilization rating to one of them.
		/// </summary>
		/// <remarks>
		/// For the person at ascending position <c>i</c> (1-based), the original selects it when <c>(i * i) / 3 &lt; rating</c>
		/// and keeps the highest match.
		/// A rating of 0 or less selects nobody, and the original shows no window then.
		/// See <c>docs/plan-civilizationScore.impl.md</c>, §15.1.
		/// </remarks>
		internal sealed class LeaderOrderDelegate(ITranslationService? translationService = null)
	{
		private readonly ITranslationService _translationService = translationService ?? TranslationServiceFactory.CreateDefault();

		/// <summary>
		/// Selects the Famous Person a rating exceeds.
		/// </summary>
		/// <param name="ratingPercent">The civilization rating.</param>
		/// <returns>The selected person, or <c>null</c> if the rating exceeds nobody.</returns>
		public LeaderOrderResult? Calculate(int ratingPercent)
		{
			IReadOnlyList<LeaderOrderEntry> orderedEntries = GetLeaderOrder();
			for (int i = 0; i < orderedEntries.Count; i++)
			{
				if (ratingPercent >= orderedEntries[i].RatingThreshold)
				{
					return CreateResult(ratingPercent, i, orderedEntries);
				}
			}

			return null;
		}

		/// <summary>
		/// Selects a Famous Person by its ascending position, for the original's fallback when the score is 0.
		/// </summary>
		/// <param name="ascendingIndex">0 for the lowest person (Dan Quayle); values above 19 are capped.</param>
		/// <param name="ratingPercent">The rating to carry in the result.</param>
		/// <returns>The selected person, or <c>null</c> if <paramref name="ascendingIndex"/> is negative.</returns>
		public LeaderOrderResult? SelectByRank(int ascendingIndex, int ratingPercent)
		{
			if (ascendingIndex < 0)
			{
				return null;
			}

			IReadOnlyList<LeaderOrderEntry> orderedEntries = GetLeaderOrder();
			int capped = Math.Min(ascendingIndex, orderedEntries.Count - 1);
			return CreateResult(ratingPercent, orderedEntries.Count - 1 - capped, orderedEntries);
		}

		/// <summary>
		/// Gets the Famous Persons, highest first, each with the lowest rating that selects it.
		/// </summary>
		/// <returns>20 entries, from Solomon the Wise down to Dan Quayle.</returns>
		public IReadOnlyList<LeaderOrderEntry> GetLeaderOrder()
		{
			string[] ascending =
			[
				_translationService.Translate("Dan Quayle"),
				_translationService.Translate("Emperor Nero"),
				_translationService.Translate("Ferdinand Marcos"),
				_translationService.Translate("Neville Chamberlain"),
				_translationService.Translate("Louis the XVI"),
				_translationService.Translate("Kaiser Wilhelm"),
				_translationService.Translate("Eric the Red"),
				_translationService.Translate("Otto von Bismarck"),
				_translationService.Translate("Simon Bolivar"),
				_translationService.Translate("Vladimir Lenin"),
				_translationService.Translate("Charles De Gaulle"),
				_translationService.Translate("Cleopatra"),
				_translationService.Translate("Winston Churchill"),
				_translationService.Translate("Suleyman the Magnificent"),
				_translationService.Translate("Franklin Roosevelt"),
				_translationService.Translate("Shogun Tokugawa"),
				_translationService.Translate("Thomas Jefferson"),
				_translationService.Translate("King Charlemagne"),
				_translationService.Translate("Emperor Augustus"),
				_translationService.Translate("Solomon the Wise"),
			];

			// (i * i) / 3 < rating  <=>  rating >= (i * i) / 3 + 1, with i the 1-based ascending position.
			return [.. ascending.Select((name, index) => new LeaderOrderEntry(name, ((index + 1) * (index + 1) / 3) + 1)).Reverse()];
		}

		private static LeaderOrderResult CreateResult(int ratingPercent, int selectedIndex, IReadOnlyList<LeaderOrderEntry> orderedEntries)
		{
			IReadOnlyList<string> orderedLeaderNames = [.. orderedEntries.Select(entry => entry.Name)];
			return new LeaderOrderResult(ratingPercent, selectedIndex, orderedEntries[selectedIndex].Name, orderedLeaderNames);
		}
	}
}
