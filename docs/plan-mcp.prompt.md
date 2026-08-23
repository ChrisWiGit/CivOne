# MCP Screenshot Prototype Plan (P0)

## 1) Purpose

This document defines a minimal first implementation to connect CivOne with MCP for AI-assisted UI checks.

P0 goal: expose only screenshot capture so an LLM can inspect rendered UI and validate expected elements visually.

---

## 2) Scope

### In scope (P0)

1. Full-frame screenshot capture
1. Optional region capture
1. Metadata per capture (`gameTick`, timestamp, width, height)
1. One MCP tool to request screenshots
1. Artifact persistence for debug/repro

### Out of scope (P0)

- Action/control API
- Snapshot/state API
- Scenario/reset API
- Event stream API
- Multiplayer/network simulation
- Public internet exposure

---

## 3) Design Principles

1. Keep scope minimal: rendering only.
1. Keep interface stable: versioned request/response DTOs.
1. Keep integration local and safe: local transport + session token.
1. Keep output reproducible: fixed image format + metadata.

---

## 4) Target Architecture

### 4.1 In-game port

Add one render automation interface:

- `IGameAutomationRenderPort`

Responsibilities:

- capture full frame,
- capture optional region,
- return PNG and metadata.

Use dependency injection; do not instantiate dependencies directly.

Suggested folders:

- `src/Services/Automation/` (port + implementation)
- `src/Concepts/Automation/` (DTOs)

### 4.2 MCP adapter process

MCP adapter maps tool calls to `IGameAutomationRenderPort`.

Responsibilities:

- validate request schema,
- apply limits/timeouts,
- persist artifacts,
- return response to MCP client.

### 4.3 Transport

Recommended:

1. Named Pipes (Windows-first)
1. Unix domain socket (optional later)
1. Localhost HTTP only if needed

No remote/public endpoint in P0.

### 4.4 Deployment options: MCP inside game vs separate process

Both options are valid.

#### Option A: In-process (MCP hosted inside CivOne)

Pros:

- smallest setup for first prototype,
- direct access to render pipeline,
- fewer infrastructure components.

Cons:

- MCP faults can impact game process stability,
- harder fault isolation,
- security boundaries are weaker.

#### Option B: Out-of-process (recommended long-term)

Pros:

- better isolation and reliability,
- easier to debug and restart independently,
- cleaner security and resource boundaries.

Cons:

- additional IPC layer required,
- slightly more operational complexity.

#### Practical recommendation

For P0 (screenshot-only), in-process hosting is acceptable and fastest.

Use a feature flag, local-only activation, and strict timeout/size guards.

When action/state tools are added, migrate to out-of-process MCP.

---

## 5) Contract Design

### 5.1 Request DTO

`CaptureScreenshotRequest`:

- `sessionId`
- `mode` (`full` or `region`)
- `region` (`x`, `y`, `width`, `height`) when `mode=region`
- `scale` (optional)
- `includeCursor` (optional)

### 5.2 Response DTO

`CaptureScreenshotResponse`:

- `schemaVersion`
- `sessionId`
- `gameTick`
- `capturedAtUtc`
- `width`
- `height`
- `format` (`png`)
- `artifactPath` (or byte payload)

### 5.3 Validation rules

- reject invalid/negative region values,
- clamp region to framebuffer bounds,
- reject oversized payload requests,
- return typed error codes for invalid input/timeouts.

---

## 6) MCP Tool Surface (P0)

Expose two tools in P0:

1. `game_capture_screenshot`
1. `game_capture_region`

Optional P0 extensions:

1. `game_get_render_metadata`

Notes:

- `game_capture_region` uses `x`, `y`, `width`, `height`.
- Region values are validated and clamped to framebuffer/canvas bounds.
- Region capture should reuse existing crop behavior in rendering bitmap composition path.

---

## 7) Determinism and Reliability

1. Capture at a stable render lifecycle point.
1. Attach `gameTick` to each capture.
1. Use fixed PNG encoding settings.
1. Add lightweight throttle/debounce to avoid noisy duplicates.

---

## 8) Security and Safety

1. Local-only binding.
1. Session token required.
1. Strict request schema validation.
1. Size limits for image outputs.
1. Per-call timeout.
1. Structured audit log.

---

## 9) Observability

Persist per session:

- request/response log,
- capture metadata,
- screenshot artifacts,
- error details.

Recommended folder:

- `temp/mcp-runs/<session-id>/`

---

## 10) Phased Delivery Plan

### Phase 0 — Contract + flag (0.5-1 day)

- define DTOs,
- add `AutomationEnabled` feature flag,
- define artifact directory layout.

Deliverable: reviewed screenshot contracts and wiring plan.

### Phase 1 — In-game render port (1-2 days)

- implement `IGameAutomationRenderPort`,
- implement full-frame capture,
- implement optional region capture,
- return metadata + PNG artifact.

Deliverable: in-process screenshot calls working.

### Phase 2 — MCP adapter (1-2 days)

- implement `game_capture_screenshot`,
- add validation, timeout, logging,
- persist artifacts by session.

Deliverable: MCP client receives screenshots from running game.

### Phase 3 — LLM visual checks (1-2 days)

Define 2-3 simple checks:

1. Main screen renders completely.
1. City view contains expected panel zones.
1. One known dialog renders correctly.

Deliverable: repeatable screenshot-based smoke checks.

---

## 11) Acceptance Criteria (P0)

1. MCP can request full-frame screenshot and receive valid PNG.
1. Region capture works and is bounds-safe.
1. Every artifact has metadata (`gameTick`, timestamp, dimensions).
1. Failures produce reproducible logs and stored artifacts.
1. At least 2 visual smoke checks run repeatably on same build.

---

## 12) Risks and Mitigations

1. **Risk:** capture timing inconsistency
   - **Mitigation:** fixed capture point in render cycle.

1. **Risk:** performance overhead
   - **Mitigation:** throttle, optional region capture.

1. **Risk:** unstable visual diffs
   - **Mitigation:** fixed PNG settings + metadata + same resolution.

---

## 13) Initial Backlog

1. Add automation feature flag.
1. Add screenshot DTOs.
1. Add render capture service abstraction.
1. Add filesystem artifact writer.
1. Add MCP screenshot tool.
1. Add 2 screenshot-based smoke checks.
1. Add troubleshooting notes.

---

## 14) Definition of Done

P0 is done when:

- screenshot MCP tool is available and documented,
- screenshots + metadata can be captured reliably,
- artifacts/logs are produced for each run,
- at least 2 visual checks are repeatable.

---

## 15) Next Step After P0

If P0 is stable, add in this order:

1. Read-only snapshot API
1. Minimal action API (`MouseDown`, `MouseUp`, `WaitTicks`)
1. Scenario/reset API

---

## 16) STDIO/JSON-RPC Integration (MCP Command Channel)

### 16.1 Purpose

Enable an AI to send commands (e.g. `move_unit`, `end_turn`) to the running game via a JSON-RPC 2.0 interface over STDIO, without blocking the SDL game loop.

### 16.2 Architecture Overview

```text
┌─────────────────────────────────────────────┐
│  CivOne Process                             │
│                                             │
│  ┌──────────────┐     ConcurrentQueue       │
│  │  STDIO/RPC   │ ──► [cmd1, cmd2, ...]     │
│  │  Thread      │                    │      │
│  └──────────────┘                    ▼      │
│                          ┌────────────────┐ │
│                          │  Game Loop     │ │
│                          │  (main thread) │ │
│                          │  Update+Render │ │
│                          └────────────────┘ │
└─────────────────────────────────────────────┘
```

Key constraints:

- SDL game loop runs unblocked on main thread.
- STDIO reader runs on a dedicated background thread.
- Commands cross thread boundary via `ConcurrentQueue<IGameCommand>`.
- Game state reads use a snapshot or explicit lock to avoid race conditions.

### 16.3 Design Principles

1. Game loop never blocks waiting for MCP input.
1. MCP thread never accesses mutable game state directly.
1. All commands are value objects (no shared mutable references).
1. Game state exposed to MCP is an immutable snapshot.
1. Use dependency injection; do not instantiate dependencies with `new`.

### 16.4 Component Design

#### 16.4.1 Command Abstraction

```csharp
// src/Concepts/Mcp/IGameCommand.cs
public interface IGameCommand
{
    string CommandId { get; }
    void Execute(IGame game);
}
```

#### 16.4.2 Command Queue Service

```csharp
// src/Services/Mcp/ICommandQueue.cs
public interface ICommandQueue
{
    void Enqueue(IGameCommand command);
    bool TryDequeue(out IGameCommand command);
}

// src/Services/Mcp/CommandQueue.cs
public sealed class CommandQueue : ICommandQueue
{
    private readonly ConcurrentQueue<IGameCommand> _queue = new();

    public void Enqueue(IGameCommand command) => _queue.Enqueue(command);

    public bool TryDequeue(out IGameCommand command) =>
        _queue.TryDequeue(out command);
}
```

#### 16.4.3 STDIO/JSON-RPC Reader (background thread)

```csharp
// src/Services/Mcp/StdioMcpListener.cs
public sealed class StdioMcpListener : IDisposable
{
    private readonly ICommandQueue _queue;
    private readonly ICommandFactory _factory;
    private Thread? _thread;
    private CancellationTokenSource? _cts;

    public StdioMcpListener(ICommandQueue queue, ICommandFactory factory)
    {
        _queue = queue;
        _factory = factory;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _thread = new Thread(ReadLoop) { IsBackground = true, Name = "MCP-STDIO" };
        _thread.Start(_cts.Token);
    }

    private void ReadLoop(object? state)
    {
        var ct = (CancellationToken)state!;
        while (!ct.IsCancellationRequested)
        {
            var line = Console.ReadLine();
            if (line is null) break;          // EOF → shutdown

            try
            {
                var command = _factory.Parse(line);   // JSON-RPC → IGameCommand
                if (command is not null)
                    _queue.Enqueue(command);
            }
            catch (Exception ex)
            {
                // log, do not crash the listener thread
                Console.Error.WriteLine($"[MCP] parse error: {ex.Message}");
            }
        }
    }

    public void Dispose() => _cts?.Cancel();
}
```

#### 16.4.4 Game Loop Integration

Drain the queue once per tick, before or after the update step:

```csharp
// Inside game loop tick (main thread)
while (_commandQueue.TryDequeue(out var command))
{
    command.Execute(_game);
}
```

#### 16.4.5 Thread-Safe Game State Snapshot

Expose a read-only snapshot for AI queries instead of raw mutable state:

```csharp
// src/Concepts/Mcp/GameSnapshot.cs
public sealed record GameSnapshot(
    int GameTick,
    IReadOnlyList<UnitSnapshot> Units,
    IReadOnlyList<CitySnapshot> Cities
    // … extend as needed
);

// src/Services/Mcp/IGameStateReader.cs
public interface IGameStateReader
{
    GameSnapshot GetSnapshot();
}

// Implementation acquires a short lock and copies relevant state:
public sealed class GameStateReader : IGameStateReader
{
    private readonly IGame _game;
    private readonly object _lock;         // shared with game loop writer

    public GameStateReader(IGame game, object sharedLock)
    {
        _game = game;
        _lock = sharedLock;
    }

    public GameSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new GameSnapshot(
                _game.GameTick,
                _game.Units.Select(u => new UnitSnapshot(u)).ToList(),
                _game.Cities.Select(c => new CitySnapshot(c)).ToList()
            );
        }
    }
}
```

> Alternatively, replace the lock with a versioned immutable snapshot written atomically by the game loop and read lock-free by the MCP thread.

### 16.5 Suggested Folder Layout

```text
src/
  Concepts/
    Mcp/
      IGameCommand.cs
      GameSnapshot.cs
      UnitSnapshot.cs
      CitySnapshot.cs
  Services/
    Mcp/
      ICommandQueue.cs
      CommandQueue.cs
      ICommandFactory.cs
      StdioMcpListener.cs
      IGameStateReader.cs
      GameStateReader.cs
```

### 16.6 Phased Delivery

| Phase | Deliverable |
| ----- | ----------- |
| A | `IGameCommand`, `CommandQueue`, queue drain in game loop |
| B | `StdioMcpListener` + `ICommandFactory` + `MoveUnitCommand` / `EndTurnCommand` |
| C | `GameSnapshot` + `IGameStateReader` + JSON response for state queries |
| D | Error handling, structured logging, cancellation on shutdown |

### 16.7 Security and Safety

1. STDIO only — no network socket opened.
1. Validate every JSON-RPC request before constructing a command.
1. Commands execute only within the game loop tick, never from the MCP thread.
1. Snapshot reads are bounded in time (short lock or atomic pointer swap).
1. Apply per-tick command rate limit to prevent flooding.

### 16.8 Acceptance Criteria

1. SDL game loop FPS is not measurably affected when MCP thread is active.
1. `move_unit` command sent via STDIO is executed in the next game tick.
1. `end_turn` command sent via STDIO is executed in the next game tick.
1. Game state query returns a consistent snapshot (no torn reads).
1. MCP thread crash does not crash the game process.
1. All new types follow dependency injection; no `new` for services.

---

## 17) Extensibility Model (Add New MCP Functions Fast)

### 17.1 Goal

Allow adding new MCP tools without modifying game loop logic.

### 17.2 Pattern

Use handler registration:

- `IMcpToolHandler` (common contract)
- one implementation per tool (`CaptureScreenshotHandler`, `CaptureRegionHandler`, later `GetGameSnapshotHandler`, etc.)
- `IMcpToolRegistry` mapping `method` -> handler

Factory behavior:

- `McpServiceFactory` returns `NoopMcpService` when MCP disabled
- `McpServiceFactory` returns `ActiveMcpService` when MCP enabled

### 17.3 Runtime Flow

1. Transport adapter reads JSON-RPC request (`id`, `method`, `params`, `sessionToken`).
1. `ActiveMcpService.Process()` resolves handler by `method` in registry.
1. Handler executes on in-game services and returns DTO.
1. Adapter writes JSON-RPC response (`id` + `result` or `error`).

### 17.4 Why This Fits Minimal-Change Requirement

1. Game loop keeps single MCP call (`Process()`) per tick.
1. New tool = new handler class + registry entry.
1. No loop rewrites for each new MCP capability.
