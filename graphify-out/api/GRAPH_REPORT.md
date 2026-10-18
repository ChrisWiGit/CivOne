# Graph Report - api  (2026-10-10)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 462 nodes · 552 edges · 17 communities (14 shown, 3 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS · INFERRED: 1 edges (avg confidence: 0.85)
- Token cost: 30,232 input · 190 output

## Graph Freshness
- Built from commit: `5f9b15b3`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Technology Advances
- Civilization Attributes
- Leader Behavior Enums
- Game Data Interface
- Attribute Modification
- Civilization Nations
- Historical Leaders
- Plugin and Data Contracts
- Unit Types
- Game Event Records
- Resource Extensions
- City Data
- Unit Orders
- Terrain Types
- Civilization Name Equality
- API Project Config
- Advance Extensions

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
- `Obsolete` --references--> `Advance`  [EXTRACTED]
  api/src/Units/Obsolete.cs → api/src/Enums/Advance.cs
- `Requires` --references--> `Advance`  [EXTRACTED]
  api/src/Units/Requires.cs → api/src/Enums/Advance.cs
- `UnitModification` --references--> `Advance`  [EXTRACTED]
  api/src/Units/UnitModification.cs → api/src/Enums/Advance.cs
- `Name` --inherits--> `BaseAttribute`  [EXTRACTED]
  api/src/Civilizations/Name.cs → api/src/BaseAttribute.cs
- `StartingPosition` --inherits--> `BaseAttribute`  [EXTRACTED]
  api/src/Civilizations/StartingPosition.cs → api/src/BaseAttribute.cs

## Import Cycles
- None detected.

## Communities (17 total, 3 thin omitted)

### Community 0 - "Technology Advances"
Cohesion: 0.03
Nodes (69): Advance, AdvancedFlight, Alphabet, Astronomy, AtomicTheory, Automobile, Banking, BridgeBuilding (+61 more)

### Community 1 - "Civilization Attributes"
Cohesion: 0.05
Nodes (27): CivOne.Civilizations, CivOne.Units, BaseAttribute, Valid, CityNames, Value, CivilizationLeader, Leader (+19 more)

### Community 2 - "Leader Behavior Enums"
Cohesion: 0.06
Nodes (28): CivOne.Enums, CivOne.Leaders, AggressionLevel, Aggressive, Friendly, Normal, DevelopmentLevel, Expansionistic (+20 more)

### Community 3 - "Game Data Interface"
Cohesion: 0.05
Nodes (35): IGameData, ActiveCivilizations, AdvanceFirstDiscovery, Cities, CitizenNames, CityNames, CivilizationIdentity, CivilizationNames (+27 more)

### Community 4 - "Attribute Modification"
Cohesion: 0.06
Nodes (26): CivOne.UserInterface, AttributeValue, HasValue, Value, CivilizationModification, CityNames, Civilization, LeaderId (+18 more)

### Community 5 - "Civilization Nations"
Cohesion: 0.06
Nodes (33): Civilization, Americans, Arabs, Aztecs, Babylonians, Barbarians, Brazilians, Byzantines (+25 more)

### Community 6 - "Historical Leaders"
Cohesion: 0.06
Nodes (33): Leader, Alexander, Atilla, Caesar, Casimir, Corvinus, Darius, Elizabeth (+25 more)

### Community 7 - "Plugin and Data Contracts"
Cohesion: 0.08
Nodes (19): CivOne, IPlugin, Author, Name, Version, PlayerLimits, UnitData, GotoX (+11 more)

### Community 8 - "Unit Types"
Cohesion: 0.07
Nodes (29): UnitType, Armor, Artillery, Battleship, Bomber, Cannon, Caravan, Carrier (+21 more)

### Community 9 - "Game Event Records"
Cohesion: 0.11
Nodes (19): CityBuilt, CityId, CityNameId, OwnerId, X, Y, CityDestroyed, CityId (+11 more)

### Community 11 - "City Data"
Cohesion: 0.12
Nodes (17): CityData, ActualSize, BaseTrade, Buildings, CurrentProduction, Food, FortifiedUnits, Id (+9 more)

### Community 12 - "Unit Orders"
Cohesion: 0.13
Nodes (14): Order, ClearPollution, Disband, Fortify, Fortress, Irrigate, Mines, NewCity (+6 more)

### Community 13 - "Terrain Types"
Cohesion: 0.13
Nodes (15): Terrain, Arctic, Desert, Forest, Grassland1, Grassland2, Hills, Jungle (+7 more)

### Community 14 - "Civilization Name Equality"
Cohesion: 0.17
Nodes (8): CivilizationName, Name, Plural, Valid, Name, NamePlural, NameValue, Value

## Knowledge Gaps
- **325 isolated node(s):** `AdvancedFlight`, `Alphabet`, `Astronomy`, `AtomicTheory`, `Automobile` (+320 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 351 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **3 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Advance` connect `Technology Advances` to `Advance Extensions`, `Civilization Attributes`, `Attribute Modification`?**
  _High betweenness centrality (0.283) - this node is a cross-community bridge._
- **What connects `AdvancedFlight`, `Alphabet`, `Astronomy` to the rest of the system?**
  _325 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Technology Advances` be split into smaller, more focused modules?**
  _Cohesion score 0.028985507246376812 - nodes in this community are weakly interconnected._
- **Why does `CivOne.Enums` connect `Leader Behavior Enums` to `Advance Extensions`, `Civilization Attributes`, `Resource Extensions`, `Unit Orders`?**
  _High betweenness centrality (0.217) - this node is a cross-community bridge._
- **Should `Civilization Attributes` be split into smaller, more focused modules?**
  _Cohesion score 0.05185185185185185 - nodes in this community are weakly interconnected._
- **Why does `UnitModification` connect `Attribute Modification` to `Technology Advances`, `Unit Types`, `Resource Extensions`?**
  _High betweenness centrality (0.209) - this node is a cross-community bridge._
- **Should `Leader Behavior Enums` be split into smaller, more focused modules?**
  _Cohesion score 0.05807200929152149 - nodes in this community are weakly interconnected._