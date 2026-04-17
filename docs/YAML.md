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

### Reference Encodings (Current Implementation)

#### Terrain values (enum `CivOne.Enums.Terrain`)

- Desert = 0
- Plains = 1
- Grassland1 = 2
- Forest = 3
- Hills = 4
- Mountains = 5
- Tundra = 6
- Arctic = 7
- Swamp = 8
- Jungle = 9
- Ocean = 10
- River = 11
- Grassland2 = 12
- None = -1 (encoded as terrain nibble `15`)

#### Notes

- `Road + RailRoad` is representable by the codec.
- `Irrigation + Mine` is representable by the codec (gameplay rules may still treat this as invalid/impossible in practice).

#### Encoding examples

In the `Terrain` column, the value in parentheses is the enum value from `CivOne.Enums.Terrain`.

The table below highlights common and useful reference encodings. For the complete list of all possible encodings, see [YAML_TILE_ENCODING.md](YAML_TILE_ENCODING.md).

| Terrain         | Road | Rail | Irr | Pol | Fort | Mine | Hut | Decimal | Base64 | Description |
| --------------- | ---- | ---- | --- | --- | ---- | ---- | --- | ------- | ------ | ----------- |
| Desert (0)      | 0    | 0    | 0   | 0   | 0    | 0    | 0   | 0       | AA     | Desert, untouched |
| Desert (0)      | 0    | 0    | 1   | 0   | 0    | 0    | 0   | 64      | BA     | Desert, irrigation |
| Desert (0)      | 1    | 0    | 1   | 0   | 0    | 0    | 0   | 80      | BQ     | Desert, road+irrigation |
| Plains (1)      | 0    | 0    | 0   | 0   | 0    | 0    | 0   | 1       | AB     | Plains, untouched |
| Plains (1)      | 1    | 0    | 0   | 0   | 0    | 0    | 0   | 17      | AR     | Plains, road |
| Plains (1)      | 0    | 1    | 0   | 0   | 0    | 0    | 0   | 33      | Ah     | Plains, rail |
| Plains (1)      | 1    | 0    | 1   | 0   | 0    | 0    | 0   | 81      | BR     | Plains, road+irrigation |
| Grassland1 (2)  | 0    | 0    | 0   | 0   | 0    | 0    | 0   | 2       | AC     | Grassland1, untouched |
| Grassland1 (2)  | 1    | 0    | 0   | 0   | 0    | 0    | 0   | 18      | AS     | Grassland1, road |
| Grassland1 (2)  | 0    | 0    | 0   | 0   | 0    | 0    | 1   | 1026    | QC     | Grassland1, hut |
| Forest (3)      | 0    | 0    | 0   | 0   | 0    | 0    | 0   | 3       | AD     | Forest, untouched |
| Forest (3)      | 0    | 0    | 0   | 0   | 0    | 1    | 0   | 515     | ID     | Forest, mine |
| Forest (3)      | 0    | 0    | 0   | 0   | 0    | 0    | 1   | 1027    | QD     | Forest, hut |
| Hills (4)       | 0    | 0    | 0   | 0   | 0    | 0    | 0   | 4       | AE     | Hills, untouched |
| Hills (4)       | 0    | 0    | 0   | 0   | 1    | 0    | 0   | 260     | EE     | Hills, fortress |
| Mountains (5)   | 0    | 0    | 0   | 0   | 0    | 0    | 0   | 5       | AF     | Mountains, untouched |
| Mountains (5)   | 0    | 0    | 0   | 0   | 1    | 0    | 0   | 261     | EF     | Mountains, fortress |
| Tundra (6)      | 0    | 0    | 0   | 0   | 0    | 0    | 0   | 6       | AG     | Tundra, untouched |
| Tundra (6)      | 0    | 0    | 0   | 0   | 0    | 0    | 1   | 1030    | QG     | Tundra, hut |
| Ocean (10)      | 0    | 0    | 0   | 0   | 0    | 0    | 0   | 10      | AK     | Ocean, untouched |
| Ocean (10)      | 1    | 0    | 0   | 0   | 0    | 0    | 0   | 26      | Aa     | Ocean, road |
| River (11)      | 0    | 0    | 0   | 0   | 0    | 0    | 0   | 11      | AL     | River, untouched |
| Grassland2 (12) | 0    | 0    | 0   | 0   | 0    | 0    | 0   | 12      | AM     | Grassland2, untouched |
| None (-1 → 15)  | 0    | 0    | 0   | 0   | 0    | 0    | 0   | 15      | AP     | No terrain, untouched |

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

The tile encoding/decoding is implemented in [src/Persistence/Util/TileCodec.cs](src/Persistence/Util/TileCodec.cs):

- **`Encode(TileDto tile)`**: Converts a tile object into exactly 2 Base64 characters
- **`Decode(string row, int offset)`**: Decodes 2 characters from a row string starting at the given offset

Tests for the TileCodec can be found in the unit test project at [xunit/src/](xunit/src/) in the persistence-related test files.

---

## SpaceShip Storage (per Player)

The spaceship state is stored under each player as an optional `SpaceShip` object.

Location in YAML:

- `GameState.Players[n].SpaceShip`

### YAML Structure

```yaml
Players:
  - Id: 1
    SpaceShip:
      Grid:
        - ECEEMEEEEEEE
        - MMSEEEEEEEEE
        - SEEEEEEEEEEE
        - EEEEEEEEEEEE
        - EEEEEEEEEEEE
        - EEEEEEEEEEEE
        - EEEEEEEEEEEE
        - EEEEEEEEEEEE
        - EEEEEEEEEEEE
        - EEEEEEEEEEEE
        - EEEEEEEEEEEE
        - EEEEEEEEEEEE
      Population: 32000
      LaunchYear: 2050
```

### `Grid` layout (12×12)

- Exactly 12 rows
- Each row has exactly 12 characters
- Row-major order (`Grid[y][x]`)

Character mapping:

- `E` = Empty
- `S` = Structural (`SSStructural`)
- `C` = Component (`SSComponent`)
- `M` = Module (`SSModule`)

Legacy input compatibility:

- `0` is also accepted while reading and treated as `E` (empty)

### Numeric fields

- `Population` is represented as `long` in DTO and clamped to `ushort` range on load (`0..65535`)
- `LaunchYear` is represented as `long` in DTO and clamped to `short` range on load (`-32768..32767`)

### Binary compatibility mapping (`SaveData`)

The YAML model corresponds to legacy binary save data in [src/IO/SaveData.cs](src/IO/SaveData.cs):

- `SpaceShips[1462]`
  - Per player block: 180 bytes
  - `36` bytes: legacy unused header data
  - `144` bytes: 12×12 cell grid
- `SpaceShipPopulation[8]` → `Population`
- `SpaceShipLaunchYear[8]` → `LaunchYear`

`SpaceShip` may be omitted/null for players without any spaceship progress.

---

## YAML Read: Range Handling and Clamp Logging

When YAML contains numeric values outside domain limits, mapping now uses a dedicated DI service instead of silent casts.

### Service

- Interface: `IYamlReadValueSanitizer`
- Implementation: `YamlReadValueSanitizer`
- Source: [src/Persistence/Model/IYamlReadValueSanitizer.cs](src/Persistence/Model/IYamlReadValueSanitizer.cs)

The service provides typed clamp methods such as:

- `ClampToByte(...)`
- `ClampToInt16(...)`
- `ClampToInt32(...)`

### Logging Behavior

If an input value is outside the allowed range:

- The value is clamped to the nearest valid boundary.
- A log entry is emitted through injected `ILogger`.
- The log includes mapper name, field name, original value, valid range, clamped value, and reason (`underflow` or `overflow`).

### Current Usage

The sanitizer is used in YAML read paths of these mappers:

- [src/Persistence/Model/PlayerDtoMapper.cs](src/Persistence/Model/PlayerDtoMapper.cs)
- [src/Persistence/Model/CityDtoMapper.cs](src/Persistence/Model/CityDtoMapper.cs)
- [src/Persistence/Model/UnitDtoMapper.cs](src/Persistence/Model/UnitDtoMapper.cs)
- [src/Persistence/Model/PalaceDtoMapper.cs](src/Persistence/Model/PalaceDtoMapper.cs)
- [src/Persistence/Model/GameStateDtoMapper.cs](src/Persistence/Model/GameStateDtoMapper.cs)

### Tests

Behavior is covered by:

- [xunit/src/persistence/Model/YamlReadValueSanitizerTest.cs](xunit/src/persistence/Model/YamlReadValueSanitizerTest.cs)

## Player

### Advances: "All Advances" Sentinel Value

Feature: In Yaml, Advances allows -1 as an indicator to represent "all advances", e.g. for debugging or testing purposes. The mapper will resolve this to the full list of advance IDs in the game.

```yaml
Players:
  - Id: 3
    PlayerGuid: dd5760ec-0df5-48ac-b4d6-f9e3acec17f3
    Advances:
    - -1
    Embassies: []
    Diplomacy:
```
