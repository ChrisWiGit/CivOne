# Plan: Civilization Score

## Scope

Reconcile CivOne's civilization score system: close the gap between the *live* score (computed fresh by
`ICivilizationScoreService` for report screens) and the *persisted* score (`Player.CivilizationScore`, a `ushort`
that is never recomputed during play), and decide what — if anything — should still be read from / written to the
original `.SVE` binary layout for this field.

**Not** in scope: replicating the original's turn-by-turn score accumulation event-for-event. CivOne already ships
a different, simpler formula (recomputed on demand from current state rather than accumulated history); this plan
treats that as a deliberate design choice to preserve, not a bug to fix, unless product decides otherwise (see
Design §1).

---

## Current state

| Layer | Status |
|---|---|
| Live formula | `Services/CivilizationScoreService.cs` — citizens + cities + population + advances + wonders + gold, recomputed on every call, human-only call sites |
| Consumers of the live formula | `Screens/Reports/CivilizationScore.cs` (F9 report), `Screens/Reports/TopLeaderScreen.cs`, `Services/HallOfFame/HallOfFameEntryComposerService.cs` — all call `Human` explicitly |
| Persisted field | `Player._civilizationScore` (`ushort`), exposed as `IPlayer.CivilizationScore` (`src/Player.cs:67,752`) |
| Persisted field — write path | **only** `PlayerDtoMapper.FromDto` on YAML load (`src/Persistence/Mapper/PlayerDtoMapper.cs:87`) |
| Persisted field — read path | `PlayerDtoMapper.ToDto` on YAML save (`:165`) — writes back whatever was last loaded, `Game.PlayerGameStateAdapter.CivilizationScore` (`src/Game.cs:434`) — used by the palace trigger |
| Palace upgrade trigger | `HumanCivScorePalaceTrigger.ShouldTrigger` (`src/Services/Palace/HumanCivScorePalaceTrigger.cs:35`) compares `player.CivilizationScore` — i.e. the **persisted**, not live, value |
| SVE binary struct | `SaveData.CivilizationScore[8]` fixed `ushort[8]`, byte offset **1864:1879** (`src/IO/SaveData.cs:99`) — matches the original layout exactly |
| SVE binary read/write | **absent** — no `Score`/`CivilizationScore` token anywhere in `SaveDataAdapter.Get.cs` or `SaveDataAdapter.Set.cs` |
| Tests | none found for `CivilizationScoreService`, `CivilizationScore` report, or `HumanCivScorePalaceTrigger` |

### Consequences of the current state

1. **The palace-upgrade-by-score trigger is effectively dead in a freshly started game.** `_civilizationScore`
   starts at its field default (`0`) and is written **only** by `PlayerDtoMapper.FromDto`. Nothing in gameplay
   (city growth, wonder completion, turn processing) ever calls the setter. A game that is never loaded from a
   YAML save keeps `CivilizationScore == 0` for its entire session, so
   `HumanCivScorePalaceTrigger.ShouldTrigger` can never return `true` (threshold is `1 + n² + n ≥ 1`).
2. **YAML save does not persist the actual score.** `PlayerDtoMapper.ToDto` reads `player.CivilizationScore`
   (`:165`), i.e. the same frozen field — so saving a game writes back the value from the *last load* (or `0` for
   a game that was never loaded), not what `ICivilizationScoreService.TotalScore(player)` would currently report.
   Loading that save and computing the live score elsewhere via the service therefore produces a **different**
   number than what is stored in the `CivilizationScore` field of the same save.
3. **The SVE binary format has the field but nothing behind it.** `SaveData.CivilizationScore[8]` faithfully
   reserves the original's 8×`ushort` block, but it is dead struct space: importing an original `.SVE` never
   populates `_civilizationScore` from it, and exporting never writes it — the 16 bytes at offset 1864 round-trip
   as whatever the binary buffer already contained (zero-filled on a freshly allocated struct).
4. **Two independent, non-interoperable scoring paths coexist**: the live service (human-only call sites, but the
   formula itself is generic — nothing stops calling it for an AI `Player`) and the persisted raw field (read by
   the palace trigger through `IPlayerGameState`, and round-tripped through YAML). They can never agree because
   nothing bridges them.

---

## Reference: original behaviour (reverse-engineered, from the CivOne-rajko decompilation)

CivOne-rajko (`/home/christian/projekte/CivOne-rajko`) is a line-for-line decompilation of the original DOS
binary — a separate, read-only reference repository, not a dependency of CivOne. File:line references below are
relative to that repo.

### 1. Storage

`Player.Score` (`short`), one per slot, `CivState.Players[8]` (`src/CivGame/CivState/Player.cs:26`,
`src/CivGame/CivState/CivState.cs:28`). Reset to `0` for every slot at new-game setup
(`src/CivGame/Game/StartGameMenu.cs:709`).

### 2. Continuous per-turn accumulation (all civilizations, human and AI alike)

Once per city per turn, during end-of-turn city processing, for **every** player slot:

```
Score += (city.ActualSize + happyBonus) - unhappyCount
```

(`src/CivGame/Game/CityWorker.cs:4036`; `unhappyCount`/`happyBonus` derived from government type, difficulty
level and empire size, `CityWorker.cs:1538-1548`, `:1574`).

Once per turn, for every completed World Wonder, **+25** to the owner's score (any civilization):

```
Players[wonderOwner].Score += 25
```
(`src/CivGame/Game/Segment_1238.cs:526`)

Once per turn, **human only**: `Score -= 10 * PollutedSquareCount` (`Segment_1238.cs:543`).

### 3. Final "Achievements" recompute (human only)

`Overlay_20.F20_0000_0ca9` is called **exclusively** with `HumanPlayerID`
(`Segment_2c84.cs:811`, `Segment_1403.cs:4078`, `Segment_1238.cs:581,781` — game end, retirement, spaceship
arrival, conquest, palace-level check). It zeroes `Players[0].Score`
(`Overlay_20.cs:1187`) and recomputes from scratch:

| Term | Formula | Ref |
|---|---|---|
| Population | Σ over all cities: `ActualSize + happyBonus - unhappyCount` | `Overlay_20.cs:1296-1298` |
| Wonders | **+20** per wonder owned (one-time, not per-turn here) | `:1416` |
| Spaceship | `successRate × population / 2`, only if arriving this year | `:1450-1484` |
| Pollution | **−10** per polluted square | `:1520-1522` |
| Peace | `+ min(peaceTurnCount * 3, 100)` | `:1529-1544` |
| Future Tech | **+5** per Future Tech researched | `:1559-1561` |

Clamped to `≥ 0` (`:1592`). An alternate "retirement bonus" formula (difficulty, turn count, opponent count) can
override this total under conditions gated by a flag at `DS:0xb884` — not fully resolved in the RE pass, called
out there as an open point.

### 4. Hall of Fame

Rating = `(DifficultyLevel + 2) * Score / 50` (approx.; see `HallOfFame.cs:385-387,557-562`), stored with name,
nation, year, population — **human only**, keyed off `HumanPlayerID` explicitly.

### 5. `.SVE` field

`Players[i].Score` (`Int16`) is serialized for all 8 slots in a flat per-field block, alongside `CityCount`,
`UnitCount`, `LandCount`, `SettlerCount`, `TotalCitySize`, `MilitaryPower`, `Ranking`, `TaxRate` — same block shape
CivOne's `SaveData.cs` already mirrors byte-for-byte (offsets 1736–1863 immediately preceding `CivilizationScore`
at 1864, `src/IO/SaveData.cs:91-99`).

### Key differences from CivOne's current formula

- Original: accumulated **over time**, per turn, for **every** civilization; wonders add flat `+25`/turn while
  owned (not a one-time `+50`); pollution/peace/future-tech/spaceship terms exist; happiness math is tied to
  government type and difficulty.
- CivOne: recomputed **from current state** on demand, human-only call sites, no time-accumulation, no pollution
  / peace / future-tech / spaceship terms, flat one-time wonder weight (`+50` each), gold converted to score
  (`/25`) — a term the original does not have at all.

These are not bugs relative to each other — CivOne's design intentionally trades "faithful turn-by-turn replica"
for "always consistent, always derivable from current state, no event hooks to maintain." See Design §1 for
whether that trade should be revisited.

---

## Design decisions

### 1. Keep the live-recompute formula as the source of truth *(recommended, needs confirmation)*

CivOne's formula is simpler to reason about and cannot drift out of sync with game state (no accumulation bugs,
no missed event hooks, no save/load duplication risk — the exact class of bug that motivates
[plan-replayHistory.prompt.md](plan-replayHistory.prompt.md) Design §6-7 for a *different* subsystem). Porting the
original's turn-accumulated formula (population delta every turn, wonder +25/turn, pollution/peace/future-tech/
spaceship terms, government-dependent happiness) would be a substantial, mostly-cosmetic effort for a number that
already has a working, simpler replacement.

**Recommendation:** do not port the original accumulation model. Instead fix the *plumbing* around the existing
live formula (Design §2-4) so persistence and gameplay agree with it. Flag as **open question for the user**:
whether product wants the classic per-turn/pollution/peace/future-tech/spaceship terms as an optional "classic
scoring" mode later — that would be a new, additive phase, not a fix.

### 2. Palace trigger must read the live score, not the frozen field

`Game.PlayerGameStateAdapter.CivilizationScore` (`src/Game.cs:434`) currently forwards the persisted
`IPlayer.CivilizationScore`. Change it to call `ICivilizationScoreService.TotalScore(player)` directly, so
`HumanCivScorePalaceTrigger` reacts to the player's actual current score instead of a value that is `0` for the
entire lifetime of a non-reloaded game.

This requires `PlayerGameStateAdapter` (or its caller) to have access to an `ICivilizationScoreService` instance —
inject via `CivilizationScoreServiceFactory.CreateDefault()` at the same seam other consumers use, or thread the
existing instance through if `Game` already owns one.

### 3. Persisted `CivilizationScore` becomes a save-time snapshot, not live gameplay state

Keep `PlayerDto.CivilizationScore` / `SaveData.CivilizationScore[8]` — dropping them would diverge further from
the original layout and from tools that may expect the field. Redefine its contract precisely:

- **On save** (`PlayerDtoMapper.ToDto`, and the future SVE writer): compute
  `ICivilizationScoreService.TotalScore(player)` at serialization time and write *that*, not the stale field.
- **On load** (`PlayerDtoMapper.FromDto`): still populate `_civilizationScore` from the DTO/SVE bytes — it is
  the correct value for whatever `player.CivilizationScore` (`IPlayer` raw accessor) means going forward: "score
  as of last save," a display/back-compat value, never a live gameplay input after Design §2's fix removes the
  only gameplay consumer.
- Net effect: the raw field becomes a pure **I/O snapshot** — always correct immediately after a save, always
  slightly stale during play (identical semantics to the original's on-disk field between two turns, incidentally
  — the original only writes to disk at save time too).

### 4. Wire up the SVE binary path

`SaveDataAdapter.Set.cs` currently has no code path touching `CivilizationScore[8]` at all — add one, mirroring
the neighbouring fixed-`ushort[8]` fields (`Ranking`, `TaxRate`) already handled there: write
`ICivilizationScoreService.TotalScore(player)` per slot (not the possibly-stale `_civilizationScore`, consistent
with Design §3). `SaveDataAdapter.Get.cs`: read the 8 values into `_civilizationScore` per player, same pattern as
the neighbouring fields.

Because CivOne's formula differs from the original (Reference, "Key differences"), a value written by CivOne and
later read by the original DOS binary (or vice versa) will show a **different number** than either program would
compute itself — this is unavoidable without porting the original formula (Design §1) and should be documented as
a known limitation, not silently hidden.

### 5. Human-only stays human-only, but the formula itself remains generic

Every current call site (`CivilizationScore` report, `TopLeaderScreen`, `HallOfFameEntryComposerService`) passes
`Human` explicitly — matching the original's practical scope (Reference §3-4). `ICivilizationScoreService
.TotalScore(Player)` takes any `Player`, so nothing structural prevents scoring AI players (e.g. for a future
debug/spectator screen); no change needed here, just noting it is already possible if ever wanted.

---

## Phases

### Phase 0 — Tests as a regression net (prerequisite)

Add `xunit/src/Services/CivilizationScoreServiceTests.cs` covering the current formula term-by-term (citizen
weighting incl. happy/unhappy/red-shirt edge cases, city/advance/wonder/gold weights, `RatingPercent` rounding and
the `threshold ≤ 0` guard) **before** touching any call site, so Phase 1-2 changes cannot silently alter the
displayed score.

### Phase 1 — Fix the palace trigger (Design §2)

- `Game.PlayerGameStateAdapter.CivilizationScore` → live `ICivilizationScoreService.TotalScore(_player)`.
- Add `HumanCivScorePalaceTriggerTests` — a live (non-loaded) game state reaching the threshold now triggers;
  today it structurally cannot (this is the regression test proving the bug existed).

### Phase 2 — Save-time snapshot (Design §3)

- `PlayerDtoMapper.ToDto`: replace `CivilizationScore = player.CivilizationScore` with a freshly computed value
  (needs `ICivilizationScoreService` available at mapping time — check whether the mapper already receives
  services via constructor injection or needs a new one).
- Extend `PlayerDtoMapperTests` (or add one) asserting a round-trip: build a player with known cities/advances/
  wonders/gold, `ToDto`, assert `CivilizationScore == service.TotalScore(player)` — not whatever `FromDto` set
  earlier in the same test.

### Phase 3 — SVE binary wiring (Design §4)

- `SaveDataAdapter.Set.cs`: write `CivilizationScore[8]` per slot from the live score, following the existing
  loop shape used for `Ranking`/`TaxRate`.
- `SaveDataAdapter.Get.cs`: read `CivilizationScore[8]` into `_civilizationScore` per slot.
- Regression test: load an original `.SVE` with known non-zero score bytes, assert `player.CivilizationScore`
  (raw) matches; save a CivOne game, assert the written bytes equal `service.TotalScore(player)` per slot.

### Phase 4 (deferred, needs product decision) — "Classic" scoring mode

Only if the open question in Design §1 comes back "yes": port the original's turn-accumulated formula
(population delta per turn for every civ, +25/turn per wonder while owned, pollution/peace/future-tech/spaceship
terms, government-dependent happiness) as a second `ICivilizationScoreService` implementation, selectable
independently of the save format. Not scheduled until that decision is made.

---

## Test plan

- `CivilizationScoreServiceTests`: every weighted term individually; `RatingPercent` rounding
  (`MidpointRounding.AwayFromZero`) and the `threshold ≤ 0 → 0` guard; `ArgumentNullException` on `null` player.
- `HumanCivScorePalaceTriggerTests`: threshold formula `1 + n² + n` at `n = 0,1,2`; AI players never trigger
  regardless of score; a freshly started (non-loaded) human game reaching the threshold triggers (Phase 1
  regression).
- `PlayerDtoMapperTests`: `ToDto` score matches a freshly computed value, not a value set earlier by `FromDto` in
  the same round-trip (Phase 2 regression).
- SVE round-trip: load a real `.SVE` with known `CivilizationScore` bytes at offset 1864; save a CivOne game and
  verify the byte block matches the live per-slot score (Phase 3).
- Manual/UI: F9 report, `TopLeaderScreen`, and `HallOfFameScreen` still show identical numbers to each other for
  the same game state after Phases 1-3 (they already share the same service call, so this should be a no-op
  check, not a fix).

---

## Remaining verification items

- **V1 — Formula parity, product decision.** Confirm with the user/maintainer whether CivOne's simplified,
  state-derived score is the intended permanent design (Design §1), or whether the original's turn-accumulated
  formula with pollution/peace/future-tech/spaceship terms should eventually be ported (Phase 4). This plan
  proceeds on the assumption that the current formula stays, only its plumbing gets fixed.
- **V2 — Mapper service access.** Verify how `PlayerDtoMapper` is constructed/injected today (`_valueSanitizer`,
  `_governmentResolver` are already fields) to decide the cleanest way to give it `ICivilizationScoreService` for
  Phase 2, without turning the mapper into a god object.
- **V3 — Cross-format score mismatch.** Once Phase 3 ships, an SVE round-tripped through CivOne will carry a
  CivOne-formula score in a field the original binary would interpret as its own formula's output. Decide whether
  this needs a user-facing note (e.g. in `SveSaveCompatibilityService`) similar to the truncation warning pattern
  used for replay data.
