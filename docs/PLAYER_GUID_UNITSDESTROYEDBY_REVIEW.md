# Review: PlayerGuid + UnitsDestroyedBy Migration

## Goal

This change prepares the persistence structure for more stable, future-proof references:

1. **Stable player identity** via `PlayerGuid` (independent of list positions).
2. **Inter-player relationship data** (`UnitsDestroyedBy`) additionally modelled **GUID-based**.
3. Continue supporting the legacy/index structure so existing code paths do not break.

---

## Summary of Changes

- `Player` now has a persistable `PlayerGuid`.
- `UnitsDestroyedBy` was changed internally from `byte[16]` to **`ushort[8]`** (one counter per target player).
- `PlayerDto` now contains an additional GUID-based structure:
  - `UnitsDestroyedByByPlayerGuid: Dictionary<Guid, long>`
- During loading, GUID-based mapping is resolved to the internal index structure.
- During saving, the GUID map is additionally generated from the internal index structure.

---

## What Was Changed

## 1) DTO Layer

### File
- [src/Persistence/Model/PlayerDto.cs](../src/Persistence/Model/PlayerDto.cs)

### Changes
- New field `PlayerGuid` added.
- New field `UnitsDestroyedByByPlayerGuid` added.
- Existing `UnitsDestroyedBy` retained (compatible index path).

### Why
- `Id` is position-dependent (`Players` list), `PlayerGuid` is stable.
- For later flexible player counts, GUID-based cross-references are more robust.
- Parallel operation (index + GUID) reduces migration risk.

---

## 2) Domain / Persistence Contracts

### Files
- [src/Persistence/Model/IPlayer.cs](../src/Persistence/Model/IPlayer.cs)
- [src/Persistence/Model/IPlayerRestorable.cs](../src/Persistence/Model/IPlayerRestorable.cs)

### Changes
- `PlayerGuid` added to `IPlayer`/`IPlayerRestorable`.
- `UnitsDestroyedBy` type changed from `byte[]` to `ushort[]`.

### Why
- Uniform availability of stable identity in the mapping layer.
- `UnitsDestroyedBy` is modelled as a per-player counter; `ushort` is the appropriate counter type.

---

## 3) Runtime Model `Player`

### Files
- [src/Player.cs](../src/Player.cs)
- [src/Persistence/Game/Player.Persistence.cs](../src/Persistence/Game/Player.Persistence.cs)

### Changes
- Backing field `_playerGuid` added (`Guid.NewGuid()` as default).
- `_unitsDestroyedBy` changed to `ushort[8]`.
- Explicit getters/setters via `IPlayer`/`IPlayerRestorable` adjusted.

### Why
- `PlayerGuid` must be present in the runtime model to keep persistence roundtrips stable.
- Relationship counters across player pairs should not be stored as raw byte slots.

---

## 4) Player Mapping

### File
- [src/Persistence/Model/PlayerDtoMapper.cs](../src/Persistence/Model/PlayerDtoMapper.cs)

### Changes
- `PlayerGuid` is taken from the DTO (a new GUID is generated if the DTO GUID is empty).
- During writing, `PlayerGuid` is returned in the DTO.
- `BuildUnitsDestroyedByArray(...)` changed to `ushort[8]`.
- Clamp for `UnitsDestroyedBy` now to `0..ushort.MaxValue`.

### Why
- Identity must remain stable across roundtrips.
- Count values should not be prematurely limited by `byte`.

---

## 5) GameState Mapping (GUID ↔ Index)

### File
- [src/Persistence/Model/GameStateDtoMapper.cs](../src/Persistence/Model/GameStateDtoMapper.cs)

### Changes
- New resolution after `MapPlayers(...)`:
  - `ResolveUnitsDestroyedByByPlayerGuid(dto, players)`
- During loading:
  - `UnitsDestroyedByByPlayerGuid` is mapped via `PlayerGuid` to the target index.
  - Values are written to the internal `ushort[]` (with clamping).
- During saving:
  - `UnitsDestroyedByByPlayerGuid` is additionally generated for each player.

### Why
- GUID→index resolution requires **all players simultaneously**; therefore it belongs in the GameState mapper.
- This keeps the player mapping local and simple, while cross-player references remain correct.

---

## 6) Tests and Mocks

### Files
- [xunit/src/Mocks/MockedIPlayer.cs](../xunit/src/Mocks/MockedIPlayer.cs)
- [xunit/src/persistence/Model/PlayerDtoMapperTest.cs](../xunit/src/persistence/Model/PlayerDtoMapperTest.cs)
- [xunit/src/persistence/Model/GameStateDtoMapperTest.cs](../xunit/src/persistence/Model/GameStateDtoMapperTest.cs)

### Changes
- Mock supports `PlayerGuid` and `ushort[] UnitsDestroyedBy`.
- Player mapping test extended with `PlayerGuid`; `UnitsDestroyedBy` fixtures adjusted (8 instead of 16, `ushort`).
- GameState mapping test extended with GUID-based `UnitsDestroyedByByPlayerGuid` data.

### Why
- Tests verify that both the new identity and the GUID map roundtrip correctly.

---

## Compatibility / Behaviour

- **Index path remains present** (`UnitsDestroyedBy: List<long>`), so existing YAML structures do not immediately break.
- **New preferred structure** is GUID-based (`UnitsDestroyedByByPlayerGuid`).
- When both are present:
  - GUID-based resolution is mapped to the internal structure.
  - Clamping protects against overflow in the legacy-adjacent runtime model.

---

## Why This Architecture Was Chosen

1. **Stability:** GUID is robust against reordering of the `Players` list.
2. **Incremental migration:** Old and new paths can coexist.
3. **Clear responsibility:** Cross-player resolution happens at the GameState level.
4. **Safety:** Values are clamped at boundaries, but stored internally with less restriction.

---

## Test Status

Executed:
- `dotnet test ... --filter "FullyQualifiedName~PlayerDtoMapperTest|FullyQualifiedName~GameStateDtoMapperTest"`

Result:
- **4/4 tests passed**.

---

## Open Follow-Up Items (optional)

- Extend docs in [docs/YAML_SAVE_COMPLETENESS.md](YAML_SAVE_COMPLETENESS.md) (GUID map as preferred schema).
- Optional: document additional merge rule for cases where index list and GUID map are simultaneously inconsistent.

