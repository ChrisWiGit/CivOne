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

#### `CivScoreTrigger` (new)

`src/Palace/CivScoreTrigger.cs`

```csharp
internal class CivScoreTrigger : IPalaceUpgradeTrigger
{
    public bool ShouldTrigger(IPlayer player, PalaceData palace)
    {
        int n = palace.UpgradeCount;
        int threshold = 1 + (n * n) + n;
        return player.CivilizationScore >= threshold;
    }
}
```

#### `PalaceUpgradeService` (new)

`src/Palace/PalaceUpgradeService.cs`

Holds a list of all registered `IPalaceUpgradeTrigger` instances. Returns `true` only when at least one trigger fires **and** the palace still has upgradeable slots.

```csharp
internal class PalaceUpgradeService
{
    private readonly IReadOnlyList<IPalaceUpgradeTrigger> _triggers;

    public bool ShouldShowPalaceUpgrade(IPlayer player, PalaceData palace)
    {
        if (!palace.CanUpgrade) return false;
        return _triggers.Any(t => t.ShouldTrigger(player, palace));
    }
}
```

New triggers are registered via constructor injection.

#### Hook in `Game.EndTurn` (modified)

`src/Game.cs`, inside `EndTurn()`, at the point where the human player's turn begins:

```csharp
if (CurrentPlayer == HumanPlayer
    && _palaceUpgradeService.ShouldShowPalaceUpgrade(Human, Human.Palace))
{
    GameTask.Enqueue(Show.BuildPalace());
}
```

---

## Part 2: `PalaceData` – New Members

`src/PalaceData.cs`

### `UpgradeCount`

Total number of upgrades applied so far (sum of all part levels and garden levels). Used as `n` in the trigger formula.

```csharp
public int UpgradeCount =>
    PalaceLevel.Sum(l => l) + GardenLevel.Sum(l => l);
```

### `CanUpgrade`

`true` as long as at least one slot still has room to grow.

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
| `src/Palace/IPalaceUpgradeTrigger.cs` | **new** |
| `src/Palace/CivScoreTrigger.cs` | **new** |
| `src/Palace/PalaceUpgradeService.cs` | **new** |
| `src/PalaceData.cs` | add `UpgradeCount`, `CanUpgrade`, `IsSlotUnlocked` |
| `src/Game.cs` | add trigger check in `EndTurn` |
| `src/Screens/PalaceView.cs` | implement `SelectStyle`; fix garden bug, unlock logic, `MouseDown`, input validation |

---

## Open Questions

- Should `SelectStyle` remember the style previously chosen for a given slot, and pre-highlight it?
- Are there additional trigger conditions beyond `CivScore` (e.g. turn count, wonders built)? They slot in as new `IPalaceUpgradeTrigger` implementations without touching existing code.
- When the palace is fully upgraded (`CanUpgrade == false`), the trigger is permanently suppressed. Should there be a one-time notification to the player?
