using System.Drawing;
using CivOne.Tiles;

namespace CivOne
{
	public interface ICityBasic
	{
		Point Location { get; }
		byte Size { get; }
		short Luxuries { get; }
		byte Owner { get; set; }
		ITile Tile { get; }

		int ContinentId { get; }

		Player Player { get; }

		int Entertainers { get; }
		int Scientists { get; }
		int Taxmen { get; }
	}
}
