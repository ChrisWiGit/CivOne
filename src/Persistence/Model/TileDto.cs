using System;
using CivOne.Enums;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
    public class TileDto
    {
        [Doc("Terrain type of the tile. Possible values: ", nameof(AllTerrainNames))]
        public Terrain Terrain { get; set; }

        public static readonly string[] AllTerrainNames = Enum.GetNames<Terrain>();

        [Doc("Whether the tile has a road (true) or not (false).")]
        public bool Road { get; set; }

        [Doc("Whether the tile has a railroad (true) or not (false).")]
        public bool RailRoad { get; set; }

        [Doc("Whether the tile has irrigation (true) or not (false).")]
        public bool Irrigation { get; set; }

        [Doc("Whether the tile is polluted (true) or not (false).")]
        public bool Pollution { get; set; }

        [Doc("Whether the tile has a fortress (true) or not (false).")]
        public bool Fortress { get; set; }
        
        [Doc("Whether the tile has a mine (true) or not (false).")]
        public bool Mine { get; set; }
        
        [Doc("Whether the tile has a hut (true) or not (false).")]
        public bool Hut { get; set; }

        [Doc("Land value of the tile, used for city site evaluation. Higher values indicate more desirable locations for founding cities. See INTERNALS.md for details on how this value is calculated.")]
        public byte LandValue { get; set; }
    }
}