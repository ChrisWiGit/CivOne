using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Services.Score
{
	/// <summary>
	/// Decides whether the human player has conquered the world, for the world-conquest score bonus.
	/// Once the condition holds it stays true, as in the original game.
	/// </summary>
	/// <remarks>
	/// The latch is runtime state only and is not saved, like the original's RAM flag.
	/// After loading a game it is recomputed until the condition holds again.
	/// See <c>docs/plan-civilizationScore.impl.md</c>, §10.
	/// </remarks>
	internal sealed class WorldConquestDelegate
	{
		private bool _latched;

		/// <summary>
		/// Returns <c>true</c> once the human player has been the only civilization left besides the barbarians.
		/// </summary>
		/// <param name="civilizations">All civilization slots of the game, barbarians included.</param>
		/// <returns><c>true</c> if the condition holds now or held on an earlier call.</returns>
		public bool IsConquered(IEnumerable<IConquestParticipant> civilizations)
		{
			ArgumentNullException.ThrowIfNull(civilizations);

			if (!_latched)
			{
				_latched = OnlyHumanRemains(civilizations);
			}

			return _latched;
		}

		private static bool OnlyHumanRemains(IEnumerable<IConquestParticipant> civilizations)
		{
			bool humanAlive = false;
			foreach (IConquestParticipant civilization in civilizations.Where(c => !c.IsBarbarian && !c.IsDestroyed))
			{
				if (!civilization.IsHuman)
				{
					return false;
				}

				humanAlive = true;
			}

			return humanAlive;
		}
	}
}
