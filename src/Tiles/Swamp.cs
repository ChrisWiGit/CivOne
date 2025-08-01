// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;

namespace CivOne.Tiles
{
	internal class Swamp : BaseTile
	{
		public override byte Movement => 2;
		public override byte Defense => 3;
		public override sbyte Food => 1;
		public override sbyte Shield => (sbyte)(Special ? 4 : 0);
		public override sbyte Trade => 0;
		public override sbyte IrrigationFoodBonus => 10;
		public override byte IrrigationCost => 15;
		public override sbyte MiningShieldBonus => 2;
		public override byte MiningCost => 15;
		public override byte RoadCost => 4;
		public override byte RailRoadCost => 4;
		public override byte FortressCost => 6;

		public Swamp(int x, int y, bool special) : base(x, y, special)
		{
			Type = Terrain.Swamp;
			Name = "Swamp";
		}
		public Swamp() : this(-1, -1, false)
		{
		}
	}
}