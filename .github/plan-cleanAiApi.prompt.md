# Plan: Clean AI-API unabhängig von Singletons

## Ziel

Aufbau einer sauberen, testbaren AI-API im `api/`-Projekt, die vollständig unabhängig von den internen Singletons (`Game.Instance`, `Map.Instance`, `Settings.Instance`) ist. Externe oder alternative KI-Implementierungen sollen nur diese Interfaces sehen — kein Zugriff auf interne CivOne-Typen.

Die interne `AI`-Klasse wird zum Adapter, der zwischen Singletons und der API vermittelt. Default-KI-Verhalten bleibt erhalten (Regressionsschutz).

---

## Kontext

### Problem: BaseInstance koppelt AI an Singletons

```
AI : BaseInstance
  └── Game.Instance   (Singleton)
  └── Map.Instance    (Singleton)
  └── Settings.Instance (Singleton)
```

Jede externe KI würde diese Kopplung erben — oder direkt gegen interne Typen kompilieren.

### Bestehende Grundlagen

- `api/src/IPlugin.cs` — Stil-Referenz für API-Interfaces
- `api/src/IGameData.cs` — Granularität der Spieldaten
- `api/src/IModification.cs` — leer, Ausgangspunkt
- `src/IGame.cs` — `IGame`, `IGameUnitsQuery`, `IGamePlayerQuery` etc.
- `src/Services/Pathfinding/IAiGotoExecutor.cs` — bereits sauberes Interface-Muster
- `src/BaseInstance.cs` — Singleton-Hub, darf von AI-API **nicht** genutzt werden

### Current runtime entry points

The plan must follow the actual game loop. The current AI does not have one central callback. It is invoked through three proven runtime entry points and one optional future hook:

1. **Unit turn**
  - `Game.EndTurn()` enqueues `Turn.New(unit)` for each unit of the active player.
  - `Game.Update()` turns that into `Turn.Move(unit)` for non-human players.
  - `Tasks/Turn.Step()` calls `Game.CurrentPlayer.AI.Move(_unit)`.
  - This is the real entry point for unit movement decisions.

2. **City production**
  - `City.NewTurn()` resolves food, shields, maintenance, production completion, and science.
  - At the end of that flow it calls `Player.AI.CityProduction(this)` for non-human players.
  - This is the real entry point for choosing new production.

3. **Research choice**
  - `Tasks/ProcessScience.Run()` handles discovery and future-tech flow.
  - When an AI player needs a technology choice it calls `_player.AI.ChooseResearch()`.
  - This is the real entry point for research selection.

4. **Optional future player-turn hook**
  - `Game.EndTurn()` also enqueues `Turn.New(CurrentPlayer)`.
  - This ends up in `Player.NewTurn()` and is a good candidate for a future `OnTurnStart(...)` hook.
  - It should stay informational in phase 1, not the primary action-return channel.

### Current action return paths

The current AI writes decisions back into the game in several different ways:

- Unit movement via `unit.MoveTo(...)`
- Unit state changes via `unit.Goto`, `unit.SkipTurn()`, `unit.Fortify = true`
- Destructive actions via `Game.DisbandUnit(unit)`
- Settler and order actions via `GameTask.Enqueue(Orders.*)`
- City production via `city.SetProduction(...)`
- Research selection via `Player.CurrentResearch = ...`

The new API must replace direct runtime mutation with explicit engine-side action translation.

---

## Phase 1 — Read-only AI-Context-Views (`api/src/AI/`)

New interfaces, readonly only, no internal runtime types:

| Interface | Inhalt |
|---|---|
| `IAiUnitView` | Position, Typ, Owner, MovesLeft, Goto, UnitClass, Role |
| `IAiCityView` | Position, Owner, Size, Production, Gebäudeliste |
| `IAiTileView` | Terrain-Typ, Sichtbarkeit, Improvements, Units (by view) |
| `IAiMapView` | `IAiTileView GetTile(int x, int y)`, Width, Height |
| `IAiPlayerView` | Gold, Forschung, Regierung, Advances, Städte, Einheiten |
| `IAiContext` | Aggregiert alles: `OwnPlayer`, `OwnUnits`, `OwnCities`, `Map`, `GameTurn` |

Recommended context split by entry point:

| Interface | Purpose |
|---|---|
| `IAiTurnContext` | Shared player-level state for one AI turn |
| `IAiUnitTurnContext` | Unit-specific decision context |
| `IAiCityTurnContext` | City-specific decision context |
| `IAiResearchContext` | Research-specific decision context |

`IAiContext` can remain as a common root object, but the engine should pass narrower entry-point contexts where possible.

---

## Phase 2 — Command-Modell (`api/src/AI/`)

Commands represent AI decisions without direct runtime mutation:

| Interface | Methoden |
|---|---|
| `IAiUnitCommand` | `Move(int dx, int dy)`, `SetGoto(int x, int y)`, `Fortify()`, `Disband()`, `FoundCity()` |
| `IAiCityCommand` | `ChooseProduction(string productionType)` |
| `IAiPlayerCommand` | `ChooseResearch(string advanceId)`, `SetTaxRate(int rate)` |

This phase should also define the engine-facing result model more explicitly:

- `IAiUnitAction` or `IAiUnitCommandSink` for unit actions
- `IAiCityDecision` for production selection
- `IAiResearchDecision` for research selection

Recommended rule: keep `GameTask`, `Orders.*`, `IUnit`, `City`, `Player`, and `Game` out of the public AI API. Those remain engine implementation details.

---

## Phase 3 — AI-Haupt-Interface (`api/src/AI/`)

The interface every AI implementation should target:

```csharp
public interface IAiPlayer
{
    void OnTurnStart(IAiContext context, IAiPlayerCommand command);
    void OnUnitTurn(IAiContext context, IAiUnitView unit, IAiUnitCommand command);
    void OnCityTurn(IAiContext context, IAiCityView city, IAiCityCommand command);
   void OnResearch(IAiResearchContext context, IAiPlayerCommand command);
}
```

`OnResearch(...)` is a first-class entry point because research is currently triggered from `ProcessScience`, not from unit or city logic.

Keep the interface synchronous in phase 1. Async can be added later if an out-of-process or LLM-based AI host is introduced.

---

## Phase 4 — Adapter im Spielkern (`src/`)

| Klasse | Aufgabe |
|---|---|
| `AiContextFactory` | Einzige Klasse, die Singletons kennt; baut `IAiContext` aus `Game.Instance`/`Map.Instance` |
| `AiPlayerAdapter` | Nimmt `IAiPlayer`, baut Context via Factory, delegiert Entscheidungen an die passenden Entry Points |
| `AiActionApplier` | Übersetzt AI-Entscheidungen zurück in Engine-Aktionen wie `MoveTo`, `SetProduction`, `CurrentResearch`, `Orders.*` und `GameTask` |
| `DefaultAiPlayer` | Implementiert `IAiPlayer` mit dem bestehenden Verhalten aus `AI.cs` |
| `AI.cs` (refactor) | Wird zu einem dünnen Legacy-Adapter oder verschwindet schrittweise zugunsten von `DefaultAiPlayer` |

Required wiring in the current runtime:

- `src/Tasks/Turn.cs`: replace `Game.CurrentPlayer.AI.Move(_unit)` with adapter-based unit dispatch
- `src/City.cs`: replace `Player.AI.CityProduction(this)` with adapter-based city dispatch
- `src/Tasks/ProcessScience.cs`: replace `_player.AI.ChooseResearch()` with adapter-based research dispatch
- `src/Player.cs`: keep `Player.NewTurn()` as optional future hook for `OnTurnStart(...)`

---

## Scope

**Eingeschlossen (Phase 1-4):**
- Unit-Movement (inkl. Settlers, Goto-Logik)
- City-Production-Entscheidung
- Tech-Research-Entscheidung

**Ausgeschlossen vorerst:**
- Diplomatie / Kriegserklärung
- Barbaren-Logik (bleibt in `AI.Barbarians.cs`)
- Government-Wechsel

Barbarian AI should remain internal until the normal player AI contract is stable. Its current behavior is special-case logic and should not shape the first public AI API.

---

## Execution model

The engine should own the turn sequence and push state into AI handlers.

Recommended phase-1 flow:

1. Engine schedules unit, city, and research decisions exactly where it already does today.
2. Adapter builds a narrow readonly context for that specific entry point.
3. AI returns one decision through command/result abstractions.
4. `AiActionApplier` translates that decision into runtime mutations.
5. The existing game loop and `GameTask` ordering remain unchanged.

This keeps the API stable even if the internal engine implementation changes later.

---

## Verifikationskriterien

1. `IAiPlayer`-Implementierung hat **kein** `using CivOne;` auf interne Typen
2. `AiContextFactory` ist die **einzige** Klasse mit Singleton-Zugriff
3. Unit-turn flow remains `Game.EndTurn()` -> `Turn.Move(unit)` -> AI adapter -> action applier
4. City production is still chosen only after `City.NewTurn()` resolves economy and build completion
5. Research selection is still triggered only from `ProcessScience`, preserving discovery and future-tech sequencing
6. Default AI (`DefaultAiPlayer`) behaves like the current implementation (tests green)
7. `AI.cs` no longer contains core decision logic once migration is complete

---

## Designentscheidungen (festgelegt)

### Unit-Aktions-Modell
Die KI gibt `IReadOnlyList<AiUnitAction>` zurück. Mehrere Aktionen pro Runde sind erlaubt. Das spiegelt das aktuelle Verhalten von `AI.Move()` wider, das in einer Schleife mehrere Teil-Schritte ausführen kann. `AiUnitAction` sollte eine sealed-Klassen-Hierarchie oder ein Discriminated-Union-ähnliches Pattern sein:

```csharp
// api/src/AI/
public abstract record AiUnitAction;
public sealed record MoveAction(int Dx, int Dy) : AiUnitAction;
public sealed record SetGotoAction(int X, int Y) : AiUnitAction;
public sealed record FortifyAction : AiUnitAction;
public sealed record DisbandAction : AiUnitAction;
public sealed record SkipTurnAction : AiUnitAction;
// Settler-spezifisch:
public sealed record FoundCityAction : AiUnitAction;
public sealed record BuildRoadAction : AiUnitAction;
public sealed record BuildIrrigationAction : AiUnitAction;
public sealed record BuildMineAction : AiUnitAction;
```

`AiActionApplier` iteriert die Liste und führt jede Aktion aus. Unbekannte Aktionstypen für eine Einheitenklasse werden ignoriert oder geloggt.

### Produktions-Identifier
Stabile numerische ID aus einem öffentlichen Enum `AiProductionId` in `api/`. Kein raw string, keine internen `IProduction`-Typen. `AiActionApplier` mapped `AiProductionId` auf den konkreten `IProduction`-Typ intern.

```csharp
// api/src/AI/
public readonly record struct AiProductionChoice(AiProductionKind Kind, ushort Id);
public enum AiProductionKind { Unit, Building, Wonder }
```

### Forschungs-Identifier
Numerische Advance-ID (ushort), identisch mit `IGameData.CurrentResearch`. Kein String.

```csharp
// api/src/AI/
public readonly record struct AiResearchChoice(ushort AdvanceId);
```

### Kartensichtbarkeit
`IAiContext` erhält einen Parameter `bool respectFogOfWar`. Default: `true` (Fog of War aktiv). Die `AiContextFactory` filtert Tiles entsprechend. So kann eine Implementierung für Debugging oder für sehr starke KI Vollsicht anfordern.

### Settler-Aktionen
Unit-Type-spezifische Unterinterfaces: Settlers bekommen `IAiSettlerCommand : IAiUnitCommand`. Entsprechend liefert `OnUnitTurn(...)` bei einem Settler-Kontext den engeren Typ oder die KI prüft den `UnitType` im Context und gibt nur Aktionen zurück, die für den Einheitentyp sinnvoll sind. `AiActionApplier` ignoriert eine `BuildRoadAction` für einen Krieger stillschweigend.

### Registrierung externer KI
Separates `IAiPlayerFactory`-Interface im `api/`-Projekt:

```csharp
// api/src/AI/
public interface IAiPlayerFactory
{
    string Name { get; }
    IAiPlayer CreateFor(byte playerId);
}
```

Das Spiel kann mehrere `IAiPlayerFactory`-Implementierungen registrieren (Dictionary per Spieler-Index). `AiPlayerAdapter` fragt pro AI-Spieler die passende Factory an. Das entkoppelt Metadaten (Name, Versionsinfo) vollständig von der Entscheidungslogik.

### Migrations-Strategie
Big Bang: `AI.cs` wird vollständig in `DefaultAiPlayer` umgeschrieben, alle drei Einstiegspunkte (`Turn.cs`, `City.cs`, `ProcessScience.cs`) in einem Commit umgestellt. `AI.cs` und `AI.Barbarians.cs` bleiben für Barbaren-Logik erhalten. Tests müssen vor dem Umbau grün sein und danach wieder grün sein.

---

## Noch offene Fragen

- Wie detailliert soll `IAiTileView` sein (Ressourcen, Special Resources, Kontinent-ID)?
  - Alles
- Soll `IAiContext` eine Referenz auf bekannte feindliche Einheiten/Städte enthalten, oder ist das Teil des Map-Views?
  - Map View
