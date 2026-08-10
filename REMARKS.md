# Remarks

## International Font Simulation

The original `FONTS.CV` file shipped with Civilization 1 contains only ASCII characters (space through `~`, i.e. codes 32–126) plus a small set of Western European characters stored in the control-character range (codes 0–31), such as `ü`, `é`, `â`, `ä`, `ö`, `ü`, `ß` and `ç`.

Players who own an English-only `FONTS.CV` — which lacks even those control-character glyphs — cannot display translated text containing non-ASCII letters without a modified font file.

### Solution

`InternationalSimulatedFontSet` extends `Fontset` and synthesises missing glyphs at runtime:

1. It first checks the static mapping table for characters already encoded in the control-character range.
2. If the character is not found there, it decomposes the Unicode code point into its base letter and combining diacritic mark (Unicode NFD), renders the base letter from the font, and draws the accent pixel-by-pixel on top.
3. As a last resort it falls back to the unaccented base letter.

### Mode selection (`FontSetFactory`)

The behaviour is controlled by the **Simulate International Font** setting (**Shift+F1 → Game Options → Language**):

| Setting              | Effect                                                                                                                            |
| -------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| **Yes**              | Always use the simulating font set                                                                                                |
| **No**               | Always use the plain font set                                                                                                     |
| **Auto** *(default)* | Use the simulating font set only when `FONTS.CV` starts with ASCII space (code 32), which identifies a standard English-only file |

### Relevant classes

| Class                           | File                                            |
| ------------------------------- | ----------------------------------------------- |
| `Fontset`                       | `src/Graphics/FontSet.cs`                       |
| `InternationalSimulatedFontSet` | `src/Graphics/InternationalSimulatedFontSet.cs` |
| `FontSetFactory`                | `src/Graphics/FontSetFactory.cs`                |

## Number of Civilizations

The game supports up to `Game.MaxPlayers` (32) total players (player 0 is always the Barbarians), so up to 30 civilizations can be controlled by the AI in a single game. This limit is bounded by two things: the `ITile.Visited` bitmask, a `uint` (32 bits, one bit per player), and the player colour tables behind `Common.PlayerColourLight`/`PlayerColourDark` (32 entries).

There are only 14 non-barbarian civilizations, organized into pairs of "buddy civilizations" (`ICivilization.PreferredPlayerNumber`, slots 1-7). For player slots 1-7 this original pairing is unchanged, and is still required by the legacy `.SVE` binary save format, which only ever stores 8 players and identifies a civilization by which of the two buddies occupies a slot (`SaveData.CivilizationIdentityFlag`). `SveSaveCompatibilityService` (`SveMaxPlayers = 8`) automatically rejects `.SVE` saves once a game has more than 8 players, falling back to the YAML `.cos` format, which has no such limit.

For player slots 8 and above (only reachable with more than 7 non-barbarian players), civilization assignment is decoupled from the player index: `CivilizationAssignment` (`src/Civilizations/CivilizationAssignment.cs`) draws from a pool of all 14 civilizations, reusing them once the pool is exhausted. Reused civilizations get a disambiguated leader/tribe name (e.g. "Caesar II"), since two players can otherwise end up playing the same civilization. Player colours also repeat every 8 slots (see `Common.PlayerColourLight`/`PlayerColourDark`) since there are only so many visually distinct EGA colours; the tribe/leader name in the UI remains the authoritative way to tell players apart once colours and civilizations repeat.

The replay system (`ReplayData.CivilizationDestroyed`) records player index, not civilization ID (this was fixed as part of raising the player limit — it previously recorded `Civilization.PreferredPlayerNumber`, which stopped being equivalent to the player index once civilization assignment was decoupled). The `Conquest` end-game screen resolves a slot's civilization from replay history when available (`ReplayData.CivilizationRespawned`) and falls back to `BaseCivilization.GetBuddyCivilizationSupplier` for older saves that do not contain respawn entries.

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

## Hall of Fame

### Where is it stored?

Hall of Fame data is persisted in:

* `HallOfFame.yaml` inside `Runtime.StorageDirectory`

For the SDL runtime on Windows, `Runtime.StorageDirectory` is:

* `%LOCALAPPDATA%/CivOne`

So the effective file path is usually:

On Windows this is `%LOCALAPPDATA%\CivOne\HallOfFame.yaml`.
On Linux and macOS this is `~/.local/share/CivOne/HallOfFame.yaml`.

### When is it read or written?

* **Read only (view mode):** opening Hall of Fame from credits/debug uses `ViewEntries(...)`.
* **Write (add score):** at end game flow (`conquest`, `defeat`, `alpha centauri`, `retire`) `AddScore()` composes current human entry and stores it.
* **Write (clear):** pressing **C** on the post-game Hall of Fame screen calls `Clear()`.

If the file is missing, view mode shows placeholders and does **not** create a file.

### Persistence rules

After loading existing entries and adding the newest one, entries are normalized:

* Sort by `Score` descending
* Tie-break by `CreatedAtUtc` descending (newer first)
* Keep only top `5` entries

### File format

The file uses YAML with PascalCase properties.

Top-level model:

* `Version` (currently default `1`)
* `Entries` (array of hall of fame entries)

Entry model fields:

* `LeaderName`: player-entered leader name
* `LeaderTitle`: difficulty title (`Chief`, `Lord`, `Prince`, `King`, `Emperor`, `Deity`)
* `CivilizationNamePlural`: tribe plural name (example: `Romans`)
* `YearLabel`: formatted game year label (example: `1850 AD`)
* `Population`: total population integer
* `Score`: final civilization score integer
* `RatingRankLabel`: historical personality label from rating calculation (top-leader table)
* `RatingPercent`: civilization rating percent integer
* `CreatedAtUtc`: UTC timestamp used for tie-breaking and chronology

### Example `HallOfFame.yaml`

```yaml
Version: 1
Entries:
  - LeaderName: "Marcus"
    LeaderTitle: "King"
    CivilizationNamePlural: "Romans"
    YearLabel: "1850 AD"
    Population: 1234567
    Score: 1876
    RatingRankLabel: "Augustus Caesar"
    RatingPercent: 74
    CreatedAtUtc: 2026-05-20T12:34:56.0000000+00:00
```

### Clear behavior detail

Clear does not always mean empty file:

* If a current human game context exists, clear keeps exactly **one** entry: the current human composed score.
* If no active game context exists (for example credits/debug without `Game.Instance`), clear falls back to empty entries.

## Touchpad Gestures and SDL

Two-finger touchpad input is used for two things on the gameplay map: scrolling (pan) and zooming.
Both are driven by SDL mouse wheel events, and only pinch would need real gesture events.
This section documents what SDL can and cannot deliver, because the limits are not obvious from the code.

### How the two gestures arrive

| Gesture | SDL event | Handled in |
| --- | --- | --- |
| Two-finger scroll, vertical | `SDL_MOUSEWHEEL` with `y = ±1` | `GamePanMapDelegate.PanMapWheel` |
| Two-finger scroll, horizontal | `SDL_MOUSEWHEEL` with `x = ±1` | `GamePanMapDelegate.PanMapWheel` |
| Ctrl + two-finger scroll | `SDL_MOUSEWHEEL` with Ctrl modifier | `GameMapZoomDelegate.MouseWheel` |
| Pinch | `SDL_MULTIGESTURE` | `SDL.Window.HandleMultiGesture` |

A touchpad scroll produces many wheel events per swipe (roughly one per 120 scroll units), so one swipe pans several tiles.

### Pitfall: event fields must be carried through `Transform`

`GameWindow.Transform(...)` rescales window pixel coordinates to canvas coordinates and builds a **new** `ScreenEventArgs` for every mouse event.
Any field that is not copied there silently arrives as `0` in the screens, even though the SDL layer filled it correctly.
This is what broke horizontal panning initially: `WheelDeltaX` was added to `ScreenEventArgs` and filled in `Window.HandleMouseWheel`, but `Transform` still used the constructor overload without it.
When adding a field to `ScreenEventArgs`, update `GameWindow.CreateScreenEventArgs`/`Transform` and `BaseScreen.MouseArgsOffset` as well.

### Horizontal scroll direction is consistent across platforms

The sign convention is the same on Linux and Windows, so no platform-specific inversion is needed:

* X11 (`SDL_x11events.c`): scroll left is X button 6, scroll right is button 7, and SDL negates the horizontal ticks before sending them (`SDL_SendMouseWheel(..., (float)-xticks, (float)yticks, ...)`). Result: left is negative, right is positive.
* Windows (`SDL_windowsevents.c`): `WM_MOUSEHWHEEL` passes the normalized wheel delta unchanged, which is positive when scrolling right.

What still differs is the user's own "natural scrolling" setting, which flips both axes at driver level on any platform.

### Pinch-to-zoom is not available on Linux

SDL2 derives `SDL_MULTIGESTURE` exclusively from touch events.
A touchpad only produces touch events if the operating system exposes it as a touch device, and on Linux it does not:

* **X11** has no gesture protocol at all. libinput recognises pinch, but `xf86-input-libinput` does not forward gestures to X clients, and XInput2 has no gesture events. The touchpad is exposed as a pointer device with button and scroll classes only, without an `XITouchClass`. `SDL_GetNumTouchDevices()` therefore returns `0` and no gesture event is ever generated. This affects every X11 application, not just CivOne.
* **Wayland** does define a gesture protocol (`zwp_pointer_gestures_v1`), but SDL2 does not implement it. SDL3 does not either; it removed the gesture API entirely. Switching SDL version would not help.
* **macOS** reports trackpad gestures as touch, so `HandleMultiGesture` is reached and pinch zoom works.
* **Windows** registers `WM_TOUCH`, which touchscreens send but precision touchpads do not. For touchpads Windows itself translates pinch into Ctrl + wheel for applications without gesture handling, which lands in the normal Ctrl-zoom path.

The verification for the Linux case was done with a small SDL2 probe program that logged every wheel, finger, and gesture event: wheel events arrived on both axes, while `SDL_GetNumTouchDevices()` reported `0` and pinching produced nothing at all.

If pinch is wanted on Linux, it has to be solved outside the game.
A gesture daemon such as `touchegg` (X11) or `libinput-gestures` reads libinput directly and can map pinch to Ctrl + scroll, which then reaches the existing zoom path without any code change.
Reading `/dev/input` inside the game would require seat/device permissions and platform-specific code, and is not worth it.

### Relevant classes

| Class / member | File |
| --- | --- |
| `SDL.Window.HandleMouseWheel` | `runtime/sdl/src/SDL/Window.MouseEvent.cs` |
| `SDL.Window.HandleMultiGesture` | `runtime/sdl/src/SDL/Window.MouseEvent.cs` |
| `SDL_MultiGestureEvent` | `runtime/sdl/src/SDL/Structs/SDL_Event.cs` |
| `GameWindow.Transform` | `runtime/sdl/src/GameWindow.cs` |
| `ScreenEventArgs` | `src/Events/ScreenEventArgs.cs` |
| `GamePanMapDelegate` | `src/Screens/GamePlayPanels/GamePanMapDelegate.cs` |
| `GameMapZoomDelegate` | `src/Screens/GamePlayPanels/GameMapZoomDelegate.cs` |

## Map Rendering Performance

Redrawing the gameplay map is the most expensive recurring operation in the game, and it gets worse
the further the player zooms out. This section records why, what was already done about it, and which
step is still open.

### Why a full map redraw is expensive

The number of rendered tiles grows as the tile size shrinks, until it is capped at the map size
(`Map.WIDTH` x `Map.HEIGHT` = 80 x 50 = 4000 tiles) in `GameMapZoomDelegate.UpdateViewportMetrics`.
At the default zoom about 1000 tiles are drawn, at maximum zoom-out four times as many.

Every one of those tiles is composed from scratch in `TileExtensions.ToBitmap`:

* two `Picture` instances are created, and each `Picture` constructor copies the palette **twice**
  (`_originalColours` and `_palette`), so four unmanaged 1 KB allocations per tile
* four to eight sprite layers are drawn on top of each other
* the result is scaled to the current tile size

There is no cache, so this happens again for every tile on every full redraw.

### What was already optimised

| Change | Effect |
| --- | --- |
| Row spans in `Bytemap.Row(int)` | `AddLayer` and both map scalers no longer validate the handle and the offset for every single pixel. The `Bytemap` indexer performs those checks per access, which previously dominated the cost of any per-pixel loop. |
| Tile palette cache in `TileExtensions` | `Resources["SP257"]` returns an owned copy of the full tile sheet. Reading it once per rendered tile copied that sheet thousands of times per frame. |
| Terrain editor base layer cache in `GameMap` | Editor overlays need a clean terrain layer on every hover change. Restoring a cached copy avoids re-rendering all visible tiles for overlay-only updates. |
| Viewport fingerprint in `GameMap` | The cached layer is only rebuilt when the visible map actually changed, instead of on a fixed interval. |
| Tick budget in `RuntimeHandler.OnUpdate` | Game ticks are derived from wall clock time, so a slow update let real time run ahead and queued further updates into the same frame. The loop then kept running instead of returning to the event loop, which stopped drawing and input handling entirely. The budget turns that into a skipped frame. |

### Open idea: tile bitmap cache

The remaining step would be a cache keyed by tile appearance plus tile size, so that a full redraw
becomes a series of blits instead of a full re-composition. `CachedSpriteCollection` already exists as
a model for this kind of cache.

**Advantages**

* The gain is largest exactly where the problem is. At high zoom-out most of the 4000 tiles are
  repetitions of a few appearances (ocean, grassland), so the hit rate would be very high.
* Each cache hit skips the whole chain described above.
* The cache invalidates itself. If the state is part of the key, a changed tile simply produces a
  different key, so no dirty flag is needed and none can be forgotten. The viewport fingerprint in
  `GameMap` could then be removed again.
* Putting the tile size in the key means a zoom change invalidates nothing and previously used zoom
  levels stay warm.

**Disadvantages**

* **Designing the key is the hard part.** A tile is not rendered from its own state alone: `Borders`,
  `DrawRoadDirections()` and `DrawRailRoadDirections()` read the neighbouring tiles. The key must
  therefore include neighbour-derived data, otherwise coastlines and roads render incorrectly - and
  only sometimes, which is hard to diagnose.
* Further parts of the key are easy to overlook: fog directions (per player), blink state, `GFX256`,
  city size and owner, units on the tile, and `TileSettings`. City labels contain the city name and
  would make the key unbounded, so they have to stay outside the cache.
* Computing the key must stay cheaper than the work it saves. On a cache miss both costs are paid.
* Memory grows without bound unless an eviction strategy is added, plus a clear on
  `Resources.ClearInstance`.
* Ownership inverts. The cache would own the returned `Bytemap` instances, so callers must not dispose
  them - the same trap that `CachedSpriteCollection` already documents. Today `ToBitmap` disposes its
  intermediate results correctly, and that would have to change.

**Recommendation:** measure what is actually left after the optimisations listed above before starting
this. The neighbour-dependent key is the part that can go wrong, and the effort may no longer pay off.

### Relevant classes

| Class / member | File |
| --- | --- |
| `TileExtensions.ToBitmap` | `src/Tiles/TileExtensions.cs` |
| `Bytemap.Row` | `src/IO/Bytemap.cs` |
| `BitmapExtensions.AddLayer` | `src/Graphics/BitmapExtensions.cs` |
| `PaletteAwareWeightedMapBitmapScaler` | `src/Services/Maps/PaletteAwareWeightedMapBitmapScaler.cs` |
| `NearestNeighborMapBitmapScaler` | `src/Services/Maps/NearestNeighborMapBitmapScaler.cs` |
| `GameMap.CacheEditorBaseLayer` | `src/Screens/GamePlayPanels/GameMap.cs` |
| `GameMap.ComputeVisibleMapFingerprint` | `src/Screens/GamePlayPanels/GameMap.cs` |
| `GameMapZoomDelegate.UpdateViewportMetrics` | `src/Screens/GamePlayPanels/GameMapZoomDelegate.cs` |
| `RuntimeHandler.OnUpdate` | `src/RuntimeHandler.cs` |
| `CachedSpriteCollection` | `src/Graphics/Sprites/CachedSpriteCollection.cs` |

## DrawText Symbols

| Symbol | Meaning      |
| ------ | ------------ |
| #      | Stick Figure |
| $      | Coin         |
| ^      | Check Mark   |
| {      | Wheat Stalk  |
| }      | Trade Arrows |
| \      | Diamond      |
| \|     | Shield       |
| ~      | Light Bulb   |
| _      | Sun          |

## DrawButton / Font IDs

`DrawButton` uses the provided `fontId` directly:

```csharp
DrawButton(string text, byte fontId, byte colour, byte colourDark, int x, int y, int width, int height)
```

There is also a shorthand overload without `fontId`, which defaults to font `1`:

```csharp
DrawButton(string text, byte colour, byte colourDark, int x, int y, int width)
// internally uses fontId = 1 and height = Resources.GetFontHeight(1) + 3
```

### Known font IDs used in code

The exact glyph shapes come from `FONTS.CV` (runtime data file, not in this repository), so visual style below is based on where each font is used in-game.

| Font ID | Typical usage in CivOne                                     | Likely visual style                |
| ------: | ----------------------------------------------------------- | ---------------------------------- |
|       0 | Standard UI text, menus, dialogs, reports                   | Default readable UI font (regular) |
|       1 | Compact UI text, many buttons/panels, small labels          | Smaller/compact UI font            |
|       2 | Newspaper headline accents (`_shout`)                       | Decorative headline style          |
|       3 | Demo / newspaper emphasis text                              | Bold or stylized display font      |
|       4 | Newspaper title (`_name`), credits text settings            | Title-like decorative font         |
|       5 | Big event/title text (city banners, game over, discovery)   | Large ornamental title font        |
|       6 | Civilopedia/body info text, intro/new game descriptive text | Thin/compact info font             |
|       8 | Unit letter overlay on sprites                              | Very compact symbol/letter font    |

### Practical button guidance

* If you want vanilla-looking UI buttons, use the shorthand overload (font `1`).
* For compact buttons, keep `height` close to `Resources.GetFontHeight(fontId) + 3`.
* If text appears vertically off-center, adjust only `height` first (the text is drawn at `y + 2`).
* If a chosen font is unavailable/out-of-range, rendering falls back to `DefaultFont`.

## Colors

| Code | Color Name   | Description             |
| ---- | ------------ | ----------------------- |
| 1    | Blue         | Standard blue           |
| 2    | Green        | Standard green          |
| 3    | Light Grey   | Grey for disabled items |
| 4    | Dark Red     | Dark red                |
| 5    | Black        | Standard black          |
| 6    | Brown        | Standard brown          |
| 7    | Light Brown  | Light brown             |
| 8    | Dark Brown   | Dark brown              |
| 9    | None         | No color                |
| 10   | Light Green  | Light green             |
| 11   | Light Blue   | Light blue              |
| 12   | Light Red    | Light red               |
| 13   | Pink         | Pink                    |
| 14   | Light Yellow | Light yellow            |
| 15   | White        | White                   |
| 16   | White        | White                   |

## Static Initializers and Tests

`Common` is touched by almost everything, so its static initializer must stay cheap and must not require a
running game. `Advances`, `Buildings` and `Wonders` are therefore **lazily initialized cached properties**,
not field initializers:

```csharp
// BAD: runs on the first touch of any Common member, needs a registered runtime.
public static IAdvance[] Advances = Reflect.GetAdvances().ToArray();

// GOOD: resolved only when the list is actually used.
private static IAdvance[]? _advances;
public static IAdvance[] Advances => _advances ??= [.. Reflect.GetAdvances()];
```

### Why this matters more than it looks

`Reflect.GetAdvances()` and friends reflect over every loaded assembly and instantiate each type, and those
constructors need `RuntimeHandler.Runtime`. With a field initializer, reading an unrelated member such as a
player colour was enough to trigger all of it.

The failure mode is nasty: **a static initializer that throws poisons the type for the whole process.** Once
`Common..cctor()` has failed, every later access throws `TypeInitializationException`, including from tests
that do register a runtime and would otherwise pass. A single test class that touches `Common` without a
runtime therefore takes down every later test in the same run.

### Rules for new tests

* A test that constructs `MockRuntime` (directly or via `TestsBase`) is fine.
* A test that touches **any** `Common` member without a runtime is only safe as long as that member does not
  pull in reflection. If you add eager static state to `Common`, such tests start failing.
* Symptom to recognize: a large number of unrelated tests failing at once with
  `TypeInitializationException: The type initializer for 'CivOne.Common' threw an exception` and an inner
  `InvalidOperationException: RuntimeHandler is not initialized`. The first failing test is not necessarily
  the culprit — look for the test class that ran first.
* The order test classes run in is not stable between runs, so this shows up as flakiness: the same filter
  can pass repeatedly and then fail. Do not dismiss it as a random glitch. Test parallelization is already
  disabled assembly-wide (`xunit/properties/AssemblyInfo.cs`), so it is never a threading race.

## Warnings suppressed

### CivOne.csproj

| Warning | Description | Why suppressed |
| --- | --- | --- |
| CA1303 | Do not pass literals as localized parameters | English texts are used as translation keys in the project; this intentionally generates many string literals. |
| CA1814 | Prefer jagged arrays over multidimensional | Not really useful and used a lot in the project. |
| CA1819 | Properties should not return arrays | Used a lot in project. |
| CA2000 | Dispose objects before losing scope | Partial ownership transfer or caching of IDisposable objects can cause analyzer false positives. |
| CA1515 | Consider making public types internal | Currently there is a mix of public and internal types for various reasons. |
| CA1062 | Validate arguments of public methods | First CA1515 must be addressed before validating arguments. |

### api/CivOne.API.csproj

| Warning | Description | Why suppressed |
| --- | --- | --- |
| CA1303 | Do not pass literals as localized parameters | English texts are used as translation keys in the project; this intentionally generates many string literals. |
| CA1515 | Consider making public types internal | Currently there is a mix of public and internal types for various reasons. |
| CA1819 | Properties should not return arrays | Used a lot in project. |
| CA1814 | Prefer jagged arrays over multidimensional | Not really useful and used a lot in the project. |
| CA1062 | Validate arguments of public methods | First CA1515 must be addressed before validating arguments. |

### runtime/sdl/CivOne.SDL.csproj

| Warning | Description | Why suppressed |
| --- | --- | --- |
| CA1712 | Do not prefix enum values with type name | SDL enums follow a specific naming convention that includes the type name. |
| CA1303 | Do not pass literals as localized parameters | English texts are used as translation keys in the project; this intentionally generates many string literals. |
| CA1515 | Consider making public types internal | Currently there is a mix of public and internal types for various reasons. |
| CA1819 | Properties should not return arrays | Used a lot in project. |
| CA1814 | Prefer jagged arrays over multidimensional | Not really useful and used a lot in the project. |
| CA1062 | Validate arguments of public methods | First CA1515 must be addressed before validating arguments. |

### civtranslate/civtranslate.csproj

| Warning | Description | Why suppressed |
| --- | --- | --- |
| CA1303 | Do not pass literals as localized parameters | The tool works with fixed text/key strings; string literals are often intentional here. |
| CA1515 | Consider making public types internal | Currently there is a mix of public and internal types for various reasons. |

### civtranslate-interactive/civtranslate-interactive.csproj

| Warning | Description | Why suppressed |
| --- | --- | --- |
| CA1303 | Do not pass literals as localized parameters | The tool works with fixed text/key strings; string literals are often intentional here. |
| CA1515 | Consider making public types internal | Currently there is a mix of public and internal types for various reasons. |

### civtranslate-mergekeys/civtranslate-mergekeys.csproj

| Warning | Description | Why suppressed |
| --- | --- | --- |
| CA1303 | Do not pass literals as localized parameters | The tool works with fixed text/key strings; string literals are often intentional here. |
| CA1515 | Consider making public types internal | Currently there is a mix of public and internal types for various reasons. |

### xunit/CivOne.UnitTests.csproj

| Warning | Description | Why suppressed |
| --- | --- | --- |
| CA1515 | Consider making public types internal | Currently there is a mix of public and internal types for various reasons. |
| CA1819 | Properties should not return arrays | Used a lot in project. |
| CA1065 | Do not raise exceptions in unexpected locations | Unit tests often intentionally raise exceptions to test error handling. |
| CA1814 | Prefer jagged arrays over multidimensional | Not really useful and used a lot in the project. |
| CA1307 | Specify StringComparison for clarity | Unit tests often intentionally use default string comparison behavior. Tests will fail if it changes. |
| CA1002 | Do not expose generic lists | Unit tests often intentionally use generic lists for simplicity. |
