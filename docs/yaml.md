# YAML format reference

This document describes the YAML representation of CivOne save and map data.
The source of truth is the code in `src` and `api/src`.

## Scope

The core game data contract is `IGameData` in `/home/runner/work/CivOne/CivOne/api/src/IGameData.cs`.
The concrete implementation is `SaveDataAdapter` in `/home/runner/work/CivOne/CivOne/src/SaveDataAdapter.cs` and related partial files.
Map persistence is implemented in `/home/runner/work/CivOne/CivOne/src/Map.LoadSave.cs`.

## Top-level YAML keys

The following top-level keys are expected to mirror `IGameData`.

| Key | Type | Notes |
| --- | --- | --- |
| `gameTurn` | `ushort` | Current game turn. |
| `humanPlayer` | `ushort` | Index of human player. |
| `randomSeed` | `ushort` | Map/random seed used for generation and loading. |
| `difficulty` | `ushort` | Difficulty level id. |
| `activeCivilizations` | `bool[8]` | Active civilization flags by player slot. |
| `civilizationIdentity` | `byte[8]` | Identity variant flags used to pick civ variant in a color slot. |
| `currentResearch` | `ushort` | Current research advance id for human player. |
| `discoveredAdvanceIDs` | `byte[8][]` | Per-player discovered advance ids. |
| `leaderNames` | `string[8]` | Per-player leader names. |
| `civilizationNames` | `string[8]` | Per-player plural civilization names. |
| `citizenNames` | `string[8]` | Per-player singular civilization names. |
| `cityNames` | `string[256]` | City name table. |
| `playerGold` | `short[8]` | Per-player gold. |
| `researchProgress` | `short[8]` | Per-player science progress. |
| `taxRate` | `ushort[8]` | Per-player tax rate. |
| `scienceRate` | `ushort[8]` | Per-player science rate. |
| `startingPositionX` | `ushort[8]` | Per-player starting X positions. |
| `government` | `ushort[8]` | Per-player government ids. |
| `cities` | `CityData[]` | Active city records only. |
| `units` | `UnitData[8][]` | Per-player unit records. |
| `wonders` | `ushort[22]` | Wonder ownership by city id, `65535` means not built. |
| `tileVisibility` | `bool[8][80,50]` | Per-player visible map tiles. |
| `advanceFirstDiscovery` | `ushort[72]` | First discovering civ for each advance id. |
| `gameOptions` | `bool[8]` | Option order is fixed, see section below. |
| `nextAnthologyTurn` | `ushort` | Next anthology turn. |
| `opponentCount` | `ushort` | Number of opponents. |
| `replayData` | `ReplayData[]` | Replay entries. |

## Game options order

`gameOptions` is positional.

1. `InstantAdvice`
2. `AutoSave`
3. `EndOfTurn`
4. `Animations`
5. `Sound`
6. `EnemyMoves`
7. `CivilopediaText`
8. `Palace`

## CityData shape

`CityData` fields mirror `/home/runner/work/CivOne/CivOne/api/src/CityData.cs`.

| Key | Type | Notes |
| --- | --- | --- |
| `id` | `byte` | Runtime city id. |
| `nameId` | `byte` | Index into `cityNames`. |
| `status` | `byte` | Bitfield. |
| `buildings` | `byte[]` | Building ids. |
| `x` | `byte` | Map X. |
| `y` | `byte` | Map Y. |
| `actualSize` | `byte` | Real city size. |
| `visibleSize` | `byte` | Visible city size. |
| `currentProduction` | `byte` | Production id. |
| `baseTrade` | `byte` | Base trade. |
| `owner` | `byte` | Owner slot id. |
| `food` | `ushort` | Stored food. |
| `shields` | `ushort` | Stored shields. |
| `resourceTiles` | `byte[6]` | City worked-tile and specialist bitfield payload. |
| `fortifiedUnits` | `byte[]` | Up to 2 fortified unit encoded bytes. |
| `tradingCities` | `byte[]` | Up to 3 linked city ids. |

## UnitData shape

`UnitData` fields mirror `/home/runner/work/CivOne/CivOne/api/src/UnitData.cs`.

| Key | Type |
| --- | --- |
| `id` | `byte` |
| `status` | `byte` |
| `x` | `byte` |
| `y` | `byte` |
| `typeId` | `byte` |
| `remainingMoves` | `byte` |
| `specialMoves` | `byte` |
| `gotoX` | `byte` |
| `gotoY` | `byte` |
| `visibility` | `byte` |
| `nextUnitId` | `byte` |
| `homeCityId` | `byte` |

## ReplayData shape

Replay parsing supports multiple incoming entry types.
Replay writing currently emits only `CivilizationDestroyed` entries.

## Map tile format (code-accurate)

The map format used by CivOne save and load logic is layer-based byte data.
It is not a two-character symbolic tile token table.
If an older document used tokens like `Ah`, treat that table as obsolete.

### Terrain layer byte values

From `/home/runner/work/CivOne/CivOne/src/Map.LoadSave.cs`.

| Byte | Terrain |
| --- | --- |
| `1` | Ocean |
| `2` | Forest |
| `3` | Swamp |
| `6` | Plains |
| `7` | Tundra |
| `9` | River |
| `10` | Grassland (`Grassland1` or `Grassland2`) |
| `11` | Jungle |
| `12` | Hills |
| `13` | Mountains |
| `14` | Desert |
| `15` | Arctic |

Any unknown terrain byte defaults to Ocean on load.

### Improvement layer bit flags

Stored in layer `y + (HEIGHT * 2)`.

| Bit mask | Meaning |
| --- | --- |
| `0x01` | City present |
| `0x02` | Irrigation |
| `0x04` | Mine |
| `0x08` | Road |

### Improvement layer 2 bit flags

Stored in layer `y + (HEIGHT * 3)`.

| Bit mask | Meaning |
| --- | --- |
| `0x01` | RailRoad |

### Additional map persistence layers

- Explored and hut helper data is stored in `x + (WIDTH * 2), y`.
- Continent id is stored in `x, y + HEIGHT`.
- Map visibility in savegame uses `tileVisibility` bit packing in `SaveData.MapVisibility`.

## Map size rules

`ValidMapSize(width, height)` currently only accepts `80 x 50`.
YAML map payloads must match this size when targeting the savegame adapter.
