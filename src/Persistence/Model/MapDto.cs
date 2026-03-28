using System.Collections.Generic;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
    /// <summary>
    /// Data Transfer Object (DTO) representing the map in Civilization I.
    /// For YAML serialization, another DTO with a more YAML-friendly structure is beeing used: MapDtoYamlRepresentation
    /// </summary>
    /// <seealso cref="MapDtoYamlRepresentation"/>
    public class MapDto
    {
        [Doc("The seed used for procedural terrain generation. This ensures that the same map can be recreated if needed.", 0, uint.MaxValue)]
        public uint TerrainSeed { get; set; }
        
        [Doc("2D array of TileDto representing the terrain and features of each tile on the map. The dimensions should match the width and height of the map.")]
        public Map2d<TileDto> Tiles { get; set; }
    }
}