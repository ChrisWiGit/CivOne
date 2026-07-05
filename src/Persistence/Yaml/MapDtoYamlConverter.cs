using System;
using System.Globalization;
using System.Linq;
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
            var yamlMap = (MapDtoYamlRepresentation?)rootDeserializer(typeof(MapDtoYamlRepresentation));

            ArgumentNullException.ThrowIfNull(yamlMap);
            ArgumentNullException.ThrowIfNull(yamlMap.Tiles);

            var tiles = DecodeMapFromRows(yamlMap.Tiles, yamlMap.LandValues);

            return new MapDto
            {
                TerrainSeed = yamlMap.TerrainSeed,
                Tiles = tiles
            };
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
			ArgumentNullException.ThrowIfNull(value);
			var mapDto = (MapDto)value;
            
            // Convert to a serializable format with encoded tiles and compact land values
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

        private Map2d<TileDto> DecodeMapFromRows(string[] encodedRows, string[] landValuesRows)
        {
            int height = encodedRows.Length;
            int width = height == 0 ? 0 : encodedRows[0].Length / 2;
            var tiles = new TileDto[width, height];

            for (int y = 0; y < height; y++)
            {
                var row = encodedRows[y] ?? string.Empty;
                if (row.Length % 2 != 0)
                {
                    throw new FormatException($"Encoded tile row at index {y} has invalid length {row.Length} (must be even).");
                }

                int rowWidth = row.Length / 2;
                if (y == 0)
                {
                    width = rowWidth;
                }
                else if (rowWidth != width)
                {
                    throw new FormatException($"Encoded tile row at index {y} has width {rowWidth}, expected {width}.");
                }

                for (int x = 0; x < width; x++)
                {
                    tiles[x, y] = _codec.Decode(row, x * 2);
                }
            }

            ApplyLandValues(tiles, landValuesRows);

            return new Map2d<TileDto>(tiles);
        }

        private static void ApplyLandValues(TileDto[,] tiles, string[] landValuesRows)
        {
            if (landValuesRows == null || landValuesRows.Length == 0)
            {
                return;
            }

            int width = tiles.GetLength(0);
            int height = tiles.GetLength(1);

            for (int y = 0; y < Math.Min(height, landValuesRows.Length); y++)
            {
                var values = SplitLandValuesRow(landValuesRows[y]);
                for (int x = 0; x < Math.Min(width, values.Length); x++)
                {
                    tiles[x, y].LandValue = values[x];
                }
            }
        }

        private static byte[] SplitLandValuesRow(string row)
        {
            if (string.IsNullOrWhiteSpace(row))
            {
                return [];
            }

            var normalized = row.Trim();
            if (normalized.StartsWith('[') && normalized.EndsWith(']'))
            {
                normalized = normalized[1..^1];
            }

            return [..
                normalized
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => ParseLandValue(x.Trim()))
            ];
        }

        private static byte ParseLandValue(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new FormatException("Land value token cannot be empty.");
            }

            if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return byte.Parse(token[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
            }

            if (byte.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var decimalValue))
            {
                return decimalValue;
            }

            return byte.Parse(token, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        }

        private static string[] ExtractLandValues(Map2d<TileDto> tiles)
        {
            int width = tiles.Width();
            int height = tiles.Height();
            var landValues = new string[height];

            for (int y = 0; y < height; y++)
            {
                var rowValues = new string[width];
                for (int x = 0; x < width; x++)
                {
                    rowValues[x] = tiles[x, y].LandValue.ToString("X2", CultureInfo.InvariantCulture);
                }

                landValues[y] = string.Join(',', rowValues);
            }

            return landValues;
        }
    }

    /// <summary>
    /// Intermediate class for YAML serialization of encoded map data.
    /// Each row is a string of Base64-encoded tile data (2 chars per tile).
    /// LandValues is stored as comma-separated hexadecimal byte strings (one row per entry) to keep YAML compact.
    /// </summary>
	/// <seealso cref="MapDto"/>
    internal class MapDtoYamlRepresentation
    {
        [Doc("The seed used for procedural terrain generation. This ensures that the same map can be recreated if needed.", 0, uint.MaxValue)]
        public uint TerrainSeed { get; set; }
        
        [Doc("Encoded tile data. Each row contains Base64-encoded tiles (2 chars per tile). Use TileCodec to decode individual tiles. See YAML.md for encoding details.")]
        public string[] Tiles { get; set; } = [];
        
        [Doc("Land values for each tile row, encoded as comma-separated 2-digit hex bytes to reduce YAML size on large maps. See INTERNALS.md for details on how this value is used.")]
        public string[] LandValues { get; set; } = [];
    }
}