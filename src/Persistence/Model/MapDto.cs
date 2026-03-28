using System.Collections.Generic;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
    public class MapDto
    {
        [Doc("The seed used for procedural terrain generation. This ensures that the same map can be recreated if needed.", 0, uint.MaxValue)]
        public uint TerrainSeed { get; set; }
        
        public Map2d<TileDto> Tiles { get; set; }
    }
}