# Plan: Event Sourcing / Replay Recording

## Summary

Record every player action — human **and** AI — along with the initial RNG seed so that any game can be fully replayed from scratch. Because all AI logic is driven by a single deterministic RNG (`Common.Random`), replaying the same seed with the same recorded actions reproduces the game identically, regardless of whether the AI code has changed since the original session.

This also lays the foundation for future network multiplayer: actions from remote players can be fed through the same recorder/player pipeline.

---

## Why Record AI Actions Too?

- AI code can change between versions. Without recorded AI actions, old replays would diverge on updated builds.
- Future network multiplayer requires synchronising actions from all players (including remote humans and AIs) through a common event stream.
- Recording AI actions costs little extra and makes the system fully self-contained.

---

## Architecture Overview

### What Gets Recorded

| Source | Actions | Path |
|--------|---------|------|
| Human player | Unit movement (8 dirs), Orders (FoundCity, BuildRoad, etc.), EndTurn | Path A + Path B |
| AI player | Unit movement, Fortify, SkipTurn, Disband, Settler Orders | Path A + Path B |
| Engine | Barbarian spawn, disasters | Reproduced from RNG seed — not recorded |

### What Does NOT Need to Be Recorded

- RNG draws (reproduced from the seed)
- Rendering / UI state
- Read-only queries

### Two Dispatch Paths (Critical Finding)

**Path A — Orders / GameTask** (`GameTask.Enqueue(Orders.*)`):  
Capturable via the existing `GameTask.Started` static event.  
Covers: FoundCity, BuildRoad, BuildIrrigation, BuildMines, BuildFortress, ClearPollution, Wait — for both human and AI (Settlers).

**Path B — Direct unit calls** (bypass GameTask entirely):  
- Human: `GameMap.MoveTo(relX, relY)` → `Game.ActiveUnit.MoveTo(relX, relY)` (all 8 directions + right-click)  
- AI (non-Settler): `unit.MoveTo()`, `unit.Fortify = true`, `unit.SkipTurn()`, `Game.DisbandUnit(unit)` — called directly in `src/AI.cs`

Both paths must emit events to the recorder.

---

## Data Model (`src/Replay/`)

### `ActionType` Enum

```
MoveUnit          // Param1=relX, Param2=relY
FoundCity
BuildRoad
BuildIrrigation
BuildMines
BuildFortress
ClearPollution
WaitUnit
FortifyUnit
SkipUnit
DisbandUnit
SetResearch       // Param1=advance index
SetProduction     // Param1=cityId, Param2=production item id
ChangeTaxRate     // Param1=new rate
EndTurn
```

### `GameAction` Record

| Field | Type | Description |
|-------|------|-------------|
| `Turn` | `int` | Game turn number |
| `PlayerIndex` | `int` | 0=human, 1+=AI or remote |
| `UnitId` | `int` | Which unit (−1 if not applicable) |
| `Type` | `ActionType` | What happened |
| `Param1` | `int` | Action-specific (e.g. relX) |
| `Param2` | `int` | Action-specific (e.g. relY) |
| `RngCounterBefore` | `int` | `Common.Random.Counter` snapshot — used for divergence detection during playback |

### `GameReplay` Container

| Field | Type | Description |
|-------|------|-------------|
| `InitialSeed` | `ushort` | Passed to `Common.SetRandomSeed()` at game start |
| `StartTurn` | `int` | For partial replays starting from a save |
| `GameVersion` | `string` | Informational only |
| `Actions` | `List<GameAction>` | Ordered action log |

### File Format

- YAML, following the `.cos` save-file conventions
- Extension: **`.cor`** (CivOne Replay)
- Location: `saves/replay/`
- Safe writes via `IAtomicFileReplacementService` (same as saves)

---

## Implementation Phases

### Phase 1 — Data Model & Serialisation

1. Create `src/Replay/ActionType.cs` — enum.
2. Create `src/Replay/GameAction.cs` — struct/record.
3. Create `src/Replay/GameReplay.cs` — container.
4. Create `src/Replay/ReplaySerializer.cs` — YAML read/write, following `YamlSaveGameStateWriter` pattern.
5. Create `src/Replay/IReplayRepository.cs` + `ReplayRepository.cs` — load/save `.cor` to `saves/replay/` using `IAtomicFileReplacementService`.

*Reference:* `src/Persistence/YamlSaveGameStateWriter.cs`, `src/Services/SaveGamePathProvider.cs`

### Phase 2 — Recorder

6. Create `src/Replay/IActionRecorder.cs`:
   - `void Start(ushort seed, int startTurn)`
   - `void Record(GameAction action)`
   - `GameReplay Stop()`
   - `bool IsActive { get; }`
7. Create `src/Replay/ActionRecorder.cs` — in-memory implementation, thread-safe.
8. Register as singleton (attach to `RuntimeHandler` or DI).

### Phase 3 — Hook Path A (Orders / GameTask)

9. In `src/RuntimeHandler.cs`, subscribe to `GameTask.Started` static event.
10. When the started task is an `Orders` instance, read player index, unit id, action type, parameters, RNG counter → `_recorder.Record(...)`.

### Phase 4 — Hook Path B (Direct Unit Calls)

**4a — Human movement** (`src/Screens/GamePlayPanels/GameMap.cs`, line 254 `MoveTo()`):  
11. After `Game.ActiveUnit.MoveTo(relX, relY)` returns `true`, emit `MoveUnit` to recorder.

**4b — AI direct calls** (`src/AI.cs`):  
12. Inject `IActionRecorder` into `AI` class.
13. Wrap the 4 direct call sites with recorder emit calls:
    - `unit.MoveTo(relX, relY)` → emit `MoveUnit`
    - `unit.Fortify = true` → emit `FortifyUnit`
    - `unit.SkipTurn()` → emit `SkipUnit`
    - `Game.DisbandUnit(unit)` → emit `DisbandUnit`

> **Note:** Do NOT convert AI direct calls to `GameTask`/`Orders` — that would require refactoring the AI's synchronous decision loop and is out of scope.

### Phase 5 — Seed Capture

14. Add `public ushort Seed` field to `src/Random.cs` (set in constructor — one line).
15. In `src/RuntimeHandler.cs` after `Common.SetRandomSeed(...)`: call `_recorder.Start(Common.Random.Seed, 0)`.
16. In `src/Game.LoadSave.cs` and `src/Game.LoadYaml.cs` after their respective `SetRandomSeed` calls: call `_recorder.Start(seed, game.GameTurn)` to support partial replays.

### Phase 6 — Playback (`src/Replay/ReplayPlayer.cs`)

17. Load `GameReplay`, call `Common.SetRandomSeed(replay.InitialSeed)`.
18. Feed actions back in order:
    - Path A actions → `GameTask.Enqueue(Orders.*)` factory methods
    - Path B actions → call `unit.MoveTo()`, set `unit.Fortify`, etc. directly
19. After each action: assert `Common.Random.Counter == action.RngCounterBefore` (divergence detection).

### Phase 7 — UI Integration

20. Add `StartRecording` / `StopRecording` game tasks (analogous to `Show.AutoSave`).
21. Hook `StartRecording` into the new-game flow in `RuntimeHandler`.
22. Add "Save Replay" to end-of-game screen or as a keyboard shortcut.
23. Add "Load Replay" entry in the main menu (alongside Load Game).

### Phase 8 — Tests (`xunit/src/replay/`)

24. `ActionRecorderTests` — start recorder, record 3 synthetic actions, stop → verify `GameReplay` contents.
25. `ReplaySerializerTests` — round-trip YAML serialise/deserialise.
26. `ReplayPlayerTests` — fixed seed + 5 hard-coded actions → replay → assert `GameTurn` and city position.
27. `RngCheckpointTests` — verify `RngCounterBefore` values match during playback.

---

## Files to Create

| Path | Purpose |
|------|---------|
| `src/Replay/ActionType.cs` | Action type enum |
| `src/Replay/GameAction.cs` | Single recorded action |
| `src/Replay/GameReplay.cs` | Replay container |
| `src/Replay/IActionRecorder.cs` | Recorder interface |
| `src/Replay/ActionRecorder.cs` | In-memory recorder |
| `src/Replay/ReplaySerializer.cs` | YAML serialisation |
| `src/Replay/IReplayRepository.cs` | Repository interface |
| `src/Replay/ReplayRepository.cs` | File-based `.cor` repository |
| `src/Replay/ReplayPlayer.cs` | Playback engine |
| `xunit/src/replay/ActionRecorderTests.cs` | Recorder unit tests |
| `xunit/src/replay/ReplaySerializerTests.cs` | Serialisation tests |
| `xunit/src/replay/ReplayPlayerTests.cs` | Playback integration tests |

## Files to Modify

| Path | Change |
|------|--------|
| `src/Random.cs` | Add `public ushort Seed` field |
| `src/Common.cs` | Expose seed after `SetRandomSeed()` |
| `src/RuntimeHandler.cs` | Call `_recorder.Start(seed)` after `SetRandomSeed`; subscribe `GameTask.Started` |
| `src/Game.LoadSave.cs` | Notify recorder of seed on `.sve` load |
| `src/Game.LoadYaml.cs` | Notify recorder of seed on `.cos` load |
| `src/AI.cs` | Inject `IActionRecorder`; emit events at 4 direct call sites |
| `src/Screens/GamePlayPanels/GameMap.cs` | Emit `MoveUnit` after successful `MoveTo()` |

---

## Scope Boundaries

**In scope:**
- Recording all human and AI player actions
- `.cor` file format (YAML, `saves/replay/`)
- Full playback from a recorded replay
- RNG counter divergence detection
- Unit and integration tests

**Out of scope:**
- Replay visualisation (fast-forward, scrubbing, timeline UI)
- Network multiplayer transport layer
- Converting AI direct calls to GameTask (separate refactoring)
- Engine-generated events (barbarian spawns, disasters) — reproduced by RNG

---

## Open Questions

1. **Partial replay from mid-game**: Combine a `.cor` file with a `.cos` save to replay from an arbitrary turn? Requires recording the save's RNG seed as `InitialSeed` and `StartTurn`. Recommended: yes, supported from the start.

2. **UI-only actions** (tax rate, research selection, production): currently no `Orders`/`GameTask` path. Should be hooked in the relevant screen handlers and added to `ActionType`. Recommended: include in Phase 4 scope.

3. **Replay speed control**: Run replay at 1× game speed or as fast as possible (headless)? Recommend: configurable speed flag on `ReplayPlayer`, default = fast/headless.
