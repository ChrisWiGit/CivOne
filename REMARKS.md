# Remarks

## Number of Civilizations

The maximum number of civilizations that can participate in a game is limited to 8. This total includes the player and the Barbarians, which means that up to 6 civilizations can be controlled by the AI in a single game.

Although there are 14 different civilizations available in the game, they are organized into pairs of "buddy civilizations." Only one civilization from each pair can appear in a game at the same time, so certain combinations are not possible. This pairing system is hardcoded throughout the game, making it difficult to modify without significant changes.

To alter or expand this behavior, it would be necessary to move away from the original game’s logic and storage format. Implementing a custom save format and new logic would allow for more flexibility in the number and combination of civilizations.

A particular challenge arises with the game's replay feature. The replay system only records the player numbers (0-7), not the civilization IDs (0-14). As a result, changing the civilization system would also require a redesign of the replay functionality to properly track and display the correct civilizations.

## Fortified Units in Cities

The original SaveGame stores up to two units per city separately in 2 bytes (`SaveData.City.FortifiedUnits`).
These units must be removed from the list of units, otherwise they will be counted twice in the city.

### Why did Sid do it this way?

I suspect this was done to increase the maximum total number of units for a civilization.
Normally, a civilization can only have 128 units, and if all cities have 2 stationed units, that would already be 256 units, so you wouldn't have any units left for attacking or exploring.
Therefore, up to 2 units per city are stored separately.
However, only the values UnitId, Fortified, and Veteran are stored.

### Or

**The structure is not used.**
At least in the original game with a low total number of units, FortifiedUnits was not used.
Maybe later in the game, when the total number of units increases, it is used, but I have not seen it yet.

Currently, we always use them!

### Further

The Fortified status is saved, but why?
If, as we assume, only the fortified units in the city are stored (`FilterUnits()`), then the Fortified status for the units in the city is irrelevant, since they cannot be moved anyway.

### "Architecture"

Currently, in CivOne, the handling of units in cities is done in the following places:

* `Game.LoadSave.cs::Save()` - This is where the SaveGame is loaded and the cities are initialized.
  * Units are saved, but without considering the FortifiedUnits.
* `SaveData.City.FortifiedUnits` - Stores up to 2 units per city in 2 bytes.
* `SaveDataAdapter.Get.cs::GetCities()` - This is where the unit bytes are taken from the SaveGame.
* `SaveDataAdapter.Set.cs::SetCities()` - This is where the unit bytes are written to the SaveGame.
* `Extensions.cs::GetCityData()` - This is where the number of units in a city is determined.
* `Extensions.cs::GetUnitData()` and `FilterUnits()` - This is where the number of units in a city is determined and filtered, by only counting the units that are not fortified in a city and do not have this city as their home.

### City View

Following todo's are to be implemented in the city view:

* Auto Build not implemented
* [x] Hotkey a selects the first unit in the city.
  * Left, Right, Up, Down keys cycle through the units in the city.
  * Space/Enter selects the unit and removes sentry or fortified status.
  * Units cannot be sentried or fortified in the city view.
  * ESC closes unit selection.
* [x] Hotkey s selects city buildings
  * Up, Down keys cycle through the buildings in the city.
  * Space/Enter selects the building to be sold.
  * ESC closes building selection and city view.
* [x] Hotkey p selects city tile view
  * Up, Down, Left, Right keys cycle through the city tiles.
  * Space/Enter selects the tile to be removed or worked.
  * ESC closes tile selection.
* [x] Hotkey 1-9 cycles through the specialists in the city.
  * Hotkey changes the specialist entertainer to be changed to tax and science and back to entertainer.
  * What about > 9?
* [x] Hotkey shift+a sets production to auto build.
* [x] On CityView Info Tile (see manual page 75)
  * Bottom row contains tiles of pollution indicators
  * Traderoutes to city with trade values
    * City name: 3 (up/down arrow symbol)
* [x] Building View
  * More than 14 buildings shows More Button

## Trading Cities

See here [Caravan (Civ1)](https://civilization.fandom.com/wiki/Caravan_(Civ1))

ChatGpt revised:

### Caravan Actions (short version for code)

* **Wonder help:**

  * If caravan enters a domestic city building a wonder → add **+50 shields**. Caravan is consumed.

* **Trade route creation:**

  * **Foreign city:** always create a trade route.
  * **Domestic city ≥10 squares away:** player chooses to create route or move on.

* **Initial windfall (cash + research):**

```text
base = (distance + 10) * (trade1 + trade2) / 24
```

* Halve (`*0.5`) if:
  * cities on same continent
  * OR same civilization
* Reduce to 2/3 (`*2/3`) if:

  * player has railroads
  * OR player has flight
* If all 4 conditions true → windfall = base \* 1/9.

### Permanent trade bonus (home city trade arrows)

```text
bonus = (trade1 + trade2 + 4) / 8
```

* Halve (`*0.5`) if same civilization.
* Distance and other factors do **not** matter.

### Limits & rules

* City keeps only **3 most profitable** routes; others give only initial windfall.
* Trade is **one-way**; target city must send its own caravan for reverse route.
* Each caravan counts toward **127-unit limit**.
* Caravan disappears after building a wonder or creating a trade route.

### Trading Routes todos

* In [City View](./src/Screens/CityManagerPanels/CityInfoUnits.cs) shall show trading cities with trade values.

## SpaceShip Launch Condition and Flight Duration

### Launch condition

The launch check is implemented in:

* [src/Services/SpaceShip/SpaceShipLaunchRules.cs](./src/Services/SpaceShip/SpaceShipLaunchRules.cs)

Current behavior:

* Launch is only possible if `SpaceShipLaunchYear == 0` (not launched yet).
* The game uses detailed rules if detailed parts exist.
* Detailed launch requires:
  * `CommandModule >= 1`
  * `HabitationModule >= 1`
  * `LifeSupportModule >= 1`
  * `PropulsionComponent >= 2`
  * `FuelComponent >= 2`
  * `StructuralTotal > 0`

Important detail:

* The command module is currently treated as automatically present when
  `LifeSupportModule + HabitationModule >= 3`, even if no explicit command module slot exists in the ship grid.
* This logic is implemented in:
  * [src/Services/SpaceShip/SpaceShipPartCounter.cs](./src/Services/SpaceShip/SpaceShipPartCounter.cs)

### Flight duration

The flight time calculation is implemented in:

* [src/Services/SpaceShip/SpaceShipScreenDataFactory.cs](./src/Services/SpaceShip/SpaceShipScreenDataFactory.cs)

Current formula:

```text
flightTimeYears = max(3.0, 22.0 - propulsionCount * 2.1 - fuelCount * 0.6)
```

Special case:

* If `propulsionCount == 0`, flight time is `0.0`.

Arrival year display is calculated in:

* [src/Screens/SpaceShipView.cs](./src/Screens/SpaceShipView.cs)

The view uses:

```text
arrivalYear = launchYear + ceil(flightTimeYears)
```

### SpaceShip Class Overview

The following table summarizes the SpaceShip-related classes and core types and what they are used for.

| Type | File | Purpose |
| ------ | ------ | --------- |
| ISpaceShip | [src/Buildings/ISpaceShip.cs](./src/Buildings/ISpaceShip.cs) | Marker interface for production entries that build spaceship parts instead of normal city buildings. |
| SpaceShipView | [src/Screens/SpaceShipView.cs](./src/Screens/SpaceShipView.cs) | Main spaceship screen (rendering, launch interaction, debug helpers). |
| SpaceShipPartSelectorDialog | [src/Screens/SpaceShip/SpaceShipPartSelectorDialog.cs](./src/Screens/SpaceShip/SpaceShipPartSelectorDialog.cs) | Modal picker for concrete module/component types. |
| SpaceShipCivilizationSelectorDialog | [src/Screens/SpaceShip/SpaceShipCivilizationSelectorDialog.cs](./src/Screens/SpaceShip/SpaceShipCivilizationSelectorDialog.cs) | Modal picker to open spaceship view for another civilization. |
| SpaceShipCivilizationListItem | [src/Screens/SpaceShip/SpaceShipCivilizationSelectorServices.cs](./src/Screens/SpaceShip/SpaceShipCivilizationSelectorServices.cs) | Row model for civilization selection list entries. |
| ISpaceShipCivilizationSelectorService | [src/Screens/SpaceShip/SpaceShipCivilizationSelectorServices.cs](./src/Screens/SpaceShip/SpaceShipCivilizationSelectorServices.cs) | Provides the civilization list for selector dialogs. |
| ISpaceShipCivilizationEligibilityEvaluator | [src/Screens/SpaceShip/SpaceShipCivilizationSelectorServices.cs](./src/Screens/SpaceShip/SpaceShipCivilizationSelectorServices.cs) | Decides whether a civilization entry is enabled/selectable. |
| SpaceShipCivilizationSelectorServices | [src/Screens/SpaceShip/SpaceShipCivilizationSelectorServices.cs](./src/Screens/SpaceShip/SpaceShipCivilizationSelectorServices.cs) | Dependency bundle used by the civilization selector dialog. |
| SpaceShipCivilizationEligibilityEvaluator | [src/Screens/SpaceShip/SpaceShipCivilizationSelectorServices.cs](./src/Screens/SpaceShip/SpaceShipCivilizationSelectorServices.cs) | Default evaluator for Apollo/ship-part based visibility rules. |
| SpaceShipCivilizationSelectionRules | [src/Screens/SpaceShip/SpaceShipCivilizationSelectorServices.cs](./src/Screens/SpaceShip/SpaceShipCivilizationSelectorServices.cs) | Shared pure rule helpers for selector eligibility. |
| SpaceShipCivilizationSelectorService | [src/Screens/SpaceShip/SpaceShipCivilizationSelectorServices.cs](./src/Screens/SpaceShip/SpaceShipCivilizationSelectorServices.cs) | Default selector service implementation reading players from game state. |
| SpaceShipCivilizationSelectorServicesFactory | [src/Screens/SpaceShip/SpaceShipCivilizationSelectorServicesFactory.cs](./src/Screens/SpaceShip/SpaceShipCivilizationSelectorServicesFactory.cs) | Factory for default selector dependencies. |
| ISpaceShipResourceService | [src/Screens/SpaceShip/SpaceShipViewServices.cs](./src/Screens/SpaceShip/SpaceShipViewServices.cs) | Resource abstraction for spaceship screen (bitmaps/fonts). |
| SpaceShipViewServices | [src/Screens/SpaceShip/SpaceShipViewServices.cs](./src/Screens/SpaceShip/SpaceShipViewServices.cs) | Aggregates all dependencies needed by SpaceShipView. |
| SpaceShipResourceServiceAdapter | [src/Screens/SpaceShip/SpaceShipViewServices.cs](./src/Screens/SpaceShip/SpaceShipViewServices.cs) | Adapter from general resource services to spaceship-specific resource contract. |
| SpaceShipViewServicesFactory | [src/Screens/SpaceShip/SpaceShipViewServicesFactory.cs](./src/Screens/SpaceShip/SpaceShipViewServicesFactory.cs) | Builds default dependency graph for SpaceShipView. |
| ISpaceShipSpriteProvider | [src/Screens/SpaceShip/ISpaceShipSpriteProvider.cs](./src/Screens/SpaceShip/ISpaceShipSpriteProvider.cs) | Contract for retrieving part sprites by component type. |
| ResourcesSpaceShipSpriteProvider | [src/Screens/SpaceShip/ResourcesSpaceShipSpriteProvider.cs](./src/Screens/SpaceShip/ResourcesSpaceShipSpriteProvider.cs) | Sprite provider backed by docker resource sprite atlas. |
| SpaceShipSpriteProviderFactory | [src/Screens/SpaceShip/SpaceShipSpriteProviderFactory.cs](./src/Screens/SpaceShip/SpaceShipSpriteProviderFactory.cs) | Singleton-like provider factory for sprite access. |
| SpaceShipPaletteAnimationDelegate | [src/Screens/SpaceShip/SpaceShipPaletteAnimationDelegate.cs](./src/Screens/SpaceShip/SpaceShipPaletteAnimationDelegate.cs) | Palette-cycle animation for spaceship lights/modules. |
| ISpaceShipService | [src/Services/SpaceShip/ISpaceShipService.cs](./src/Services/SpaceShip/ISpaceShipService.cs) | High-level build/launch/screen-data service contract. |
| IPlayerSpaceRace | [src/Services/SpaceShip/ISpaceShipService.cs](./src/Services/SpaceShip/ISpaceShipService.cs) | Minimal player projection required by spaceship services. |
| ISpaceShipServiceFactory | [src/Services/SpaceShip/ISpaceShipService.cs](./src/Services/SpaceShip/ISpaceShipService.cs) | Factory for per-player spaceship services. |
| ISpaceShipPlacementRules | [src/Services/SpaceShip/ISpaceShipService.cs](./src/Services/SpaceShip/ISpaceShipService.cs) | Placement rule contract for adding parts to grid. |
| ISpaceShipLaunchRules | [src/Services/SpaceShip/ISpaceShipService.cs](./src/Services/SpaceShip/ISpaceShipService.cs) | Launch readiness rule contract. |
| ISpaceShipScreenDataFactory | [src/Services/SpaceShip/ISpaceShipService.cs](./src/Services/SpaceShip/ISpaceShipService.cs) | Creates derived screen metrics from current ship/player state. |
| ISpaceShipSlotBlueprint | [src/Services/SpaceShip/ISpaceShipService.cs](./src/Services/SpaceShip/ISpaceShipService.cs) | Slot layout and ordering contract for canonical ship grid. |
| SpaceShipOverlaySpriteIds | [src/Services/SpaceShip/ISpaceShipService.cs](./src/Services/SpaceShip/ISpaceShipService.cs) | Constants for overlay sprite groups. |
| SpaceShipOverlaySprite | [src/Services/SpaceShip/ISpaceShipService.cs](./src/Services/SpaceShip/ISpaceShipService.cs) | Overlay sprite data record with visibility/offset helpers. |
| ISpaceShipSlotBlueprintFactory | [src/Services/SpaceShip/ISpaceShipService.cs](./src/Services/SpaceShip/ISpaceShipService.cs) | Factory contract for blueprint instances. |
| SpaceShipService | [src/Services/SpaceShip/SpaceShipService.cs](./src/Services/SpaceShip/SpaceShipService.cs) | Main orchestration service for add-part, launch check, and screen data. |
| SpaceShipServiceFactory | [src/Services/SpaceShip/SpaceShipServiceFactory.cs](./src/Services/SpaceShip/SpaceShipServiceFactory.cs) | Concrete factory wiring player + rules + data factory into service. |
| SpaceShipServiceFactoryProvider | [src/Services/SpaceShip/SpaceShipServiceFactory.cs](./src/Services/SpaceShip/SpaceShipServiceFactory.cs) | Provides cached normal and debug service factories. |
| SpaceShipPlacementRules | [src/Services/SpaceShip/SpaceShipPlacementRules.cs](./src/Services/SpaceShip/SpaceShipPlacementRules.cs) | Canonical slot-based placement algorithm. |
| DebugSpaceShipPlacementRules | [src/Services/SpaceShip/DebugSpaceShipPlacementRules.cs](./src/Services/SpaceShip/DebugSpaceShipPlacementRules.cs) | Relaxed placement rules for debug/testing flows. |
| SpaceShipLaunchRules | [src/Services/SpaceShip/SpaceShipLaunchRules.cs](./src/Services/SpaceShip/SpaceShipLaunchRules.cs) | Launch validity checks for legacy and detailed ships. |
| DebugSpaceShipLaunchRules | [src/Services/SpaceShip/DebugSpaceShipLaunchRules.cs](./src/Services/SpaceShip/DebugSpaceShipLaunchRules.cs) | Relaxed launch checks for debug/testing flows. |
| SpaceShipPartOptions | [src/Services/SpaceShip/SpaceShipPartOptions.cs](./src/Services/SpaceShip/SpaceShipPartOptions.cs) | Maps generic part families to concrete build options. |
| SpaceShipPartCounts | [src/Services/SpaceShip/SpaceShipPartCounter.cs](./src/Services/SpaceShip/SpaceShipPartCounter.cs) | Aggregated count model used for launch/data calculations. |
| SpaceShipPartCounter | [src/Services/SpaceShip/SpaceShipPartCounter.cs](./src/Services/SpaceShip/SpaceShipPartCounter.cs) | Grid scanner that calculates all part counters. |
| SpaceShipScreenData | [src/Services/SpaceShip/SpaceShipScreenData.cs](./src/Services/SpaceShip/SpaceShipScreenData.cs) | Immutable data model for sidebar and mission metrics. |
| SpaceShipScreenDataFactory | [src/Services/SpaceShip/SpaceShipScreenDataFactory.cs](./src/Services/SpaceShip/SpaceShipScreenDataFactory.cs) | Computes support, energy, mass, fuel, success, flight time, etc. |
| SpaceShipComponentTypeMapper | [src/Services/SpaceShip/SpaceShipComponentTypeMapper.cs](./src/Services/SpaceShip/SpaceShipComponentTypeMapper.cs) | Maps slot-map symbols to concrete component types. |
| SpaceShipSlotBlueprint | [src/Services/SpaceShip/SpaceShipSlotBlueprint.cs](./src/Services/SpaceShip/SpaceShipSlotBlueprint.cs) | Canonical 12x12 map, footprints, and placement order definitions. |
| SpaceShipSlotBlueprintFactory | [src/Services/SpaceShip/SpaceShipSlotBlueprint.cs](./src/Services/SpaceShip/SpaceShipSlotBlueprint.cs) | Creates blueprint instances. |
| SpaceShipSlotBlueprintFactoryProvider | [src/Services/SpaceShip/SpaceShipSlotBlueprint.cs](./src/Services/SpaceShip/SpaceShipSlotBlueprint.cs) | Shared factory provider and canonical grid size constants. |
| SpaceShipDto | [src/Persistence/Model/SpaceShipDto.cs](./src/Persistence/Model/SpaceShipDto.cs) | Persistence DTO for grid, population, and launch year. |
| SpaceShipGridMap2D | [src/Persistence/Model/SpaceShipGridMap2d.cs](./src/Persistence/Model/SpaceShipGridMap2d.cs) | Compact 2D grid model for spaceship component serialization. |
| SpaceShipGridMapYamlTypeConverter | [src/Persistence/Yaml/SpaceShipGridMapYamlTypeConverter.cs](./src/Persistence/Yaml/SpaceShipGridMapYamlTypeConverter.cs) | YAML converter for SpaceShipGridMap2D row-based serialization. |

## DrawText Symbols

| Symbol | Meaning         |
|--------|-----------------|
| #      | Stick Figure    |
| $      | Coin            |
| ^      | Check Mark      |
| {      | Wheat Stalk     |
| }      | Trade Arrows    |
| \      | Diamond         |
| \|     | Shield          |
| ~      | Light Bulb      |
| _      | Sun             |

## Colors

| Code | Color Name     | Description         |
|------|----------------|---------------------|
| 1    | Blue           | Standard blue       |
| 2    | Green          | Standard green      |
| 3    | Light Grey     | Grey for disabled items |
| 4    | Dark Red       | Dark red            |
| 5    | Black          | Standard black      |
| 6    | Brown          | Standard brown      |
| 7    | Light Brown    | Light brown         |
| 8    | Dark Brown     | Dark brown          |
| 9    | None           | No color            |
| 10   | Light Green    | Light green         |
| 11   | Light Blue     | Light blue          |
| 12   | Light Red      | Light red           |
| 13   | Pink           | Pink                |
| 14   | Light Yellow   | Light yellow        |
| 15   | White          | White               |
| 16   | White          | White               |
