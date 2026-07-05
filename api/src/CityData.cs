// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Diagnostics.CodeAnalysis;

namespace CivOne
{
	[SuppressMessage("Microsoft.ValueType", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "This struct is only used for data transfer and is not intended to be compared for equality.")]
	public struct CityData
	{
		public byte Id { get; set; }
		public byte NameId { get; set; }
		public byte Status { get; set; }
		public byte[] Buildings { get; set; }
		public byte X { get; set; }
		public byte Y { get; set; }

		/// <summary>
		/// The reals size of the city as not always visible to the player.
		/// </summary>
		public byte ActualSize { get; set; }

		/// <summary>
		/// The size of the city as seen by the player.
		/// </summary>
		public byte VisibleSize { get; set; }
		public byte CurrentProduction { get; set; }

		public byte BaseTrade { get; set; }
		public byte Owner { get; set; }
		public ushort Food { get; set; }
		public ushort Shields { get; set; }
		public byte[] ResourceTiles { get; set; }

		/// <summary>
		/// Up to 2 fortified units in this city.
		/// </summary>
		public byte[] FortifiedUnits { get; set; }

		/// <summary>
		/// Up to 3 trading cities.
		/// </summary>
		public byte[] TradingCities { get; set; }
	}
}