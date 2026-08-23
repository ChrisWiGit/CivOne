# AI / A* Detailed Review

Scope:
- src/AI.cs
- src/AI.Barbarians.cs
- src/AStar.cs

Severity model:
- Kritisch: crash/data corruption/invalid game state likely
- Wichtig: major logic defect or high gameplay impact
- Medium: robustness/performance/maintainability issue with visible impact
- Niedrig: cleanup/readability/minor perf

## Findings

### Kritisch

1) Potential crash: fixed-size path buffer overrun in A*
- Location: src/AStar.cs:673
- Theme: Edge Cases, Off-by-one/Bounds, Exception handling
- Problem:
  - A fixed array with length 200 is used:
    - `AStar.sPosition[] Positions = new sPosition[200];`
    - then `path.CopyTo(Positions, 0);`
  - If `path.Count > 200`, `CopyTo` throws `ArgumentException`.
  - On large maps or long sea routes this is realistic.
- Possible solution:
  - Avoid fixed buffer. Return next step without copying full path, or allocate `new sPosition[path.Count]` dynamically.
  - Preferred: use `List<sPosition>` and read last item directly.

2) Potential out-of-range map access near poles
- Location: src/AStar.cs:481, src/AStar.cs:489, src/AStar.cs:498, src/AStar.cs:568
- Theme: NullReference/Index risks, Edge Cases
- Problem:
  - Access patterns `Map[iXXX, iY + iYY]` appear without Y-bound checks in `DistanceToLand` and `CheckNmeNighbors`.
  - If `iY` is at top/bottom rows, `iY + iYY` can be `< 0` or `>= Map.HEIGHT`.
  - Depending on indexer implementation, this can throw (or rely on implicit behavior not guaranteed here).
- Possible solution:
  - Guard Y explicitly before indexing:
    - `int y = iY + iYY; if (y < 0 || y >= Map.HEIGHT) continue;`

3) Invalid state risk after disband in generic AI movement
- Location: src/AI.cs:128
- Theme: Ungültige Zustände, Logikfehler
- Problem:
  - In `else` branch: non-land unit gets disbanded, but method continues and still uses the same `unit` object in loop.
  - If disband mutates/removes unit state, subsequent access can hit invalid references/state.
- Possible solution:
  - Return immediately after disband:
    - `if (unit.Class != UnitClass.Land) { Game.DisbandUnit(unit); return; }`

4) Current active defender can be disbanded while still executing logic
- Location: src/AI.cs:115, src/AI.cs:119-123
- Theme: Ungültige Zustände, Logikfehler
- Problem:
  - Defensive pruning loop can select `unit` itself for disband (`FirstOrDefault` from full tile list).
  - Loop then continues and still dereferences `unit.Tile` in while condition.
  - This can produce undefined behavior depending on disband side effects.
- Possible solution:
  - Exclude current unit from disband candidates.
  - Or if `disband == unit`, disband then `return` immediately.

### Wichtig

5) A* heuristic computed from current node instead of neighbor
- Location: src/AStar.cs:315
- Theme: Logikfehler
- Problem:
  - `float h = Heuristic(currentNode.Position);` inside neighbor loop.
  - Correct A* uses heuristic of the candidate/neighbor node.
  - Current code reduces algorithm quality and can produce suboptimal paths or extra expansions.
- Possible solution:
  - Replace with `float h = Heuristic(NeighborNode.Position);`

6) Repeated full city queries in same decision block
- Location: src/AI.Barbarians.cs:65, src/AI.Barbarians.cs:67
- Theme: N+1 queries, unnötige allocations
- Problem:
  - `Game.GetCities()` executed multiple times in close sequence (`Any`, `Where...First`).
  - Re-enumeration and repeated filtering for same data each turn.
- Possible solution:
  - Cache once in local variable (e.g. `City[] cities = Game.GetCities();`) and reuse.

7) Mixed-stack tile check depends on first unit only
- Location: src/AI.Barbarians.cs:171
- Theme: Logikfehler, Edge Cases
- Problem:
  - Uses `x.Units[0].Owner == 0` to classify tile as friendly.
  - If stack order changes or tile has mixed ownership, classification can be wrong.
- Possible solution:
  - Use `x.Units.Any(u => u.Owner == 0)` (or a stricter all-owner rule depending desired behavior).

### Medium

8) Static mutable pathfinding state not thread-safe
- Location: src/AStar.cs:32, src/AStar.cs:348
- Theme: Race Conditions
- Problem:
  - Shared mutable static state (`_nodes`, `ii`) across searches/instances.
  - If pathfinding is ever parallelized, races and cross-search contamination possible.
- Possible solution:
  - Move mutable state to instance scope or guard with synchronization.
  - Keep static only for immutable constants.

9) Minor dead code / allocation smell in defender loop
- Location: src/AI.cs:118
- Theme: unnötige allocations
- Problem:
  - `IUnit[] units = unit.Tile.Units.Where(x => x != unit).ToArray();` is unused.
  - Allocates each loop iteration for no effect.
- Possible solution:
  - Remove variable/allocation.

10) Minor dead code in settler branch
- Location: src/AI.cs:49
- Theme: unnötige allocations / maintainability
- Problem:
  - `hasCity` is computed but never used.
- Possible solution:
  - Remove variable.

## Topic coverage summary

- Logikfehler: Found (items 3, 4, 5, 7)
- Edge Cases: Found (items 1, 2, 7)
- NullReference-Risiken: Indirect invalid-state and indexing risks found (items 2, 3, 4)
- Race Conditions: Found as architectural risk (item 8)
- Fehlerhafte Async-Nutzung: No finding (no async/await usage in reviewed files)
- Fehlerhafte Exception-Behandlung: Found via crash-prone bounds/copy operations without guard (items 1, 2)
- Off-by-one Fehler: No classic +/-1 loop defect confirmed; bounds issue found (item 2)
- Ungültige Zustände: Found (items 3, 4)
- Unnötige Allocations: Found (items 6, 9, 10)
- N+1 Queries: Found pattern in repeated city fetching (item 6)
- Memory Leaks: No persistent leak pattern confirmed in reviewed scope
- Deadlocks: No finding (no locks/synchronization primitives used)
- Exceptions werden geschluckt: No explicit catch-swallow blocks found in reviewed scope

## Suggested fix order

1) Fix item 1 and 2 first (hard crash risk).
2) Fix item 3 and 4 (invalid state risk during AI turn execution).
3) Fix item 5 (path quality and potentially AI behavior/performance).
4) Clean up items 6-10.
