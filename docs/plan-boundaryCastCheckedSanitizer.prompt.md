## Plan: Boundary-Cast Hardening with `ICheckedValueSanitizer`

Goal: Safeguard critical numeric boundary casts outside `Extensions` (persistence/export paths) without unnecessarily changing domain logic.

### Background
Several save/snapshot paths convert values into narrow target types (`byte`, `ushort`, `short`). These points are prone to over/underflow or silent wrap-around.

The current approach using `ICheckedValueSanitizer` + `ValueSanitizerFactory` should be continued consistently.

---

## Scope

### In Scope (next step)
1. `src/Persistence/GameStateHandler.cs`
2. `src/Game.LoadSave.cs`
3. `src/SaveDataAdapter.cs` (start with clearly high-risk casts only)

### Out of Scope (for now)
- Broad refactoring in pure domain logic
- Parsing error paths that intentionally throw exceptions (e.g., YAML token parser)
- Legacy RNG/emulation paths with intentional wrap-around

---

## Implementation Approach

1. **Identify boundary casts**
   - Mark all casts in save/snapshot objects targeting `byte`/`ushort`/`short`.
2. **Switch targeted locations to sanitizer usage**
   - Instead of direct casts or plain `Math.Clamp(...)`:
     - `CheckedToByteOrClamp(...)`
     - `CheckedToUInt16OrClamp(...)`
     - `CheckedToInt16OrClamp(...)`
3. **Ensure logging/diagnostics**
   - Pass a clear field path (e.g., `"GameStateHandler.PlayerTaxRate"`).
4. **Protect tests**
   - Existing tests must not regress.
   - For integration bases (`TestsBase`, `TestsBase2`), keep override activation via `UncheckedCastValueSanitizer`.

---

## How to Use Checks with the Class

### Standard (production)
Get the active checked sanitizer through the factory:

```csharp
var checkedSanitizer = ValueSanitizerFactory.GetCheckedValueSanitizer();
var safeValue = checkedSanitizer.CheckedToUInt16OrClamp(rawValue, nameof(GameStateHandler), "PlayerTaxRate");
```

### In tests with legacy/no-clamp behavior
Temporarily override via scope:

```csharp
using var scope = ValueSanitizerFactory.UseCheckedValueSanitizer(new UncheckedCastValueSanitizer());
// Test code with desired legacy-like unchecked behavior
```

---

## Concrete Candidates (first wave)

### `src/Persistence/GameStateHandler.cs`
- `RandomSeed = (ushort)game.TerrainMasterWord`
- `Difficulty = (ushort)game.Difficulty`
- `TaxRate`, `ScienceRate`, `StartingPositionX`, `Government`
- `OpponentCount = (ushort)(game.Players.Length - 2)`

### `src/Game.LoadSave.cs`
- `HumanPlayer`, `Difficulty`, `TaxRate`, `ScienceRate`, `StartingPositionX`, `Government`, `OpponentCount`

### `src/SaveDataAdapter.cs`
- `GameYear = (short)Common.TurnToYear(value)`
- `HumanPlayerBit = (byte)(0x01 << value)`
- then review additional locations incrementally

---

## Acceptance Criteria
1. Critical boundary casts in the 3 target files are migrated to `ICheckedValueSanitizer`.
2. Logging contains clear field paths.
3. No behavior change in existing integration tests (especially with `TestsBase`/`TestsBase2`).
4. Build + relevant unit/integration tests are green.

---

## Verification
1. `dotnet build runtime/sdl/CivOne.SDL.csproj -c DebugWindows`
2. Focused tests:
   - `dotnet test --filter "FullyQualifiedName~SaveDataAdapterTests"`
   - `dotnet test --filter "FullyQualifiedName~GameStateHandler"`
   - `dotnet test --filter "FullyQualifiedName~ValueSanitizerTest"`
3. Optional in-game smoke test: run save/load and check logs for over/underflow signals.

---

## Suggested Order
- First `GameStateHandler` (clear snapshot boundary),
- then `Game.LoadSave`,
- finally `SaveDataAdapter` (higher density of bit/format-sensitive locations).