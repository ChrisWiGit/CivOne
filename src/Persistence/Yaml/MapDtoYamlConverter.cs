using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Persistence.Model;
using CivOne.Persistence.Model.Attributes;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Yaml
{
    /// <summary>
    /// YAML converter for MapDto that encodes Map2d{TileDto} into a compact string array using TileCodec.
    /// Each tile is encoded as exactly 2 Base64 characters, so a row of 80 tiles becomes 160 characters.
    /// </summary>
    public class MapDtoTileDtoYamlConverter : IYamlTypeConverter
    {
        private readonly TileCodec _codec = new();

        public bool Accepts(Type type)
        {
            return type == typeof(MapDto);
        }

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            throw new NotImplementedException("MapDto deserialization from YAML not yet implemented");
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            var mapDto = (MapDto)value;
            
            // Convert to a serializable format with encoded tiles and separate land values
            var encodedRows = EncodeMapToRows(mapDto.Tiles);
            var landValues = ExtractLandValues(mapDto.Tiles);
            
            var mapYamlRepresentation = new MapDtoYamlRepresentation
            {
                TerrainSeed = mapDto.TerrainSeed,
                Tiles = encodedRows,
                LandValues = landValues
            };
            
            serializer(mapYamlRepresentation, typeof(MapDtoYamlRepresentation));
        }

        private string[] EncodeMapToRows(Map2d<TileDto> tiles)
        {
            int width = tiles.Width();
            int height = tiles.Height();
            var rows = new string[height];

            for (int y = 0; y < height; y++)
            {
                var rowBuilder = new System.Text.StringBuilder(width * 2);
                for (int x = 0; x < width; x++)
                {
                    var tileDto = tiles[x, y];
                    var encoded = _codec.Encode(tileDto);
                    rowBuilder.Append(encoded);
                }
                rows[y] = rowBuilder.ToString();
            }

            return rows;
        }

        private byte[][] ExtractLandValues(Map2d<TileDto> tiles)
        {
            int width = tiles.Width();
            int height = tiles.Height();
            var landValues = new byte[height][];

            for (int y = 0; y < height; y++)
            {
                landValues[y] = new byte[width];
                for (int x = 0; x < width; x++)
                {
                    landValues[y][x] = tiles[x, y].LandValue;
                }
            }

            return landValues;
        }
    }

    /// <summary>
    /// Intermediate class for YAML serialization of encoded map data.
    /// Each row is a string of Base64-encoded tile data (2 chars per tile).
    /// LandValues is stored as a separate 2D array to keep the tile encoding compact.
    /// </summary>
    internal class MapDtoYamlRepresentation
    {
        [Doc("The seed used for procedural terrain generation. This ensures that the same map can be recreated if needed.", 0, uint.MaxValue)]
        public uint TerrainSeed { get; set; }
        
        [Doc("Encoded tile data. Each row contains Base64-encoded tiles (2 chars per tile). Use TileCodec to decode individual tiles.")]
        public string[] Tiles { get; set; }
        
        [Doc("Land values for each tile. Higher values indicate more desirable locations for founding cities.")]
        public byte[][] LandValues { get; set; }
    }
}