// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CivOne
{
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "These types are only used for replay data and are closely related to the ReplayData class, so it makes sense to nest them.")]
	public abstract class ReplayData
	{
		public class CityBuilt : ReplayData
		{
			public byte OwnerId { get; private set; }
			public int CityId { get; private set; }
			public int CityNameId { get; private set; }
			public int X { get; private set; }
			public int Y { get; private set; }

			public CityBuilt(int turn, byte ownerId, int cityId, int cityNameId, int x, int y) : base(turn)
			{
				OwnerId = ownerId;
				CityId = cityId;
				CityNameId = cityNameId;
				X = x;
				Y = y;
			}
		}

		public class CityDestroyed : ReplayData
		{
			public int CityId { get; private set; }
			public int CityNameId { get; private set; }
			public int X { get; private set; }
			public int Y { get; private set; }

			public CityDestroyed(int turn, int cityId, int cityNameId, int x, int y) : base(turn)
			{
				CityId = cityId;
				CityNameId = cityNameId;
				X = x;
				Y = y;
			}
		}

		public class CivilizationDestroyed : ReplayData
		{
			public int DestroyedId { get; private set; }
			public int DestroyedById { get; private set; }

			public CivilizationDestroyed(int turn, byte destroyedId, byte destroyedById) : base(turn)
			{
				Debug.Assert(destroyedId <= MaxPlayerIndex, "Invalid player index in replay data.");
				Debug.Assert(destroyedById <= MaxPlayerIndex, "Invalid player index in replay data.");

				DestroyedId = destroyedId;
				DestroyedById = destroyedById;
			}
		}

		/// <summary>
		/// A destroyed player slot was taken over by a new civilization.
		/// Recorded so screens that look back at the game (e.g. the conquest screen) know which civilization
		/// occupied a slot at any point in time, instead of deriving it from the initial random seed.
		/// </summary>
		public class CivilizationRespawned : ReplayData
		{
			/// <summary>
			/// The player slot that respawned. Matches <see cref="CivilizationDestroyed.DestroyedId"/>.
			/// </summary>
			public int PlayerId { get; private set; }

			/// <summary>
			/// The Id of the civilization that now occupies the slot.
			/// </summary>
			public int CivilizationId { get; private set; }

			/// <summary>
			/// Creates a respawn entry.
			/// </summary>
			/// <param name="turn">The game turn the respawn happened on.</param>
			/// <param name="playerId">The player slot that respawned.</param>
			/// <param name="civilizationId">The Id of the civilization taking over the slot.</param>
			public CivilizationRespawned(int turn, byte playerId, byte civilizationId) : base(turn)
			{
				Debug.Assert(playerId <= MaxPlayerIndex, "Invalid player index in replay data.");
				Debug.Assert(civilizationId <= 15, "Invalid civilization ID in replay data.");

				PlayerId = playerId;
				CivilizationId = civilizationId;
			}
		}

		// Mirrors CivOne.Game.MaxPlayers - 1 (this project cannot reference the Game class in the main assembly).
		private const int MaxPlayerIndex = 31;

		public int Turn { get; private set; }

		protected ReplayData(int turn)
		{
			Turn = turn;
		}
	}
}