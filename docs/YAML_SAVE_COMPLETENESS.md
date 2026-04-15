# YAML Persistence – Completeness Analysis

Comparison of the YAML save format against the original binary Civ1 savegame format (`SaveData`).

Last updated: April 2026 (Branch `yaml-review`)

---

## ✅ Previously Reported Gaps – Now Implemented

The following items were originally reported as missing but have since been implemented:

| Item | Status |
|------|--------|
| `CityDto.Food` and `CityDto.Shields` | ✅ Implemented in `CityDto` and `CityDtoMapper` |
| `PlayerDto.StartX` | ✅ Implemented in `PlayerDto` and `PlayerDtoMapper` |
| `PlayerDto.HumanContactTurn` | ✅ Implemented |
| `GameStateDto.PeaceTurns` | ✅ Implemented |
| `PlayerDto.FutureTechCount` | ✅ Implemented |
| `PlayerDto` statistics (EpicRanking, MilitaryPower, CivilizationScore, UnitsLost, UnitsDestroyedBy) | ✅ Implemented |
| `PlayerDto.Diplomacy` (DiplomacyEntryDto with RawFlags) | ✅ Implemented |
| `SpaceShipDto` per player | ✅ Implemented |

---

## 🔵 Present in Binary but **not implemented in game logic**

These fields exist in the binary format (`SaveData`) but are not read during loading and are not
actively used by the game logic. This is a general feature gap, not a YAML-specific problem.

| Field in `SaveData` | State in game code |
|---------------------|--------------------|
| `ContinentStrategy`, `ContinentDefense`, `ContinentAttack`, `ContinentCities` | AI data, not read |
| `StrategicLocation*` (Status/Policy/X/Y) | Not read |
| `LandPathFinding[260]` | Not read |
| `DebugSwitches` | Not read |
| `ScoreChart[8*150]` / `PeaceChart[8*150]` | Not read |

---

## 🟢 YAML Stores More Than the Binary

The YAML format stores some data that is **not directly** present in the binary format or must
be derived from it:

| Field | Binary | YAML | Note |
|-------|--------|------|------|
| `Player.Anarchy` | ❌ not direct | ✅ `PlayerDto.Anarchy` | In binary derived from Government state |
| `Player.LuxuriesRate` | ❌ derived | ✅ `PlayerDto.LuxuriesRate` | Binary: `10 - TaxRate - ScienceRate` |
| `Player.CityNamesSkipped` | ❌ missing | ✅ `PlayerDto.CityNamesSkipped` | Counter for next city name |
| `CityDto.VisibleSizes[]` | 1 byte (human only) | ✅ array per player | Finer per-player control |
| `CityDto.WasInDisorder` | ❌ missing | ✅ present | Turn state flag |
| `CityDto.Specialists` | ❌ from ResourceTiles | ✅ explicit | Direct state instead of bitmask |
| `GlobalWarmingDto` | 3 fields | ✅ structured | Clearer than binary raw data |
| `PlayerDto.PlayerGuid` | ❌ missing | ✅ stable player identity | GUID for cross-reference stability |
| `PlayerDto.UnitsDestroyedByByPlayerGuid` | ❌ missing | ✅ GUID-based | More robust than index-based |

---

## ✅ Fully Implemented (Binary ↔ YAML)

| Area | Status |
|------|--------|
| `GameTurn`, `HumanPlayer`, `CurrentPlayer` | ✅ |
| `Difficulty`, `OpponentCount` | ✅ |
| `RandomSeed` / `TerrainSeed` (separate) | ✅ |
| `AnthologyTurn` | ✅ |
| Players: Gold, Science, TaxRate, ScienceRate, Government | ✅ |
| Players: Advances, Embassies, CurrentResearch | ✅ |
| Players: LeaderName, TribeName, TribeNamePlural | ✅ |
| Players: Palace | ✅ |
| Players: Explored / Visible (tile visibility) | ✅ |
| Players: Food, StartX, HumanContactTurn, FutureTechCount | ✅ |
| Players: EpicRanking, MilitaryPower, CivilizationScore | ✅ |
| Players: UnitsLost, UnitsDestroyedBy (index + GUID) | ✅ |
| Players: Diplomacy (RawFlags, decoded placeholder) | ✅ |
| Players: SpaceShip (Grid, Population, LaunchYear) | ✅ |
| Cities: Position, owner, name, size | ✅ |
| Cities: Production, buildings, wonders | ✅ |
| Cities: ResourceTiles, status flags | ✅ |
| Cities: TradingCities, ContinentId | ✅ |
| Cities: Food, Shields | ✅ |
| Units: Type, position, goto, status, moves | ✅ |
| Units: Veteran, Sentry, Fortify, Order | ✅ |
| Units: HomeCity (via GUID) | ✅ |
| Map: Terrain tiles, MapSeed | ✅ |
| `AdvanceOrigin` (first discoverer per advance) | ✅ |
| `GameOptions` (8 flags) | ✅ |
| `CityNames` (256 entries) | ✅ |
| `Wonders` (22 entries, owner city) | ✅ |
| `ReplayData` | ✅ (partial – only CivilizationDestroyed fully implemented) |
| `GlobalWarming` (Count, Pollution, Indicator) | ✅ |
| `PeaceTurns` | ✅ |

---

## 📋 Remaining Optional Items (Low Priority)

These items are either AI-internal data (can be reconstructed) or historical charts not yet
needed for gameplay:

| Item | Description | Priority |
|------|-------------|----------|
| `ScoreChart[8*150]` / `PeaceChart[8*150]` | Historical score/peace curves per player (end screen) | Low |
| `ContinentStrategy/Defense/Attack/Cities` | AI continent strategy per player (reconstructable) | Optional |
| `StrategicLocation*` (X/Y/Status/Policy) | AI strategic points per player (reconstructable) | Optional |
| `LandPathFinding[260]` | Internal pathfinding cache (reconstructable) | Optional |
| `DebugSwitches` | Development-only | Optional |

