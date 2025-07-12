// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tiles
{
	public interface ITile : ICivilopedia
	{
		int X { get; }
		int Y { get; }
		Terrain Type { get; }
		bool Special { get; }
		byte ContinentId { get; set; }
		byte LandValue { get; set; }
		byte LandScore { get; }
		byte Movement { get; }
		byte Defense { get; }
		sbyte Food { get; }
		sbyte Shield { get; }
		sbyte Trade { get; }	
		sbyte IrrigationFoodBonus { get; }
		/// <summary>
		/// Number of turns required to irrigate this terrain
		/// </summary>
		byte IrrigationCost { get; }
		sbyte MiningShieldBonus { get; }
		/// <summary>
		/// Number of turns required to build mines on this terrain
		/// </summary>
		byte MiningCost { get; }
		byte Borders { get; }
		bool Road { get; set; }
		/// <summary>
		/// Number of turns required to build a road on this terrain
		/// </summary>
		byte RoadCost { get; }
		bool RailRoad { get; set; }
		/// <summary>
		/// Number of turns required to build a railroad on terrain with road
		/// </summary>
		byte RailRoadCost { get; }
		bool Irrigation { get; set; }
		bool Fortress { get; set; }
		byte FortressCost { get; set; }
		bool Mine { get; set; }
		bool Hut { get; set; }
		byte Visited { get; }
		void Visit(byte owner);
		bool IsOcean { get; }
		City City { get; }
        bool HasCity { get; }

        IUnit[] Units { get; }
		ITile this[int relativeX, int relativeY] { get; }
	}
}