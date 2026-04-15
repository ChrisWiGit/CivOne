# Full Spaceship Gameplay Implementation Plan

## Purpose

This document describes what is still required to implement the spaceship feature end-to-end in gameplay, persistence, UI, and legacy binary compatibility.

Current branch status: YAML schema support exists, but gameplay and binary adapter integration are still partial.

---

## 1. Current State (What is already done)

### Implemented

- Spaceship production buildings exist:
  - [src/Buildings/SSStructural.cs](src/Buildings/SSStructural.cs)
  - [src/Buildings/SSComponent.cs](src/Buildings/SSComponent.cs)
  - [src/Buildings/SSModule.cs](src/Buildings/SSModule.cs)
- Apollo gate for availability exists in production filtering:
  - [src/Player.cs](src/Player.cs#L263-L265)
- YAML DTO model exists:
  - [src/Persistence/Model/SpaceShipDto.cs](src/Persistence/Model/SpaceShipDto.cs)
  - [src/Persistence/Model/SpaceShipGridMap2d.cs](src/Persistence/Model/SpaceShipGridMap2d.cs)
  - [src/Persistence/Yaml/SpaceShipGridMapYamlTypeConverter.cs](src/Persistence/Yaml/SpaceShipGridMapYamlTypeConverter.cs)
- Player runtime fields exist:
  - [src/Player.cs](src/Player.cs#L60-L62)
- Player YAML mapping exists:
  - [src/Persistence/Model/PlayerDtoMapper.cs](src/Persistence/Model/PlayerDtoMapper.cs#L83-L190)

### Not implemented yet

- Component placement/gameplay logic is still TODO:
  - [src/City.cs](src/City.cs#L1218)
- Spaceship UI/menu is disabled:
  - [src/Screens/GamePlay.cs](src/Screens/GamePlay.cs#L119)
- Binary save/load bridge is missing in adapter/interfaces:
  - [api/src/IGameData.cs](api/src/IGameData.cs)
  - [src/SaveDataAdapter.cs](src/SaveDataAdapter.cs)
  - [src/SaveDataAdapter.Get.cs](src/SaveDataAdapter.Get.cs)
  - [src/SaveDataAdapter.Set.cs](src/SaveDataAdapter.Set.cs)
  - [src/Game.LoadSave.cs](src/Game.LoadSave.cs)

---

## 2. Required Work to Reach “Fully Implemented”

## Phase A — Domain Rules and State Model

1. Define authoritative spaceship domain rules:
   - Valid slot topology in 12x12 grid.
   - Allowed counts and constraints for `Structural`, `Component`, `Module`.
   - Population/capacity formula and launch prerequisites.
2. Add helper/service APIs (recommended):
   - `ISpaceShipRulesService`
   - `ISpaceShipAssembler`
   - `ISpaceShipProgressService`
3. Keep player data as source of truth:
   - `SpaceShipGrid`
   - `SpaceShipPopulation`
   - `SpaceShipLaunchYear`

Deliverable: deterministic validation and assembly behavior independent of UI.

---

## Phase B — Production Integration (Core Gameplay)

1. Replace TODO in city production flow:
   - Update [src/City.cs](src/City.cs#L1213-L1221) to apply built part to player spaceship state.
2. Convert `ISpaceShip` production item to component type:
   - `SSStructural` -> `SpaceShipComponentType.Structural`
   - `SSComponent` -> `SpaceShipComponentType.Component`
   - `SSModule` -> `SpaceShipComponentType.Module`
3. Add failure/overflow behavior:
   - If no valid slot is available, define exact outcome (reject build, refund shields, or nearest-slot placement).
4. Trigger post-build recalculation:
   - Recompute population capacity and launch readiness after each component addition.

Deliverable: building a spaceship part changes real game state, not only messaging.

---

## Phase C — UI and Player Interaction

1. Enable the menu entry currently disabled:
   - [src/Screens/GamePlay.cs](src/Screens/GamePlay.cs#L119)
2. Add a dedicated spaceship screen:
   - grid visualization (12x12)
   - counts and required/remaining parts
   - population estimate
   - launch status and launch action
3. Add player feedback:
   - notifications after part placement
   - readiness warning/explanation if launch is blocked

Deliverable: player can inspect and launch spaceship from in-game UI.

---

## Phase D — Launch, Travel, and Victory Flow

1. Define launch transition:
   - set `SpaceShipLaunchYear`
   - freeze or snapshot relevant ship parameters
2. Implement travel-time computation and arrival year.
3. Integrate with win condition processing:
   - check each turn if arrival is reached
   - trigger spaceship victory sequence
4. Define interaction with existing victory/endgame systems.

Deliverable: complete spaceship victory path works in live games.

---

## Phase E — Binary Save/Load Parity (Legacy Bridge)

1. Extend `IGameData` with spaceship fields:
   - per-player grid payload
   - `SpaceShipPopulation[8]`
   - `SpaceShipLaunchYear[8]`
2. Implement adapter get/set logic:
   - [src/SaveDataAdapter.Get.cs](src/SaveDataAdapter.Get.cs)
   - [src/SaveDataAdapter.Set.cs](src/SaveDataAdapter.Set.cs)
   - map 8 player blocks from `SaveData.SpaceShips[1462]`
3. Wire save and load in [src/Game.LoadSave.cs](src/Game.LoadSave.cs):
   - write player spaceship runtime state into adapter
   - restore state from adapter back to players
4. Preserve compatibility with existing offsets in [src/IO/SaveData.cs](src/IO/SaveData.cs#L140-L149).

Deliverable: spaceship state roundtrips correctly in legacy `.SVE` files.

---

## Phase F — AI and Balance

1. AI priority rules for spaceship parts after Apollo.
2. AI launch decision policy.
3. Balance checks for production cost and race timing.

Deliverable: AI can participate in spaceship race and finish games.

---

## 3. Test Plan (Required)

## Unit tests

- Grid placement validity and rejection cases.
- Mapping tests for component type conversion.
- Population and launch-year calculations.
- YAML roundtrip of `SpaceShipDto`.

## Integration tests

- Build part in city -> player spaceship state changes.
- Save YAML -> Load YAML -> state unchanged.
- Save SVE -> Load SVE -> state unchanged.

## Gameplay tests

- Full flow: Apollo -> build parts -> launch -> victory.
- Regression: non-spaceship gameplay unaffected.

---

## 4. Recommended Implementation Order

1. **Phase A** (rules/service contracts)
2. **Phase B** (production integration in city flow)
3. **Phase C** (screen and menu enablement)
4. **Phase D** (launch and victory)
5. **Phase E** (legacy binary parity)
6. **Phase F** (AI behavior)
7. Comprehensive tests and polish

---

## 5. Legacy-Coupled Areas to Track Carefully

These areas must remain explicit during implementation:

- Fixed legacy binary storage ranges (`ushort`, `short`).
- `SaveData.SpaceShips[1462]` block structure and offsets.
- Coexistence of modern YAML model with old binary representation.
- Backward loading behavior for old saves that may not contain meaningful spaceship data.

---

## 6. Definition of Done

Spaceship is “fully implemented” when all conditions are true:

- Building spaceship parts updates player spaceship state in-game.
- Player can inspect and launch spaceship from UI.
- Launch can produce victory via turn progression.
- YAML and SVE save/load both preserve spaceship state exactly.
- AI can build/launch spaceship.
- Automated tests cover domain, persistence, and gameplay flow.
