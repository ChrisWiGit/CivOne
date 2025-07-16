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
using CivOne.Tiles;

namespace CivOne.Units
{
	internal class Fighter : BaseUnitAir
	{
		public override void Explore()
		{
			Explore(2);
		}
		
		protected override bool CanAttackEnemy(ITile moveTarget)
		{
			// Only Fighter can attack air units and all other units.
			return true;
		}

		public override void SkipTurn()
		{
			FuelLeft = 0;

			base.SkipTurn();
		}

		public Fighter() : base(6, 4, 2, 10)
		{
			Type = UnitType.Fighter;
			Name = "Fighter";
			RequiredTech = new Flight();
			ObsoleteTech = null;
			SetIcon('A', 1, 1);
            Role = UnitRole.AirAttack;
        }
	}
}