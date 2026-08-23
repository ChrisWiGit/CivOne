# AI Migration Plan

## 1. Goal

Create a phased migration path to decouple the current built-in AI and enable a future external dynamic AI provider.

Important requirement:
- It must be possible to use different AI implementations for different civilizations.

Scope:

- Keep current gameplay behavior stable during migration.
- Refactor around current entry points only.
- Avoid big-bang rewrite.

Primary entry points to migrate:

- Unit decision call: [src/Tasks/Turn.cs](src/Tasks/Turn.cs#L32)
- City production calls: [src/City.cs](src/City.cs#L935), [src/City.cs](src/City.cs#L1366)
- Research selection calls: [src/Tasks/ProcessScience.cs](src/Tasks/ProcessScience.cs#L59), [src/Tasks/ProcessScience.cs](src/Tasks/ProcessScience.cs#L78)
- Current hard binding: [src/Player.cs](src/Player.cs#L95)

## 2. Migration Strategy (3 Phases)

1. Phase 1: Interface-Seam
2. Phase 2: Adapter
3. Phase 3: External AI Provider

Each phase is independently releasable.

---

## 3. Phase 1: Interface-Seam

### 3.1 Objective

Introduce a gameplay-level AI contract and route all AI entry points through this contract, while still using existing class `AI` as backend.

### 3.2 Deliverables

- New interface `IPlayerAiController` (or equivalent name) with methods matching current entry points.
- Controller selection seam that can resolve AI per civilization (not only global mode).
- Player-facing access changed from concrete `AI` to interface.
- Callers (`Turn`, `City`, `ProcessScience`) depend on interface only.
- No functional behavior change expected.

### 3.3 Proposed interface shape

```csharp
public interface IPlayerAiController
{
    void Move(IUnit unit);
    void CityProduction(City city);
    void ChooseResearch();
}
```

Keep interface minimal in Phase 1.
Do not add strategy or world-model methods yet.

### 3.4 Concrete code changes

1. Add new interface file.
2. Add provider/factory abstraction (for example `IPlayerAiControllerFactory`) with civilization-aware selection:
   - `IPlayerAiController CreateFor(Player player)`
3. Add a civilization-to-profile resolver abstraction (for example `IAiCivilizationProfileResolver`).
4. Replace `Player.AI` concrete type with interface type.
5. Update all call sites:
   - [src/Tasks/Turn.cs](src/Tasks/Turn.cs#L32)
   - [src/City.cs](src/City.cs#L935)
   - [src/City.cs](src/City.cs#L1366)
   - [src/Tasks/ProcessScience.cs](src/Tasks/ProcessScience.cs#L59)
   - [src/Tasks/ProcessScience.cs](src/Tasks/ProcessScience.cs#L78)

### 3.5 Acceptance criteria

- Project builds.
- Existing AI behavior tests still pass:
  - [xunit/src/AIBehaviorTests.cs](xunit/src/AIBehaviorTests.cs)
  - [xunit/src/AiGotoExecutorTests.cs](xunit/src/AiGotoExecutorTests.cs)
- No gameplay path still references concrete `AI` directly except in adapter/wiring layer.
- AI controller can be resolved per civilization without changing callers.

### 3.6 Risks

- Circular dependencies if interface is placed in wrong namespace/assembly.
- Null lifecycle issues for AI controller creation per `Player`.

### 3.7 Rollback

- Revert only seam commits.
- Keep old property signature in `Player` until build/test green.

---

## 4. Phase 2: Adapter

### 4.1 Objective

Wrap existing class `AI` with an adapter implementing `IPlayerAiController`, so old logic remains untouched while call sites are fully decoupled.

### 4.2 Deliverables

- `LegacyAiControllerAdapter` (name suggestion) that forwards to existing `AI` methods.
- Factory/provider returns adapter instance per non-human player and can choose different adapter variants by civilization profile.
- Existing class `AI` remains source of behavior.

Optional in Phase 2 (recommended):
- Add lightweight civilization adapters that still delegate to legacy logic but allow civ-specific tuning hooks, for example:
  - `LegacyMilitaristicAiAdapter`
  - `LegacyExpansionistAiAdapter`

### 4.3 Adapter pattern mapping

```text
IPlayerAiController.Move           -> AI.Move
IPlayerAiController.CityProduction -> AI.CityProduction
IPlayerAiController.ChooseResearch -> AI.ChooseResearch
```

### 4.4 Concrete code changes

1. Add adapter class near AI domain (for example under `src/AI/` or `src/Services/AI/`).
2. Move direct `AI.Instance(player)` calls into adapter internals only.
3. Keep existing barbarian behavior unchanged in [src/AI.Barbarians.cs](src/AI.Barbarians.cs).
4. Keep pathfinding abstractions unchanged:
   - [src/Services/Pathfinding/IAiGotoExecutor.cs](src/Services/Pathfinding/IAiGotoExecutor.cs)
   - [src/Services/Pathfinding/AiGotoExecutorFactory.cs](src/Services/Pathfinding/AiGotoExecutorFactory.cs)

### 4.5 Acceptance criteria

- All Phase 1 criteria remain green.
- Behavior parity confirmed by targeted tests plus manual AI turn smoke test.
- `Turn`, `City`, `ProcessScience` no longer know class `AI` type.
- At least two civilizations can be configured to use different controller registrations, even if both currently wrap legacy behavior.

### 4.6 Risks

- Hidden static coupling in `AI` may leak through adapter boundary.
- Duplicate instance caching between old `AI.Instance` and new factory lifecycle.

### 4.7 Rollback

- Switch factory wiring back to direct legacy controller implementation.
- Keep interface seam intact.

---

## 5. Phase 3: External AI Provider

### 5.1 Objective

Introduce an external provider implementation behind `IPlayerAiController` and allow runtime selection between legacy and external AI.

This phase must support per-civilization provider selection.

### 5.2 Deliverables

- `ExternalAiController` implementation of `IPlayerAiController`.
- Provider abstraction to isolate transport/protocol details.
- Runtime strategy switch (setting/feature flag) with fallback to legacy adapter.
- Civilization-specific AI profile mapping (for example: Romans -> external profile A, Egyptians -> legacy profile B).

### 5.3 Required architecture additions

1. AI provider port
   - Example: `IExternalAiProvider` with request/response DTOs.
2. Civilization profile mapping
  - Example: `IAiProfileRegistry` keyed by civilization id/name.
  - Registry maps each civilization to a controller type and provider profile.
3. Mapper layer
   - Convert CivOne runtime state -> provider request model.
   - Convert provider decision -> in-game action.
4. Safety/fallback policy
   - Timeout budget per decision.
   - Validation of external outputs.
   - Automatic fallback to legacy adapter on invalid/timeout responses.

### 5.4 Decision boundaries by method

- `Move(IUnit)`
  - Input: unit state, local map slice, nearby units/cities, turn context.
  - Output: action intent (`MoveTo`, `SkipTurn`, `ClearGoto`, etc.).

- `CityProduction(City)`
  - Input: city status, available production, strategic summary.
  - Output: production choice.

- `ChooseResearch()`
  - Input: available advances, civ state.
  - Output: selected advance.

### 5.5 Runtime wiring and toggle

Add configuration key similar to existing behavior toggles in [src/Settings.cs](src/Settings.cs), then expose in setup UI in [src/Screens/Setup.cs](src/Screens/Setup.cs).

Suggested modes:

- `Legacy`
- `ExternalWithFallback`
- `ExternalStrict` (optional, later)

Add civilization-level override config, for example:

```json
{
  "defaultMode": "Legacy",
  "civilizationOverrides": {
    "Romans": "ExternalWithFallback",
    "Egyptians": "Legacy",
    "Babylonians": "ExternalWithFallback"
  },
  "providerProfiles": {
    "Romans": "aggressive-v1",
    "Babylonians": "science-v1"
  }
}
```

Resolution rule:
1. Civilization override (if present)
2. Global default mode
3. Fallback to legacy adapter on any provider failure

### 5.6 Acceptance criteria

- Legacy mode remains behavior-compatible with Phase 2.
- External mode functions end-to-end for all three entry methods.
- Fallback mode demonstrates safe recovery on provider errors/timeouts.
- Add integration tests for provider contract and fallback path.
- Different civilizations can run different AI modes in the same match.
- Different civilizations can run different external profiles in the same match.

### 5.7 Risks

- Performance/frame pacing impact from synchronous provider calls.
- Non-determinism from dynamic external decisions.
- Hard-to-reproduce regressions without AI replay fixtures.

### 5.8 Rollback

- Switch mode to `Legacy` only.
- Keep external provider code dormant behind feature toggle.

---

## 6. Cross-Phase Work Items

### 6.1 Test plan

- Preserve and run existing tests:
  - [xunit/src/AIBehaviorTests.cs](xunit/src/AIBehaviorTests.cs)
  - [xunit/src/AiGotoExecutorTests.cs](xunit/src/AiGotoExecutorTests.cs)
  - [xunit/src/UnitGotoServiceImplTests.cs](xunit/src/UnitGotoServiceImplTests.cs)
- Add new tests incrementally:
  - Interface seam wiring tests.
  - Adapter forwarding tests.
  - External provider contract + fallback tests.
  - Per-civilization routing tests (controller and profile selection).

### 6.2 Observability

Add structured logging around AI decision calls:

- Method name
- Decision duration
- Result/fallback reason

Keep logs low-noise by default and enable verbose mode via setting.

### 6.3 Non-goals during migration

- Do not rewrite legacy `AI` behavior logic in Phase 1/2.
- Do not alter barbarian spawn game design in this migration.
- Do not redesign pathfinding algorithms in this migration.

---

## 7. Suggested Milestones

- M1 (Phase 1 complete): interface seam merged, no behavior change.
- M2 (Phase 2 complete): adapter in place, concrete `AI` hidden behind interface.
- M3 (Phase 3 alpha): external provider works for `ChooseResearch` first.
- M4 (Phase 3 beta): external provider handles all 3 entry points with fallback.
- M5 (Phase 3 release): stable toggleable external AI mode.

## 8. Exit Criteria

Migration considered complete when:

1. Gameplay pipeline depends only on `IPlayerAiController`.
2. Legacy AI remains available as stable fallback.
3. External provider mode is switchable, validated, and observable.
4. Different civilizations can be assigned different AI controllers/profiles at runtime.
5. Build + relevant tests pass in mixed-mode matches (legacy + external in same game).
