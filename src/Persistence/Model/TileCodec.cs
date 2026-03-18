using System;
using CivOne.Enums;

namespace CivOne.Persistence.Model
{
	/// AI AI AI AI WARNING
    /// <summary>
    /// Encodes and decodes a <see cref="TileDto"/> to/from exactly 2 Base64 characters.
    ///
    /// 12-bit layout (bits 11..0):
    ///   bit 11   : unused (always 0)
    ///   bits 10..7 : (unused padding, all 0)
    ///   bits  3..0 : Terrain  (4 bits, values 0-12; -1 is stored as 15)
    ///   bit   4    : Road
    ///   bit   5    : RailRoad
    ///   bit   6    : Irrigation
    ///   bit   7    : Pollution
    ///   bit   8    : Fortress
    ///   bit   9    : Mine
    ///   bit  10    : Hut
    ///
    /// The 12 bits are split into two 6-bit groups, each mapped to the standard
    /// Base64 alphabet: ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/
    ///
    /// Example: Tundra (6), no flags → "AG"
    ///          Ocean  (10), no flags → "AK"
    ///          Plains (1), Road=true → "AR"
    ///
    /// A complete map row of 80 tiles becomes a 160-character YAML string.
    /// </summary>
    public static class TileCodec
    {
        private const string Alphabet =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

        /// <summary>Terrain.None (-1) is stored as 15 in the 4-bit terrain field.</summary>
        private const int NoneTerrain = 15;

        // Reverse lookup table for O(1) decode.
        private static readonly int[] Reverse = BuildReverse();

        private static int[] BuildReverse()
        {
            var table = new int[128];
            for (int i = 0; i < table.Length; i++) table[i] = -1;
            for (int i = 0; i < Alphabet.Length; i++) table[Alphabet[i]] = i;
            return table;
        }

        /// <summary>Encodes a single tile into exactly 2 characters.</summary>
        public static string Encode(TileDto tile)
        {
            int terrainBits = tile.Terrain == Terrain.None
                ? NoneTerrain
                : (int)tile.Terrain & 0xF;

            int value = terrainBits
                | (tile.Road       ? 1 << 4 : 0)
                | (tile.RailRoad   ? 1 << 5 : 0)
                | (tile.Irrigation ? 1 << 6 : 0)
                | (tile.Pollution  ? 1 << 7 : 0)
                | (tile.Fortress   ? 1 << 8 : 0)
                | (tile.Mine       ? 1 << 9 : 0)
                | (tile.Hut        ? 1 << 10 : 0);

            return new string(new[]
            {
                Alphabet[(value >> 6) & 0x3F],
                Alphabet[value & 0x3F]
            });
        }

        /// <summary>Decodes 2 characters from a row string at the given offset.</summary>
        public static TileDto Decode(string row, int offset)
        {
            if (offset + 1 >= row.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), "Row string too short.");

            char c1 = row[offset];
            char c2 = row[offset + 1];

            int v1 = c1 < 128 ? Reverse[c1] : -1;
            int v2 = c2 < 128 ? Reverse[c2] : -1;

            if (v1 < 0 || v2 < 0)
                throw new FormatException(
                    $"Invalid tile encoding '{c1}{c2}' at position {offset}.");

            int value = (v1 << 6) | v2;

            int terrainRaw = value & 0xF;
            Terrain terrain = terrainRaw == NoneTerrain
                ? Terrain.None
                : (Terrain)terrainRaw;

            return new TileDto
            {
                Terrain   = terrain,
                Road      = (value & (1 << 4)) != 0,
                RailRoad  = (value & (1 << 5)) != 0,
                Irrigation = (value & (1 << 6)) != 0,
                Pollution = (value & (1 << 7)) != 0,
                Fortress  = (value & (1 << 8)) != 0,
                Mine      = (value & (1 << 9)) != 0,
                Hut       = (value & (1 << 10)) != 0,
            };
        }
    }
}
