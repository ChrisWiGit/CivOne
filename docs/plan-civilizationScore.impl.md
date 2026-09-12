# Plan: Civilization Score — Original Mode

Implementation plan for this repository.

The reverse-engineering notes in [plan-civilizationScore.prompt.md](plan-civilizationScore.prompt.md) and the
follow-up analyses are used here **only as a source of facts about the original DOS game**. Their file and line
references point into a separate decompilation repository and do not apply to CivOne. Every CivOne-side statement
below was verified against the current working tree.

## Goal

Replace CivOne's invented scoring formula with the original's, fix the two systems that consume the score
(palace upgrades, save files), and correct the city happiness model that the score depends on.

## Decisions taken

| # | Decision |
|---|---|
| D1 | **Port the original formula.** CivOne's own formula (advances ×10, wonders ×50, gold/25) is dropped. |
| D2 | **Recompute only, no per-turn accumulation.** See §2. |
| D3 | **Happiness is corrected to match the original**, and the score consumes that one corrected model. Planned separately — see [plan-cityCitizenService.impl.md](plan-cityCitizenService.impl.md). |
| D4 | **The 40 % luxury reference is reproduced, side-effect free.** See §3. |
| D5 | **Spaceship success rate is read live**, not frozen. See §4. |
| D6 | Palace threshold is corrected to the original formula including difficulty scaling. See §5. |
| D7 | **The world-conquest condition ships in both forms, switchable** — latched flag and derived-at-score-time. See §10. |
| D8 | **SVE stays byte-compatible with the original; YAML is free.** YAML may carry richer per-player data as long as it collapses back to the SVE layout on export. See §11. |

---

## 1. The formula

```
score  =  Σ over the player's cities: (Size + Happy − Unhappy)        // happiness at the 40 % luxury reference, D4
        + 20 × wonders currently held
        + (successRate × SpaceShipPopulation) / 2                     // only in the spaceship's arrival year
        − 10 × PollutedSquaresCount                                   // global counter, not per player
        + min(PeaceTurns × 3, 100)                                    // only while Year > 0 (AD), see below
        + 5  × FutureTechCount

score  =  clamp(score, 0, short.MaxValue)

if the player has eliminated every other civilization:
    score = max(score, 400 + 100 × AiOpponentCount + 2 × (550 − GameTurn))
```

Notes on the terms:

- `successRate` is a percentage 0–100; `SpaceShipPopulation` is stored in units of ten thousand (~1–99). Both
  factors are pre-scaled, so the term stays in the low thousands and cannot overflow the 16-bit field.
- The spaceship term is clamped to `0` before it is added (a negative product contributes nothing).
- Pollution and peace turns are **global** values in the original, not per-player. CivOne matches this.
- **The peace bonus has a year gate:** it is skipped entirely while `Year <= 0`, i.e. in BC years. This is
  belt-and-braces in the original — the peace counter is itself only advanced while `Year > 0` — but reproduce
  the gate, not just the counter, so the two cannot drift apart.
- **The `min(…, 100)` cap sits on the score term, not on the counter.** The counter runs uncapped and a second
  consumer reads it with a different cap (`× 2`, max 60). Do not clamp `Game.PeaceTurns` itself.
- **The peace counter resets only on a declaration of war** — not on the first attack — and the reset is skipped
  when either party is player 0. Wars against barbarians therefore do not break the peace streak. Check CivOne's
  reset site against this.
- The world-conquest bonus *replaces* the score when it is larger — it does not add to it. The condition is a
  latched flag in the original, set when the active-civilization mask equals "human plus barbarians only".
  CivOne ships that and a derived variant behind a switch (D7, §10), latched by default.
- **There is no `SCORING COMPLETED` state to reproduce.** The original guards it with `SpaceshipFlags & 0x100`,
  which is an off-by-eight bug: the bit is `1 << (playerID + 8)` for slot 0, i.e. the barbarians, who never build
  a spaceship. The branch is unreachable. See §5.

### Inputs, all available in CivOne today

| Term | Source |
|---|---|
| City size, happy, unhappy | `City.GetCitizenTypes()` → [CityCitizenService.cs](../src/Screens/Services/CityCitizenService.cs) — **corrected first, see [plan-cityCitizenService.impl.md](plan-cityCitizenService.impl.md)** |
| Wonders held | `player.Cities.Sum(c => c.Wonders.Length)` |
| Spaceship population / arrival year | `Player.SpaceShipPopulation`, `Player.SpaceShipLaunchYear`; arrival year already derived in [Game.cs:551-555](../src/Game.cs#L551-L555) |
| Spaceship success rate | `SpaceShipScreenDataFactory.CalculateSuccessProbability` ([:58](../src/Services/SpaceShip/SpaceShipScreenDataFactory.cs#L58)) — needs to be reachable outside the screen data factory |
| Polluted squares | `IGlobalWarmingService.PollutedSquaresCount` |
| Peace turns | `Game.PeaceTurns`, advanced in [Game.cs:593](../src/Game.cs#L593) |
| Future tech | `Player.FutureTechCount` |
| Difficulty, turn, opponents | `Game.Difficulty`, `Game.GameTurn`, player list |

No new persistent game state is required.

---

## 2. Why recompute only (D2)

The original keeps a `short Score` per player and accumulates into it every turn (city size delta, `+25` per
owned wonder, `−10` per polluted square). That accumulation is dead weight: the turn loop **zeroes the score of
all eight players** at its start, and the achievements routine then zeroes and overwrites the human player's
field again — it does not add to it.

So no accumulated value ever survives a turn boundary, for the human player *or* for the AI slots. Every score
the original can display is a snapshot of the current state.

Therefore: implement the recompute, skip the per-turn accumulation. This costs **no** fidelity at all — not even
for AI players, contrary to what an earlier revision of this plan claimed — needs no turn hook across all eight
players, and adds no new state.

Worth recording because it shows the accumulation is vestigial: the two paths disagree with each other. The
per-turn accumulation credits `+25` per wonder, the recompute `+20`. Only the recompute is observable, so §1
uses `20`.

### Slot 0 is the barbarians, not the human player

An earlier revision of this plan claimed the routine's hard-coded write to `Players[0].Score` "coincides with the
human player". That is wrong on both sides. In the original, `HumanPlayerID` is forced into `1..7` at game start
(a rolled `0` is re-rolled), and civilization id `0` is the barbarian nation. CivOne has the same layout:
`_players[0]` is built from the civilization with `PreferredPlayerNumber == 0`
([Game.LoadSave.cs:163-167](../src/Game.LoadSave.cs#L163-L167)), with an explicit "don't need for barbarians"
guard.

So `Players[0].Score` is the **barbarian slot used as a scratch variable**. Two consequences:

- The score routine never writes the human player's `Score` field at all. What sits in
  `Players[HumanPlayerID].Score` is only the per-turn accumulator described above — the vestigial one.
- All eight slots are written to the SVE file, so a byte-compatible save has to pick a slot mapping rather than
  inherit one. Decided in D8 / §11: CivOne mirrors the original — human score into slot 0, zeros elsewhere.

The other original quirk we do not reproduce: the routine mutates real game state while measuring (see §3).

---

## 3. The 40 % luxury reference (D4)

The original does not evaluate happiness with the player's actual tax and luxury sliders. It temporarily forces
`TaxRate = 6 − ScienceRate`, which pins `10 − Science − Tax` to a constant `4` — i.e. **happiness is always
measured as if the luxury slider were at 40 %**. The purpose is clear: the score cannot be inflated by pushing
luxuries up for one turn before the score is taken.

We reproduce that reference value, but **not** the way the original obtains it. The original mutates persistent
state — it saves and restores only `TaxRate`, while running the full per-city simulation, leaving other side
effects (trade values, growth and production thresholds) changed until the next real end of turn. That is a
disassembly artefact, not a design we want.

Implementation: make the happiness computation accept the luxury rate as a parameter instead of reading it from
the player. `City.Luxuries` currently derives from `player.LuxuriesRate` and feeds `CityCitizenService`
([:167](../src/Screens/Services/CityCitizenService.cs#L167)); the scoring path passes the fixed reference rate,
every other caller passes the player's real rate and behaves exactly as before.

---

## 4. Spaceship success rate (D5)

The original stores the success rate in a player field that is only rewritten when the player opens the spaceship
screen, so between visits it holds a stale value. CivOne computes the same quantity live in
`SpaceShipScreenDataFactory`.

We use the live value. Reproducing the frozen field would mean a new persistent player field plus a save-format
entry, for a difference that is only observable if the player opens the spaceship screen and then dismantles
parts without reopening it. Recorded as a known deviation (§8).

Work needed: `CalculateSuccessProbability` is a private static on the screen-data factory. Lift it into the
spaceship service so both the screen and the score service can call it, without duplicating the formula.

---

## 5. Palace upgrade threshold (D6)

Original: with `n = PalaceLevel + 1` and `D = Difficulty` (0 easiest … 5 hardest)

```
threshold(n) = floor((7 − D) × n × (n + 1) / 2 / 2) + 1
```

CivOne today ([HumanCivScorePalaceTrigger.cs:34](../src/Services/Palace/HumanCivScorePalaceTrigger.cs#L34)):

```csharp
int n = player.Palace.UpgradeCount;
long threshold = 1L + ((long)n * n) + n;
```

At `D = 3` the original reduces to exactly `n² + n + 1`. So CivOne already has the right curve, hard-coded to
difficulty 3 and with `n = UpgradeCount` instead of `UpgradeCount + 1`. Two small corrections, not a rewrite:

1. `n = UpgradeCount + 1`.
2. difficulty coefficient `(7 − D)`, requiring `Difficulty` on `IPlayerGameState` (or a second interface member
   supplied by `Game.PlayerGameStateAdapter`).

The original does **not** cap the palace level at 37 — the counter keeps incrementing; only the redraw is skipped
beyond 37. That is harmless there because the threshold grows with `n`. CivOne needs no cap either:
`PalaceData.UpgradeCount` is `7 levels × 4 + 3 gardens × 3 = 37` by construction, and `CanUpgrade`
([PalaceData.cs:79](../src/PalaceData.cs#L79)) already returns `false` at the maximum.

### The three flags in the palace check

All three are now understood; none is a mystery gate.

| Flag | Meaning | CivOne |
|---|---|---|
| `ImprovementFlags0 & 0x1` | Not a gate at all — it is the Palace *improvement* bit. The loop uses it to find the city that holds the palace. | CivOne locates the palace city directly; nothing to port. |
| `GameSettingFlags & 0x80` | Set at game start as part of `GameSettingFlags = 0xfa` and never cleared anywhere in the decompilation. | Always satisfied — now **established**, not assumed. |
| `SpaceshipFlags & 0x100` | **Dead code — an off-by-eight bug in the original.** | Not reproduced. Nothing to implement. |

The `0x100` case needs spelling out, because it also removes a rule an earlier revision of this plan had already
adopted. The high spaceship bit is `1 << (playerID + 8)`, so `0x100` is slot 0 — the barbarians, who never build
a spaceship. The bit is never set and every `& 0x100` test in the original falls into the "not set" branch. The
place that gets it right tests `0xfe00`, i.e. bits 9–15 for players 1–7.

Therefore: **palace upgrades are never blocked, and `SCORING COMPLETED` is unreachable.** Do not implement a
"player has a spaceship" predicate for either the palace trigger or the score service.

For the record, since it took work to establish: the bit *would* be set on the first completed spaceship part —
not at launch, and not when the spaceship screen is opened. The launch sets the low bit `1 << playerID`. Moot,
given the above, but it settles what the flag means if it is ever needed.

---

## 6. Current state and defects being fixed

| Area | Where | State |
|---|---|---|
| Score formula | [CivilizationScoreService.cs](../src/Services/CivilizationScoreService.cs) | CivOne's own formula — replaced by §1 |
| Consumers | [CivilizationScore.cs](../src/Screens/Reports/CivilizationScore.cs) (F9), [TopLeaderScreen.cs](../src/Screens/Reports/TopLeaderScreen.cs), [HallOfFameEntryComposerService.cs](../src/Services/HallOfFame/HallOfFameEntryComposerService.cs) | all resolve the service through `CivilizationScoreServiceFactory.CreateDefault()` and pass the human player |
| Stored field | `Player._civilizationScore` (`ushort`), read via `IPlayer.CivilizationScore` ([Player.cs:752](../src/Player.cs#L752)) | written **only** by `IPlayerRestorable.CivilizationScore` ([Player.Persistence.cs:240](../src/Persistence/Game/Player.Persistence.cs#L240)), whose only caller is [PlayerDtoMapper.cs:87](../src/Persistence/Mapper/PlayerDtoMapper.cs#L87) (YAML load) |
| YAML save | [PlayerDtoMapper.cs:165](../src/Persistence/Mapper/PlayerDtoMapper.cs#L165) | writes the stored field straight back |
| Palace trigger | [Game.cs:434](../src/Game.cs#L434) → the trigger | reads the stored field |
| SVE struct | [SaveData.cs:99](../src/IO/SaveData.cs#L99), `fixed ushort CivilizationScore[8]`, offset 1864:1879 | reserved, never read or written |
| Tests | [PlayerDtoMapperTest.cs](../xunit/src/persistence/Model/PlayerDtoMapperTest.cs) round-trips `CivilizationScore = 333` | none for the score service, the report or the trigger |
| Happiness tests | [CityCitizenServiceImplTests.cs](../xunit/src/CityCitizenServiceImplTests.cs) (35), [CityHappy.cs](../xunit/src/CityHappy.cs) (11) | substantial and green — the baseline for the separate happiness plan |

Defects that follow:

1. **The palace upgrade trigger is dead in any game that was never loaded from YAML.** `_civilizationScore`
   defaults to `0`, nothing in gameplay ever sets it, and the lowest threshold is `1`.
2. **YAML saves store a stale score** — whatever the last load produced, `0` for a fresh game, while the reports
   on screen show a different number.
3. **The SVE score block is dead space.**
4. **The score and the reports can never agree**, because nothing connects the service to the field.

After this plan the score is a derived value again — but derived by the *original's* formula, so the number in
the save file is meaningful to the original binary as well.

---

## 7. Phases

### Phase 1 — Correct the happiness model (D3)

**Moved to its own document: [plan-cityCitizenService.impl.md](plan-cityCitizenService.impl.md).**

The score's largest term is `Σ (Size + Happy − Unhappy)`, so it is only as correct as `CityCitizenService` —
but correcting that model turned out to be a larger piece of work than the score port itself: it touches ten
consumers and changes how the game plays. It therefore ships as its own deliverable, with its own decisions
(H1–H7), its own open points and its own tests. This plan simply consumes the result.

What the score needs from it:

| Need | Covered by |
|---|---|
| A per-city `Size + Happy − Unhappy` that matches the original | the whole of that plan |
| No positive contribution from a negative unhappy count | its §3 — per-stage normalisation and clamp ordering |
| The luxury rate as a caller-supplied parameter, for the 40 % reference (§3 here) | its H7 / step 9 |

**Phase 2 must not start before that plan's switch is flipped on** (its §7). Until then the running game still
uses the old happiness model, and a score computed against it would be wrong in exactly the way this plan exists
to fix.

### Phase 2 — The score service

- Rewrite `CivilizationScoreService.TotalScore` to §1. Drop the citizen/city/advance/wonder/gold weights and the
  `GoldPerScorePoint` constant with them.
- The signature currently takes the concrete `Player`, which is hard to construct in a test (the existing
  `MockedIPlayer` is an `IPlayer`, not a `Player`). Narrow it to the state the formula reads — cities, wonders,
  spaceship fields, future tech — and inject the global inputs (`IGlobalWarmingService`, peace turns, difficulty,
  turn, opponent count) rather than reaching for `Game.Instance` inside the service. Resolve any singleton
  fallback lazily, per `CLAUDE.md`.
- Lift `CalculateSuccessProbability` out of `SpaceShipScreenDataFactory` into the spaceship service (§4).
- `RatingPercent` is unaffected; keep it and its rounding behaviour.
- Implement the peace bonus's `Year > 0` gate (§1). There is no spaceship gate — `0x100` is dead code (§5).
- Take the world-conquest condition as an injected `WorldConquestPredicate` (D7, §10); the service must not know
  which of the two variants it holds.
- **Wonder counting is already correct.** The original reads the wonder→city table directly, so obsolete wonders
  count, captured cities count for the captor, and destroyed cities drop out. CivOne's
  `player.Cities.Sum(c => c.Wonders.Length)` gives the same three answers — *provided* CivOne's wonder list
  travels with a captured city and disappears with a destroyed one. Verify that once (Q9); no formula change.
- Tests `xunit/src/Services/CivilizationScoreServiceTests.cs`: every term in isolation; the arrival-year gate on
  the spaceship term; the BC/AD gate on the peace term; the `0` floor; the world-conquest bonus replacing rather
  than adding, and only when larger; `short.MaxValue` saturation; `ArgumentNullException` on a null player;
  wonder counting across capture and destruction.

### Phase 3 — Palace trigger (D6)

- Apply the two corrections from §5; expose `Difficulty` through `IPlayerGameState`. `n = UpgradeCount + 1` is
  confirmed: the original's counter starts at 0, is incremented by exactly one per upgrade, and is queried as
  `PalaceLevel + 1`. The coefficient `(7 − D)` runs `7…3` over the original's difficulties and continues to `2`
  at CivOne's Deity level.
- **No spaceship gate**, and no end-of-game block either — `0x100` is unreachable (§5). Remove the current
  end-of-game condition rather than replacing it.
- Side note for whoever touches the Hall of Fame: the original derives its rank from the same counter, as
  `(PalaceLevel >> 1) − 1`. Out of scope here; recorded so it is not re-derived from scratch.
- Make the trigger read the live score. Once `IPlayer.CivilizationScore` is derived (Phase 4),
  [Game.cs:434](../src/Game.cs#L434) needs no change — but assert that in a test rather than assuming it.
- Tests `xunit/src/Services/Palace/HumanCivScorePalaceTriggerTests.cs`: the threshold at `n = 0, 1, 2` across
  difficulties 0–5, exact-threshold boundary triggers, AI players never trigger, null player / null palace /
  `!CanUpgrade` return `false`, and the regression case — a human player in a game that was never loaded from a
  save, above the threshold, triggers. That case fails on today's code, which is the proof the defect existed.

### Phase 4 — Persistence

**Derived getter.** `IPlayer.CivilizationScore` ([Player.cs:752](../src/Player.cs#L752)) computes the live score
instead of returning `_civilizationScore`, clamped into `ushort`, with the service resolved lazily:

```csharp
private readonly ICivilizationScoreService? _civilizationScoreService;   // ctor parameter, default null
private ICivilizationScoreService CivilizationScoreService =>
    _civilizationScoreService ??= CivilizationScoreServiceFactory.CreateDefault();
```

Every producer — palace trigger, YAML save, SVE save — becomes correct through this one change, with no new
constructor parameter in `PlayerDtoMapper` (already 10 dependencies behind a `CA1707` suppression), none in
`Game.PlayerGameStateAdapter`, none in `Game.LoadSave`.

**Remove the stored field (D-Q3).** Once the getter is derived, nothing reads `_civilizationScore` and neither
save format needs it back on load (§11). Delete all three of it:

1. the `_civilizationScore` field on `Player` ([Player.cs:752](../src/Player.cs#L752) region),
2. `IPlayerRestorable.CivilizationScore` ([Player.Persistence.cs:240](../src/Persistence/Game/Player.Persistence.cs#L240)),
3. its only caller, the `FromDto` write in [PlayerDtoMapper.cs:87](../src/Persistence/Mapper/PlayerDtoMapper.cs#L87).

Keeping it as a write-only inbox was the alternative; it buys nothing and leaves a field that looks authoritative
and is not.

**YAML.** `PlayerDtoMapper.ToDto` needs no edit — it reads `player.CivilizationScore`, now live. The DTO keeps
its `CivilizationScore` member: YAML is unconstrained (D8) and a per-player score is useful for reports and
debugging. `FromDto` simply stops writing it back.

The existing round-trip assertion (`CivilizationScore = 333`,
[:341](../xunit/src/persistence/Model/PlayerDtoMapperTest.cs#L341)) **will fail**, because `ToDto` no longer
echoes what `FromDto` stored. That failure is the intended signal. Rewrite it: build a player with known state,
`ToDto`, assert the DTO carries the computed score.

**SVE binary**, following the existing `TaxRate` pattern end to end:

1. [SaveDataAdapter.Get.cs](../src/SaveDataAdapter.Get.cs) —
   `private ushort[] GetCivilizationScore() => GetArray<ushort>(nameof(SaveData.CivilizationScore), 8);`
2. [SaveDataAdapter.Set.cs](../src/SaveDataAdapter.Set.cs) —
   `private void SetCivilizationScore(ushort[] values) => SetArray(nameof(SaveData.CivilizationScore), values);`
3. [SaveDataAdapter.cs](../src/SaveDataAdapter.cs) — public `ushort[] CivilizationScore` property mirroring
   `TaxRate` (`:192`), plus the member on `IGameData`.
4. [Game.LoadSave.cs](../src/Game.LoadSave.cs) — save side only, among the other per-player arrays (`:70-90`).
   **There is no load side.** The value is derived, and `IPlayerRestorable.CivilizationScore` is gone; reading
   the block back would only reintroduce the stale-value defect this plan exists to remove. Use the
   `CVS.CheckedUInt16` guard the neighbouring fields use on the way out.

**Which slot gets what (D8, §11).** SVE mirrors the original: the human player's computed score into **slot 0**
— the barbarian scratch slot the original itself writes — and `0` in the other seven. This is what makes the file
loadable by the original binary without surprises, and it costs nothing, because the original zeroes the whole
block at the start of its next turn loop anyway.

YAML is not bound by this: it may carry a score per player. `Game.LoadSave`'s SVE writer is the single place that
collapses that to the slot-0 layout, so a YAML-originated game saved as SVE produces the same bytes as one that
was SVE all along.

Tests: `SaveDataAdapterTests` round-trips the block at offset 1864 per slot with synthesised bytes; a separate
test pins the collapse — a game with distinct per-player scores writes them all to `0` except slot 0, which
carries the human player's.

### Phase 5 — dropped

An earlier revision reserved a phase for reproducing the per-turn accumulation so that AI scores would be
available. It is dropped: the original zeroes every player's score at the start of each turn loop (§2), so the
accumulation is unobservable for AI players too. If AI scores are ever wanted, run the same recompute over an AI
player — no accumulator, no turn hook.

---

## 8. Known deviations from the original

Documented deliberately, not hidden:

1. **No per-turn accumulation** (§2) — unobservable in the original for every player, so this costs nothing.
2. **Spaceship success rate is live, not frozen at the last screen visit** (§4). Note that the original stores
   this value in the SVE file, so CivOne's SVE output differs from the original's at that field. The original
   also always writes it into `Players[HumanPlayerID]`, even when the screen is opened for an AI player — a
   genuine bug we do not reproduce.
3. **The 40 % luxury reference is computed without mutating game state** (§3). The original leaves trade and
   growth side effects behind until the next end of turn; we do not.
4. **The barbarian-slot scratch write is not reproduced** (§2). The original writes its result to
   `Players[0].Score`, the barbarian slot; CivOne writes each player's score to that player's own slot. See D8 / §11
   for the save-compatibility consequence.
5. **Two original bugs are not reproduced.** `SpaceshipFlags & 0x100` tests the barbarian slot instead of the
   human player's (off by eight), making the palace block and `SCORING COMPLETED` unreachable — we implement the
   reachable behaviour, i.e. neither (§5). And the spaceship success rate is always written into the human
   player's slot even when the screen is opened for an AI player.
6. **The `+25`/`+20` wonder inconsistency is not reproduced** (§2). The original's dead accumulation path used
   `+25`; only the recompute's `+20` is observable.
7. **Palace level is capped at 37 in CivOne**, where the original lets the counter run past it and only stops
   redrawing. Behaviourally equivalent, since the threshold grows with the level.

The happiness model carries its own deviation list — see [plan-cityCitizenService.impl.md](plan-cityCitizenService.impl.md) §10.

---

## 9. Open points

| # | Point | Blocks |
|---|---|---|
| Q1 | ~~Gate flag `0x80` semantics.~~ **Resolved**, see §5: set at game start as part of `0xfa` and never cleared, so always satisfied. `0x100` likewise resolved — "a spaceship exists", not "the game ended". | — |
| Q2 | ~~Keep `_civilizationScore` and `IPlayerRestorable.CivilizationScore` as a loaded-value inbox?~~ **Decided: remove them.** Nothing reads the field once the getter is derived, and both save formats ignore it on load (§11). Phase 4 deletes the field, the `IPlayerRestorable` member, and the `FromDto` write. | — |
| Q3 | ~~Is the world-conquest condition tracked, or derived at score time?~~ **Decided: ship both, switchable** (D7, §10). The two agree when the condition first becomes true and diverge only if a civilization appears afterwards — reachable in CivOne through the editor, not in the original. Latched is the default. | — |
| Q4 | ~~SVE slot mapping.~~ **Decided: SVE mirrors the original, YAML is free** (D8, §11). SVE writes the human player's score into slot 0 and zeroes the rest; YAML may carry per-player scores and collapses to that layout on SVE export. Cheap, because the original treats the whole block as volatile scratch. | — |

The happiness model carries its own open points — the reservoir refill reading, the luxury chain overlap and
wonder ownership — in [plan-cityCitizenService.impl.md](plan-cityCitizenService.impl.md) §11.

## 10. World-conquest condition, both forms (D7)

The original latches a flag when the active-civilization mask equals `(1 << HumanPlayerID) | 1` — the human plus
barbarians, i.e. every other civilization destroyed. Deriving the same condition from the player list at score
time gives the same answer **at the moment it first becomes true**, but the two diverge afterwards:

| | latched | derived |
|---|---|---|
| last rival destroyed | true | true |
| a new civilization appears later (civil war, respawn, editor) | stays true | falls back to false |

CivOne has a map editor and may grow civil-war-style mechanics, so the divergence is reachable here even though
it is not in the original. Ship both, selectable, with the original's behaviour as the default.

Per `CLAUDE.md`'s delegate chapter this is interchangeable behaviour, so it becomes two delegate classes behind
one native delegate type:

```csharp
// Predicate over the current player set; no service, no locator.
public delegate bool WorldConquestPredicate(IPlayerGameState state);

internal sealed class LatchedWorldConquestDelegate { ... }   // sets once, never clears
internal sealed class DerivedWorldConquestDelegate { ... }   // recomputed from the player list each call
```

`CivilizationScoreService` takes the delegate as a constructor parameter and never decides which one it is.
The selection sits in `CivilizationScoreServiceFactory`, driven by one `ISettings` entry
(`WorldConquestMode`, default = latched, matching the original). The latched variant needs one `bool` of state;
put it on the player alongside the other latched flags, not in the service, so it survives a save.

Tests: both delegates against the same player sequence — destroy all rivals (both true), then add a
civilization (latched still true, derived false); the factory returns the latched one by default.

## 11. Save formats (D8)

**SVE is the constraint, YAML is not.** The rule: an SVE file CivOne writes must be loadable by the original, and
any YAML file must collapse to that same SVE layout when the player saves as SVE.

That is cheaper than it looks, because the original's score block is effectively **volatile scratch**:

- the turn loop zeroes all eight `Score` fields at its start (§2), and
- the achievements routine recomputes and overwrites slot 0 during the human player's palace check, every turn.

So whatever CivOne writes into the block is discarded by the original before anything reads it. SVE compatibility
here means matching the *layout*, not the values — there is no value the original can be confused by.

Concretely:

| Format | What we write |
|---|---|
| SVE | The original's shape: the human player's computed score in **slot 0** (the barbarian scratch slot the original itself uses), `0` in the other seven. 8 × `ushort` at offset 1864, unchanged. |
| YAML | Whatever is useful — a per-player score for every slot, computed live. No compatibility constraint. |
| YAML → SVE | Collapse: take the human player's score, write it to slot 0, zero the rest. One function, tested. |

On **load**, both formats ignore the stored value entirely: `IPlayer.CivilizationScore` is derived (Phase 4), so
nothing needs to round-trip. This is what makes the whole thing safe — we never depend on a field the original
treats as scratch.

## 12. Happiness model — see the separate plan

How the corrected `CityCitizenService` lands (parallel subclass, factory switch, the `virtual` commit, the
reservoir traps and the time-boxed cleanup) is documented in
[plan-cityCitizenService.impl.md](plan-cityCitizenService.impl.md) §7. Nothing in it is score-specific.

## 13. Test plan (summary)

- Happiness tests live in the separate plan ([plan-cityCitizenService.impl.md](plan-cityCitizenService.impl.md) §12).
- `CivilizationScoreServiceTests` — every term, arrival-year gate, BC/AD peace gate, `0` floor, conquest bonus
  semantics, saturation, null argument, wonder counting across capture and destruction (Phase 2).
- `HumanCivScorePalaceTriggerTests` — threshold across difficulties 0–4, `n = UpgradeCount + 1`, boundaries, AI
  exclusion, null guards, and the never-loaded-game regression (Phase 3).
- `WorldConquestDelegateTests` — latched vs. derived across "all rivals destroyed, then a civilization appears";
  factory default is latched (§10).
- `PlayerDtoMapperTest` — rewritten score assertion; `FromDto` no longer writes the score back (Phase 4).
- `SaveDataAdapterTests` — score block at offset 1864 round-trips per slot; the SVE collapse is pinned (human
  score in slot 0, zeros elsewhere), and nothing reads the block back on load (Phase 4, §11).
- Manual: F9 report, `TopLeaderScreen` and `HallOfFameScreen` show the same number for the same state; city
  screens still behave sensibly once the happiness plan's switch is on.
