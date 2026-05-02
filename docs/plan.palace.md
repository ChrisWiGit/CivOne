# Palace Screen – Implementation Plan

## Overview

This document covers two concerns:

1. **When** the palace upgrade screen is shown to the player (trigger system).
2. **What** still needs to be implemented or fixed in the palace screen itself.

---

## Part 1: Palace Upgrade Trigger System

### Current State

`Show.BuildPalace()` exists in `src/Tasks/Show.cs` and produces a `PalaceView(build: true)` which starts in `Stage.Message`. However, it is only reachable via the **debug menu** (`DebugOptions.cs`).

The palace upgrade trigger must fire at end-of-turn when the player's civilization score crosses the next threshold – and potentially from other conditions in the future.

### Trigger Formula

The threshold for the *n*-th upgrade is:

$$S(n) = 1 + n^2 + n$$

where `n` is the total number of upgrades the player has already applied to the palace (sum of all part levels and garden levels).

| Upgrade # (n) | Score threshold S(n) |
|:---:|:---:|
| 0 | 1 |
| 1 | 3 |
| 2 | 7 |
| 3 | 13 |
| 4 | 21 |
| … | … |

### Design: Extensible Trigger System

Additional trigger conditions are expected in the future (exact rules still unknown). The design must allow new triggers to be added without modifying existing code.

#### `IPalaceUpgradeTrigger` (new)

`src/Palace/IPalaceUpgradeTrigger.cs`

```csharp
internal interface IPalaceUpgradeTrigger
{
    bool ShouldTrigger(IPlayer player, PalaceData palace);
}
```

#### `PalaceScoreTrigger` (new)

`src/Palace/PalaceScoreTrigger.cs`

```csharp
//`src/Palace/IPalaceUpgradeTrigger.cs` 
public interface IPlayerGameState {
    // Placeholder for any gamestate info the triggers might need in the future (e.g. turn count, wonders built, etc.)
    int CivilizationScore { get; }
    IPalaceData Palace { get; }
    bool IsHuman { get; }
}


//`src/Palace/PalaceScoreTrigger.cs`
public class HumanCivScorePalaceTrigger : IPalaceUpgradeTrigger
{
    public bool ShouldTrigger(IPlayerGameState player)
    {
        if (!player.Palace.CanUpgrade) return false;
        if (!player.IsHuman) return false;   // currently only human players otherwise AI must chose palace upgrades, which is not implemented yet

        int n = player.Palace.UpgradeCount;
        int threshold = 1 + (n * n) + n;
        return player.CivilizationScore >= threshold;
    }
}

```

#### `PalaceUpgradeService` (new)

`src/Palace/PalaceUpgradeService.cs`

Holds a list of all registered `IPalaceUpgradeTrigger` instances. Returns `true` only when at least one trigger fires.

```csharp
// `src/Palace/PalaceUpgradeService.cs`
internal class PalaceUpgradeService
{
    private readonly IReadOnlyList<IPalaceUpgradeTrigger> _triggers;

    public PalaceUpgradeService(IReadOnlyList<IPalaceUpgradeTrigger> triggers)
    {
        _triggers = triggers;
    }

    public bool ShouldShowPalaceUpgrade(IPlayerGameState player)
    {
        return _triggers.Any(t => t.ShouldTrigger(player));
    }
}
```

New triggers are registered via constructor injection.

Use constructor injection in `Game` (no `InitPalaceUpgradeService()`, no singleton factory):

```csharp
private readonly IPalaceUpgradeService _palaceUpgradeService;

private Game(..., IPalaceUpgradeService palaceUpgradeService) : this(CreateValueSanitizer())
{
    _palaceUpgradeService = palaceUpgradeService;
}
```

The composition root (`CreateGame`, load constructors, tests) is responsible for creating and passing the concrete service instance.

#### Hook in `Game.EndTurn` (modified)

`src/Game.cs`, inside `EndTurn()`, at the point after the current turn's game state updates:

```csharp
// Check palace upgrade trigger for ALL players (future AI support: trigger evaluation is player-agnostic)
foreach (Player player in Players)
{
    if (_palaceUpgradeService.ShouldShowPalaceUpgrade(player))
    {
        // Enqueue show-palace action only for human player (AI palace upgrades handled separately in future)
        if (player.IsHuman)
        {
            GameTask.Enqueue(Show.BuildPalace());
        }
    }
}
```

**Rationale:**
- Trigger evaluation (`ShouldShowPalaceUpgrade`) is player-independent; check runs for all players
- `GameTask.Enqueue()` only fires for human – prepares for future AI palace decision logic (when implemented, AI will call its own decision routine instead of `Show.BuildPalace()`)

---

## Part 2: `PalaceData` – New Members

`src/PalaceData.cs`

### `UpgradeCount`

Total number of upgrades applied so far (sum of all part levels and garden levels). Used as `n` in the trigger formula.

`IPalaceData` already has `GetPalaceLevel(int index)` and `GetGardenLevel(int index)`, so we can sum over those:

```csharp
public int UpgradeCount =>
    PalaceLevel.Sum(l => l) + GardenLevel.Sum(l => l);
```

### `CanUpgrade`

`true` as long as at least one slot still has room to grow.

Put in IPalaceData
```csharp
public bool CanUpgrade =>
    Enumerable.Range(0, 7).Any(i => IsSlotUnlocked(i) && PalaceLevel[i] < 4)
    || GardenLevel.Any(l => l < 3);
```

### `IsSlotUnlocked(int index)`

Encapsulates the unlock logic for the seven palace segment slots.

| Index | Part | Unlocked when |
|:---:|---|---|
| 3 | Center | always |
| 2 | Left wing 1 | Level[3] > 0 (always) |
| 4 | Right wing 1 | Level[3] > 0 (always) |
| 1 | Left wing 2 | Level[2] > 0 |
| 5 | Right wing 2 | Level[4] > 0 |
| 0 | Left tower | Level[1] > 0 |
| 6 | Right tower | Level[5] > 0 |

Gardens (A / B / C, indices 0–2) are always available up to level 3.

---

## Part 2b: Palace Sprite Loading Abstraction (DI-Friendly)

To avoid coupling `PalaceView` to global `Resources`, introduce an interface dedicated to palace sprite loading.

### Goal

- `PalaceView` should depend on an abstraction (`IPalaceSpriteProvider`), not on static/global `Resources`.
- New UI themes or extra images can be added by introducing new provider implementations.
- Existing vanilla assets remain available through a default provider implementation.

### New Interface

`src/Screens/Palace/IPalaceSpriteProvider.cs`

```csharp
internal interface IPalaceSpriteProvider
{
    Picture GetBackground();
    Picture GetGardenBackdrop(byte gardenLevel);
    Picture GetGardenBrush(int gardenIndex, byte gardenLevel);
    Picture GetPalacePart(PalaceStyle style, PalacePart part, int level);
}
```

### Default Implementation (Vanilla Resources)

`src/Screens/Palace/ResourcesPalaceSpriteProvider.cs`

```csharp
internal sealed class ResourcesPalaceSpriteProvider : IPalaceSpriteProvider
{
    private readonly Resources _resources;

    public ResourcesPalaceSpriteProvider(Resources resources)
    {
        _resources = resources;
    }

    // maps CBACK/CBACKS*/CBRUSH* and GetPalace(...) calls
}
```

### Asset Mapping (kept as-is)

The default provider maps the current resource keys exactly as used today:

#### Palace Structure Images

**Palast-Teilkomponenten** – geladen via `Resources.GetPalace(style, part, level)`:
- Source files: `CASTLE0` (frame 0), `CASTLE1` through `CASTLE4` (upgraded frames)
- Each file contains sprite data for all palace parts at various styles/offsets
- Offset calculation:
  - Classical style: X offset +160
  - Islamic style: Y offset +100
  - Medieval (default): no offset

| Palace Part | Dimensions | Source | Notes |
|---|---|---|---|
| LeftTower | 35×101 | CASTLE* [x, 1] | |
| LeftTowerWall | 57×101 | CASTLE* [x, 1] | |
| Wall | 48×101 | CASTLE* [x, 1] | |
| Center | 52×99 | CASTLE* [x, 1] | Main palace centerpiece |
| RightTowerWall | 57×101 | CASTLE* [x, 1] | |
| RightTower | 35×101 | CASTLE* [x, 1] | |

#### Background and Decorative Images

Loaded directly from `Resources` cache via sprite name:
- `CBACK` – main palace screen background (320×200)
- `CBACKS1`, `CBACKS2`, `CBACKS3` – garden backdrop variations (positioned Y=135)
- `CBRUSH0` through `CBRUSH5` – garden plant/foliage decorations (positioned at garden slot X,Y)

### Wiring in `PalaceView`

`PalaceView` receives `IPalaceSpriteProvider` via constructor injection:

```csharp
private readonly IPalaceSpriteProvider _sprites;

public PalaceView(bool build, IPalaceSpriteProvider sprites)
{
    _sprites = sprites;
}
```

All previous direct `Resources[...]` and `Resources.GetPalace(...)` usages in `PalaceView` are replaced with `_sprites` calls.

### Composition Root Responsibilities

- Build `ResourcesPalaceSpriteProvider` once from the existing `Resources` instance.
- Pass provider to all `PalaceView` creation sites (`Show.BuildPalace()`, sidebar palace button, tests if needed).
- Future theme/mod providers can be composed as decorator or fallback chain without changing `PalaceView`.

---

## Part 3: `PalaceView` Screen – Stage Machine

### Full Flow

```
[build = false]
  View ──(Enter / Click)──► Destroy

[build = true]
  Message ──(Enter / Click)──► SelectPart
  SelectPart ──(key 1–7)──► SelectStyle
  SelectPart ──(key A–C)──► Morph       ← gardens skip style selection
  SelectStyle ──(key 1–3)──► Morph
  Morph ──(noise done)──► View
```

### Stage.Message

Already implemented. Displays `KING/PALACE` text; Enter / click advances to `SelectPart`.

### Stage.SelectPart

Mostly implemented. The following fixes are required:

| # | Issue | Location | Fix |
|---|-------|----------|-----|
| 1 | Only `Level[i] < 4` checked; unlock chain ignored | `HasUpdate` / `KeyDown` | Use `IsSlotUnlocked(i) && Level[i] < 4` |
| 2 | Garden bug: `GetGardenLevel(2)` hard-coded | `KeyDown` ~L265 | Replace `2` with `index` |
| 3 | `MouseDown` SelectPart handler commented out | `MouseDown` ~L308 | Uncomment and implement |
| 4 | Invalid input caught by try/catch | `KeyDown` ~L283 | Validate before acting; remove TODO |
| 5 | Palette slot selection goes directly to `Morph` | `KeyDown` | Route `1–7` keys through `SelectStyle` instead |
| 6 | Text positioning (numbers/letters) not adjustable | `HasUpdate` ~L194-210 | Add configurable Y-offsets for fine-tuning |

#### Y-Offset Configuration

Two Y-levels render number keys (1–7) and letter keys (A–C). Each needs a configurable offset for manual adjustment:

```csharp
// PalaceView.cs – field additions
private const int PALACE_NUMBERS_Y_OFFSET = 0;   // Base: 144 (font 14), 145 (font 5)
private const int GARDEN_LETTERS_Y_OFFSET = 0;   // Base: 160 (font 14), 161 (font 5)
```

Application:
- Palace numbers: `144 + oy + PALACE_NUMBERS_Y_OFFSET` and `145 + oy + PALACE_NUMBERS_Y_OFFSET`
- Garden letters: `160 + oy + GARDEN_LETTERS_Y_OFFSET` and `161 + oy + GARDEN_LETTERS_Y_OFFSET`

### Stage.SelectStyle (not yet implemented)

Shows a modal prompt: `"Which style shall we use?"`

Options displayed with keyboard shortcuts:

```
1 = Medieval   2 = Classical   3 = Islamic
```

Pending state (stored as fields on `PalaceView`):

```csharp
private int _pendingPartIndex;
```

On key `1`, `2`, or `3`:

```csharp
PalaceStyle style = (PalaceStyle)(args.KeyChar - '0');
byte newLevel = (byte)(palace.GetPalaceLevel(_pendingPartIndex) + 1);
palace.SetPalace(_pendingPartIndex, (byte)style, newLevel);
_palaceMorph = DrawPalace();   // snapshot before change
_currentStage = Stage.Morph;
_update = true;
```

### Stage.Morph

Already implemented (noise transition). Ends in `Stage.View`.

---

## Part 4: Files Changed / Created

| File | Change |
|------|--------|
| `src/Palace/IPalaceUpgradeTrigger.cs` | **new** interface |
| `src/Palace/IPalaceUpgradeService.cs` | **new** interface (must be public for DI pattern) |
| `src/Palace/HumanCivScorePalaceTrigger.cs` | **new** trigger implementation |
| `src/Palace/PalaceUpgradeService.cs` | **new** service implementation |
| `src/PalaceData.cs` | add `UpgradeCount`, `CanUpgrade`, `IsSlotUnlocked` |
| `src/Game.cs` | add `_palaceUpgradeService` field via constructor injection; call trigger in `EndTurn` |
| `src/Game.NewGame.cs` | pass `IPalaceUpgradeService` from composition root into `Game` constructor |
| `src/Game.LoadSave.cs` | pass `IPalaceUpgradeService` into constructor when loading from SVE |
| `src/Game.LoadYaml.cs` | pass `IPalaceUpgradeService` into constructor when loading from YAML |
| `src/Screens/Palace/IPalaceSpriteProvider.cs` | **new** sprite-loading abstraction for palace screen |
| `src/Screens/Palace/ResourcesPalaceSpriteProvider.cs` | **new** default implementation backed by existing `Resources` |
| `src/Screens/PalaceView.cs` | add Y-offset constants, implement `SelectStyle` stage, fix garden bug, unlock logic, input validation |
| `src/Screens/PalaceView.cs` | replace direct `Resources` access with `IPalaceSpriteProvider` |
| `src/Tasks/Show.cs` | inject `IPalaceSpriteProvider` when creating `PalaceView` |
| `src/Screens/GamePlayPanels/SideBar.cs` | inject `IPalaceSpriteProvider` when opening palace screen |
| `src/Game.cs` | add trigger check in `EndTurn` |
| `src/Screens/PalaceView.cs` | implement `SelectStyle`; fix garden bug, unlock logic, `MouseDown`, input validation |

---

## Last Part

All tests must pass.

## Open Questions

- Should `SelectStyle` remember the style previously chosen for a given slot, and pre-highlight it?
  - No
- Are there additional trigger conditions beyond `CivScore` (e.g. turn count, wonders built)? They slot in as new `IPalaceUpgradeTrigger` implementations without touching existing code.
  - Not yet known; will be added in the future as needed.
- When the palace is fully upgraded (`CanUpgrade == false`), the trigger is permanently suppressed. Should there be a one-time notification to the player?
  - no
