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
	public struct CityData
	{
		public byte Id;
		public byte NameId;
		public byte Status;
		public byte[] Buildings;
		public byte X, Y;

		/// <summary>
		/// The reals size of the city as not always visible to the player.
		/// </summary>
		public byte ActualSize;

		/// <summary>
		/// The size of the city as seen by the player.
		/// </summary>
		public byte VisibleSize;
		public byte CurrentProduction;

		public byte BaseTrade;
		public byte Owner;
		public ushort Food, Shields;
		public byte[] ResourceTiles;

		/// <summary>
		/// Up to 2 fortified units in this city.
		/// </summary>
		public byte[] FortifiedUnits;

		/// <summary>
		/// Up to 3 trading cities.
		/// </summary>
		public byte[] TradingCities;
	}
}