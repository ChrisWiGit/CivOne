# UnitsDestroyedBy - Gameplay Implementation Guide

**Created:** 2026-04-12
**Status:** Analysis & Roadmap
**Goal:** Implementation of unit destruction tracking in the game

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Current Status](#current-status)
3. [Implementation Points](#implementation-points)
4. [Required Changes](#required-changes)
5. [UI/Display Concepts](#uidisplay-concepts)
6. [Implementation Roadmap](#implementation-roadmap)
7. [Code Examples](#code-examples)
8. [Critical Points](#critical-points)

---

## 🎯 Overview

The `UnitsDestroyedBy` feature enables tracking, for each player, how many units they have destroyed against every other player. This is useful for:

* **Statistics:** Military success rates
* **Diplomacy:** "You have already destroyed 42 of my units!"
* **Gameplay:** Dynamic conflict mechanics
* **Replay Data:** Trackable war history

**Data Structure:**

```
Player.UnitsDestroyedBy[8]  // One entry per possible opponent (0-7)
```

---

## ✅ Current Status

### What already exists

✅ **Persistence Layer (100%)**

* `IPlayer.UnitsDestroyedBy` - `ushort[]` array with 8 entries
* Storage in YAML format (`UnitsDestroyedByByPlayerGuid`)
* Loading from save files using `UnitsDestroyedByResolver`
* Mapping between GUID-based (YAML) and index-based (runtime) structures
* Tests for resolver with 18 comprehensive test cases

✅ **Refactoring completed (as of 2026-04-12)**

* New `UnitsDestroyedByResolver` class in `src/Persistence/Model/`
* Complex logic extracted from `GameStateDtoMapper`
* Testable via dependency injection
* Default instance if not injected

### What is NOT implemented

❌ **Gameplay Integration (0%)**

* **No increment logic:** Nowhere is `UnitsDestroyedBy[targetIndex]++` called
* **No UI:** No statistical display of these values
* **No combat hooks:** Combat system does not update these values
* **No usage:** Values are loaded but not used for gameplay mechanics

---

## 🔧 Implementation Points

### Where are units destroyed?

The following table shows all places where units are destroyed and where tracking should occur:

| Scenario           | File(s)                         | Method                 | Priority  | Complexity |
| ------------------ | ------------------------------- | ---------------------- | --------- | ---------- |
| **Combat**         | `src/Combat.cs` (?)             | Unit combat resolution | 🔴 High   | 🟡 Medium  |
| **Disband**        | `src/Game.cs`                   | `DisbandUnit()`        | 🟡 Medium | 🟢 Low     |
| **City Attack**    | `src/City.cs`                   | City conquest logic    | 🔴 High   | 🟡 Medium  |
| **Anarchy/Unrest** | `src/Government/Anarchy.cs` (?) | Disband during unrest  | 🟡 Medium | 🟢 Low     |
| **Disaster**       | `src/Disaster/` (?)             | Volcano, Earthquake    | 🟢 Low    | 🟢 Low     |
| **Unit Aging**     | `src/Unit.cs` (?)               | Unit expiration        | 🟢 Low    | 🟢 Low     |

#### Detailed Implementation Scenarios

**1. Combat System** (Highest Priority)

```
If Unit A (from Player 1) fights Unit B (from Player 2)
and Unit B loses:
  → Player 1.UnitsDestroyedBy[Player 2 Index]++
```

**2. City Conquest**

```
If a city from Player 1 is conquered by Player 2
and there were defending units:
  → For each destroyed unit:
     Player 2.UnitsDestroyedBy[Player 1 Index]++
```

**3. Unrest-related Destruction**

```
If units are disbanded during anarchy:
  → Self-destruction (do not track)
```

**4. Message Output**

```
After unit destruction, optionally display a message:
  "Unit destroyed! The Romans have now killed 42 of our units."
```

---

## 🔄 Required Changes

### Phase 1: IPlayer Interface (BLOCKER - MUST COME FIRST)

**File:** `src/IPlayer.cs` or `src/Contracts/IPlayer.cs`

**Change:** Add writable property

```csharp
public interface IPlayer
{
    // ... existing properties ...
    
    // BEFORE: read-only?
    ushort[] UnitsDestroyedBy { get; }
    
    // AFTER: writable
    ushort[] UnitsDestroyedBy { get; set; }
}
```

**Alternative:** If `UnitsDestroyedBy` is already writable, no change required.

**Verification:**

```bash
grep -n "UnitsDestroyedBy" src/Contracts/IPlayer.cs
# or
grep -n "interface IPlayer" src/*.cs
```

---

### Phase 2: Unit Tracking Centralization

**New Service:** `src/Game/UnitDestructionTracker.cs`

```csharp
namespace CivOne.Game
{
    /// <summary>
    /// Central place for unit destruction tracking.
    /// Called by Combat, DisbandUnit, etc.
    /// </summary>
    public class UnitDestructionTracker
    {
        private readonly IPlayer[] _players;

        public UnitDestructionTracker(IPlayer[] players)
        {
            _players = players;
        }

        /// <summary>
        /// Registers the destruction of a unit.
        /// </summary>
        /// <param name="unit">The destroyed unit</param>
        /// <param name="destroyedBy">Player who destroyed the unit (null = disaster)</param>
        public void RecordUnitDestruction(IUnit unit, IPlayer destroyedBy)
        {
            if (destroyedBy == null) return;
            if (unit.Owner == destroyedBy) return;  // Ignore self-destruction

            var ownerIndex = Array.IndexOf(_players, unit.Owner);
            var destroyerIndex = Array.IndexOf(_players, destroyedBy);

            if (ownerIndex < 0 || destroyerIndex < 0) return;
            if (destroyerIndex >= destroyedBy.UnitsDestroyedBy.Length) return;

            destroyedBy.UnitsDestroyedBy[ownerIndex]++;
        }

        /// <summary>
        /// Returns the number of destroyed units.
        /// </summary>
        public ushort GetDestroyedCount(IPlayer attacker, IPlayer victim)
        {
            var victimIndex = Array.IndexOf(_players, victim);
            if (victimIndex < 0 || victimIndex >= attacker.UnitsDestroyedBy.Length)
                return 0;
            
            return attacker.UnitsDestroyedBy[victimIndex];
        }
    }
}
```

---

### Phase 3: Integration into Gameplay

#### In `Game.DisbandUnit()`

```csharp
public void DisbandUnit(IUnit unit)
{
    // ... existing code ...
    
    // NEW: Track destruction only if disbanded by another player
    if (unit.Owner != CurrentPlayer)
    {
        _unitDestructionTracker.RecordUnitDestruction(unit, CurrentPlayer);
    }
    
    // ... rest of existing code ...
}
```

#### In Combat Resolution

```csharp
private void ResolveCombat(IUnit attacker, IUnit defender, bool attackerWins)
{
    IUnit loser = attackerWins ? defender : attacker;
    IPlayer winner = attackerWins ? attacker.Owner : defender.Owner;
    
    // NEW: Track destruction
    _unitDestructionTracker.RecordUnitDestruction(loser, winner);
    
    // ... rest of combat logic ...
}
```

---

## 💻 UI/Display Concepts

### Concept 1: Civilopedia - Military Statistics Tab

(unchanged UI mock preserved)

---

### Concept 2: Diplomacy Screen - War Stats Panel

(unchanged UI mock preserved)

---

### Concept 3: In-Game Message after Battle

(unchanged UI mock preserved)

---

## 📅 Implementation Roadmap

### Phase 1: Preparation (Week 1)

| Task                                                | Time  | Status | Priority   |
| --------------------------------------------------- | ----- | ------ | ---------- |
| Verify `IPlayer.UnitsDestroyedBy` writable property | 30min | ⏳ TODO | 🔴 BLOCKER |
| Find combat/disband code                            | 1h    | ⏳ TODO | 🔴 BLOCKER |
| Write `UnitDestructionTracker` class                | 1h    | ⏳ TODO | 🟠 High    |
| Unit tests for `UnitDestructionTracker`             | 1h    | ⏳ TODO | 🟠 High    |

### Phase 2: Core Integration (Week 2-3)

| Task                                | Time | Status | Priority   |
| ----------------------------------- | ---- | ------ | ---------- |
| Implement combat tracking           | 2h   | ⏳ TODO | 🔴 BLOCKER |
| Implement DisbandUnit tracking      | 1h   | ⏳ TODO | 🟠 High    |
| City conquest tracking              | 1.5h | ⏳ TODO | 🟠 High    |
| Integration tests (persistence)     | 1h   | ⏳ TODO | 🟠 High    |
| System tests (combat → save → load) | 1h   | ⏳ TODO | 🟡 Medium  |

### Phase 3: UI Implementation (Week 4-5)

| Task                               | Time | Status | Priority    |
| ---------------------------------- | ---- | ------ | ----------- |
| Civilopedia military tab design    | 2h   | ⏳ TODO | 🟡 Medium   |
| Civilopedia military tab rendering | 3h   | ⏳ TODO | 🟡 Medium   |
| Battle report message              | 1.5h | ⏳ TODO | 🟡 Medium   |
| Diplomacy integration prototype    | 2h   | ⏳ TODO | 🟢 Optional |

### Phase 4: Polish & QA (Week 6)

| Task                            | Time  | Status | Priority    |
| ------------------------------- | ----- | ------ | ----------- |
| Test edge cases                 | 1.5h  | ⏳ TODO | 🟡 Medium   |
| Test older save files           | 1h    | ⏳ TODO | 🟡 Medium   |
| Performance check (large saves) | 1h    | ⏳ TODO | 🟢 Optional |
| Update documentation            | 30min | ⏳ TODO | 🟢 Optional |

**Total estimate: 19.5 hours = ~2–3 weeks part-time**

---

## ⚠️ Critical Points

### 1. Thread Safety

If the combat system is multi-threaded, array access must be synchronized.

### 2. Overflow Handling

The `ushort[]` structure is limited to 65,535 units per opponent.

### 3. Save Compatibility

Older save files must load with initial zero values.

### 4. Replay Consistency

Random destruction events must be replayable consistently.

### 5. Performance Considerations

* Array lookup is O(1) ✓
* `Array.IndexOf()` is O(n) → consider caching
* ~1–2 increments per combat ✓
* No performance concerns expected

### 6. Diplomat Integration

Stats should be visible during peace negotiations.

---

## 📚 References

### Existing Files

* **Persistence:** `src/Persistence/Model/UnitsDestroyedByResolver.cs`
* **Tests:** `xunit/src/persistence/Model/UnitsDestroyedByResolverTest.cs`
* **Mapper:** `src/Persistence/Model/GameStateDtoMapper.cs`
* **Persistence Contracts:** `src/Persistence/Game/Player.Persistence.cs`

### Files to Search

* `src/Game.cs` - DisbandUnit, combat resolution
* `src/Player.cs` - HandleExtinction, disband logic
* `src/City.cs` - City conquest logic
* `src/Unit.cs` - Unit lifecycle
* `src/Combat.cs` or `src/Game/Combat.cs` (?)

---

## 🎯 Next Steps

1. **Verify** whether `IPlayer.UnitsDestroyedBy` is already writable
2. **Search** where units are currently destroyed
3. **Prototype implementation** of `UnitDestructionTracker`
4. **Integration** in 1–2 places (e.g., DisbandUnit)
5. **Test & verify** that save/load works
6. **UI prototype** for statistics display

---

**Last update:** 2026-04-12
**Documentation version:** 1.0
