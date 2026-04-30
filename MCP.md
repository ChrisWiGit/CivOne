# MCP Integration

CivOne includes a built-in MCP server for local automation.
The current scope includes screenshot capture, structured game-state read access, and COS save listing/loading/saving.

## Enable MCP

Start the SDL runtime with `--mcp`.
Use `--mcp-artifacts <path>` to choose where captured images are stored.
Use `--mcp-saves <path>` to choose where MCP save listing/loading reads `.cos` files from.
Use `--mcp-no-auth` when you want to run without session-token authentication.
Use `--mcp-http` to host MCP over local HTTP (`127.0.0.1`) instead of stdio.
Use `--mcp-http-port <port>` to override the default HTTP port (`8765`).
Use `--mcp-http-timeout-ms <ms>` to configure HTTP request timeout (default `30000`).

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

Windows for local manual HTTP testing:

```cmd
CivOne.SDL.exe --mcp-http --mcp-no-auth --mcp-saves ".\\.saves"
```

Linux or macOS:

```sh
./CivOne.SDL --mcp
```

By default, artifacts are written below the CivOne storage directory in `temp/mcp-runs/`.

## Transport and authentication

### stdio transport (default)

The server uses JSON-RPC 2.0 over standard input and standard output.
Send one JSON object per line to stdin.
Read one JSON object per line from stdout.

### HTTP transport (`--mcp-http`)

When `--mcp-http` is enabled, the server listens on:

- `http://127.0.0.1:8765/mcp/` (default)
- or `http://127.0.0.1:<your-port>/mcp/` when `--mcp-http-port` is set

HTTP transport accepts `POST` requests with a JSON-RPC body.
Notifications (requests without `id`) return HTTP `202`.
Request timeout uses `--mcp-http-timeout-ms`.

### Manual execution via OpenAPI (VS Code)

You can execute MCP tool calls manually in VS Code using the OpenAPI file at [mcp/openapi.yml](mcp/openapi.yml).

Recommended flow:

1. Start CivOne with HTTP MCP enabled (for example `--mcp-http --mcp-no-auth`).
2. Open [mcp/openapi.yml](mcp/openapi.yml) in VS Code and use your OpenAPI client extension's "Try it out" feature.
3. Choose an endpoint such as `/mcp/game_list_saves`.
4. Keep the prefilled JSON-RPC envelope and usually only change:
  - `id` (optional, any scalar)
  - `params.arguments` (only when the tool needs input)

Important:

- The HTTP endpoint expects JSON-RPC requests, not raw tool arguments.
- The prefilled examples in [mcp/openapi.yml](mcp/openapi.yml) already include `method: "tools/call"` and the correct `params.name`.
- Paths like `/mcp/<toolName>` are convenience OpenAPI paths; execution still goes through JSON-RPC `tools/call`.

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

HTTP example (`tools/list`):

```sh
curl -sS http://127.0.0.1:8765/mcp/ \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":"1","method":"tools/list"}'
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
| `game_get_map_size` | Returns map dimensions and wrap semantics for the active game. | none |
| `game_get_map_window` | Returns a bounded map window using required `x`, `y`, `width`, `height` and optional visibility overlay. | `x`, `y`, `width`, `height` |
| `game_get_map_landvalues_window` | Returns bounded map land values for required `x`, `y`, `width`, `height`. | `x`, `y`, `width`, `height` |
| `game_get_visibility` | Returns explored/visible masks for a player, optionally bounded by a box. | `playerId` |
| `game_get_state` | Returns a JSON projection of the current game state (summary by default, path-based subsets on demand). | none |
| `game_get_entities_index` | Returns a compact index of all players and cities with stable GUIDs and display names. | none |
| `game_validate_path` | Validates a dot-notation GameState path without returning the full node payload. | `path` |
| `game_get_units` | Returns units with optional filters (`playerId`, `playerGuid`, `className`, `locationRadius`) and optional key projection. | none |
| `game_get_city` | Returns a single city by `cityId`, `cityName`, or `cityNameStartsWith`, including compact production/unrest/happiness views. | one selector: `cityId` or `cityName` or `cityNameStartsWith` |
| `game_get_player` | Returns one full player by `playerId` (index) or `playerGuid`, with optional key projection. | one selector: `playerId` or `playerGuid` |
| `game_list_saves` | Returns metadata for valid `.cos` save files in the configured MCP saves folder. Invalid files are omitted. | none |
| `game_load` | Loads a `.cos` save by `fileName` or `saveGuid` from the configured MCP saves folder. | one selector: `fileName` or `saveGuid` |
| `game_save` | Saves the current game as a new `.cos` file in the configured MCP saves folder using a timestamped filename. Existing files are never overwritten. | none |
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
- `runtime.mcpSaves`
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

`game_save` accepts no required arguments.
It writes to the same folder used by `game_list_saves` and `game_load`.
The generated file name format is `savegame_mcp_<UTC yyyyMMddHHmmssfff>.cos`.
No existing file is overwritten.
If the generated name already exists, the tool returns `FILE_EXISTS` with the message `file exists, wait a second, till next try`.
The success payload includes `fileName` and the newly generated `saveGuid`.

## `game_save` request/response example

Request:

```json
{
  "jsonrpc": "2.0",
  "id": "save-1",
  "method": "tools/call",
  "sessionToken": "<token>",
  "params": {
    "name": "game_save",
    "arguments": {}
  }
}
```

Success response shape:

```json
{
  "ok": true,
  "truncated": false,
  "maxChars": 32000,
  "returnedChars": 107,
  "data": {
    "fileName": "savegame_mcp_20260430184512123.cos",
    "saveGuid": "9c4ebc62-5dbd-4f06-b59a-0ae153569ac2"
  }
}
```

FILE_EXISTS response shape:

```json
{
  "ok": false,
  "truncated": false,
  "maxChars": 32000,
  "returnedChars": 0,
  "error": {
    "code": "FILE_EXISTS",
    "message": "file exists, wait a second, till next try",
    "failedSegment": "fileName"
  }
}
```

`game_get_map_size` accepts no arguments and returns:

- `width`, `height`
- `wrapX`, `wrapY`

`game_get_map_window` accepts:

- required `x`, `y`, `width`, `height`
- optional `visibilityForPlayerId` (0-based)
- optional `format` (`decoded` or `encoded`, default `decoded`)
- optional `includeMeta` (for `encoded`, `meta` is always included)

Bounds for `x`, `y`, `width`, and `height` are strict:

- `x >= 0`, `y >= 0`
- `width > 0`, `height > 0`
- `x + width <= mapWidth`, `y + height <= mapHeight`

Invalid bounds return `INVALID_BOUNDS`.

When `format = encoded`, `meta` includes a docs path reference ([docs/YAML_TILE_ENCODING.md](docs/YAML_TILE_ENCODING.md)) and notes that `landValues` are not included.

`game_get_map_landvalues_window` accepts:

- required `x`, `y`, `width`, `height`

Bounds are validated with the same strict rules as `game_get_map_window`.
The response includes `rows` in `hex-csv-per-row` format (for example `"01,0A,FF"`).

## `game_get_map_landvalues_window` request/response example

Request:

```json
{
  "jsonrpc": "2.0",
  "id": "12",
  "method": "tools/call",
  "sessionToken": "<token>",
  "params": {
    "name": "game_get_map_landvalues_window",
    "arguments": {
      "x": 0,
      "y": 0,
      "width": 3,
      "height": 2
    }
  }
}
```

Response shape:

```json
{
  "ok": true,
  "truncated": false,
  "maxChars": 32000,
  "returnedChars": 187,
  "data": {
    "mapSize": { "width": 80, "height": 50 },
    "bounds": { "x": 0, "y": 0, "width": 3, "height": 2 },
    "landValuesFormat": "hex-csv-per-row",
    "rows": [
      "01,0A,FF",
      "02,0B,10"
    ]
  }
}
```

`game_get_visibility` accepts:

- required `playerId` (0-based)
- optional `x`, `y`, `width`, `height` as a bounding box

If any bounding-box field is provided, all four fields must be provided.

`game_get_entities_index` accepts no arguments and returns:

- `gameTurn` — current turn number
- `players[]` — `{ id, playerGuid, tribeName }` for every player
- `cities[]` — `{ id, name, owner, ownerGuid }` for every city across all players

This is a cheap first call to discover entity identifiers before querying details with `game_get_players` or `game_get_cities`.

`game_validate_path` accepts:

- required `path` — dot-notation path that must start with `GameState` (e.g. `GameState.Players[0].Gold`)

Returns `{ ok, valid, path, valueKind }` when the path resolves successfully, or `{ ok, valid=false, error }` with a structured error code when it does not. Does **not** return the node value — use `game_get_state` with the same path for that.

Error codes specific to `game_validate_path`:

| Code | Meaning |
| ---- | ------- |
| `MISSING_PARAM` | `path` was not provided. |
| `INVALID_PATH_ROOT` | Path does not start with `GameState`. |
| `INVALID_PATH` | Path syntax is invalid or a segment does not exist. |

`game_get_units` accepts optional:

- `playerId` (0-based owner index)
- `playerGuid` (stable owner GUID)
- `className` (case-insensitive exact match)
- `locationRadius` object with `x`, `y`, and `radius` (Euclidean distance)
- `keys` (array of field names)

The response returns units with owner metadata (`owner`, `ownerGuid`, `ownerTribeName`).

`game_get_city` accepts:

- exactly one selector: `cityId` (GUID) or `cityName` (exact, case-insensitive) or `cityNameStartsWith` (prefix, case-insensitive)
- optional `keys` (array of field names)

The response returns one city enriched with:

- `productionView`: compact production state (`productionId`, `price`, `buyPrice`, `storedShields`, `remainingShields`)
- `unrestView`: compact unrest state (`isRiot`, `wasInDisorder`, `isCelebrating`, `celebrationCancelled`)
- `happinessView`: compact mood summary (`size`, `entertainerCount`, `specialistCount`, `mood`)

`game_get_player` accepts:

- exactly one selector: `playerId` (0-based index) or `playerGuid` (GUID)
- optional `keys` (array of field names)

The response returns one full player object (or projected keys only).

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

`game_get_state`, `game_get_entities_index`, `game_validate_path`, `game_get_units`, `game_get_city`, `game_get_player`, `game_list_saves`, `game_load`, `game_get_players`, `game_get_cities`, `game_get_settings`, `game_get_map_size`, `game_get_map_window`, `game_get_map_landvalues_window`, and `game_get_visibility` use a default response limit of `32000` characters.
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
        "--mcp-saves", "${workspaceFolder}/.saves"
      ]
    }
  }
}
```

Use `${workspaceFolder}` in `--mcp-artifacts` to keep screenshots inside the project.
Use `${workspaceFolder}` in `--mcp-saves` to make MCP save listing/loading deterministic per workspace.
VS Code resolves this variable relative to the workspace root before starting the process.

If you prefer token auth, remove `--mcp-no-auth` and forward `MCP_SESSION_TOKEN` from stderr in your client flow.

After the server is added, open Chat in VS Code and ask for a tool-driven action such as:

```text
Use the CivOne MCP tools to capture the current game screen and tell me whether the city view is open.
```

You can also use the chat tool picker to enable or disable CivOne tools for a prompt.
VS Code discovers tool definitions from the MCP server and makes them available to chat once the server starts successfully.

## JSON Schemas

Every tool has a pair of JSON Schema (Draft-07) files under `docs/mcp-tools/<tool_name>/`:

| File | Purpose |
| ---- | ------- |
| `input.schema.json` | Valid arguments for the `arguments` object of a `tools/call` request. |
| `output.schema.json` | Shape of the JSON inside the MCP `content[0].text` response field. |

Tool schemas:

| Tool | Schema folder |
| ---- | ------------- |
| `game_capture_screenshot` | [docs/mcp-tools/game_capture_screenshot/](docs/mcp-tools/game_capture_screenshot/) |
| `game_capture_region` | [docs/mcp-tools/game_capture_region/](docs/mcp-tools/game_capture_region/) |
| `game_get_settings` | [docs/mcp-tools/game_get_settings/](docs/mcp-tools/game_get_settings/) |
| `game_get_map_size` | [docs/mcp-tools/game_get_map_size/](docs/mcp-tools/game_get_map_size/) |
| `game_get_map_window` | [docs/mcp-tools/game_get_map_window/](docs/mcp-tools/game_get_map_window/) |
| `game_get_map_landvalues_window` | [docs/mcp-tools/game_get_map_landvalues_window/](docs/mcp-tools/game_get_map_landvalues_window/) |
| `game_get_visibility` | [docs/mcp-tools/game_get_visibility/](docs/mcp-tools/game_get_visibility/) |
| `game_get_state` | [docs/mcp-tools/game_get_state/](docs/mcp-tools/game_get_state/) |
| `game_get_entities_index` | [docs/mcp-tools/game_get_entities_index/](docs/mcp-tools/game_get_entities_index/) |
| `game_validate_path` | [docs/mcp-tools/game_validate_path/](docs/mcp-tools/game_validate_path/) |
| `game_get_units` | [docs/mcp-tools/game_get_units/](docs/mcp-tools/game_get_units/) |
| `game_get_city` | [docs/mcp-tools/game_get_city/](docs/mcp-tools/game_get_city/) |
| `game_get_player` | [docs/mcp-tools/game_get_player/](docs/mcp-tools/game_get_player/) |
| `game_list_saves` | [docs/mcp-tools/game_list_saves/](docs/mcp-tools/game_list_saves/) |
| `game_load` | [docs/mcp-tools/game_load/](docs/mcp-tools/game_load/) |
| `game_get_players` | [docs/mcp-tools/game_get_players/](docs/mcp-tools/game_get_players/) |
| `game_get_cities` | [docs/mcp-tools/game_get_cities/](docs/mcp-tools/game_get_cities/) |

When adding or changing a tool, update the corresponding schema files in the same commit.

## More details

See [docs/MCP.md](docs/MCP.md) for internal design notes and implementation background.
For a full real-world game-state payload example, see [docs/SAVEGAME_EXAMPLE.cos](docs/SAVEGAME_EXAMPLE.cos).
