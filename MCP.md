# MCP Integration

CivOne includes a built-in MCP server for local automation.
The current scope includes screenshot capture and structured game-state read access.

## Enable MCP

Start the SDL runtime with `--mcp`.
Use `--mcp-artifacts <path>` to choose where captured images are stored.
Use `--mcp-no-auth` when you want to run without session-token authentication.

Windows:

```cmd
CivOne.SDL.exe --mcp
```

Windows with custom artifact folder:

```cmd
CivOne.SDL.exe --mcp --mcp-artifacts "C:\my-ai-tests\screenshots"
```

Windows for direct VS Code MCP client use without token handling:

```cmd
CivOne.SDL.exe --mcp --mcp-no-auth
```

Linux or macOS:

```sh
./CivOne.SDL --mcp
```

By default, artifacts are written below the CivOne storage directory in `temp/mcp-runs/`.

## Transport and authentication

The server uses JSON-RPC 2.0 over standard input and standard output.
Send one JSON object per line to stdin.
Read one JSON object per line from stdout.

It supports the MCP lifecycle methods expected by VS Code:

- `initialize`
- `initialized`
- `shutdown`
- `exit`

On startup the server writes a one-time session token to stderr:

```text
MCP_SESSION_TOKEN=a3f8...
```

Include this token in every request as `sessionToken`.
Requests without a valid token are rejected with `-32001`.
`initialize` and `initialized` are accepted without a token.
When started with `--mcp-no-auth`, token validation is disabled for all methods.

## MCP lifecycle examples

Initialize request:

```json
{
  "jsonrpc": "2.0",
  "id": "init-1",
  "method": "initialize",
  "params": {
    "protocolVersion": "2025-06-18",
    "clientInfo": { "name": "vscode", "version": "1.x" },
    "capabilities": {}
  }
}
```

Initialize response shape:

```json
{
  "jsonrpc": "2.0",
  "id": "init-1",
  "result": {
    "capabilities": {
      "tools": {}
    },
    "serverInfo": {
      "name": "civone-mcp",
      "version": "1.0.0"
    }
  }
}
```

Initialized notification (no response expected):

```json
{
  "jsonrpc": "2.0",
  "method": "initialized",
  "params": {}
}
```

## Discover available tools

Request:

```json
{"jsonrpc":"2.0","id":"1","method":"tools/list","sessionToken":"<token>"}
```

Response shape:

```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "result": {
    "tools": [
      {
        "name": "game_capture_screenshot",
        "description": "Captures a full-frame PNG screenshot of the current game state.",
        "inputSchema": { }
      }
    ]
  }
}
```

## Call a tool

Preferred call style:

```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "method": "tools/call",
  "sessionToken": "<token>",
  "params": {
    "name": "game_capture_screenshot",
    "arguments": {
      "sessionId": "run-001",
      "includeCursor": false
    }
  }
}
```

Direct method call also works:

```json
{
  "jsonrpc": "2.0",
  "id": "3",
  "method": "game_capture_region",
  "sessionToken": "<token>",
  "params": {
    "sessionId": "run-001",
    "x": 10,
    "y": 20,
    "width": 320,
    "height": 200,
    "includeCursor": false
  }
}
```

## Available tools

| Tool | Description | Required params |
| ---- | ----------- | --------------- |
| `game_capture_screenshot` | Captures a full-frame PNG screenshot. | none |
| `game_capture_region` | Captures a cropped PNG screenshot. | `x`, `y`, `width`, `height` |
| `game_get_settings` | Returns persistent and runtime settings, with readable labels for enum and flag values. | none |
| `game_get_state` | Returns a JSON projection of the current game state (summary by default, path-based subsets on demand). | none |
| `game_get_players` | Returns players data (all or one player) with optional key projection. | none |
| `game_get_cities` | Returns city data (all, by player, or by city id) with optional key projection. | none |

All tools accept optional `sessionId` and `includeCursor` fields.
`includeCursor` is currently reserved for future use.

`game_get_settings` accepts optional:

- `keys` (array of dotted keys such as `display.graphicsMode`, `gameOptions.sound`, `runtime.mcpEnabled`)

If `keys` is omitted, the tool returns the full grouped settings payload.
Enum-like and flag-like settings return both raw values and readable labels, for example:

- `display.graphicsMode` → `{ "value": 1, "name": "Graphics256", "text": "256 colors" }`
- `gameOptions.sound` → `{ "value": 0, "name": "Default", "text": "Default" }`
- `patches.globalWarmingFeatureFlags` → `{ "value": 1, "names": ["SeaLevelRise"], "text": "SeaLevelRise" }`

Current key catalogue for `game_get_settings` (source of truth: [src/Mcp/Tools/GameGetSettingsToolHandler.cs](src/Mcp/Tools/GameGetSettingsToolHandler.cs)):

- `paths.storageDirectory`
- `paths.captureDirectory`
- `paths.dataDirectory`
- `paths.pluginsDirectory`
- `paths.savesDirectory`
- `paths.cosSavesDirectory`
- `paths.soundsDirectory`
- `display.windowTitle`
- `display.graphicsMode`
- `display.fullScreen`
- `display.scale`
- `display.aspectRatio`
- `display.expandWidth`
- `display.expandHeight`
- `display.windowWidth`
- `display.windowHeight`
- `display.windowPosX`
- `display.windowPosY`
- `display.cursorType`
- `display.destroyAnimation`
- `patches.rightSideBar`
- `patches.revealWorld`
- `patches.debugMenu`
- `patches.deityEnabled`
- `patches.arrowHelper`
- `patches.customMapSize`
- `patches.pathFinding`
- `patches.autoSettlers`
- `patches.riverFastMovement`
- `patches.canalCity`
- `patches.preferSveSaveFormat`
- `patches.useUncheckedCastSanitizer`
- `patches.globalWarmingFeatureFlags`
- `gameOptions.instantAdvice`
- `gameOptions.autoSave`
- `gameOptions.endOfTurn`
- `gameOptions.animations`
- `gameOptions.sound`
- `gameOptions.enemyMoves`
- `gameOptions.civilopediaText`
- `gameOptions.palace`
- `gameOptions.taxRate`
- `runtime.demo`
- `runtime.setup`
- `runtime.dataCheck`
- `runtime.mcpEnabled`
- `runtime.mcpNoAuth`
- `runtime.free`
- `runtime.showCredits`
- `runtime.showIntro`
- `runtime.loadSaveGameSlot`
- `runtime.loadCosFile`
- `runtime.initialSeed`
- `runtime.profileName`
- `runtime.noSound`
- `runtime.softwareRender`
- `runtime.mcpArtifacts`
- `runtime.mcpMaxJsonChars`

Notes:

- `display.windowWidth`, `display.windowHeight`, `display.windowPosX`, and `display.windowPosY` currently return placeholder text (`"currently not implemented"`).
- Key matching in the handler is case-insensitive, but the canonical names above are the recommended spellings.

`game_get_state` accepts optional `path` (dot notation + array indices), for example:

- `GameState.GameTurn`
- `GameState.Players[0].Civilization`
- `GameState.Map`

If `path` is omitted, the tool returns a compact summary (turn, player, aggregated stats), not the full state.

`game_get_players` accepts optional:

- `playerId` (0-based)
- `keys` (array of field names)

If `keys` is omitted, all fields are returned.

## Recommended high-value game-state fields (compact)

For LLM-driven automation, these fields provide the best value/size ratio:

- **Stable IDs**: `playerGuid`, `id` (city/player), `cityId` filter support
- **Turn context**: `gameTurn`, `currentPlayer`, `humanPlayer`, `difficulty`
- **City planning**: `name`, `owner`, `size`, `currentProduction`, `buildings`, `wonders`
- **Player strategy**: `tribeName`, `gold`, `science`, `currentResearch`, `advances`
- **Diplomacy links**: `diplomacy[].targetPlayerGuid` for durable cross-player references

Large structures such as full `map`, `explored`, and `visible` should be queried only when explicitly needed.

### Suggested `keys` profiles

Use these minimal profiles to avoid truncation and reduce repeated calls:

- `game_get_players` (minimal):
  - `keys`: `id`, `playerGuid`, `tribeName`, `gold`, `science`, `currentResearch`
- `game_get_players` (diplomacy):
  - `keys`: `id`, `tribeName`, `diplomacy`
- `game_get_cities` (minimal):
  - `keys`: `id`, `name`, `owner`, `size`
- `game_get_cities` (production):
  - `keys`: `id`, `name`, `size`, `currentProduction`, `buildings`, `wonders`

`game_get_cities` accepts optional:

- `playerId` (0-based owner filter)
- `cityId` (GUID string)
- `keys` (array of field names)

If `keys` is omitted, all fields are returned.

## `game_get_settings` result model

The `text` field contains JSON with an object root.

Default grouped response shape:

```json
{
  "ok": true,
  "truncated": false,
  "maxChars": 32000,
  "returnedChars": 812,
  "data": {
    "paths": {
      "savesDirectory": "..."
    },
    "display": {
      "graphicsMode": {
        "value": 1,
        "name": "Graphics256",
        "text": "256 colors"
      }
    },
    "patches": {},
    "gameOptions": {},
    "runtime": {}
  }
}
```

Filtered response example (`keys` provided):

```json
{
  "ok": true,
  "truncated": false,
  "maxChars": 32000,
  "returnedChars": 173,
  "data": {
    "display.graphicsMode": {
      "value": 1,
      "name": "Graphics256",
      "text": "256 colors"
    },
    "runtime.mcpEnabled": {
      "value": true,
      "text": "Enabled"
    }
  }
}
```

Invalid keys return the same structured MCP error envelope used by the other JSON tools with `code = "INVALID_KEYS"`.

## `game_get_settings` request example

```json
{
  "jsonrpc": "2.0",
  "id": "9",
  "method": "tools/call",
  "sessionToken": "<token>",
  "params": {
    "name": "game_get_settings",
    "arguments": {
      "keys": ["display.graphicsMode", "gameOptions.sound", "runtime.profileName"]
    }
  }
}
```

## `game_get_state` result model

The `text` field contains JSON with an object root.

Success shape:

```json
{
  "ok": true,
  "path": "GameState.Players[0].Civilization",
  "truncated": false,
  "maxChars": 32000,
  "returnedChars": 39,
  "data": {
    "leaderClassName": "Caesar"
  }
}
```

Error shape:

```json
{
  "ok": false,
  "path": "GameState.Players[x]",
  "truncated": false,
  "maxChars": 32000,
  "returnedChars": 0,
  "error": {
    "code": "INVALID_PATH",
    "message": "Array index must be an integer.",
    "path": "GameState.Players[x]",
    "failedSegment": "[x]"
  }
}
```

When no game session is active (no game loaded), `game_get_state` returns:

```json
{
  "ok": false,
  "path": null,
  "truncated": false,
  "maxChars": 32000,
  "returnedChars": 0,
  "error": {
    "code": "NO_GAME",
    "message": "No active game session.",
    "path": null,
    "failedSegment": null
  }
}
```

If the response would exceed the configured size limit, payload is truncated and metadata is included:

```json
{
  "ok": false,
  "truncated": true,
  "maxChars": 32000,
  "returnedChars": 29744,
  "totalChars": 98125,
  "strategy": "head-preview",
  "dataPreview": "{...}",
  "error": {
    "code": "PAYLOAD_TRUNCATED",
    "message": "The payload exceeded the configured size limit and was truncated."
  }
}
```

## `game_get_state` request examples

Default summary (no path):

```json
{
  "jsonrpc": "2.0",
  "id": "10",
  "method": "tools/call",
  "sessionToken": "<token>",
  "params": {
    "name": "game_get_state",
    "arguments": {}
  }
}
```

Specific path:

```json
{
  "jsonrpc": "2.0",
  "id": "11",
  "method": "tools/call",
  "sessionToken": "<token>",
  "params": {
    "name": "game_get_state",
    "arguments": {
      "path": "GameState.Players[0].Cities"
    }
  }
}
```

## Size limit configuration

`game_get_state`, `game_get_players`, `game_get_cities`, and `game_get_settings` use a default response limit of `32000` characters.
You can override it via runtime setting key `mcp-max-json-chars`.

## Screenshot result

Successful screenshot calls return:

| Field | Meaning |
| ----- | ------- |
| `sessionId` | Session identifier passed in the request. |
| `gameTick` | Game tick at capture time. |
| `capturedAtUtc` | UTC timestamp. |
| `width` | Image width in pixels. |
| `height` | Image height in pixels. |
| `format` | Always `png`. |
| `artifactPath` | Saved image path using forward slashes. |

## Tool call response format

All `tools/call` responses wrap the result in an MCP-compliant `content` array:

```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"sessionId\":\"run-001\",\"gameTick\":42,...}"
      }
    ]
  }
}
```

The `text` field contains a JSON-serialised `McpScreenshotResult` object.
This format is required by VS Code's MCP chat bridge.

## Server lifecycle and shutdown

The server can be stopped in three ways:

1. **`exit` notification** — client sends `{"method":"exit"}` after a `shutdown` handshake.
2. **`shutdown` + `exit` sequence** — standard MCP two-step graceful shutdown.
3. **stdin EOF** — VS Code closes the stdin pipe when stopping the server via the UI ("Stop server"). The server detects this and calls `Environment.Exit(0)` automatically.

All three paths terminate the process cleanly.

## Error codes

| Code | Meaning |
| ---- | ------- |
| `-32001` | Missing or invalid session token. |
| `-32600` | Invalid request. |
| `-32601` | Method not found. |
| `-32602` | Invalid params. |
| `-32603` | Internal error. |
| `-32700` | Parse error. |

## Use from Visual Studio Code

Visual Studio Code can work with MCP servers through `.vscode/mcp.json` or the `MCP: Add Server` command.
For a direct setup without token handling, use `--mcp-no-auth` in your workspace config:

```json
{
  "servers": {
    "civone": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "runtime/sdl/bin/Debug/net9.0/CivOne.SDL.dll",
        "--mcp",
        "--mcp-no-auth",
        "--mcp-artifacts", "${workspaceFolder}/.mcp-artifacts",
        "--load-cos", "${workspaceFolder}/.saves/savegame_11.cos"
      ]
    }
  }
}
```

Use `${workspaceFolder}` in `--mcp-artifacts` to keep screenshots inside the project.
VS Code resolves this variable relative to the workspace root before starting the process.

If you prefer token auth, remove `--mcp-no-auth` and forward `MCP_SESSION_TOKEN` from stderr in your client flow.

After the server is added, open Chat in VS Code and ask for a tool-driven action such as:

```text
Use the CivOne MCP tools to capture the current game screen and tell me whether the city view is open.
```

You can also use the chat tool picker to enable or disable CivOne tools for a prompt.
VS Code discovers tool definitions from the MCP server and makes them available to chat once the server starts successfully.

## More details

See [docs/MCP.md](docs/MCP.md) for internal design notes and implementation background.
For a full real-world game-state payload example, see [docs/SAVEGAME_EXAMPLE.cos](docs/SAVEGAME_EXAMPLE.cos).
