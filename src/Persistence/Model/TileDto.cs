using CivOne.Enums;

namespace CivOne.Persistence.Model
{
    public class TileDto
    {
        public Terrain Terrain { get; set; }
        public bool Road { get; set; }
        public bool RailRoad { get; set; }
        public bool Irrigation { get; set; }
        public bool Pollution { get; set; }
        public bool Fortress { get; set; }
        public bool Mine { get; set; }
        public bool Hut { get; set; }
    }
}


// using CivOne.Enums;
// using CivOne.Units;

// namespace CivOne.Tiles
// {
// 	public interface ITile : ICivilopedia
// 	{
// 		Terrain Type { get; }
// 		byte ContinentId { get; set; }
// 		byte LandValue { get; set; }
// 		byte LandScore { get; }
// 		bool Road { get; set; }
// 		bool RailRoad { get; set; }
// 		bool Irrigation { get; set; }
// 		bool Pollution { get; set; }
// 		bool Fortress { get; set; }
// 		bool Mine { get; set; }
// 		bool Hut { get; set; }
// }