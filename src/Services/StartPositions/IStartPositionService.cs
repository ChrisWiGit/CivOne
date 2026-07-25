// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Finds valid starting positions for a batch of civilizations. Implementations only find coordinates;
	/// they never create units or mutate the map. The caller is responsible for placing the actual units.
	/// </summary>
	public interface IStartPositionService
	{
		/// <summary>
		/// Finds a starting position for every candidate in a single batch, so algorithms that depend on the
		/// total number of candidates (e.g. dividing the map into equally sized areas) can do so consistently.
		/// </summary>
		/// <param name="candidates">The civilizations that need a starting position.</param>
		/// <param name="context">Shared map and game state needed to evaluate candidate tiles.</param>
		/// <returns>Results in the same order as <paramref name="candidates"/>.</returns>
		IReadOnlyList<StartPositionResult> FindStartPositions(IReadOnlyList<StartPositionCandidate> candidates, StartPositionContext context);
	}
}
