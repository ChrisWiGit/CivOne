# YAML Persistence Change Log and Legacy Traceability (April 2026)

This document summarizes the recent YAML persistence changes in the `yaml-review` branch and highlights where behavior is still connected to legacy Civ1 binary semantics.

---

## 1) Scope

The changes cover two main tracks:

1. **Player identity and cross-player statistics hardening**
   - Stable `PlayerGuid`
   - `UnitsDestroyedBy` modernization with backward-compatible legacy path

2. **Spaceship persistence model introduction**
   - New structured DTO representation for spaceship data
   - Compact YAML encoding for spaceship component grid
   - Explicit mapping notes to legacy `SaveData` layout

---

## 2) Files Added / Updated

### Added

- [src/Enums/SpaceShipComponentType.cs](src/Enums/SpaceShipComponentType.cs)
- [src/Persistence/Model/SpaceShipDto.cs](src/Persistence/Model/SpaceShipDto.cs)
- [src/Persistence/Model/SpaceShipGridMap2d.cs](src/Persistence/Model/SpaceShipGridMap2d.cs)
- [src/Persistence/Yaml/SpaceShipGridMapYamlTypeConverter.cs](src/Persistence/Yaml/SpaceShipGridMapYamlTypeConverter.cs)

### Updated

- [src/Persistence/Model/PlayerDto.cs](src/Persistence/Model/PlayerDto.cs)
- [src/Persistence/Model/PlayerDtoMapper.cs](src/Persistence/Model/PlayerDtoMapper.cs)
- [src/Persistence/Model/IPlayerRestorable.cs](src/Persistence/Model/IPlayerRestorable.cs)
- [src/Player.cs](src/Player.cs)
- [src/Persistence/Yaml/YamlWriter.cs](src/Persistence/Yaml/YamlWriter.cs)
- [src/Persistence/Yaml/YamlReader.cs](src/Persistence/Yaml/YamlReader.cs)
- [YAML.md](YAML.md)

Related review/background docs:

- [docs/PLAYER_GUID_UNITSDESTROYEDBY_REVIEW.md](docs/PLAYER_GUID_UNITSDESTROYEDBY_REVIEW.md)
- [docs/YAML_SAVE_COMPLETENESS.md](docs/YAML_SAVE_COMPLETENESS.md)

---

## 3) Spaceship: New YAML Model

### 3.1 New DTO surface

`PlayerDto` now includes:

- `SpaceShip: SpaceShipDto`

`SpaceShipDto` contains:

- `Grid: SpaceShipGridMap2d`
- `Population: long`
- `LaunchYear: long`

### 3.2 Grid encoding format

The spaceship grid is serialized as 12 strings (12×12), one character per cell:

- `E` = Empty
- `S` = Structural
- `C` = Component
- `M` = Module

Legacy read compatibility:

- `0` is accepted and interpreted as `E`.

### 3.3 Runtime storage

`Player` now has internal spaceship persistence fields:

- `SpaceShipGrid` (`SpaceShipComponentType[12,12]`)
- `SpaceShipPopulation` (`ushort`)
- `SpaceShipLaunchYear` (`short`)

`IPlayerRestorable` was extended to restore these values from YAML.

---

## 4) Legacy Connection Points (Important)

This section lists exactly where legacy compatibility still exists.

### 4.1 Legacy binary schema mapping intent

The YAML spaceship model is intentionally aligned to fields in [src/IO/SaveData.cs](src/IO/SaveData.cs):

- `SpaceShips[1462]`
- `SpaceShipPopulation[8]`
- `SpaceShipLaunchYear[8]`

Documented mapping details are in [YAML.md](YAML.md).

### 4.2 Clamp-based compatibility boundary

Although YAML uses `long` for `Population` and `LaunchYear`, the mapper clamps values to legacy runtime ranges:

- `Population` -> `ushort`
- `LaunchYear` -> `short`

This preserves compatibility with legacy-sized runtime/binary storage.

### 4.3 Legacy path not yet fully wired for spaceship roundtrip

The DTO/model/converter work is in place, but full binary adapter integration still depends on legacy bridge work in save/load adapters.

In other words: **schema and YAML mapping are ready; full end-to-end binary parity is still partially pending**.

### 4.4 Gameplay still legacy-incomplete

Spaceship gameplay assembly logic still includes TODO behavior in city production flow (legacy feature gap), so persistence is prepared ahead of full gameplay implementation.

---

## 5) PlayerGuid + UnitsDestroyedBy: Legacy Bridge

### 5.1 Stable identity introduction

`PlayerGuid` is now persisted and mapped. This decouples references from list order and reduces fragility compared to index-only legacy references.

### 5.2 Dual-path compatibility for destroyed-by stats

Both paths are supported:

1. **Legacy/index path**
   - `UnitsDestroyedBy` (index-oriented list)
2. **Preferred stable path**
   - `UnitsDestroyedByByPlayerGuid` (dictionary keyed by `PlayerGuid`)

### 5.3 Why this is a legacy bridge

Legacy logic and existing data often use player indices. The new Guid map is resolved back to internal index-based runtime structures where needed, keeping current behavior compatible while enabling migration toward stable references.

---

## 6) YAML Converter Registration Changes

Standard YAML pipelines now include spaceship grid conversion support:

- Writer registration in [src/Persistence/Yaml/YamlWriter.cs](src/Persistence/Yaml/YamlWriter.cs)
- Reader registration in [src/Persistence/Yaml/YamlReader.cs](src/Persistence/Yaml/YamlReader.cs)

This ensures spaceship grids are serialized/deserialized in a compact human-readable form.

---

## 7) What Remains Legacy-Coupled (Checklist)

The following points remain intentionally coupled to legacy behavior:

- Runtime numeric ranges (`ushort`/`short`) for spaceship fields
- Binary field layout expectations from `SaveData` offsets/blocks
- Index-based runtime arrays for some cross-player stats (with Guid bridge layered on top)
- Existing gameplay TODO paths for spaceship progression

---

## 8) Recommended Next Steps

1. Complete adapter-level binary roundtrip verification for spaceship fields.
2. Add focused tests for:
   - spaceship YAML -> runtime -> YAML roundtrip
   - clamp behavior edge cases
   - legacy `0` grid symbol compatibility
3. Document merge/precedence rules when both legacy index stats and Guid-based stats are present but inconsistent.

---

## 9) Practical Interpretation

Current status can be summarized as:

- **Data model modernization is in place** (DTO + YAML format + converters).
- **Legacy compatibility is intentionally preserved** (clamping, index bridges, SaveData alignment).
- **Some end-to-end legacy integration remains pending**, mainly in full binary adapter parity and gameplay completion.
