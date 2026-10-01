# Graph Report - api  (2026-09-19)

## Corpus Check
- 44 files · ~6,589 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 168 nodes · 155 edges · 35 communities detected
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 4 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- [[_COMMUNITY_Community 0|Community 0]]
- [[_COMMUNITY_Community 1|Community 1]]
- [[_COMMUNITY_Community 2|Community 2]]
- [[_COMMUNITY_Community 3|Community 3]]
- [[_COMMUNITY_Community 4|Community 4]]
- [[_COMMUNITY_Community 5|Community 5]]
- [[_COMMUNITY_Community 6|Community 6]]
- [[_COMMUNITY_Community 7|Community 7]]
- [[_COMMUNITY_Community 8|Community 8]]
- [[_COMMUNITY_Community 9|Community 9]]
- [[_COMMUNITY_Community 10|Community 10]]
- [[_COMMUNITY_Community 11|Community 11]]
- [[_COMMUNITY_Community 12|Community 12]]
- [[_COMMUNITY_Community 13|Community 13]]
- [[_COMMUNITY_Community 14|Community 14]]
- [[_COMMUNITY_Community 15|Community 15]]
- [[_COMMUNITY_Community 16|Community 16]]
- [[_COMMUNITY_Community 17|Community 17]]
- [[_COMMUNITY_Community 18|Community 18]]
- [[_COMMUNITY_Community 19|Community 19]]
- [[_COMMUNITY_Community 20|Community 20]]
- [[_COMMUNITY_Community 21|Community 21]]
- [[_COMMUNITY_Community 22|Community 22]]
- [[_COMMUNITY_Community 23|Community 23]]
- [[_COMMUNITY_Community 24|Community 24]]
- [[_COMMUNITY_Community 25|Community 25]]
- [[_COMMUNITY_Community 26|Community 26]]
- [[_COMMUNITY_Community 27|Community 27]]
- [[_COMMUNITY_Community 28|Community 28]]
- [[_COMMUNITY_Community 29|Community 29]]
- [[_COMMUNITY_Community 30|Community 30]]
- [[_COMMUNITY_Community 31|Community 31]]
- [[_COMMUNITY_Community 32|Community 32]]
- [[_COMMUNITY_Community 33|Community 33]]
- [[_COMMUNITY_Community 34|Community 34]]

## God Nodes (most connected - your core abstractions)
1. `IPluginCapabilityContractV1` - 8 edges
2. `ReplayData` - 5 edges
3. `Name` - 5 edges
4. `IPluginCapabilityDescriptor` - 5 edges
5. `Extensions` - 4 edges
6. `IGameData` - 4 edges
7. `LeaderModification` - 4 edges
8. `BaseAttribute` - 3 edges
9. `Resources` - 3 edges
10. `CityNames` - 3 edges

## Surprising Connections (you probably didn't know these)
- `CivilizationLeader` --inherits--> `BaseAttribute`  [EXTRACTED]
  api\src\Civilizations\CivilizationLeader.cs →   _Bridges community 0 → community 10_
- `Name` --inherits--> `BaseAttribute`  [EXTRACTED]
  api\src\Units\Name.cs →   _Bridges community 0 → community 4_
- `StartingPosition` --inherits--> `BaseAttribute`  [EXTRACTED]
  api\src\Civilizations\StartingPosition.cs →   _Bridges community 0 → community 12_
- `Development` --inherits--> `BaseAttribute`  [EXTRACTED]
  api\src\Leaders\Development.cs →   _Bridges community 0 → community 13_
- `Militarism` --inherits--> `BaseAttribute`  [EXTRACTED]
  api\src\Leaders\Militarism.cs →   _Bridges community 0 → community 14_

## Communities

### Community 0 - "Community 0"

Cohesion: 0.11
Nodes (11): Aggression, CivOne.Leaders, BaseAttribute, CityNames, CivOne.Civilizations, CivOne.Units, Moves, CivOne.Units (+3 more)

### Community 1 - "Community 1"

Cohesion: 0.14
Nodes (7): IDisposable, CivOne, IGameData, CivOne.Leaders, LeaderModification, CivOne, Resources

### Community 2 - "Community 2"

Cohesion: 0.21
Nodes (10): CivOne.Plugins, IAiPluginCapabilityDescriptor, IBehaviorPluginCapabilityDescriptor, IBehaviorPluginRuntime, IMapGeneratorPluginCapabilityDescriptor, IPluginCapabilityDescriptor, IPluginMapGeneratorRuntime, IVisualPluginCapabilityDescriptor (+2 more)

### Community 3 - "Community 3"

Cohesion: 0.17
Nodes (7): CivilizationModification, CivOne.Civilizations, CivOne.UserInterface, MenuModification, Modification, CivOne.Units, UnitModification

### Community 4 - "Community 4"

Cohesion: 0.25
Nodes (4): CivOne.Civilizations, CivOne.Leaders, CivOne.Units, Name

### Community 5 - "Community 5"

Cohesion: 0.25
Nodes (1): IPluginCapabilityContractV1

### Community 6 - "Community 6"

Cohesion: 0.48
Nodes (6): CityBuilt, CityDestroyed, CivilizationDestroyed, CivilizationRespawned, CivOne, ReplayData

### Community 7 - "Community 7"

Cohesion: 0.33
Nodes (2): CivOne, Extensions

### Community 8 - "Community 8"

Cohesion: 0.4
Nodes (3): Attribute, BaseAttribute, CivOne

### Community 9 - "Community 9"

Cohesion: 0.5
Nodes (2): AttributeValue, CivOne

### Community 10 - "Community 10"

Cohesion: 0.5
Nodes (2): CivilizationLeader, CivOne.Civilizations

### Community 11 - "Community 11"

Cohesion: 0.5
Nodes (1): CivOne.Civilizations

### Community 12 - "Community 12"

Cohesion: 0.5
Nodes (2): CivOne.Civilizations, StartingPosition

### Community 13 - "Community 13"

Cohesion: 0.5
Nodes (2): CivOne.Leaders, Development

### Community 14 - "Community 14"

Cohesion: 0.5
Nodes (2): CivOne.Leaders, Militarism

### Community 15 - "Community 15"

Cohesion: 0.5
Nodes (2): Attack, CivOne.Units

### Community 16 - "Community 16"

Cohesion: 0.5
Nodes (2): CivOne.Units, Defense

### Community 17 - "Community 17"

Cohesion: 0.5
Nodes (2): CivOne.Units, GoldPrice

### Community 18 - "Community 18"

Cohesion: 0.5
Nodes (2): CivOne.Units, Price

### Community 19 - "Community 19"

Cohesion: 0.67
Nodes (2): CivOne, Modification

### Community 20 - "Community 20"

Cohesion: 0.67
Nodes (2): CivOne, IPlugin

### Community 21 - "Community 21"

Cohesion: 0.67
Nodes (2): CivOne, PlayerLimits

### Community 22 - "Community 22"

Cohesion: 0.67
Nodes (2): AdvanceExtensions, CivOne.Enums

### Community 23 - "Community 23"

Cohesion: 1.0
Nodes (1): CivOne

### Community 24 - "Community 24"

Cohesion: 1.0
Nodes (1): CivOne

### Community 25 - "Community 25"

Cohesion: 1.0
Nodes (1): CivOne.Enums

### Community 26 - "Community 26"

Cohesion: 1.0
Nodes (1): CivOne.Enums

### Community 27 - "Community 27"

Cohesion: 1.0
Nodes (1): CivOne.Enums

### Community 28 - "Community 28"

Cohesion: 1.0
Nodes (1): CivOne.Enums

### Community 29 - "Community 29"

Cohesion: 1.0
Nodes (1): CivOne.Enums

### Community 30 - "Community 30"

Cohesion: 1.0
Nodes (1): CivOne.Enums

### Community 31 - "Community 31"

Cohesion: 1.0
Nodes (1): CivOne.Enums

### Community 32 - "Community 32"

Cohesion: 1.0
Nodes (1): CivOne.Enums

### Community 33 - "Community 33"

Cohesion: 1.0
Nodes (0): 

### Community 34 - "Community 34"

Cohesion: 1.0
Nodes (0): 

## Knowledge Gaps
- **49 isolated node(s):** `CivOne`, `CivOne`, `CivOne`, `CivOne`, `CivOne` (+44 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **Thin community `Community 23`** (2 nodes): `CityData.cs`, `CivOne`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 24`** (2 nodes): `UnitData.cs`, `CivOne`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 25`** (2 nodes): `CivOne.Enums`, `AggressionLevel.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 26`** (2 nodes): `Civilization.cs`, `CivOne.Enums`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 27`** (2 nodes): `DevelopmentLevel.cs`, `CivOne.Enums`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 28`** (2 nodes): `Leader.cs`, `CivOne.Enums`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 29`** (2 nodes): `MilitarismLevel.cs`, `CivOne.Enums`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 30`** (2 nodes): `Order.cs`, `CivOne.Enums`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 31`** (2 nodes): `Terrain.cs`, `CivOne.Enums`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 32`** (2 nodes): `UnitType.cs`, `CivOne.Enums`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 33`** (1 nodes): `CivOne.API.AssemblyInfo.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 34`** (1 nodes): `CivOne.API.AssemblyInfo.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.