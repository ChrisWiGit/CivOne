# YAML Persistence Consolidated Status

Last updated: 2026-04-24
Scope: Consolidated view of YAML save/load work, replay coverage, legacy traceability, and remaining gaps.

## Executive Summary

YAML save/load support is implemented end-to-end and wired into the runtime load UI for `.cos` files.

The previously reported ReplayData heap-corruption blocker is now resolved in the active snapshot path:
- Replay data is now included again in game-state snapshots in [src/Persistence/GameStateHandler.cs](../src/Persistence/GameStateHandler.cs#L110).

Main remaining functional gap:
- ReplayData domain coverage is still partial (only 3 replay event domain classes exist), so YAML files containing other replay event kinds still fail to map back to runtime objects.

## What Is Confirmed Implemented

### 1. YAML Load Pipeline

The full YAML load pipeline is implemented and integrated:
- Map/runtime tile restoration
- Player and unit restoration framework
- YAML mapper dependency factory for load scenarios
- Runtime game reconstruction from GameState
- Load UI dispatch for `.cos` files

Reference: [docs/YAML_LOAD_IMPLEMENTATION.md](YAML_LOAD_IMPLEMENTATION.md)

### 2. YAML Save Completeness (Core State)

The following core areas are implemented and mapped:
- Players, cities, units, map tiles
- Options, advance origins, wonders, city names
- Seeds, turn/current player/human player
- Diplomacy and player statistics
- Spaceship DTO support
- Global warming and peace turns

Reference: [docs/YAML_SAVE_COMPLETENESS.md](YAML_SAVE_COMPLETENESS.md)

### 3. ReplayData Snapshot Inclusion (Heap Corruption Issue)

Status: resolved for YAML snapshot creation path.

Current code includes replay data in the snapshot:
- [src/Persistence/GameStateHandler.cs](../src/Persistence/GameStateHandler.cs#L110)

This removes the previous behavior where YAML saves dropped replay data entirely.

## Remaining Functional Gaps

### 1. ReplayData Domain Coverage Is Partial

Current runtime domain classes in [api/src/ReplayData.cs](../api/src/ReplayData.cs):
- Implemented: `CityBuilt`, `CityDestroyed`, `CivilizationDestroyed`

Still missing as domain classes:
- `WarDeclared`
- `PeaceMade`
- `AdvanceDiscovered`
- `UnitFirstBuilt`
- `GovernmentChanged`
- `WonderBuilt`
- `ReplaySummary`
- `CivRankings`
- `CityCaptured`

Mapping status in [src/Persistence/Mapper/ReplayDataDtoMapper.cs](../src/Persistence/Mapper/ReplayDataDtoMapper.cs):
- `ToDto`: only implemented domain classes are mapped.
- `FromDto`: throws `NotImplementedException` for missing replay kinds.

Impact:
- YAML files containing unsupported replay event types cannot complete DTO-to-domain mapping.

### 2. Spaceship Roundtrip Validation Still Incomplete

Implementation exists (DTO + mapping + YAML converter), but dedicated validation gaps remain:
- YAML -> runtime -> YAML roundtrip tests for spaceship payloads
- Clamp boundary tests (`Population` to `ushort`, `LaunchYear` to `short`)
- Legacy symbol compatibility test (`0` interpreted as empty)

References:
- [docs/YAML_LEGACY_TRACEABILITY_CHANGES.md](YAML_LEGACY_TRACEABILITY_CHANGES.md)
- [src/Persistence/Model/SpaceShipDto.cs](../src/Persistence/Model/SpaceShipDto.cs)
- [src/Persistence/Mapper/PlayerDtoMapper.cs](../src/Persistence/Mapper/PlayerDtoMapper.cs)

### 3. Binary Path Gap for Settler Wait State

Open item remains in binary save path:
- `MovesSkip` for settlers is not fully persisted in binary save format.

Reference:
- [src/Extensions.cs](../src/Extensions.cs)

### 4. Manual UI Smoke Test Pending

Still recommended:
- Place a `.cos` save in the configured saves folder.
- Load via in-game Load UI.
- Validate map, cities, units, players, and continue-play re-save behavior.

Reference: [docs/YAML_LOAD_IMPLEMENTATION.md](YAML_LOAD_IMPLEMENTATION.md)

## Legacy-Coupled but Intentional

These remain intentionally tied to legacy semantics:
- Spaceship runtime numeric ranges (`ushort` / `short`) with YAML-side clamping
- Some index-based runtime structures with GUID bridge layers
- Binary-layout-aligned field expectations for compatibility

Reference: [docs/YAML_LEGACY_TRACEABILITY_CHANGES.md](YAML_LEGACY_TRACEABILITY_CHANGES.md)

## Optional/Low-Priority Items

These are currently non-blocking and mostly reconstructable or not used by active logic:
- Score and peace history charts
- AI continent strategy arrays
- Strategic location policy/status arrays
- Land pathfinding cache arrays
- Debug switches

Reference: [docs/YAML_SAVE_COMPLETENESS.md](YAML_SAVE_COMPLETENESS.md)

## Recommended Next Execution Order

1. Complete missing ReplayData domain classes and enable full mapper cases.
2. Add focused spaceship roundtrip/boundary compatibility tests.
3. Execute manual `.cos` UI smoke test and document findings.
4. Decide whether binary export from GameState is required before implementing full binary writer path.

## Source Documents Consolidated

- [docs/YAML_TODO.md](YAML_TODO.md)
- [docs/YAML_SAVE_COMPLETENESS.md](YAML_SAVE_COMPLETENESS.md)
- [docs/YAML_LOAD_IMPLEMENTATION.md](YAML_LOAD_IMPLEMENTATION.md)
- [docs/YAML_LEGACY_TRACEABILITY_CHANGES.md](YAML_LEGACY_TRACEABILITY_CHANGES.md)
