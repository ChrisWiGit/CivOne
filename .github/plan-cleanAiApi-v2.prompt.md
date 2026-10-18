# Plan: Clean AI-API v2 — Turn Session + Dynamic Context + Command Gateway

## Goal

Build a clean, testable AI API in `api/` that stays fully independent from internal singletons such as `Game.Instance`, `Map.Instance`, and `Settings.Instance`.
External or alternative AI implementations must only see API interfaces and stable DTOs.
They must never depend on internal CivOne runtime types.

Compared to the previous plan, this version removes the action sink model completely.
Turn-based AI gets exactly one entry point per turn.
The AI reads the current allowed game state through a dynamic readonly context view and executes validated commands through a command gateway.

Default AI behavior must stay unchanged from a gameplay perspective.

---

## Core idea

Human players already operate through one turn flow:
they inspect visible state, decide actions, execute actions immediately, and stop when they end their turn.

Turn-based AI should follow the same model.

New contract:

1. Host starts one AI turn session for one civilization.
2. AI receives one turn entry point.
3. AI reads live readonly state through the session context.
4. AI executes commands through validated gateways.
5. Engine applies valid commands immediately.
6. Engine exposes resulting state changes through an event journal.
7. AI ends its turn explicitly, or host ends it automatically when the call returns.

This removes scattered AI hooks such as unit-turn, city-turn, and research-turn callbacks from the public AI contract.

---

## Context

### Problem: current AI shape couples behavior to internal runtime flow

Current runtime AI behavior depends on internal engine entry points and singleton access.
That creates several problems:

- External AI would inherit singleton coupling.
- AI behavior splits across multiple entry points.
- Snapshot reuse and consistency become hard to reason about.
- Future plugin AI registration stays unclear.
- Runtime validation spreads across several dispatch paths.

### Existing foundations

- `api/src/IPlugin.cs` provides style reference for public API interfaces.
- `api/src/IGameData.cs` shows existing public data granularity.
- `src/IGame.cs` contains query-oriented patterns already used inside runtime.
- `src/Services/Pathfinding/IAiGotoExecutor.cs` shows clean interface-driven separation.
- `src/BaseInstance.cs` stays internal and must not become part of the new AI contract.

### Important architectural shift

Old model:

- multiple runtime-driven hooks
- immutable turn snapshots
- action sink output

New model:

- one turn entry point
- dynamic readonly context view
- direct validated command execution
- event journal for incremental resync

---

## Public API shape (`api/src/AI/`)

### Main turn-based controller

```csharp
public interface ITurnBasedController
{
  void OnTurn(ITurnSession session);
}
```

This is the only turn-based gameplay entry point for AI.
No separate `OnUnitTurn`, `OnCityTurn`, or `OnResearch` public methods remain.

### Turn session

```csharp
public interface ITurnSession
{
  ITurnContext Context { get; }
  IEventJournal Events { get; }
  ITurnCommandGateway Commands { get; }
  void EndTurn();
}
```

Responsibilities:

- `Context` contains live readonly truth.
- `Events` exposes journal-based change tracking.
- `Commands` executes validated actions immediately.
- `EndTurn()` ends the current AI turn explicitly.

If `OnTurn(...)` returns without calling `EndTurn()`, host ends the turn automatically and logs a diagnostic note.

### Context is live readonly truth

`ITurnContext` is not a snapshot.
It is a readonly live view against the current engine state.
Repeated reads may return different results if previous commands changed the game state.

Rules:

- AI can read current allowed state at any time during its turn.
- AI can never mutate runtime state through the context.
- Context only exposes information the current player is allowed to know.
- Context remains turn-based only in this phase.

### Event journal is hint system, not truth

`IEventJournal` exists to tell AI what changed.
It is not the source of truth.
Source of truth always stays `ITurnContext`.

Rules:

- Events describe state or visibility changes.
- Events never replace context reads.
- Command failures do not enter the journal.
- Events are read in ascending sequence order.

### Command gateway

```csharp
public interface ITurnCommandGateway
{
  IUnitCommandGateway Units { get; }
  ICityCommandGateway Cities { get; }
  IResearchCommandGateway Research { get; }
}
```

Diplomacy commands are intentionally out of first implementation scope.
Phase 2 will add dedicated diplomacy interfaces without changing the turn-session model.

Commands work on stable runtime IDs.
They return result objects instead of throwing for normal gameplay failure.

---

## Data model

### Stable runtime IDs

All important runtime entities used by AI get stable IDs stored in runtime and persisted in save data.

Included:

- units
- cities
- civilizations
- turn-based AI implementations

Rules:

- IDs are globally unique.
- IDs are never reused.
- IDs survive save/load.
- AI may only act on its own controllable entities, even if foreign visible entities also expose IDs.

### View interfaces

Public DTO/view interfaces stay readonly and independent from runtime types.
Expected views include at least:

- `IUnitView`
- `ICityView`
- `ITileView`
- `IMapView`
- `ICivilizationView`

Each view uses stable IDs plus public enums or public data contracts.

### Visibility model

Turn-based AI follows player fog-of-war rules.

- `visible` data shows current full information.
- `explored` data shows last known terrain-level information.
- unseen tiles return no information.
- foreign units and cities only appear when currently visible.
- last known city state may expose explicitly tracked fields such as previously seen city size where runtime already supports it.

If a command reveals new map information, AI can observe that by re-reading context and by reading new journal events.

---

## Event journal (`api/src/AI/Events/`)

### Journal contract

```csharp
public interface IEventJournal
{
  EventReadResult ReadSince(ulong sequence);
  ulong CurrentSequence { get; }
}
```

```csharp
public readonly record struct EventReadResult(
  bool CursorExpired,
  bool RequiresFullResync,
  ulong FromSequence,
  ulong ToSequence,
  IReadOnlyList<AgentEvent> Events);
```

Rules:

- Journal is append-only from AI perspective.
- Events are never popped or destructively consumed.
- Each event has monotonically increasing `ulong` sequence number.
- `ReadSince(sequence)` returns newer events in ascending order.
- Journal uses bounded retention internally.
- If requested sequence is too old, result sets `CursorExpired = true` and `RequiresFullResync = true`.

AI reaction to expired cursor:

1. discard incremental assumptions
2. re-read full context
3. move local cursor to current valid journal position

### What belongs in journal

Journal contains only real engine changes or visibility changes relevant to AI.

Examples:

- unit moved into newly visible area
- new tiles became visible
- city production completed
- research completed
- own unit destroyed
- own city lost
- wonder completed and became known

Journal does not contain command validation errors.
Those stay in command results.

### Command-to-journal bridge

Every command result exposes sequence boundaries so AI can read exactly the journal slice that followed the command.

```csharp
public interface ICommandResult
{
  bool Success { get; }
  string ErrorCode { get; }
  string? ErrorMessage { get; }
  ulong SequenceBefore { get; }
  ulong SequenceAfter { get; }
}
```

Typical AI pattern:

1. execute command
2. inspect `Success`
3. if needed call `ReadSince(result.SequenceBefore)`
4. process returned events
5. re-read affected context data

This solves the “what changed after move/attack/build” problem without destructive event handling.

---

## Command model (`api/src/AI/Commands/`)

### General rules

- Commands apply immediately when valid.
- Commands never mutate state silently.
- Gameplay-invalid commands return error codes.
- Host does not expect AI to catch gameplay errors via exceptions.
- Exceptions remain reserved for severe technical failures.

### Example command groups

Unit commands:

- move own unit by ID
- fortify own unit
- wake own unit
- clear goto
- set goto
- found city
- build road
- build irrigation
- build mine
- disband

City commands:

- choose production for city by ID

Research commands:

- choose current research

### Command failure behavior

Examples:

- invalid destination
- blocked move
- wrong unit type for build action
- city not owned by current player
- per-type action limit exceeded
- total action limit exceeded

Command returns failure result.
AI may continue unless host policy ends the turn because a hard limit was reached.

### Host-side action limits

Limits belong to host policy, not AI API semantics.

Rules:

- configurable per action type
- configurable global per-turn limit
- failed commands still count toward limits
- when a hard limit is reached, further commands return failure and host ends the turn

This protects runtime from endless AI loops such as repeated rail movement.

---

## AI registration and metadata (`api/src/AI/Registration/`)

### Public registration contract

```csharp
public interface IAgentInformation
{
  string GetName();
  string GetAuthor();
  (int Major, int Minor, int Patch) GetVersion();
  string GetDescription();
  Guid GetUuid();
}
```

```csharp
public interface IAgentMemory
{
  void SetMemory(string yaml);
  string GetMemory();
}
```

```csharp
public interface IAgentRegistration
{
  IAgentInformation GetInformation();
  IAgentMemory GetMemory();
  ITurnBasedController GetTurnBasedController();
}
```

Purpose:

- identify AI implementation
- expose stable AI UUID
- support YAML-based persistent AI memory
- expose turn-based controller implementation

### Memory rules

AI memory is host-owned only at save/load boundaries.

Rules:

- host loads savegame
- host finds AI by UUID
- host calls `SetMemory(yaml)` with saved content
- AI keeps memory internally while game runs
- host calls `GetMemory()` only when saving game
- on AI exception, host may call `GetMemory()` for logging/debugging

There is no periodic write-back after each turn.
That avoids persisting state that no longer matches current savegame timeline.

Memory storage format:

- intended format is YAML for readability and debugging
- actual persisted file naming can be savegame-based and UUID-based
- storage details stay host implementation detail

### Host infrastructure stays internal

Public API includes registration interfaces.
Internal registry, loader, plugin scanning, player binding, and runtime mapping stay inside host implementation.

Examples internal only:

- agent registry
- DLL loader
- savegame to agent binding
- player to agent mapping

---

## Runtime model in game core (`src/`)

### Core host classes

| Class | Responsibility |
|---|---|
| `TurnBasedAgentHost` | Starts turn session, calls controller, catches exceptions, enforces limits, auto-ends turns |
| `TurnSession` | Concrete runtime implementation of `ITurnSession` |
| `TurnContext` | Concrete live readonly context implementation |
| `EventJournal` | Per-player journal storage with bounded retention and monotonic sequence numbers |
| `TurnCommandGateway` | Root command gateway implementation |
| `UnitCommandGateway` | Unit command validation and execution |
| `CityCommandGateway` | City production command validation and execution |
| `ResearchCommandGateway` | Research command validation and execution |
| `AgentBindingResolver` | Resolves which registered agent controls which player |
| `DefaultTurnBasedAgent` | New adapter around current built-in AI behavior |

### Exception policy

Host catches exceptions thrown by AI code during `OnTurn(...)`.

Rules:

- gameplay-invalid command: return error code, AI may continue
- AI exception: log error, capture AI memory for diagnostics, end current AI turn immediately
- game must not crash because plugin AI failed

### Scheduling policy

Initial implementation uses sequential civilization turns.
One civilization acts at a time, in normal game turn order.

Scheduling strategy must be abstracted so future strategies can exist, for example:

- after all human input
- regular turn order
- future parallel/experimental scheduling

Turn-based API does not implement realtime behavior.
Realtime support will use separate interfaces later.

---

## Runtime integration plan

### Single gameplay hook

Runtime should converge to one public AI turn entry point per controlled civilization turn.
Engine remains authoritative for legality and execution.

High-level flow:

```text
Host selects current AI-controlled civilization
  -> build turn session
  -> call ITurnBasedController.OnTurn(session)
    -> AI reads session.Context
    -> AI reads session.Events
    -> AI executes session.Commands.*
    -> engine applies valid commands immediately
    -> engine appends resulting journal events
    -> AI ends with session.EndTurn()
  -> if AI returned without EndTurn(), host ends turn automatically
```

### Legacy bridge strategy

Current `AI` implementation remains gameplay reference.
During migration, a new built-in turn-based agent adapts old AI behavior into the new session/command model.

Target state:

- old `Player.AI` field stops being public execution contract
- old unit/city/research hook calls stop being primary AI API surface
- built-in AI runs through same turn-based host path as future external agents

### Internal runtime rewiring

Expected runtime changes:

- `Player` binds to resolved registered agent instead of calling legacy AI directly
- turn orchestration creates one turn session per AI player turn
- command gateways internally call existing movement, production, research, and order systems
- event journal receives entries from engine state changes and visibility changes
- built-in AI no longer depends on public sink interfaces because sink model is removed

---

## Scope

### Included in first implementation

- one turn-based entry point
- live readonly turn context
- event journal with sequence cursor model
- unit commands
- city production commands
- research commands
- stable runtime IDs persisted in save data
- YAML-based AI memory save/load contract
- host-side limit enforcement
- built-in AI adapter through new contract

### Explicitly out of scope for first implementation

- full diplomacy runtime behavior
- out-of-turn negotiation responders
- realtime AI interfaces
- asynchronous AI host execution
- Barbarian AI migration
- human city autobuild changes
- plugin loading polish beyond minimal registration plumbing

Human autobuild stays unchanged.
Barbarian AI stays internal until normal AI contract stabilizes.
Diplomacy becomes Phase 2.

---

## Phase 2 — Diplomacy extension

Diplomacy is intentionally prepared, not implemented, in first pass.

Planned later:

- diplomacy command gateway
- negotiation state model
- human response flow
- out-of-turn AI response path
- stable negotiation IDs

Turn session model stays valid.
Diplomacy will plug into that model rather than replace it.

---

## Verification criteria

1. Public turn-based AI contract exposes one gameplay entry point: `ITurnBasedController.OnTurn(ITurnSession)`.
2. Public API contains no action sink interfaces.
3. Public turn context is live readonly view, not immutable snapshot.
4. `ITurnSession` cleanly separates truth (`Context`), hints (`Events`), and mutations (`Commands`).
5. Event journal uses monotonic `ulong` sequence numbers and ascending reads.
6. Journal expiry signals full resync requirement explicitly.
7. Command failures stay in `ICommandResult`, not in journal.
8. Command results expose `SequenceBefore` and `SequenceAfter`.
9. Stable runtime IDs exist for AI-relevant entities and survive save/load.
10. AI memory roundtrip uses `SetMemory(string)` on load and `GetMemory()` on save.
11. AI exception ends only current AI turn, not whole game.
12. Host can log AI memory on exception for debugging.
13. Human autobuild behavior remains unchanged.
14. Diplomacy remains out of first implementation scope.
15. Built-in AI runs through same turn host path as future external agents.

---

## Migration strategy

### Step 1

Define new public interfaces in `api/`:

- registration
- metadata
- memory
- turn session
- turn context
- event journal
- command gateways
- command results
- readonly views

### Step 2

Add stable runtime IDs and save/load support where missing.

### Step 3

Implement host runtime pieces in `src/`:

- session
- context
- journal
- command gateways
- agent binding resolver
- turn host

### Step 4

Wrap current built-in AI in `DefaultTurnBasedAgent` using new contract.

### Step 5

Switch normal AI player execution from legacy scattered calls to turn-session host.

### Step 6

Add focused tests for:

- turn entry and auto-end behavior
- live context after command execution
- journal read ordering
- `CursorExpired` full resync signal
- command result sequence range
- invalid command result handling
- save/load memory roundtrip
- AI exception containment
- regression against built-in AI behavior

---

## Final design summary

Turn-based AI no longer submits actions into a sink.
It runs inside one host-controlled turn session.

`Context` is truth.
`Events` is hint journal.
`Commands` mutate state immediately or return failure.

This keeps engine authority inside runtime, keeps public AI contracts clean, and gives external AI implementations one stable mental model that matches how a human plays a turn.

## Additional

noch info: reasearch, production von units, buildings, wonders sollen über die internen namen gehen (keine uuid). also barracks (so glaub heißt das) baut man dann in der city.
