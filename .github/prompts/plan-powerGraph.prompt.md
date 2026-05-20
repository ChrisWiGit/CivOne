## Plan: Extend PowerGraph ReplayData

Important caveat: the original ReplayData design does not currently contain clear evidence that it was intended to preserve enough information for a DOS-style PowerGraph reconstruction.

Important caveat: before committing to a ReplayData-only solution, additional reverse-engineering should be done to confirm whether there are other savegame or runtime structures that already preserve the required power-history information.

The current implementation plan assumes PowerGraph will be implemented by storing power history explicitly as ReplayData.PowerSnapshot as the single source of truth, with optional legacy binary import/export through SaveData.PowerChart. Snapshots are recorded every 4 turns (Oedo), with population taken from the previous turn and gold/techs taken from the Oedo turn itself.

**Steps**
1. Phase 1 - Define the domain model and DTO contract.
2. Add a new ReplayData subclass PowerSnapshot to api/src/ReplayData.cs (turn + 8 power values, validated length), following the existing subtype pattern. *blocks 3-5*
3. Add a new nested DTO structure PowerSnapshotData to src/Persistence/Model/ReplayDataDto.cs (array/list for 8 values).
4. Extend src/Persistence/Mapper/ReplayDataDtoMapper.cs with bidirectional mapping for PowerSnapshot (ToDto switch + FromDto case), including consistency checks matching the existing event types. *depends on 2-3*
5. Add roundtrip tests for PowerSnapshot in xunit/src/persistence/Model/ReplayDataDtoMapperTest.cs (valid, invalid length, multiple nested properties). *can run in parallel with 6, depends on 2-4*
6. Phase 2 - Runtime data collection (4-turn snapshots).
7. Add a hook in src/Game.cs during turn progression: after a new full-round turn is entered, write PowerSnapshot every 4 turns. *depends on 2*
8. Implement the measurement order: buffer the population snapshot from the previous turn, use gold + techs from the Oedo turn, and calculate power with the formula Power = Gold + 96*Techs + 256*Population. Clamp or normalize values as needed for graph storage and rendering. *depends on 7*
9. If useful, introduce a small internal helper in the Game context (for example BuildPowerSnapshotValues) so the formula and timing remain testable. *can run in parallel with 10, depends on 7*
10. Phase 3 - Render the PowerGraph.
11. In src/Screens/PowerGraph.cs, read history from ReplayData.PowerSnapshot, map it to the X axis (50-turn grid), and render lines per civilization while reusing the existing colors and legend. *depends on 7-8*
12. Make Y scaling robust (max=0 case, large values compressed through linear normalization to visible height; optionally add a soft cap later if DOS-like compression is desired). *depends on 11*
13. Add a fallback when no history exists: show the current snapshot or an empty grid with a hint, without crashing. *depends on 11*
14. Phase 4 - Legacy binary compatibility (optional but recommended).
15. Add PowerChart[8*150] as a legacy chart field in src/IO/SaveData.cs, grouped near the existing chart fields if possible.
16. Add access methods for PowerChart in src/SaveDataAdapter.Get.cs and src/SaveDataAdapter.Set.cs (read/write the 8x150 bytes).
17. In src/Game.LoadSave.cs and the corresponding save paths, add mapping between legacy PowerChart and ReplayData.PowerSnapshot: load -> produce ReplayData; save -> materialize chart from ReplayData. *depends on 7-8, can run in parallel with 18*
18. Tolerate older saves without PowerChart (chosen decision): no exception, use runtime history from the first available snapshot onward. *depends on 17*
19. Phase 5 - Verification.
20. Add unit tests for the formula and timing (1-turn population lag, 4-turn Oedo interval, correct civilization ordering).
21. Add mapper tests and persistence tests (YAML roundtrip with PowerSnapshot, legacy binary roundtrip if steps 15-18 are implemented).
22. Manual in-game verification: open the PowerGraph from the debug menu, play 20-40 turns, and validate visible line changes against expected gold/tech/population changes.

**Relevant files**
- c:/Users/Christian/Documents/Projekte/CivOne-Chris-2/src/Screens/PowerGraph.cs - current placeholder UI; extend it to render the data lines.
- c:/Users/Christian/Documents/Projekte/CivOne-Chris-2/src/Game.cs - turn progression and ReplayData hook for 4-turn snapshots.
- c:/Users/Christian/Documents/Projekte/CivOne-Chris-2/api/src/ReplayData.cs - new PowerSnapshot domain type.
- c:/Users/Christian/Documents/Projekte/CivOne-Chris-2/src/Persistence/Model/ReplayDataDto.cs - DTO for PowerSnapshot.
- c:/Users/Christian/Documents/Projekte/CivOne-Chris-2/src/Persistence/Mapper/ReplayDataDtoMapper.cs - extend bidirectional mapping.
- c:/Users/Christian/Documents/Projekte/CivOne-Chris-2/xunit/src/persistence/Model/ReplayDataDtoMapperTest.cs - new mapper test cases.
- c:/Users/Christian/Documents/Projekte/CivOne-Chris-2/src/IO/SaveData.cs - legacy PowerChart field (optional binary path).
- c:/Users/Christian/Documents/Projekte/CivOne-Chris-2/src/SaveDataAdapter.Get.cs - legacy import for PowerChart.
- c:/Users/Christian/Documents/Projekte/CivOne-Chris-2/src/SaveDataAdapter.Set.cs - legacy export for PowerChart.
- c:/Users/Christian/Documents/Projekte/CivOne-Chris-2/src/Game.LoadSave.cs - load and synchronize legacy data with ReplayData.

**Verification**
1. dotnet test c:/Users/Christian/Documents/Projekte/CivOne-Chris-2/xunit/CivOne.UnitTests.csproj
2. dotnet build c:/Users/Christian/Documents/Projekte/CivOne-Chris-2/runtime/sdl/CivOne.SDL.csproj -c DebugWindows
3. Test YAML roundtrip: save and load a game containing PowerSnapshot entries, then validate ReplayData entry count and contents.
4. If legacy support is enabled: load a binary save, inspect the PowerGraph, save again, reload, and compare curve consistency.
5. Manually validate the population lag: force city growth before an Oedo turn, execute the Oedo turn, and compare the plotted point against the expected formula.

**Decisions**
- Data source: ReplayData.PowerSnapshot as the single source of truth.
- Legacy: SaveData.PowerChart only for compatibility import/export, not as the primary runtime source.
- Compatibility: tolerate older savegames without PowerChart; use a non-failing fallback.
- Scope in: PowerGraph rendering, snapshot pipeline, DTO/mapper, tests, optional legacy binary path.
- Scope out: full implementation of all remaining ReplayData types (War/Peace/Wonder/etc.), except what is directly required for PowerGraph.
- Rejected approach: reconstruct the PowerGraph solely from existing or commented replay actions. Reason: the current event set contains neither civilization-specific gold history nor civilization-specific population history per Oedo snapshot; ReplaySummary is global, and CivRankings contains only order, not values.

**Further considerations**
1. Scaling: start with linear normalization; later add a more DOS-like compression model (for example piecewise or logarithmic) in a follow-up if needed.
2. Reduce memory-layout risk: only add legacy PowerChart if adapter changes and tests are completed in the same PR; otherwise start with ReplayData/YAML only.
3. Observability: optionally add a debug overlay or logging for computed power values at each Oedo turn to detect formula mismatches quickly.