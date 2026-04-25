using System;
using CivOne.Enums;

namespace CivOne.Persistence.Model
{
    /// <summary>
    /// Encodes and decodes a <see cref="TileDto"/> to/from exactly 2 Base64 characters.
    ///
    /// 12-bit layout (bits 11..0):
    ///   bit 11   : Special (Oasis for Desert; special resource for other terrain types)
    ///              If 0 on decode, the caller should fall back to TileIsSpecial for backwards compatibility.
    ///   bit  10    : Hut
    ///   bit   9    : Mine
    ///   bit   8    : Fortress
    ///   bit   7    : Pollution
    ///   bit   6    : Irrigation
    ///   bit   5    : RailRoad
    ///   bit   4    : Road
    ///   bits  3..0 : Terrain  (4 bits, values 0-12; -1 is stored as 15)
    ///
    /// The 12 bits are split into two 6-bit groups, each mapped to the standard
    /// Base64 alphabet: ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/
    ///
    /// Example: Tundra (6), no flags → "AG"
    ///          Ocean  (10), no flags → "AK"
    ///          Plains (1), Road=true → "AR"
    ///
    /// The codec itself is not limited to any fixed row width; the caller
    /// determines how many tiles to encode/decode. For a standard Civ1 map
    /// row of 80 tiles the resulting YAML string is 160 characters long.
    /// </summary>
    public class TileCodec
    {
        private readonly string _alphabet =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

        /// <summary>Terrain.None (-1) is stored as 15 in the 4-bit terrain field.</summary>
        private readonly int _noneTerrain = 15;

        // Reverse lookup table for O(1) decode.
        private readonly int[] _reverse;

        public TileCodec(int[] reverse = null)
        {
            _reverse = reverse ?? BuildReverse();
        }

        private int[] BuildReverse()
        {
            var table = new int[128];
            for (int i = 0; i < table.Length; i++) table[i] = -1;
            for (int i = 0; i < _alphabet.Length; i++) table[_alphabet[i]] = i;
            return table;
        }

        /// <summary>Encodes a single tile into exactly 2 characters.</summary>
        public string Encode(TileDto tile)
        {
            int terrainBits = tile.Terrain == Terrain.None
                ? _noneTerrain
                : (int)tile.Terrain & 0xF;

            int value = terrainBits
                | (tile.Road       ? 1 << 4 : 0)
                | (tile.RailRoad   ? 1 << 5 : 0)
                | (tile.Irrigation ? 1 << 6 : 0)
                | (tile.Pollution  ? 1 << 7 : 0)
                | (tile.Fortress   ? 1 << 8 : 0)
                | (tile.Mine       ? 1 << 9 : 0)
                | (tile.Hut        ? 1 << 10 : 0)
                | (tile.Special    ? 1 << 11 : 0);

            return new string(
			[
				_alphabet[(value >> 6) & 0x3F],
                _alphabet[value & 0x3F]
            ]);
        }

        /// <summary>Decodes 2 characters from a row string at the given offset.</summary>
        public TileDto Decode(string row, int offset)
        {
            if (offset + 1 >= row.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), "Row string too short.");

            char c1 = row[offset];
            char c2 = row[offset + 1];

            int v1 = c1 < 128 ? _reverse[c1] : -1;
            int v2 = c2 < 128 ? _reverse[c2] : -1;

            if (v1 < 0 || v2 < 0)
                throw new FormatException(
                    $"Invalid tile encoding '{c1}{c2}' at position {offset}.");

            int value = (v1 << 6) | v2;

            int terrainRaw = value & 0xF;
            Terrain terrain = terrainRaw == _noneTerrain
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
                Special   = (value & (1 << 11)) != 0,
            };
        }
    }
}
