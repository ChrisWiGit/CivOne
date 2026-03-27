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

        public byte LandValue { get; set; }
        public byte LandScore { get; set; }
    }
}