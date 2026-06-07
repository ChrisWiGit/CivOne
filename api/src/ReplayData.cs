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
using System.Reflection;

namespace CivOne
{
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
				Debug.Assert(destroyedId >= 0 && destroyedById <= 7, "Invalid civilization ID in replay data.");
				Debug.Assert(destroyedById >= 0 && destroyedById <= 7, "Invalid civilization ID in replay data.");

				DestroyedId = destroyedId;
				DestroyedById = destroyedById;
			}
		}

		public int Turn { get; private set; }

		protected ReplayData(int turn)
		{
			Turn = turn;
		}
	}
}