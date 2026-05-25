# AI Integration Analysis

## 1. Goal and Context

This document analyzes how AI is integrated in CivOne.
Focus is architecture and embedding points in normal gameplay flow.
It does not focus on crashes.

Primary goals:

- Show what AI logic is implemented.
- Show where AI methods are called from the core loop.
- List TODOs and obvious incomplete areas.
- Judge extraction readiness for a future dynamic AI.

## 2. Architecture Overview (General -> Detailed)

Main AI implementation currently lives in class `AI` (partial class):

- [src/AI.cs](src/AI.cs)
- [src/AI.Barbarians.cs](src/AI.Barbarians.cs)

Hard binding to player model:

- Class `Player`: [src/Player.cs](src/Player.cs#L95)

```csharp
internal AI AI => !IsHuman ? AI.Instance(this) : null;
```

This means gameplay code does not consume an abstract top-level AI interface.
It consumes concrete class `AI` through `Player.AI`.

## 3. Entry Points in Normal Game Flow (Where AI gets called)

### 3.1 Unit turn entry point

Class `Turn`: [src/Tasks/Turn.cs](src/Tasks/Turn.cs#L32)

```csharp
Game.CurrentPlayer.AI.Move(_unit);
```

This is the central runtime entry point for AI unit decisions.

Related scheduling path:

- [src/Game.cs](src/Game.cs#L576) enqueues `Turn.Move(unit)` for active non-human flow.

### 3.2 City production entry points

Class `City`: [src/City.cs](src/City.cs#L1366)

```csharp
if (Player.IsHuman)
{
    UpdateAutoBuild();
}
else
{
    Player.AI.CityProduction(this);
}
```

Additional hook:

- [src/City.cs](src/City.cs#L935)

```csharp
AI.Instance(Player).CityProduction(this);
```

### 3.3 Research selection entry point

Class `ProcessScience`: [src/Tasks/ProcessScience.cs](src/Tasks/ProcessScience.cs#L59)

```csharp
if (_human)
    GameTask.Enqueue(new TechSelect(_player));
else
    _player.AI.ChooseResearch();
```

Also called after AI completes a discovery:

- [src/Tasks/ProcessScience.cs](src/Tasks/ProcessScience.cs#L78)

```csharp
_player.AI.ChooseResearch();
```

### 3.4 Barbarian behavior entry point

Class `AI`: [src/AI.cs](src/AI.cs#L41)

```csharp
if (unit.Owner == 0)
{
    BarbarianMove(unit);
    return;
}
```

Barbarian runtime behavior is implemented in class `AI` partial file:

- [src/AI.Barbarians.cs](src/AI.Barbarians.cs)

Spawn trigger path (separate from move behavior):

- [src/Game.cs](src/Game.cs#L346)
- [src/Civilizations/Barbarian.cs](src/Civilizations/Barbarian.cs)

## 4. Implemented AI Behavior

### 4.1 Quick Overview (What + Where)

This section summarizes currently implemented AI behavior at a glance.
Detailed logic and code snippets follow in the next subsections.

| Behavior Area | Main Method(s) | Short Description | Where | Jump |
| --- | --- | --- | --- | --- |
| Unit movement and combat decisions | `Move(IUnit unit)` | Handles settler logic, defensive garrison behavior, and generic movement/attack fallback logic. | [src/AI.cs](src/AI.cs#L35) | [Go to 4.2](#42-class-ai---method-moveiunit-unit) |
| Research selection | `ChooseResearch()` | Picks next research from available techs using a random choice. | [src/AI.cs](src/AI.cs#L227) | [Go to 4.3](#43-class-ai---method-chooseresearch) |
| City production decisions | `CityProduction(City city)` | Applies rule-based production priorities (defense, core buildings, settlers, military/support, fallback). | [src/AI.cs](src/AI.cs#L241) | [Go to 4.4](#44-class-ai---method-cityproductioncity-city) |
| Barbarian unit behavior | `BarbarianMove(IUnit unit)`, `BarbarianMoveWater(IUnit unit)`, `BarbarianMoveLand(IUnit unit)` | Splits barbarian behavior into land/water movement with separate tactical handling. | [src/AI.Barbarians.cs](src/AI.Barbarians.cs#L33) | [Go to 4.5](#45-class-ai-partial---barbarian-methods) |

### 4.2 Class `AI` -> method `Move(IUnit unit)`

File:

- [src/AI.cs](src/AI.cs#L35)

Implemented branches:

- Settler heuristics (city founding + tile improvements + random local move).
- Defensive garrison handling (`Militia`/`Phalanx`/`Musketeers`/`Riflemen`/`MechInf`).
- Generic non-settler movement with `Goto` targeting, attack-cancel heuristics, fallback policies.

Embedding with pathfinding seam:

- [src/AI.cs](src/AI.cs#L141)

```csharp
IAiGotoExecutor gotoExecutor = _gotoExecutorFactory.CreateFor(unit);
AiGotoExecutionResult gotoExecutionResult = gotoExecutor.TryExecute(unit);
```

### 4.3 Class `AI` -> method `ChooseResearch()`

File:

- [src/AI.cs](src/AI.cs#L227)

Current behavior:

- Picks random advance from `Player.AvailableResearch`.
- No weighted strategic planner.

### 4.4 Class `AI` -> method `CityProduction(City city)`

File:

- [src/AI.cs](src/AI.cs#L241)

Current production strategy:

1. Keep baseline defensive units.
2. Build key basic improvements by tech gates.
3. Create settlers by leader profile + city count thresholds.
4. Build military/diplomat/caravan by simple rules.
5. Fallback random production.

### 4.5 Class `AI` partial -> barbarian methods

File:

- [src/AI.Barbarians.cs](src/AI.Barbarians.cs#L33)

Implemented methods:

- `BarbarianMove(IUnit unit)`
- `BarbarianMoveWater(IUnit unit)`
- `BarbarianMoveLand(IUnit unit)`

Barbarian water movement delegates to `IUnitGotoService`:

- [src/AI.Barbarians.cs](src/AI.Barbarians.cs#L119)

```csharp
ITile next = _unitGotoService.GotoStep(unit);
```

## 5. AI Embedding Through Pathfinding Abstractions

Main classes and files:

- `IAiGotoExecutor`: [src/Services/Pathfinding/IAiGotoExecutor.cs](src/Services/Pathfinding/IAiGotoExecutor.cs)
- `AiGotoExecutorFactory`: [src/Services/Pathfinding/AiGotoExecutorFactory.cs](src/Services/Pathfinding/AiGotoExecutorFactory.cs)
- `SmartAiGotoExecutor`: [src/Services/Pathfinding/SmartAiGotoExecutor.cs](src/Services/Pathfinding/SmartAiGotoExecutor.cs)
- `IPathfinder`: [src/Services/Pathfinding/IPathfinder.cs](src/Services/Pathfinding/IPathfinder.cs)
- `PathfinderFactory`: [src/Services/Pathfinding/PathfinderFactory.cs](src/Services/Pathfinding/PathfinderFactory.cs)
- `AStarPathfinderAdapter`: [src/Services/Pathfinding/AStarPathfinderAdapter.cs](src/Services/Pathfinding/AStarPathfinderAdapter.cs)
- `IUnitGotoService`: [src/Services/Pathfinding/IUnitGotoService.cs](src/Services/Pathfinding/IUnitGotoService.cs)
- `UnitGotoServiceImpl2`: [src/Services/Pathfinding/UnitGotoServiceImpl2.cs](src/Services/Pathfinding/UnitGotoServiceImpl2.cs)

Feature toggle entry path:

- Settings field and persistence: [src/Settings.cs](src/Settings.cs#L352)
- UI menu integration: [src/Screens/Setup.cs](src/Screens/Setup.cs#L487)

Factory toggle snippet:

- [src/Services/Pathfinding/AiGotoExecutorFactory.cs](src/Services/Pathfinding/AiGotoExecutorFactory.cs#L29)

```csharp
if (_isComputerPlayerPathfindingEnabled())
{
    return _smartGotoExecutor;
}

return _noOpGotoExecutor;
```

## 6. TODOs and Obvious Incomplete Areas

### 6.1 Explicit TODOs (AI-adjacent)

- `Barbarian` spawn timing/composition:
  - [src/Civilizations/Barbarian.cs](src/Civilizations/Barbarian.cs#L28)
  - [src/Civilizations/Barbarian.cs](src/Civilizations/Barbarian.cs#L52)

- Non-human game-over continuation in turn flow:
  - [src/Tasks/Turn.cs](src/Tasks/Turn.cs#L121)

- Tribal hut barbarian creation marked approximation:
  - [src/Units/TribalHuts/Handler/BarbariansEventHandler.cs](src/Units/TribalHuts/Handler/BarbariansEventHandler.cs#L36)

### 6.2 Obvious architectural gaps

- No gameplay-level AI interface used by turn/city/science pipeline.
- Class `Player` hardwires concrete `AI`.
- One concrete AI class mixes multiple concerns (unit tactics, city production, research, barbarian behavior).
- Many static/global dependencies (`Game`, `Common.Random`, `Settings.Instance`, `Map.Instance`) reduce extraction flexibility.

## 7. Extraction Readiness (for Dynamic AI)

### 7.1 Reusable seams already present

- `IAiGotoExecutor` / `IPathfinder` pathfinding seam is already interface-based.
- `IUnitGotoService` gives a separate seam for barbarian navigation.

### 7.2 Main blockers

- Missing top-level interface like `IPlayerAiController` consumed by `Turn`, `City`, `ProcessScience`.
- `Player.AI` exposes concrete class instead of abstraction.

### 7.3 Practical refactor order

1. Introduce gameplay AI contract and route all entry points through it.
2. Keep current class `AI` as default adapter implementation.
3. Split decisions into specialized strategy services.
4. Move random/world access behind injectable ports.

## 8. Test Evidence Around Current AI Architecture

- `AIBehaviorTests`: [xunit/src/AIBehaviorTests.cs](xunit/src/AIBehaviorTests.cs)
- `AiGotoExecutorTests`: [xunit/src/AiGotoExecutorTests.cs](xunit/src/AiGotoExecutorTests.cs)
- `UnitGotoServiceImplTests`: [xunit/src/UnitGotoServiceImplTests.cs](xunit/src/UnitGotoServiceImplTests.cs)

These tests validate local AI mechanisms (production/path execution/path cost logic), but there is no broad end-to-end strategic AI test suite.

## 9. Bottom Line

AI is integrated deeply into normal gameplay flow via hard call sites in `Turn`, `City`, and `ProcessScience`.
Movement/pathfinding already has useful abstraction seams.
For a future dynamic AI, key prerequisite is replacing concrete `Player.AI` usage with a gameplay-level AI interface across all entry points.
