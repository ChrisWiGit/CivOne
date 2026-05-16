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
using CivOne.Services.Random;

namespace CivOne.Screens.Reports
{
	internal readonly record struct LeaderOrderEntry(string Name, int RatingThreshold);

	internal readonly record struct LeaderOrderResult(int RatingPercent, int SelectedLeaderIndex, string SelectedLeaderName, IReadOnlyList<string> OrderedLeaderNames);

	internal sealed class LeaderOrderDelegate
	{
		private readonly IRandomService _randomService;

		public LeaderOrderDelegate(IRandomService randomService = null)
		{
			_randomService = randomService ?? RandomServiceFactory.Create();
		}

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
				new LeaderOrderEntry("Sulayman the Magnificent", 200),
				new LeaderOrderEntry("Winston Churchill", 184),
				new LeaderOrderEntry("Cleopatra", 167),
				new LeaderOrderEntry("Charles De Gaulle", 150),
				new LeaderOrderEntry("Vladimir Lenin", 134),
				new LeaderOrderEntry("Otto von Bismarck", 117),
				new LeaderOrderEntry("Eric the Red", 100),
				new LeaderOrderEntry("Kaiser Wilhelm", 84),
				new LeaderOrderEntry("Louis the XVI", 67),
				new LeaderOrderEntry("Neville Chamberlain", 50),
				new LeaderOrderEntry("Ferdinand Marcos", 34),
				new LeaderOrderEntry("Emperor Nero", 17),
				new LeaderOrderEntry("Dan Quayle", 0)
			];
		}

		private int GetSelectedLeaderIndex(int ratingPercent, IReadOnlyList<LeaderOrderEntry> orderedEntries)
		{
			for (int i = 0; i < orderedEntries.Count; i++)
			{
				if (ratingPercent >= orderedEntries[i].RatingThreshold)
				{
					return i;
				}
			}

			return orderedEntries.Count - 1;
		}
	}
}