# DTO Diagnostics and Overflow Safety

This document reflects the current CivOne codebase. The earlier version assumed that both DTO diagnostics and overflow protection still needed to be introduced from scratch. That is no longer accurate.

## Current Status

The overflow-safety work is partially implemented already:

- `CivOne/src/Persistence/Util/IValueSanitizer.cs` contains `ValueSanitizer`, which performs checked numeric casts, clamps out-of-range values, and logs overflow or underflow events.
- `CivOne/src/Persistence/Util/ICheckedValueSanitizer.cs` defines the checked-cast contract used by legacy binary save/load code.
- `CivOne/src/Persistence/Factories/ValueSanitizerFactory.cs` exposes both the default sanitizer and a scoped checked-sanitizer override for tests.
- `CivOne/src/Persistence/Mapper/GameStateDtoMapper.cs` already validates some DTO integrity constraints during deserialization, including `HumanPlayer` and `CurrentPlayer` index bounds.
- `CivOne/src/SaveDataAdapter.cs`, `CivOne/src/Game.LoadSave.cs`, and `CivOne/src/Extensions.cs` already use checked sanitizer calls in several cast-sensitive runtime paths.
- `CivOne/xunit/src/persistence/Model/ValueSanitizerTest.cs` covers sanitizer overflow and underflow logging behavior.

The DTO diagnostics work described in the previous version is not implemented yet:

- There is no `IGameStateDiagnosticsService` in the repository.
- There is no diagnostics profile model such as `DefaultDiagnosticsProfile` or `SveCompatibilityProfile`.
- There is no `diagnostics.txt` output in the save flow.
- `CivOne/src/Services/YamlSaveGameService.cs` currently writes the YAML save file only.
- `CivOne/src/Persistence/YamlSaveGameStateWriter.cs` currently serializes the save DTO only.

## What Is Still Important

The useful part of the original plan is still the split between two concerns:

1. DTO diagnostics for structural and plausibility checks.
2. Overflow safety for cast-sensitive runtime and persistence paths.

That split should remain, but the work should start from the current implementation baseline instead of assuming an empty foundation.

## Recommended Scope

### Phase 1: Add DTO diagnostics without blocking saves

Introduce a diagnostics layer that analyzes the generated `GameStateDto` after mapping and before final save completion.

Recommended output model:

- Severity: `Error`, `Warning`, `Info`
- Stable code identifier per rule
- Field or object path
- Actual value and expected constraint when available
- Human-readable message

Recommended initial rules:

- Player index validation for `HumanPlayer` and `CurrentPlayer`
- Required-null checks for DTO sections that must exist
- Cross-reference validation for player, city, and diplomacy references
- Map shape checks where format compatibility requires fixed dimensions
- Plausibility warnings for unusually large but type-valid values such as gold, science, or future tech counters

This phase should remain report-only. It should not block YAML or COS save generation yet.

### Phase 2: Add diagnostics output to the save flow

Integrate diagnostics after DTO creation in the YAML save pipeline:

- `CivOne/src/Services/YamlSaveGameService.cs`
- `CivOne/src/Persistence/YamlSaveGameStateWriter.cs`

The output can be a sibling file such as `diagnostics.txt` next to the save file. The writer should include:

- Timestamp
- Diagnostics profile name
- Summary counts by severity
- Deterministic finding order
- Full finding details

There is already a save path abstraction available through:

- `CivOne/src/Services/ISaveGamePathProvider.cs`
- `CivOne/src/Services/SaveGamePathProvider.cs`

Those are still relevant if diagnostics output needs a stable location policy.

### Phase 3: Expand overflow protection where it still matters

Do not duplicate existing sanitizer work. Focus only on remaining gaps:

- Audit cast-sensitive paths that still rely on unchecked conversions.
- Prefer the existing checked sanitizer interfaces over ad hoc `checked` blocks when the goal is to clamp and log.
- Use explicit `checked` blocks only where failing fast is the correct behavior.
- Add targeted tests for newly protected paths rather than broad mechanical changes.

### Phase 4: Consider optional save gating later

If diagnostics become reliable enough, add an optional compatibility profile that can block specific save formats on `Error` findings. That should be introduced only after report-only diagnostics are stable and covered by tests.

## Important Corrections to the Previous Version

The following assumptions from the earlier document are outdated and should not be used as implementation guidance:

- `GameStateDtoMapper` is located in `CivOne/src/Persistence/Mapper/GameStateDtoMapper.cs`, not under a `Model` folder.
- Overflow safety is not purely future work; a checked sanitizer implementation already exists and is in active use.
- There is no diagnostics writer or diagnostics service in the current save pipeline.
- `CivOne/CivOne.csproj` does not currently enable project-wide overflow checking via a build property such as `CheckForOverflowUnderflow`.
- `CivOne/xunit/src/persistence/YamlSaveGameStateWriterTest.cs` currently tests a stubbed writer and is not yet a diagnostics integration test.

## Relevant Files

- `CivOne/src/Services/YamlSaveGameService.cs` - save-flow entry point for YAML and COS output.
- `CivOne/src/Persistence/YamlSaveGameStateWriter.cs` - DTO serialization point after mapping.
- `CivOne/src/Persistence/Mapper/GameStateDtoMapper.cs` - current DTO mapping and basic deserialization validation.
- `CivOne/src/Persistence/Model/GameStateDto.cs` - top-level DTO definition for diagnostics rules.
- `CivOne/src/Persistence/Model/MapDto.cs` - map structure and dimension-related checks.
- `CivOne/src/Persistence/Util/IValueSanitizer.cs` - checked cast, clamp, and logging behavior.
- `CivOne/src/Persistence/Util/ICheckedValueSanitizer.cs` - checked-cast abstraction for runtime paths.
- `CivOne/src/Persistence/Factories/ValueSanitizerFactory.cs` - sanitizer factory and scoped test override.
- `CivOne/src/Services/ISaveGamePathProvider.cs` - save path abstraction.
- `CivOne/src/Services/SaveGamePathProvider.cs` - save path resolution logic.
- `CivOne/xunit/src/persistence/Model/ValueSanitizerTest.cs` - sanitizer behavior tests.
- `CivOne/xunit/src/persistence/Model/GameStateDtoMapperTest.cs` - mapper round-trip and contract coverage.
- `CivOne/xunit/src/persistence/YamlSaveGameStateWriterTest.cs` - likely extension point for future save diagnostics tests.

## Verification Targets

When this work is implemented, verification should focus on narrow, deterministic checks:

1. A DTO with invalid player indices produces diagnostics findings with stable codes.
2. A valid DTO produces no error findings.
3. Save generation can emit `diagnostics.txt` next to the save artifact without changing normal save behavior.
4. Diagnostics output is deterministic in both summary and finding order.
5. Existing overflow-sanitizer behavior remains covered and no previously protected cast path regresses to unchecked behavior.

## Recommended Next Step

The best next implementation slice is to add a small report-only DTO diagnostics service that runs immediately after `GameStateDto` creation and returns findings in memory. Once that contract is stable, wire it into file output and only then consider optional save gating.