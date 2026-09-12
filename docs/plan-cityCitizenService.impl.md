# Plan: City Happiness — Original Model

Implementation plan for `CityCitizenService` and the types around it.

This document is **self-contained**. It was split out of
[plan-civilizationScore.impl.md](plan-civilizationScore.impl.md), which needed a corrected happiness model as the
input to its score formula — but the work below stands on its own and ships on its own, whether or not the score
port ever lands.

Facts about the original DOS game come from a separate decompilation repository. File and line references into
that repository are deliberately **not** reproduced here; they do not apply to CivOne. Every CivOne-side
statement was verified against the current working tree.

## Goal

Replace CivOne's invented city-happiness base term with the original's, correct the modifiers that are wrong,
and add the two mechanisms CivOne is missing entirely (per-stage normalisation and the unhappy overflow
reservoir).

## Decisions taken

| # | Decision |
|---|---|
| H1 | **Port the original's base term.** CivOne's `6 − Difficulty` content limit plus `ApplyEmperorEffects` is replaced by the original's government- and empire-size-dependent formula. |
| H2 | **One model, not a scoring-only variant.** Every consumer of `GetCitizenTypes()` sees the corrected model. |
| H3 | **Red shirts stay** as CivOne's representation of the original's overflow reservoir — driven by the reservoir, not by the frozen 37-city literal. |
| H4 | **The reservoir refill ships in both readings, switchable** — halving and full drain — decided later by play testing. See §6. |
| H5 | **This lands as a parallel subclass** behind a factory switch, with its own test class; the existing model and its tests stay untouched until the switch flips. Time-boxed. See §7. |
| H6 | **Difficulty 5 (Deity) is CivOne's own**, and the formula is extrapolated to it. Marked as a balance decision, not fidelity. See §2. |
| H7 | **The luxury rate becomes a parameter** rather than being read from the player. Needed by the score's 40 % reference; harmless everywhere else. |

---

## 1. The original's model

For the human player:

```
base    = 14 − 2 × Difficulty
base    = ((GovernmentType / 2) + 2) × (base / 2)
E       = ((cityId % base) + CityCount − base) / base
unhappy = E + Size + Difficulty − 6                     // NOT clamped here — see §3

corruption = (trade × X × 3) / (Government × 20 + 80)
corruption = 0            if Government == Democracy
corruption = corruption/2 if Courthouse                  // Courthouse only — the Palace does not reduce it

lux     = clamp( ((10 − Science − Tax) × (trade − corruption) + 5) / 10, 0, trade )
lux    += Entertainers × 2
lux    += lux / 2   if Marketplace
lux    += lux / 2   if Bank
happy   = lux / 2
```

For AI players `unhappy = Size − 3` — and that is the only difference, see §5.

Two details of the luxury chain that are easy to get wrong: the `+5` sits **inside the numerator**
(`(A × B + 5) / 10`, i.e. rounding applied to the product, not afterwards), and `Entertainers × 2` is added
**before** Marketplace and Bank, so entertainer luxury is multiplied by them too. The clamp's upper bound is the
**raw** trade, while the product uses trade *minus* corruption — the two values are deliberately different.

### Modifiers — exact

| Modifier | Effect | Condition |
|---|---|---|
| Colosseum | `unhappy −= 3` | the building alone, no advance |
| Cathedral | `−6` with Michelangelo, else `−4` | outer gate: the advance **Religion** |
| Temple | `−2` with Mysticism, else `−1` with Ceremonial Burial, else `0` | `else if`, **not** additive |
| Oracle | a further `−2` with Mysticism, else `−1` | inside the Temple block, owner has the wonder |
| Hanging Gardens | `happy++` | owner has the wonder |
| Cure For Cancer | `happy++` | owner has the wonder |
| Shakespeare's Theatre | `unhappy = 0` | the wonder's city **is** this city — no owner check |
| J.S. Bach's Cathedral | `unhappy −= 2` | owner has the wonder **and** same continent as the wonder's city |

Corrections this forces on CivOne:

- **Michelangelo's Chapel is global for its owner, not continental.** The original's wonder-ownership helper
  checks ownership and obsolescence only — no geography. CivOne's `CathedralDelta`
  ([:449-467](../src/Screens/Services/CityCitizenService.cs#L449-L467)) restricts it to the city's continent;
  that check is removed. Only J.S. Bach is continent-bound, and CivOne already has that right.
- **Cathedral is inverted today.** The original is `−4` base and `−6` with Michelangelo; CivOne has it the other
  way round. And the whole Cathedral block is gated on the advance **Religion**, which CivOne does not check.
- **Temple/Oracle stay as they are.** The original is additive where CivOne doubles (`<<= 1`), but the numbers
  coincide: Mysticism gives `−2 −2 = −4` against CivOne's `−2 × 2 = −4`, Ceremonial Burial `−1 −1 = −2` against
  `−1 × 2 = −2`. No change needed — add a comment saying the forms differ but the results do not, so nobody
  "fixes" it later.
- **Wonder ownership follows the city.** Capturing a city transfers its wonder's effect to the captor. And
  obsolescence is checked against **all** civilizations 1–7: a wonder goes obsolete as soon as *any* player has
  the obsoleting advance, not only its owner. Verify CivOne's `HasWonderEffect` / `WonderObsolete` against both
  (Q3).
- **Shakespeare has no owner check** in the original — the wonder works for whoever holds the city. That falls
  out of "ownership follows the city" and matches CivOne's `_cityBuildings.HasWonder<>()`.

### War weariness

```
w        = (WomensSuffrage ? 0 : 1) + (Government == Democracy ? 1 : 0)
unhappy += w × AttackCapableUnitsAwayFromTheirHomeCity
```

Women's Suffrage removes the base point entirely and Democracy adds a second — so Democracy *with* Women's
Suffrage is as weary as Republic *without* it. CivOne's `ApplyDemocracyEffects`
([:348-365](../src/Screens/Services/CityCitizenService.cs#L348-L365)) uses `1` for Republic and `2` for Democracy
with no Women's Suffrage term; add it.

### Martial law detail

Beyond the unit list, the original also counts two unidentified city slots towards martial law, and it applies
the conversion only while `unhappy != 0`. The second half matters (CivOne's `UnhappyToContent` already no-ops on
zero); the two extra slots are not ported — recorded as deviation 4.

### Stage order — exact

Five normalisation calls (`N`), with exactly this between them:

```
1.  base term  (human: E formula / AI: Size − 3),  overflow → reservoir
    ── N ──
2.  happy = lux / 2
    ── N ──
3.  Colosseum, Cathedral (Religion gate), Temple (+ Oracle)
    ── N ──
4.  martial law  (Government < Republic)  XOR  war weariness  (Government >= Republic)
    ── N ──
5.  Hanging Gardens++, Cure For Cancer++, Shakespeare = 0, J.S. Bach −2
    ── N ──
```

Stage 4: martial law and war weariness are an `if/else` on government in the original, never both. CivOne calls
`ApplyMartialLaw` and `ApplyDemocracyEffects` in sequence
([Stage4:196-205](../src/Screens/Services/CityCitizenService.cs#L196-L205)); their internal guards happen to make
them mutually exclusive today, but make the exclusivity explicit.

Stage 5: **Shakespeare runs before the last `N`**, so the reservoir refill can partially undo its `unhappy = 0`.
In a large empire Shakespeare is therefore not absolute — which is why Q1 affects Shakespeare, not just the
reservoir in general.

---

## 2. How CivOne differs today (H1, H6)

CivOne today ([:237-240](../src/Screens/Services/CityCitizenService.cs#L237-L240)):

```
contentLimit = 1 + 5 − Difficulty                       // = 6 − Difficulty
unhappy      = max(Size − contentLimit, 0)              // = max(Size + Difficulty − 6, 0)
```

The original's per-city term contains that expression verbatim. The whole difference is one additive
empire-size penalty:

```
unhappy_original = unhappy_civone + E
E = trunc( ((cityId % base) + CityCount − base) / base )
base = ((GovernmentType / 2) + 2) × (7 − Difficulty)
```

Because `cityId % base ∈ [0, base−1]`, the numerator stays below `base` as long as `CityCount ≤ base`, so
**`E = 0` and the two formulations are bit-identical** up to that empire size. Beyond it, one city per `base`
crosses the threshold at a time, staggered by `cityId`.

| Difficulty | `base` Despotism / Monarchy / Republic | first city penalised | every city penalised |
|---|---|---|---|
| 0 Chieftain | 14 / 21 / 28 | 15 / 22 / 29 | 28 / 42 / 56 |
| 1 Warlord | 12 / 18 / 24 | 13 / 19 / 25 | 24 / 36 / 48 |
| 2 Prince | 10 / 15 / 20 | 11 / 16 / 21 | 20 / 30 / 40 |
| 3 King | 8 / 12 / 16 | 9 / 13 / 17 | 16 / 24 / 32 |
| 4 Emperor | 6 / 9 / 12 | 7 / 10 / 13 | 12 / 18 / 24 |
| 5 Deity *(CivOne only)* | 4 / 6 / 8 | 5 / 7 / 9 | 8 / 12 / 16 |

(`GovernmentType / 2 + 2`: Anarchy/Despotism → 2, Monarchy/Communism → 3, Republic/Democracy → 4.)

**The original has five levels, 0–4** — Chieftain … Emperor, clamped to `[0, 4]` in its menu. Rows 0–4 are
ported fact.

**Difficulty 5 (Deity) is CivOne's own addition** (H6), switchable via `Settings.DeityEnabled`
([Game.cs:189-190](../src/Game.cs#L189-L190)); `MaxDifficulty` is 5 only when it is on. Its row is an
**extrapolation** — `(7 − D)` simply continues to `2`, giving `base = 4 / 6 / 8` and an empire-size penalty from
5 cities under Despotism. That is steeper than anything the original can produce; flag it as a balance decision
to review in play, not as fidelity.

Everything here must therefore work for `Difficulty ∈ [0, MaxDifficulty]`, read from settings — never
hard-coded to 4 or 5. `CalculateCityStats` already gets this right with its "do not use `_game.MaxDifficulty`
here" comment ([:234-237](../src/Screens/Services/CityCitizenService.cs#L234-L237)); keep that property.

### CivOne's stand-in, and why replacing it generalises rather than discards

`ApplyEmperorEffects` ([:265-291](../src/Screens/Services/CityCitizenService.cs#L265-L291)) fires only at
difficulty ≥ 4 and from 13 cities, then downgrades one citizen (13–24), the whole city (≥ 25), and adds red
shirts from 37. It diverges structurally:

1. **Government type has no effect at all in CivOne.** In the original it scales `base` — Despotism → Monarchy
   is ×1.5, → Republic ×1.33. Largest single deviation, present on every difficulty.
2. **Onset.** The original penalises from `base + 1` cities on *every* difficulty; CivOne from a fixed 13 and
   only at difficulty ≥ 4.
3. **Growth.** `E` grows linearly and unbounded; CivOne's effect is capped.

But the constants are not arbitrary: `MinEmperorCityCount = 13`, `MinEmperorCityCountStage2 = 25`,
`MinRedShirtCityCount = 37` and the `/12` step in `NumberOfRedShirts`
([:297-307](../src/Screens/Services/CityCitizenService.cs#L297-L307)) are exactly the steps of
`trunc(((cityId % 12) + CityCount − 12) / 12)` — **the `E` formula with `base` frozen at 12**, i.e. Emperor +
Republic, exactly what the in-code comment claims. CivOne hard-coded one row of the table and lost the
government and difficulty dependency. Replacing `ApplyEmperorEffects` with `E` therefore generalises the
existing behaviour.

### What this changes in play

Republic and Democracy become noticeably stronger for large empires, Despotism weaker, and the AI loses a
handicap it was never meant to carry (§5). Existing saves will play differently. That is intended — and it is
why this ships and is reviewed on its own.

---

## 3. Normalisation after every stage — the missing piece

The original does not compute `happy` and `unhappy` in one pass. It calls a normalisation routine **five times**
— after the base term, after luxuries, after buildings, after martial law, after wonders. In full:

```
N(specialists):
    while (reservoir >= 0 && unhappy < reservoir):     // reservoir flows back
        reservoir--;  unhappy++
    happy   = clamp(happy,   0, Size)
    unhappy = clamp(unhappy, 0, Size)
    while (happy + unhappy > clamp(Size − specialists, 0, 99)):
        if reservoir > 0: reservoir--
        else:             happy = clamp(happy − 1, 0, Size)
        unhappy = clamp(unhappy − 1, 0, Size)
```

Without it the base term is simply wrong at the small end: `Size + Difficulty − 6` is `−5` for a size-1 city on
difficulty 0.

Two consequences the implementation must honour:

- **Order matters.** `E` is added *before* the lower clamp: `max(Size + Difficulty − 6 + E, 0)`, never
  `max(Size + Difficulty − 6, 0) + E`. Since `E ≥ 0` the two differ exactly where the base term is negative —
  i.e. for every small city in a large empire, which is the common case the empire-size penalty exists for.
- **Clamping is per stage, not once at the end.** A modifier that would drive `unhappy` below zero loses the
  surplus at that point; it does not carry over to offset a later increase.

CivOne's `CitizenTypes` bookkeeping already re-counts after every stage
([Stage1–Stage5](../src/Screens/Services/CityCitizenService.cs#L148-L216)) and its `DebugService.Assert(ct.Sum()
== _city.Size)` invariants enforce the ceiling structurally. What is missing is the reservoir flow-back and the
lower clamp ordering around `E`.

---

## 4. The unhappy overflow reservoir (H3, H4)

When `unhappy` exceeds the city size, the original does not discard the surplus. It parks the excess in a side
variable, sets `unhappy = Size`, and the normalisation routine **refills from that reservoir** whenever a
modifier later reduces `unhappy`. So a heavily over-unhappy city needs more Temple/Colosseum/luxury capacity
than its visible unhappy count suggests before anything improves.

There is no distinct very-unhappy *citizen type* in the original — its screens know three categories plus
specialists. But the semantics CivOne's red shirts express, "costs more to pacify", is a genuine mechanism,
implemented as a reservoir rather than as a type. **CivOne's red shirts are a legitimate representation of a
real mechanism**, not an invention, and stay (H3).

CivOne's red-shirt machinery is `WearRedShirt` plus the special cases in `UpgradeCitizens`
([:530-540](../src/Screens/Services/CityCitizenService.cs#L530-L540)) and `UnhappyToContent`
([:610-621](../src/Screens/Services/CityCitizenService.cs#L610-L621)). Rule: the *count* comes from the original
(`E` plus the reservoir), the *representation* stays CivOne's — red shirts are now driven by `unhappy` exceeding
`Size` instead of by the frozen 37-city literal.

---

## 5. AI players — one term, not one model

The `PlayerID == HumanPlayerID` branch sets the base term and **nothing else**. Everything after it — Colosseum,
Cathedral, Temple, Oracle, martial law, war weariness, all four wonders, all five normalisation calls — is
shared code and runs identically for AI cities with their own government.

So the difference is exactly one term:

```
human: unhappy = max(Size + Difficulty − 6 + E, 0)
AI:    unhappy = Size − 3
```

It is one `if (isHuman)` at a single site, and leaving it out is not neutral: CivOne currently applies the human
base — difficulty scaling *and* the empire-size penalty — to AI cities, a handicap the original's AI never
carries.

This is the **only** part of this plan with no effect on the civilization score, which sums the human player's
cities alone. Treat it as optional and drop it from the PR without hesitation if it complicates review.

---

## 6. Two readings of the reservoir refill, switchable (H4)

The refill loop's termination condition `unhappy < reservoir` makes the flow-back stop halfway — it ends as soon
as `unhappy` has caught up with what is left in the reservoir, so roughly half of a modifier's improvement is
immediately undone. The alternative reading is a **full drain**, where improvements do nothing at all until the
reservoir is empty. A swapped compare operand in the decompilation would produce exactly that difference, and
the decompilation repository holds no raw disassembly to settle it.

So ship both, select by setting, default to what the decompilation literally says (halving). Per `CLAUDE.md`'s
delegate chapter this is interchangeable behaviour:

```csharp
public delegate void ReservoirRefill(ref int unhappy, ref int reservoir);

internal sealed class HalvingReservoirRefillDelegate { ... }   // while (reservoir >= 0 && unhappy < reservoir)
internal sealed class FullDrainReservoirRefillDelegate { ... } // drain until reservoir is empty
```

The subclass takes the delegate; the setting (`ReservoirRefillMode`) selects it in the factory, never inside the
class. Because the refill runs inside all five normalisation calls, **both variants must be exercised by the
test suite**, including Shakespeare's Theatre (§1, stage 5).

**Settle it later by experiment in DOSBox** (Q1): Emperor, empire well above `base`, a city with a large
reservoir, then build a Colosseum. Full drain → `unhappy` does not move until the reservoir is exhausted.
Halving → `unhappy` drops immediately by about half the effect.

---

## 7. How this lands: a parallel subclass (H5)

The corrected model does **not** replace `CityCitizenService` in place. It arrives as
`OriginalCityCitizenService : CityCitizenService`, selected in the factory, so the current model and its tests
keep running unchanged until the switch is flipped.

Why the extra class is worth it:

- **The existing tests stop being a problem.** They keep guarding the old class, which stays valid and green.
  Nothing is expected to break, so *any* red test is a real regression — a much sharper signal.
- **The risky moment becomes one line.** Game behaviour changes when the factory switches, not when the code
  lands. Trivially revertible, and play-testable before anyone commits to it.
- **It composes with H4**, one level up: a switch at the class instead of at the delegate.

The pattern already exists here — [CityCitizenServiceImplShim.cs](../xunit/src/Mocks/CityCitizenServiceImplShim.cs)
subclasses `CityCitizenService` and overrides its two virtual members — so this is the established way to extend
this class, not a new idea.

### What it costs

**A preparatory commit making members `virtual`.** Only `CathedralDelta`
([:449](../src/Screens/Services/CityCitizenService.cs#L449)) and `HasBachsCathedral`
([:469](../src/Screens/Services/CityCitizenService.cs#L469)) are virtual today. The subclass also needs
`CalculateCityStats`, `Stage1`–`Stage5`, `ApplyEmperorEffects` and `ApplyDemocracyEffects`. Adding `virtual` is
behaviour-neutral: separate commit, no test touched, easy to review.

### Why all five stages, and what the override actually looks like

The root of it is a model mismatch, not the inheritance. The original works on **two counters** — `happy` and
`unhappy` as plain ints, plus the reservoir. CivOne works on a **citizen array**: `ct.Citizens[]` is the truth
and the counts are *derived* from it by `CountCitizenTypes` at the end of every stage
([:158](../src/Screens/Services/CityCitizenService.cs#L158), [:179](../src/Screens/Services/CityCitizenService.cs#L179), …).

So `Normalise()` cannot be a transcription. In an array model it has to count, correct, and then **write the
difference back into the array** by up- or downgrading citizens.

**Half of it is free.** The normalisation's second loop — the ceiling `happy + unhappy ≤ Size − specialists` —
is already structurally guaranteed here: the array has exactly `Size` entries and
`DebugService.Assert(ct.Sum() == _city.Size)` follows every stage. You cannot have more citizens than seats.
What remains is only the reservoir flow-back (the first loop) and the lower clamp around `E` — considerably
less than §3's pseudocode suggests.

**`Stage1` must be rewritten in full**, without calling `base`:

```csharp
protected int Stage1(ref CitizenTypes ct)
{
    (int initialUnhappyCount, int initialContent) = CalculateCityStats(ct);
    ct = StageBasic(ct, initialContent, initialUnhappyCount);
    ApplyEmperorEffects(ct);          // must go, but sits in the middle of the body
    ...
    return initialContent;            // consumed later by Stage4
}
```

Two reasons: `ApplyEmperorEffects` is wired into the body, and `CalculateCityStats` returns a `(int, int)` tuple
with no room for the reservoir surplus. Per `CLAUDE.md`'s side-effect rule, note that the return value
`initialContent` flows through `Stage4` into `ApplyDemocracyEffects`
([:199](../src/Screens/Services/CityCitizenService.cs#L199)) — it must survive the rewrite even though the
computation above it changes.

**`Stage2`–`Stage5` are cheap.** The base already recounts at the end, so `Normalise` just layers on top:

```csharp
protected override CitizenTypes Stage3(CitizenTypes ct)
{
    ct = base.Stage3(ct);   // building effects + CountCitizenTypes, unchanged
    return Normalise(ct);   // reservoir flow-back + lower clamp
}
```

Four times the same line. That is where the inheritance pays for itself.

### Two traps

**`CitizenTypes` is a struct** — visible from `public readonly bool Valid()`. That is why `Stage2`–`Stage5`
return `ct` instead of mutating it, and it means the reservoir cannot live inside the struct without changing a
public type that ten consumers use. It belongs in a private field on the subclass.

**That field needs an explicit reset.** The service is short-lived — `ICityCitizenService.Create` builds one per
city evaluation — but `GetCitizenTypes()` and `EnumerateCitizens()` can both run on the same instance. Without a
reset at the top of both, the second call inherits the first call's reservoir. This is the kind of defect tests
miss, because a test usually exercises only one of the two entry points; add one that calls both in sequence.

### The switch

`ICityCitizenService.Create` ([:64](../src/Screens/Services/ICityCitizenService.cs#L64)) hard-codes
`new CityCitizenService(...)`. That is the single selection point, and per `CLAUDE.md` the choice belongs in the
factory rather than inside either class. Add one setting (`OriginalHappinessModel`), defaulting to **off** while
the subclass is being built and flipped to **on** once the play test passes.

### Time-box — this is a transitional state

Two parallel happiness models violate H2 ("one model"). That is acceptable while the new one is being validated
and **not** acceptable afterwards. The work is not finished when the switch flips; it is finished when the old
one is gone:

1. `OriginalCityCitizenService` lands with the setting off. Old tests green, new tests green.
2. Play test decides H4 (reservoir refill) and confirms the model.
3. Setting flips to on; the default path is the corrected model.
4. **Cleanup commit:** fold the subclass into `CityCitizenService`, delete the setting, delete the superseded
   tests listed in §9 step 0, and re-derive `CityHappy.cs` against the corrected model. One model again.

Step 4 is part of this work, not a follow-up someone might get to. If it is skipped, the repository owns two
happiness models indefinitely — the outcome H2 exists to prevent.

---

## 8. Sourcing `cityId`

The staggering term `cityId % base` needs the original's **numeric city slot**, 0–255. CivOne has no such field:
`ICityBasic.Id` ([:19](../src/ICityBasic.cs#L19)) is a `Guid`. The equivalent is the city's position in
`Game._cities`, which is already what the SVE writer treats as the slot
([Game.LoadSave.cs:94](../src/Game.LoadSave.cs#L94)).

**Compute the index from the array, do not store it.** CivOne has a map editor, so cities can be created and
removed at arbitrary positions and any persisted index would go stale. A derived index is always consistent with
the list it came from, and matches the original, where the slot was likewise just an array position.

- Add a city-index lookup to `IGameCitizenDependency` (e.g. `int GetCityIndex(ICityBasic city)`), backed by
  `Game._cities.IndexOf(...)`. The service reads it once per `GetCitizenTypes()` call.
- `Game` already builds per-call city lookups ([Game.cs:241](../src/Game.cs#L241)); if the linear scan shows up
  in profiling, cache a `Guid → index` map invalidated on city add/remove, not on every access.
- Consequence to accept: adding or deleting a city in the editor renumbers the ones after it, so *which* cities
  carry the empire-size penalty first can shift. That is inherent to the original's model — the penalty is
  distributed by slot, not by any property of the city — and is only visible while `E` differs across cities,
  i.e. between `base + 1` and `2 × base` cities.

---

## 9. What this touches, and the steps

### Blast radius

`GetCitizenTypes()` has ten call sites. Only one of them is the civilization score:

| Consumer | Effect of the change |
|---|---|
| [City.cs](../src/City.cs) `:449`, `:532`, `:1229`, `:1559`, `:1602` | civil disorder, growth, celebration — the gameplay core |
| [CivilizationScore.cs:138](../src/Screens/Reports/CivilizationScore.cs#L138) | the F9 report |
| [TradeReport.cs:62](../src/Screens/Reports/TradeReport.cs#L62) | trade/tax figures |
| [TopCities.cs:161](../src/Screens/Reports/TopCities.cs#L161) | city ranking |
| [Diplomat.cs:39](../src/Units/Diplomat.cs#L39) | incite-revolt cost |
| [Icons.cs:339](../src/Graphics/Icons.cs#L339) | citizen icons on the city screen |

### Delivery

Its own branch, its own PR, its own merge, titled for what it is — "city happiness aligned with the original" —
not as preparation for anything. Because of H5 the PR changes no behaviour on its own.

Natural commit boundaries: (a) the `virtual` commit, (b) the subclass with steps 1–4, (c) the independent
corrections in steps 5–7, (d) the factory switch, (e) the cleanup.

### Steps

0. **Baseline, and the `virtual` commit.** The existing suites —
   [CityCitizenServiceImplTests.cs](../xunit/src/CityCitizenServiceImplTests.cs) (35 tests, 1162 lines) and
   [CityHappy.cs](../xunit/src/CityHappy.cs) (11 end-to-end cases) — are **not** modified here and must stay
   green throughout. Any red test in them is a regression, full stop.

   Two preparatory moves, each its own commit:

   a. Run both suites green and keep the result as the reference.
   b. Add `virtual` to `CalculateCityStats`, `Stage1`–`Stage5`, `ApplyEmperorEffects` and
      `ApplyDemocracyEffects`. Behaviour-neutral; no test touched.

   These six tests encode behaviour the subclass deliberately changes. They keep guarding the **old** class and
   are removed only in the cleanup step (§7), not now:

   | Test | Behaviour it pins, which the subclass changes |
   |---|---|
   | `CalculateCityStatsAllCases` (`:1073`) | the base term without `E` and without the human/AI split |
   | `ApplyEmperorEffectsTests` (`:1014`) | the method the subclass replaces with `E` |
   | `NumberOfRedShirtsTests` (`:996`) | the `36 / 12` literals |
   | `CathedralDeltaTest` (`:616`) | the inverted −4/−6 **and** the continent restriction |
   | `ApplyBuildingEffectsTestsCathedrals` (`:715`) | same, at the caller |
   | `ApplyDemocracyEffectsTests` (`:925`) | no Women's Suffrage term |

1. Extend `CalculateCityStats` with `Government`, `CityCount` and the city index to compute `base` and `E`.
   Government and city count are already reachable (`_city.PlayerIntf.Government`,
   `_game.GetPlayer(...).Cities.Length` — `ApplyEmperorEffects` uses the latter today); the index comes from the
   lookup in §8. The existing `contentLimit` expression is already correct and stays. Apply the lower clamp
   **after** adding `E` (§3). Gate the whole term on `isHuman`; AI cities get `Size − 3` — *optional, §5*.
2. Replace `ApplyEmperorEffects` with `E`. Do not keep both.
3. Add the overflow reservoir (§4): surplus beyond `Size` is parked and refilled as modifiers reduce `unhappy`.
   Drive the red-shirt representation from it instead of from `MinRedShirtCityCount`. The refill goes in as
   **two switchable delegates** (H4, §6), default halving — no need to wait for the DOSBox result.
4. Add `Normalise` at the five stage boundaries — `Stage1` rewritten in full, `Stage2`–`Stage5` as
   `base.StageN(...)` plus `Normalise(...)`. Only the reservoir flow-back and the lower clamp need implementing;
   the ceiling is already structural. See §7 for the details and the two traps.
5. Fix `CathedralDelta` ([:449-467](../src/Screens/Services/CityCitizenService.cs#L449-L467)) on two counts:
   **−4 base, −6 with Michelangelo** (currently inverted), and **drop the continent check** — Michelangelo is
   global for its owner. Gate the whole Cathedral block on the advance Religion.
6. Extend the luxury computation to the full original form (§1): corruption (Democracy → 0, Courthouse halves,
   Palace does not), `+5` inside the numerator, clamp upper bound on **raw** trade, `Entertainers × 2`, then
   `+50 %` each for Marketplace and Bank — in that order. Check against CivOne's existing `City.Luxuries` /
   `EntertainerLuxuries` before adding anything (Q2).
7. Add the Women's Suffrage term to war weariness in `ApplyDemocracyEffects`, and make stage 4's
   martial-law-XOR-weariness split explicit.
8. Verify CivOne's wonder ownership and obsolescence against the original's rules (Q3): effect follows the city
   on capture, and a wonder is obsolete as soon as *any* civilization has the obsoleting advance.
9. Parameterise the luxury rate (H7): the rate flows in from the caller, defaulting to the player's real rate.
   The civilization score passes a fixed 40 % reference instead; every other caller behaves exactly as before.
10. Tests — see §11.

---

## 10. Known deviations from the original

1. **Red shirts as a citizen type.** The original has the *mechanism* (overflow reservoir) but not the *type* —
   its screens know three citizen categories plus specialists. CivOne keeps its fourth sprite as the
   representation. The frozen `13 / 25 / 37 / step 12` thresholds are *not* kept.
2. **`DebugFlags & 0x2` is not reproduced.** When that bit is cleared the original zeroes `happy` and `unhappy`
   at the end of the computation. Its default has the bit set, so the normal path is the one implemented; there
   is simply no equivalent switch here.
3. **Temple/Oracle keep CivOne's doubling form.** The original is additive; the two produce identical numbers
   for every combination, so the code is left alone with a comment.
4. **The two unidentified martial-law slots are not ported.** The documented three-unit martial law is
   implemented.
5. **Difficulty 5 (Deity) does not exist in the original** and is served by extrapolating the formula (H6).

---

## 11. Open points

| # | Point | Blocks |
|---|---|---|
| Q1 | **Reservoir refill: halving or full drain?** Not decidable from the decompilation. Shipped as a switch (H4, §6), default halving, so it blocks nothing. Settle by DOSBox experiment: Emperor, empire well above `base`, large reservoir, build a Colosseum — full drain → `unhappy` does not move until the reservoir is exhausted; halving → it drops immediately by about half the effect. Affects Shakespeare as well. | nothing; decides a default |
| Q2 | Does CivOne's existing `City.Luxuries` / `EntertainerLuxuries` already implement parts of the full luxury chain (corruption, `+5` rounding, Marketplace/Bank, entertainer doubling)? Establish before step 6 so nothing is applied twice. | step 6 |
| Q3 | Does CivOne's wonder list travel with a captured city and disappear with a destroyed one, and does `WonderObsolete` check *all* civilizations rather than only the owner? | step 8 |
| Q4 | Deity (difficulty 5) extrapolates to `base = 4` under Despotism — an empire-size penalty from 5 cities, steeper than anything the original produces. Confirm in play or pick a floor. | nothing; balance review |

---

## 12. Test plan

- `CityCitizenServiceImplTests` / `CityHappy` — **unchanged and green throughout** (H5). Superseded tests are
  deleted only in the cleanup step (§7).
- `OriginalCityCitizenServiceTests` (new) — the base term per difficulty (0–4, plus Deity when enabled) **and
  government**; human vs. AI base; the `E = 0` agreement zone from §2's table; the first-penalised-city boundary
  per row; `cityId % base` staggering; **the clamp ordering** (a size-1 city on difficulty 0 in a large empire —
  the case that separates the two orderings); the reservoir, in **both** refill variants (H4); the stage order
  (a modifier that overshoots loses the surplus at its own `N`, not later); Cathedral with/without Michelangelo
  and without Religion; Michelangelo across continents; the luxury chain including Marketplace/Bank and
  entertainer multiplication; war weariness with and without Women's Suffrage; each remaining modifier in
  isolation; the luxury-rate parameter; and **`GetCitizenTypes()` followed by `EnumerateCitizens()` on the same
  instance** — the reservoir-reset trap from §7.
- A corrected-model sibling of `CityHappy.cs` (new) — the eleven entertainer/temple integration cases
  **re-derived by hand** against the new formula, not adjusted until green. These are what catch a mistake in
  the luxury chain.
- `GetCityIndex` — stable for an unchanged list; the staggering test pins the mapping index → penalised city
  explicitly, so a future change to the index source fails loudly.
- Manual: city screens, disorder and celebration behave sensibly; the six consumers in §9 still render.
