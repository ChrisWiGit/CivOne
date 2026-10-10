# Graph Report - api  (2026-10-10)

## Corpus Check
- 42 files · ~5,734 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 462 nodes · 552 edges · 17 communities (14 shown, 3 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS · INFERRED: 1 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `5f9b15b3`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Advance
- BaseAttribute
- CivOne.Enums
- IGameData
- UnitModification
- Civilization
- Leader
- UnitData
- UnitType
- ReplayData
- .GetInternalResourceBytes
- CityData
- Order
- Terrain
- CivilizationName
- CivOne.API.csproj
- Advance.cs

## God Nodes (most connected - your core abstractions)
1. `Advance` - 73 edges
2. `IGameData` - 41 edges
3. `Leader` - 36 edges
4. `Civilization` - 34 edges
5. `UnitType` - 30 edges
6. `BaseAttribute` - 21 edges
7. `CityData` - 18 edges
8. `CivOne.Enums` - 18 edges
9. `LeaderModification` - 16 edges
10. `UnitModification` - 16 edges

## Surprising Connections (you probably didn't know these)
- `LeaderModification` --references--> `AttributeValue`  [EXTRACTED]
  api/src/Leaders/LeaderModification.cs → api/src/AttributeValue.cs
- `Name` --inherits--> `BaseAttribute`  [EXTRACTED]
  api/src/Civilizations/Name.cs → api/src/BaseAttribute.cs
- `StartingPosition` --inherits--> `BaseAttribute`  [EXTRACTED]
  api/src/Civilizations/StartingPosition.cs → api/src/BaseAttribute.cs
- `Aggression` --inherits--> `BaseAttribute`  [EXTRACTED]
  api/src/Leaders/Aggression.cs → api/src/BaseAttribute.cs
- `Development` --inherits--> `BaseAttribute`  [EXTRACTED]
  api/src/Leaders/Development.cs → api/src/BaseAttribute.cs

## Import Cycles
- None detected.

## Communities (17 total, 3 thin omitted)

### Community 0 - "Advance"
Cohesion: 0.03
Nodes (69): Advance, AdvancedFlight, Alphabet, Astronomy, AtomicTheory, Automobile, Banking, BridgeBuilding (+61 more)

### Community 1 - "BaseAttribute"
Cohesion: 0.05
Nodes (27): CivOne.Civilizations, CivOne.Units, BaseAttribute, Valid, CityNames, Value, CivilizationLeader, Leader (+19 more)

### Community 2 - "CivOne.Enums"
Cohesion: 0.06
Nodes (28): CivOne.Enums, CivOne.Leaders, AggressionLevel, Aggressive, Friendly, Normal, DevelopmentLevel, Expansionistic (+20 more)

### Community 3 - "IGameData"
Cohesion: 0.05
Nodes (35): IGameData, ActiveCivilizations, AdvanceFirstDiscovery, Cities, CitizenNames, CityNames, CivilizationIdentity, CivilizationNames (+27 more)

### Community 4 - "UnitModification"
Cohesion: 0.06
Nodes (26): CivOne.UserInterface, AttributeValue, HasValue, Value, CivilizationModification, CityNames, Civilization, LeaderId (+18 more)

### Community 5 - "Civilization"
Cohesion: 0.06
Nodes (33): Civilization, Americans, Arabs, Aztecs, Babylonians, Barbarians, Brazilians, Byzantines (+25 more)

### Community 6 - "Leader"
Cohesion: 0.06
Nodes (33): Leader, Alexander, Atilla, Caesar, Casimir, Corvinus, Darius, Elizabeth (+25 more)

### Community 7 - "UnitData"
Cohesion: 0.08
Nodes (19): CivOne, IPlugin, Author, Name, Version, PlayerLimits, UnitData, GotoX (+11 more)

### Community 8 - "UnitType"
Cohesion: 0.07
Nodes (29): UnitType, Armor, Artillery, Battleship, Bomber, Cannon, Caravan, Carrier (+21 more)

### Community 9 - "ReplayData"
Cohesion: 0.11
Nodes (19): CityBuilt, CityId, CityNameId, OwnerId, X, Y, CityDestroyed, CityId (+11 more)

### Community 11 - "CityData"
Cohesion: 0.12
Nodes (17): CityData, ActualSize, BaseTrade, Buildings, CurrentProduction, Food, FortifiedUnits, Id (+9 more)

### Community 12 - "Order"
Cohesion: 0.13
Nodes (14): Order, ClearPollution, Disband, Fortify, Fortress, Irrigate, Mines, NewCity (+6 more)

### Community 13 - "Terrain"
Cohesion: 0.13
Nodes (15): Terrain, Arctic, Desert, Forest, Grassland1, Grassland2, Hills, Jungle (+7 more)

### Community 14 - "CivilizationName"
Cohesion: 0.17
Nodes (8): CivilizationName, Name, Plural, Valid, Name, NamePlural, NameValue, Value

## Knowledge Gaps
- **325 isolated node(s):** `net9.0`, `Microsoft.NET.Sdk`, `HasValue`, `Value`, `Valid` (+320 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 351 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **3 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Advance` connect `Advance` to `Advance.cs`, `BaseAttribute`, `UnitModification`?**
  _High betweenness centrality (0.283) - this node is a cross-community bridge._
- **What connects `net9.0`, `Microsoft.NET.Sdk`, `HasValue` to the rest of the system?**
  _325 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Advance` be split into smaller, more focused modules?**
  _Cohesion score 0.028985507246376812 - nodes in this community are weakly interconnected._
- **Why does `CivOne.Enums` connect `CivOne.Enums` to `Advance.cs`, `BaseAttribute`, `.GetInternalResourceBytes`, `Order`?**
  _High betweenness centrality (0.217) - this node is a cross-community bridge._
- **Should `BaseAttribute` be split into smaller, more focused modules?**
  _Cohesion score 0.05185185185185185 - nodes in this community are weakly interconnected._
- **Why does `UnitModification` connect `UnitModification` to `Advance`, `UnitType`, `.GetInternalResourceBytes`?**
  _High betweenness centrality (0.209) - this node is a cross-community bridge._
- **Should `CivOne.Enums` be split into smaller, more focused modules?**
  _Cohesion score 0.05807200929152149 - nodes in this community are weakly interconnected._