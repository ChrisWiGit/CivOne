# YAML Save Format

This document outlines the structure of the YAML save format for the new way of saving and loading games in CivOne.
Providing a base to add new functionality to the game that are currently not supported by the old save format, such as bigger maps.

---

## Tile Storage System (TileCodec)

### TLDR

Each tile is stored in exactly **2 Base64 characters** encoding 12 bits:

- **4 bits Terrain** (0-12, where -1 is encoded as 15)
- **8 bits Flags** (Road, RailRoad, Irrigation, Pollution, Fortress, Mine, Hut)

Examples:

- `AG` = Tundra, no flags
- `AK` = Ocean, no flags
- `AR` = Plains with Road

---

### Detailed Explanation

#### Bit Layout (12 bits total)

```text
Bit 11:     Unused (always 0)
Bits 10..7: Flags (Hut, Mine, Fortress, Pollution)
Bits 6..4:  Flags (Irrigation, RailRoad, Road)
Bits 3..0:  Terrain-ID
```

#### Bit Assignment

| Bit | Purpose    | Values           |
| --- | ---------- | ---------------- |
| 3-0 | Terrain    | 0-12 (15 = None) |
| 4   | Road       | 0/1              |
| 5   | RailRoad   | 0/1              |
| 6   | Irrigation | 0/1              |
| 7   | Pollution  | 0/1              |
| 8   | Fortress   | 0/1              |
| 9   | Mine       | 0/1              |
| 10  | Hut        | 0/1              |

#### Encoding in Base64

The 12 bits are split into two 6-bit groups:

- **First character**: Bits 11-6
- **Second character**: Bits 5-0

Each 6-bit group is mapped to the Base64 alphabet:
`ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/`

---

### Valid Bit Combinations (Realistic Scenarios)

#### Constraints

- **Irrigation + Mine**: Impossible (mutually exclusive)
- **Road + RailRoad**: Contradictory but technically allowed

#### Table of Valid Combinations

| Terrain       | Road | Rail | Irr | Pol | Fort | Mine | Hut | Decimal | Base64 | Description                       |
| ------------- | ---- | ---- | --- | --- | ---- | ---- | --- | ------- | ------ | --------------------------------- |
| Ocean (10)    | 0    | 0    | 0   | 0   | 0    | 0    | 0   | 10      | AK     | Ocean, untouched                  |
| Ocean (10)    | 1    | 0    | 0   | 0   | 0    | 0    | 0   | 138     | Aa     | Ocean with Road                   |
| Ocean (10)    | 1    | 1    | 0   | 0   | 0    | 0    | 0   | 154     | A6     | Ocean with Road + RailRoad        |
| Ocean (10)    | 0    | 0    | 0   | 1   | 0    | 0    | 0   | 138     | CK     | Ocean with Pollution              |
| Plains (1)    | 0    | 0    | 0   | 0   | 0    | 0    | 0   | 1       | AB     | Plains, untouched                 |
| Plains (1)    | 1    | 0    | 0   | 0   | 0    | 0    | 0   | 17      | AR     | Plains with Road                  |
| Plains (1)    | 0    | 1    | 0   | 0   | 0    | 0    | 0   | 33      | Ah     | Plains with RailRoad              |
| Plains (1)    | 1    | 1    | 0   | 0   | 0    | 0    | 0   | 49      | Ax     | Plains with Road + RailRoad       |
| Plains (1)    | 0    | 0    | 1   | 0   | 0    | 0    | 0   | 65      | BB     | Plains with Irrigation            |
| Plains (1)    | 1    | 0    | 1   | 0   | 0    | 0    | 0   | 81      | BR     | Plains with Road + Irrigation     |
| Plains (1)    | 0    | 1    | 1   | 0   | 0    | 0    | 0   | 97      | Bh     | Plains with RailRoad + Irrigation |
| Plains (1)    | 0    | 0    | 0   | 1   | 0    | 0    | 0   | 129     | Ah     | Plains with Pollution             |
| Forest (4)    | 0    | 0    | 0   | 0   | 0    | 1    | 0   | 548     | ID     | Forest with Mine                  |
| Hills (2)     | 0    | 0    | 0   | 0   | 1    | 0    | 0   | 258     | EE     | Hills with Fortress               |
| Hills (2)     | 1    | 0    | 0   | 0   | 1    | 0    | 0   | 274     | EU     | Hills with Road + Fortress        |
| Mountain (3)  | 0    | 0    | 0   | 0   | 1    | 0    | 0   | 772     | EF     | Mountain with Fortress            |
| Mountain (3)  | 1    | 0    | 0   | 0   | 1    | 0    | 0   | 788     | EV     | Mountain with Road + Fortress     |
| Desert (5)    | 0    | 0    | 1   | 0   | 0    | 0    | 0   | 101     | BA     | Desert with Irrigation            |
| Desert (5)    | 1    | 0    | 1   | 0   | 0    | 0    | 0   | 117     | BQ     | Desert with Road + Irrigation     |
| Grassland (0) | 0    | 0    | 0   | 0   | 0    | 0    | 1   | 1024    | QC     | Grassland with Hut                |
| Grassland (0) | 1    | 0    | 0   | 0   | 0    | 0    | 0   | 16      | AS     | Grassland with Road               |
| Grassland (0) | 1    | 1    | 0   | 0   | 0    | 0    | 0   | 48      | Ay     | Grassland with Road + RailRoad    |
| Forest (4)    | 0    | 0    | 0   | 0   | 0    | 0    | 1   | 548     | QD     | Forest with Hut                   |
| Tundra (6)    | 0    | 0    | 0   | 0   | 0    | 0    | 1   | 1030    | QG     | Tundra with Hut                   |
| Tundra (6)    | 0    | 0    | 0   | 0   | 0    | 0    | 0   | 6       | AG     | Tundra with no flags              |

---

### Decoding Examples

#### Example 1: "AR"

- 'A' = Index 0 = 000000 (binary)
- 'R' = Index 17 = 010001 (binary)
- Combined: 000000010001 = 17
- Terrain: 0001 = 1 (Plains)
- Road: 1 (bit 4 set)
- Result: **Plains with Road**

#### Example 2: "AG"

- 'A' = 0
- 'G' = 6 = 000110
- Combined: 000000000110 = 6
- Terrain: 0110 = 6 (Tundra)
- No flags
- Result: **Tundra, untouched**

---

### Storage Efficiency

- Standard Civ1 map: 80×50 tiles = 4,000 tiles
- Per tile: 2 Base64 characters
- Total storage: 8,000 characters + YAML overhead
- Compression: ~1.5 bytes per tile (vs. ~10+ bytes for direct storage)

---

### Implementation

The tile encoding/decoding is implemented in [src/Persistence/Model/TileCodec.cs](src/Persistence/Model/TileCodec.cs):

- **`Encode(TileDto tile)`**: Converts a tile object into exactly 2 Base64 characters
- **`Decode(string row, int offset)`**: Decodes 2 characters from a row string starting at the given offset

Tests for the TileCodec can be found in the unit test project at [xunit/src/](xunit/src/) in the persistence-related test files.
