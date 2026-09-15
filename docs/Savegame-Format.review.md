# Savegame Format Review — Analysis vs. CivOne Implementation

Comparison of [`docs/Savegame-Format.analyze.md`](Savegame-Format.analyze.md) — decoded from the
[OpenCiv1](https://github.com/rajko-horvat/OpenCiv1) port of the original DOS code — against this
repository's `.SVE`/`.MAP` handling, cross-checked against
[`docs/SaveGame/memory_map_SVE_EN.txt`](SaveGame/memory_map_SVE_EN.txt).

Sources inspected in this repo:

| What | Where |
|---|---|
| Binary struct layout | [src/IO/SaveData.cs](../src/IO/SaveData.cs) |
| Read path | [src/SaveDataAdapter.Get.cs](../src/SaveDataAdapter.Get.cs) |
| Write path | [src/SaveDataAdapter.Set.cs](../src/SaveDataAdapter.Set.cs) |
| Field accessors / defaults | [src/SaveDataAdapter.cs](../src/SaveDataAdapter.cs) |
| Game state load & save | [src/Game.LoadSave.cs](../src/Game.LoadSave.cs) |
| `.MAP` layers | [src/Map.LoadSave.cs](../src/Map.LoadSave.cs) |
| SVE compatibility gate | [src/Services/SveSaveCompatibilityService.cs](../src/Services/SveSaveCompatibilityService.cs) |

**Summary:** the overall layout matches — 37,856 bytes, every block boundary identical.
The problems are in field *interpretation* and in how little of the file we actually preserve.

---

## 1. Defects in CivOne

### 1.1 `ContinentAttack` and `ContinentDefense` are swapped

[src/IO/SaveData.cs:99-100](../src/IO/SaveData.cs#L99-L100)

```
CivOne:            1928 ContinentDefense    2184 ContinentAttack
Analysis + memory map: 1928 continent_attack    2184 continent_defense
```

Neither field is read or written today, so there is no runtime impact — but every future
editor, AI port, or tooling built on this struct inherits the mistake. Rename both.

### 1.2 Wrong occupancy test for city slots

[src/SaveDataAdapter.Get.cs:134](../src/SaveDataAdapter.Get.cs#L134)

```csharp
if (city.Status == 0xFF) continue;
```

The original tests `ActualSize != 0` (analysis section 13.3). DOS never clears unused city
slots — they hold leftovers from earlier games, and `Status` is rarely `0xFF` there.
Loading a genuine DOS save therefore produces ghost cities with garbage owner and position.

Fix: skip on `ActualSize == 0`, keeping the `0xFF` check as a fallback because
`DefaultCityData` ([src/SaveDataAdapter.cs:27](../src/SaveDataAdapter.cs#L27)) writes `0xFF`
into our own saves.

### 1.3 Fortified-unit bit layout contradicts the analysis

[src/Game.LoadSave.cs:258-262](../src/Game.LoadSave.cs#L258-L262)

```csharp
int unitId   = fortifiedUnit & 0x3F;
bool fortified = (fortifiedUnit & 0x40) != 0;
bool veteran   = (fortifiedUnit & 0x80) != 0;
```

Analysis section 3.1 says bit 6 (`0x40`) is the **veteran** flag, bit 7 is unused, and `-1`
marks an empty slot. One of the two is wrong; this has to be settled against the original
code (`Segment_1866`).

### 1.4 Operator precedence loses the veteran status

[src/Game.LoadSave.cs:270](../src/Game.LoadSave.cs#L270)

```csharp
unit.Status = fortified ? 8 : 0 + (veteran ? 32 : 0);
// parses as: fortified ? 8 : (veteran ? 32 : 0)
```

A fortified veteran silently becomes a non-veteran. Independent of 1.3 — the parentheses
are simply missing.

### 1.5 Unit byte +8 is discarded as padding

[src/IO/SaveData.cs:64](../src/IO/SaveData.cs#L64) declares `_padding1` where the analysis
has `GoToNextDirection`. It is zeroed on save, so the goto path of every moving unit breaks
on a round trip.

### 1.6 Civilization identity likely read from the wrong field

[src/Game.LoadSave.cs:152-153](../src/Game.LoadSave.cs#L152-L153) derives the civilization
from `CivilizationIdentityFlag` at `0x93DE`. Both the memory map (`civ#.who`) and the
analysis (`nationality_id[8]`) place the per-civ identity at offset **1912** — which CivOne
calls `LeaderGraphics` and never touches. `0x93DE` is documented as "identity known", a
different thing.

### 1.7 The replay writer drops almost everything

[src/SaveDataAdapter.Set.cs:252-262](../src/SaveDataAdapter.Set.cs#L252-L262) handles only
`CivilizationDestroyed` (`0xD`); every other event hits `default: continue`. The reader
understands 11 event types. Every save discards the chronicle.

### 1.8 Debug hex dump in the load path

[src/SaveDataAdapter.Get.cs:245](../src/SaveDataAdapter.Get.cs#L245) prints the entire
replay block to the console on every load. Leftover debugging; remove it.

---

## 2. What is missing

### 2.1 Round-trip loss is the core problem

`new SaveDataAdapter()` starts from a zeroed struct
([src/SaveDataAdapter.cs:374](../src/SaveDataAdapter.cs#L374)), and `Save()` populates only
about 30 fields. Everything else is **zeroed on write**, even when it was present in the
file we just loaded:

| Block | Offset | State in CivOne |
|---|---|---|
| `diplomacy[8][8]` | `0x0648` | never read, never written |
| `continent_strategy` / `_attack` / `_defense` / `_city_count` | `0x0548`–`0x0A88` | never read, never written |
| `continent_size` / `ocean_size` / `build_site_count` | `0x0A88`–`0x0B88` | never read, never written |
| `score_graph` / `peace_graph` | `0x0BA8`, `0x1058` | never read, never written |
| `strat_active/policy/x/y[8][16]` | `0x6660`–`0x67E0` | never read, never written |
| `land_pathfind` | `0x8AA6` | never read, never written (see analysis 7.6) |
| `units_in_production`, `units_destroyed`, `lost_units` | `0x0318`, `0x68F0`, `0x869E` | never read, never written |
| `tech_acquired_from` | `0x885E` | never read, never written |
| spaceship block, palace block, `palace_level` | `0x8BD2`–`0x93B8` | never read, never written |
| `land_count`, `military_power`, `ranking`, `score` | `0x06E8`–`0x0748` | never read, never written |
| `max_tech_count`, `debug_switches`, `cumulative_epic_ranking` | `0x8BAA`, `0x8BAE`, `0x8BC2` | never read, never written |

**Cheapest high-value fix:** when saving a game that was loaded from `.SVE`, start from the
loaded struct instead of a zeroed one. Unknown blocks then survive untouched.

### 2.2 Domain concepts we do not model at all

The analysis derives these in detail; CivOne has no equivalent:

* Diplomacy bit semantics — war is `(dip & 3) == 1`, not a dedicated bit; the 16-turn decay
  tick; symmetric bits must be written in both directions (analysis 8).
* `continent_strategy` as the AI build lever, sharing its value range with
  `UnitDefinition.UnitCategory` (analysis 9).
* `cumulative_epic_ranking`, including the report counter parked in slot 0 (analysis 10).

---

## 3. What the analysis should take from CivOne

### 3.1 `.MAP` layer semantics — CivOne knows more

Analysis section 11 lists the `+80`, `+160`, `y+100`, `y+150` layers as unidentified.
[src/Map.LoadSave.cs](../src/Map.LoadSave.cs) names three of them:

| Layer | Content |
|---|---|
| `(x, y+100)` | improvements: `0x01` city, `0x02` irrigation, `0x04` mine, `0x08` road |
| `(x, y+150)` | `0x01` railroad |
| `(x+160, y)` | explored bitmask per civ; `0` means a hut is visible |
| `(x+80, y+100)` / `(x+80, y+150)` | duplicate of the two improvement layers |

### 3.2 Terrain layer: raw byte vs. mapped type

The analysis states "type 10 = ocean" for the terrain layer. That is the *mapped* type.
The raw byte 10 is grassland; ocean is raw byte 1. Raw table from `SaveTerrainLayer`:

| Byte | Terrain | Byte | Terrain |
|---:|---|---:|---|
| 1 (and any unknown) | Ocean | 11 | Jungle |
| 2 | Forest | 12 | Hills |
| 3 | Swamp | 13 | Mountains |
| 6 | Plains | 14 | Desert |
| 7 | Tundra | 15 | Arctic |
| 9 | River | 10 | Grassland |

### 3.3 `game_settings` bit assignment

Listed as undocumented in the analysis. CivOne assigns bits 0..7 as InstantAdvice, AutoSave,
EndOfTurn, Animations, Sound, EnemyMoves, CivilopediaText, Palace
([src/Game.LoadSave.cs:117-127](../src/Game.LoadSave.cs#L117-L127)) — but carries a
`// TODO: is bit order compatible with CivDOS?`. The analysis can settle it from the
original code.

### 3.4 `continent_size_unused` is resolved

The analysis leaves this open. Memory map and our struct agree: **16** used entries each for
continent and ocean sizes, followed by 96 dead bytes. The original code reads 64 but only
ever indexes 0–15.

---

## 4. Where the analysis needs to go deeper

1. **`game_settings` (`0x8AA4`) and `debug_switches` (`0x8BAE`) bit layout.** Marked open,
   and we are guessing. Highest priority because it is immediately actionable.
2. **Replay entry lengths.** The analysis says 1–2 payload bytes; our reader consumes four
   for event ID 1 (owner, name id, x, y) while its own XML comment says five. Three
   statements, three lengths. The full ID-to-length table from `F0_1866_250e_AddReplayData`
   is required — one wrong delta desynchronises the rest of the stream.
3. **Fortified-unit bits 6 and 7** (see 1.3) — a direct contradiction that must be decided.
4. **`civ#.who` at offset 1912** — what does it index, and how does it relate to
   `civs_identity_flag` at `0x93DE`? Our civilization assignment for DOS saves depends on it.
5. **Palace block.** The analysis has `palace_data1/2[12]`; the memory map is far finer
   (`item1..7.level`, `deco1..3.level`, `item1..7.style`, plus six unknown shorts). The
   unknowns should be resolvable from the original code.
6. **The 36 "unused" bytes per civ in the spaceship block.** Likely part counts or build
   progress; needed for `SPACESHIP_FULL_IMPLEMENTATION_PLAN.md`.
7. **`ocean_size` indexing.** Continent and ocean ids share the numbering space of the
   `.MAP` layer at `y+50`. How is an ocean id mapped onto 0–15?
8. **Occupancy test for unit slots.** Documented for cities (`ActualSize != 0`), not for
   units — we assume `TypeID == 0xFF`, unverified.

---

## 5. Suggested order of work

Low risk, immediate value:

1. Rename `ContinentAttack` / `ContinentDefense` (1.1).
2. Add the missing parentheses in the fortified-unit status (1.4).
3. Remove the replay hex dump from the load path (1.8).
4. Base an `.SVE` save on the loaded struct rather than a zeroed one (2.1).

Requires a decision from the original code first:

5. City slot occupancy test (1.2).
6. Fortified-unit bit layout (1.3), civilization identity field (1.6).
7. `GoToNextDirection` byte (1.5), replay writer coverage (1.7).
