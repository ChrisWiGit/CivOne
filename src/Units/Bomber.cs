// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Advances;
using CivOne.Enums;

namespace CivOne.Units
{
	internal class Bomber : BaseUnitAir
	{
		private static readonly byte MAX_MOVES = 8;
		public override void Explore()
		{
			Explore(2);
		}

		public override void SkipTurn()
		{
			MovesLeft = 0;

			if (CanLandOnCarrier())
			{
				FuelLeft = TotalFuel;
				return;
			}

			if (FuelLeft > MAX_MOVES)
			{
				// second turn allowed.
				FuelLeft = MAX_MOVES;
				return;
			}
			FuelLeft = 0;

			base.SkipTurn();
		}

		public override void NewTurn()
		{
			base.NewTurn();
			TryToLandOnCarrier(MAX_MOVES);
		}

		public Bomber() : base(12, 12, 1, MAX_MOVES)
		{
			Type = UnitType.Bomber;
			Name = "Bomber";
			RequiredTech = new AdvancedFlight();
			ObsoleteTech = null;
			SetIcon('A', 1, 2);

			TotalFuel *= 2;
			FuelLeft = TotalFuel;
			MovesLeft = MAX_MOVES;
		}
	}
}