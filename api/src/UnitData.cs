// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne
{
	public struct UnitData
	{
		public byte Id { get; set; }
		public byte Status { get; set; }
		public byte X { get; set; }
        public byte Y { get; set; }
		public byte TypeId { get; set; }
		public byte RemainingMoves { get; set; }
		/// Remaining fuel (flight unit), Task progress (settlers). Others, always 0.
        public byte SpecialMoves { get; set; }
		public byte GotoX { get; set; } 
        public byte GotoY { get; set; }
		public byte Visibility { get; set; }
		public byte NextUnitId { get; set; }
		public byte HomeCityId { get; set; }
	}
}