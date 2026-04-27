# Changes

## Comment

I did not browse all issues on github at first, so I did not recognize that some of my fixes have an issue.

## History

* Feature: Extended fixed-layout UI behavior in `Aspect Ratio = Expand` mode to avoid stretching and keep screens centered.
  * `Palace`, `CityView`, `Conquest`, `Civilopedia` now renders as centered 320x200 content instead of stretching across the expanded canvas.
  * All report screens (Demographics, City Status, Attitude Survey, Science Report, Trade Report) are now rendered as centered 320x200 content instead of stretching across the expanded canvas.
  * `Civilopedia` entry pages (icon/title/content/terrain text) are centered in expanded windows.
* Fix: Civilopedia entries could show icons but no text.
* Fix: Debug overlay dialogs/menus could stretch across the expanded canvas.
  * Menus do not stretch across the expanded canvas and are now centered with fixed width.
  * Input dialogs are now centered and have a fixed width instead of stretching across the expanded canvas.
* Fix: Improved Newspaper rendering in `Aspect Ratio = Expand` mode.
  * Keeps the newspaper at its original 320x200 aspect ratio (centered), instead of stretching it to the full expanded canvas.
  * Redraws the newspaper correctly after window resize, so it no longer disappears while the screen is resized.
* Feature: Added 'r' to rename a city in city view.
* Feature: Introduced `SveSaveCompatibilitySnapshot` to validate SVE savegame compatibility.
  * This enables future gameplay extensions that may no longer fit into the legacy SVE save format and therefore require the newer COS format.
  * Saving in SVE format is disabled when a game is loaded from the new COS format (`CivOneSave`), i.e. once a game is loaded from COS, it can only be saved in COS to prevent accidental data loss.
* Feature: Hardening against silent out-of-range errors
  * Added boundary checks for types with smaller range than `int` and consistent logging of overflow/underflow events during save/snapshot mapping.
  * Prepared plan for more checks. See [docs/plan-boundaryCastCheckedSanitizer.prompt.md](docs/plan-boundaryCastCheckedSanitizer.prompt.md) for details.
* Feature: Autosave format is configurable in patches menu (YAML or COS).
  * `YAML` autosave writes `autosave.cos` to the last used folder (fallback profile save folder).
* Feature: In Yaml, Advances allows -1 as an indicator to represent "all advances", e.g. for debugging or testing purposes. The mapper will resolve this to the full list of advance IDs in the game.
* Feature: Gameplay updates
  * Future Tech handling:
    * Added per-player `FutureTechCount` runtime state.
    * When no normal research is available and science threshold is reached, Future Tech is applied.
    * Human player increments both counters (`FutureTechCount` + legacy global `PlayerFutureTech`), AI increments only per-player counter.
  * Human contact tracking:
    * `HumanContactTurn` is set when a non-human player gains visibility of a human-owned unit/city.
    * Human exploration does not modify AI contact counters.
  * Diplomacy persistence:
    * Added per-player `Diplomacy` field (8 target entries with raw bitmask flags) to the YAML/game persistence model.
    * Added `DiplomacyDecodedDto` as placeholder for future decoded diplomacy flags.
    * **Not yet active in gameplay** — diplomacy flags are loaded and saved but not yet modified during gameplay.
  * Peace timer (minimal integration):
    * `PeaceTurns` now increases by 1 when a full game turn advances without hostile action.
    * Any hostile action during a turn resets `PeaceTurns` to `0` on the next turn advance.
  * Refactor: `Player.Explore(...)`
    * Kept original visibility update logic as a dedicated contiguous method block.
    * Moved contact-tracking behavior into separate helper methods to avoid mixing with legacy core code.
* Feature:Implemented Future Tech counter for players, stored in save files and used for game logic (e.g. victory conditions).
  * Each new Future Tech increases the counter by 1.
* Fix:Heap corruption due to buffer allocation and indexing issues in Win32 folder browser and Bytemap copy operations.
* YAML: Added `UnitsDestroyedBy` statistics per player
  * Each player now tracks how many units they have destroyed, broken down by opponent.
  * **Not yet active in gameplay** — counters are loaded and saved but not yet incremented during combat.
  * See [docs/UNITS_DESTROYED_BY_GAME_IMPLEMENTATION.md](docs/UNITS_DESTROYED_BY_GAME_IMPLEMENTATION.md) for the planned runtime implementation.
* Implemented YAML savegame format to save and load games
  * Implemented extensive DTO and Mapper classes to convert between internal game state and YAML save format.
  * Using modern software design principles, patterns and methods.
  * Using extensive Tests to ensure correctness and maintainability of the code.
  * Added YAML-based loading and saving for game state persistence.
  * Added command line loading of YAML save files via `--load-cos <path>`.
  * Added runtime support setting `LoadCosFile` to start directly from a YAML save file.
  * Designed to support future in-memory model refactoring while keeping save format mapping isolated.
  * Current scope focuses on CivOne YAML save files and does not target binary CIV save compatibility.
  * Prepared for future changes to how data is handled in memory for more flexibility and maintainability.
  * TestBase and TestBase2 now load the Earth map from bundled `earth.yml` instead of relying on `MAP.PIC`, ensuring consistent test environments without external dependencies.
  * Added space ship grid mapping support in YAML **Not yet active in gameplay** (see [docs/SPACESHIP_FULL_IMPLEMENTATION_PLAN.md](docs/SPACESHIP_FULL_IMPLEMENTATION_PLAN.md)).
  * See [YAML Save Format](YAML.md) for more details.
* Fix: Corrected city economy calculation after refactoring the city economy breakdown logic.
  * Refactoring: Extracted the city economy breakdown calculation into a separate service (CityEconomyServiceImpl) to improve separation of concerns and testability.
* Fix: InstantAdvice messages now only appear once.
* Fix: Improved CityView panorama road generation to prevent random road breaks while keeping layout natural.
  * Added targeted post-processing to close only real single-tile gaps on the road axes.
  * Restores missing intersection tiles when at least two adjacent road segments connect to the crossing.
  * Keeps a minimum number of houses when removing isolated tiles, avoiding empty-looking city views.
* Fix: Improved SDL keyboard event conversion for modifier + digit combinations (top-row digits), including debug diagnostics.
  * Ctrl + Shift + 0 now maps reliably to digit input for specialist hotkeys.
  * Added detailed DEBUG keyboard logs (raw scancode/keycode/modifier and converted key event).
* Feature: Added a new hotkey (Tab) to cycle through production filter modes in the city production menu (All, Units, Buildings, Wonders, All).
* Fix: Fixed an issue where the game would freeze in city production menu screen, if more than 20 items were available and the player hit "More..." to see the next page of items.
* Github pipeline to build and automatic testing and creating release artifacts (Windows, Linux)
* Feature: Added Debug keys for debugging purposes in DEBUG mode
  * Ctrl + Shift + F12 to hit debugger breakpoint
  * Ctrl + Shift + F9/F10 to decrease/increase event loop wait time by 1 ms.
    This will allow for debugging in Linux to use the mouse in Visual Studio. Due to a known problem in SDL2,
     mouse events are not handled correctly if the program is run under a debugger. This occurs, if the debugger
     is triggered by a breakpoint or an exception. Adding a sleep may increase the chances of mouse events being processed.
* Fix: Fixed an issue where resuming from the debugger caused the game loop to stall user input until all pending ticks were processed.
* Major refactor and extensive tests of city happiness mechanics in [CityCitizenServiceImpl.cs](src/Screens/Services/CityCitizenServiceImpl.cs)
  * Fix: The original code contained duplicated logic in City::Citizens and City::Residents methods.
  * Completely rewritten with unit tests ([xunit/src/CityCitizenServiceImplTests.cs](xunit/src/CityCitizenServiceImplTests.cs)) and performance tests ([xunit/src/CityCitizenServiceImplPerformanceTests.cs](xunit/src/CityCitizenServiceImplPerformanceTests.cs))
    * No need for game simulation to test the logic.
  * Implement building, wonders and advancements effects on happiness
    * including MichelangelosChapel and JSBachsCathedral on same continent as city with wonder
  * Marshall Law effects on happiness
  * Democracy/Republic effects on happiness
  * Luxury tax rate effects on happiness
  * Entertainer specialists effects on happiness
  * RedShirts citizens on Emperor difficulty level with player more than 36 cities.
  * Base performance tests for future changes and optimizations
  * Optimization of calling the new implementation by decreasing calls to CityCitizenServiceImpl
* Fix: Michelangelo's Chapel now gives +6 happiness instead of +4 with Cathedral if on same continent as city with wonder.
* Fix: Allow Palace to be build again after it was sold.
* Fix: City science calculation is now correct according to original game behavior.
  * Libraries and Universities receive a 66% bonus to science each if Isaac Newton's College is built, not obsolete and SETI Program is not built otherwise 50% bonus each.
  * Copernicus Observatory doubles the total science output of the city if it is not obsolete.
* Fix: Goto with mouse does not show mouse cursor if native cursor instead of built-in is used.
* Feature: Add pollution tracking to Player and update Demographics display
* Feature: Implement pollution clearing functionality for Settlers (4 Rounds to clear)
  * Hotkey 'p' to clear pollution on the tile where the settler is located.
  * Fix: prevent 'p' to be used for pillage if Shift is NOT pressed.
* Fix: Avoid negative frame index in CityView animations for invaders/revolters and "We love the president" day.
* Tax and Science calculations are incoorporated with player rates.
* Feature: Implement global warming mechanics
  * The global warming level is indicated by a lamp icon in the sidebar.
    * none, dark red, light red, yellow, white stages for none, 1, 2-3, 4-5, 6+ polluted squares
    * More than 8 polluted squares causes global warming event.
    * After every global warming event, the polluted tiles are cleared and a warming counter is increased.
    * Next global warming event happens at 8 + (warming counter * 2) polluted squares.
  * Implementation of calculating pollution is done in GlobalWarmingCountServiceImpl.cs
  * Changing tiles is done in GlobalWarmingScourgeServiceImpl.cs
  * Changes can be easily done by inheritance and overriding methods.
  * Debug menu option to trigger global warming event immediately.
  * Patches menu option to enable/disable extended pollution mechanisms
    * Rises sea level and converts land tiles to water tiles in 10% of cases on affected tiles.
    * Tiles that are affected by global warming:
      * All river tiles
      * Jungle tiles
      * Swamp tiles
      * Tundra tiles
      * Arctic tiles
      * Tiles on top and bottom 3 rows of map (pole ice caps)
    * Removes units on affected tiles
    * Removes improvements on affected tiles
    * Removes pole ice caps (Arctic and Tundra tiles on top and bottom 3 rows of map) in 20% of cases.
* Feature: Implement pollution mechanics and visual representation in city management
* Fix: Removed duplicate update check in DrawLayer to allow using false as return value in HasUpdate in city screens and still be able to draw the contents.
* Feature: Citizens in Attitude Survey screen and Top Five Cities screen are drawn tightly packed in big cities (size > 20) to show up to 99 citizens.
  * TopCities screen can be resized in width
* Feature: Citizens in city view are drawn tightly packed in big cities (size > 20) to show up to 99 citizens.
  * Specialists are shown and can be interacted with (click to change specialist status)
  * Routine for calculating citizen positions refactored to allow use in other places later (e.g.
  attitude survey screen and top five cities screen)
* Feature: Added hotkey '1' to '9' and '0' to cycle specialists tax, science from 1 to 10
  * If modifier key Shift is pressed: 11 to 20
  * Ctrl is pressed: 21 to 30
  * Alt is pressed: 31 to 40
  * Ctrl + Shift is pressed: 41 to 50
  * Alt + Shift is pressed: 51 to 60
  * Alt + Ctrl is pressed: 61 to 70
  * Alt + Ctrl + Shift is pressed: 71 to 80
  * Control + Shift + 0 does not work because KeyDown is not called.
* Refactor selling city buildings with mouse to allow for extend view mode (in settings) for arbitrary screen sizes.
  * Using IInteractiveButton to handle button drawing and mouse clicks
* Feature: Added trading value calculations and display for trading cities in CityInfoUnits
  * Trading cities are shown in City View (City Info Units panel).
  * Trade value is calculated based on the original game formula.
  * Trade value is shown in the city info units panel.
  * Trade value is added to the total gold of the player.
  * Trade value has Gold icon behind the value instead of trade arrows.
    * Trade arrows is incorrect in original game but the small font (id=1) does not have a gold icon.
  * See [Trading Cities](REMARKS.md#trading-cities) for more details.
* Feature: Added hotkey 'p' to select tiles in city view map.
  * Press 'p' to start tile selection.
  * Use Up/Down/Left/Right arrow keys to move the selection cursor.
  * Press Space/Enter to add or remove the resource.
  * Press 'p' or ESC to close tile selection.
  * Only visible tiles can be selected.
  * Selection wraps around the map.
* Feature: Added Trading cities
  * Up to 3 Trading cities can be established per city.
  * Trading cities are stored and loaded from save files.
  * Cities are shown in City View (City Info Units panel).
  * See [Trading Cities](REMARKS.md#trading-cities) for more details.
* Feature: Added hotkey 's' to sell buildings in city view.  
  * Press 's' to start building selection.
  * Use Up/Down arrow keys to cycle through buildings.
  * Press Space/Enter to sell the selected building.
  * Press 's' or ESC to close building selection.
  * If a building was already sold this turn, a message dialog is shown and no building can be sold again this turn. This is not exactly like in the original game, but more user friendly.
  * If more than 14 buildings are available, up/down will switch pages.
  The original game just exits the city view without any message.
* Feature: Added hotkey 'f' and 's' to set fortified or sentry status for units in city view.  
* Feature: Added hotkeys to CityManager
  * 1-9 to cycle specialists tax, science
  * hotkey 'a' to select units in city. Use arrow keys to cycle through units. Use space/enter to select unit and remove fortified/sentry status. ESC/'a' to close unit selection.
  * Mouse click on unit icon to remove fortified/sentry status.
  * Hotkey Shift+'a' to select auto build item.
* Fix: specialists can only be changed if city is greater than 4 size
* Fix: Add help context with alt+h to city production menu items.
  * Fixes NPE if alt+h is pressed in production menu.
* Feature: While showing the intro screen (credits) the first letter of each menu entry can be used as a hotkey to immediately execute the corresponding menu item. Like in the original game. Issue #1.
  * s for Start a New Game
  * l for Load a Saved Game
  * e for EARTH
  * c for Customize World
  * v for View Hall of Fame (not implemented)
* Fix: When a unit with more than one standard moving points (cavalerie, armor etc.) but has less current moving points, and the terrain target is a tile with more than one moving point (mountain, hills, etc.), there is a 10% chance that moving fails. (#180 Terrain difficulty not preventing units from moving). Implemented through observation.
* Fix: City view may show 0 houses.
  * Improved house placement algorithm to add at least 2 houses in city view instead of 0. (Issue original repo: #32, #61 of forked repo)
* Feature: City specialists (tax, science, and entertainer) and City status (e.g. riot, coastal, hydro, auto build, tech stolen, celebration or rapture, building sold) are now saved and restored from save files.
* FortifiedUnits data structure and do not count to unit max count for the civilization.
  * Feature: Up to 2 fortified units in a city are now stored in save game in separate slot.
  * See [Remarks](REMARKS.md#fortified-units-in-cities) for more details.
  * Though this may not be the exact behavior of the original game. Must be tested.
* Fix: Units in a city that are not fortified are now correctly restored from save files (previously these were lost).
* Feature: Extended loading and saving for City properties:  VisibleSize, TradingCities and BaseTrade (still not used though).
* Fix: Fix TradeReport to not show negative trade values (City::TradeTotal)
  * This can happen when corruption exceeds the total trade generated by resource tiles, often in cities located on distant continents with high corruption.
  * Negative trade is not displayed in the city screen, but appears in the trade report and confuses players.
  * This may also happen in the original game, but it was not tested if this is the case.  
* Conquest screen now shows "The entire world hails" and waits for user input before closing the screen/game.
* F12 shows debug menu if enabled (remember using first character of each item to cycle faster through the menu).
* Added Remarks file to document the game behavior and author's thinking.
* Feature: Added respawn of civilizations if it gets destroyed until 0 AD.
  * Its buddy civilization will be spawned (see Civilization.cs).
  * A civ with same color will be spawned only once.
* Fix: Civilization replay destruction handling to use PreferredPlayerNumber to keep civilization id between 0 and 7, otherwise crashes original game and JCivEdit.
* Air units retain movement points on carrier (won't land).
* Settler city animation turned off if animation setting is off.
* Extended window scale factors up to 8 (previously 4)
* Added sub menu "Game behavior menu" to patches menu with settings for the game
  * Canal City crossing (no movement points lost)
  * Fast movement on rivers
  * Other existing items from parent menu
    * Use smart path finding
    * Use auto settlers
* Settings for Canal City
  * Added a setting to allow crossing a canal city without losing all movement points.
  * The default is false, so the original behavior is kept.
* Major refactor of transport logic
  * Use OOP with polymorphism to handle transport logic for land, air, and sea units.
  * A sea unit determines if and which type of unit can be transported.
  * Makes code more readable and maintainable.
* Added fast movement on rivers for unit
  * Can be enabled in the settings (patches)
* Added the settings (formerly only available at the start of the game) to the debug menu.
  * Be aware that not all settings are available within the game or may even crash the game.
* Enhanced debug menu
  * First character of menu items selects the menu item or the next item with the same first character.
  * Up/Down keys on first and last menu item select the last or first item (cycle through the menu).
* Only a single barbarian leader brings ransom.
* Carrier planes are now correctly hidden when carrier is moved (like any other sea unit).
* AI does no more attack with a carrier
* Make Barbarians Diplomat abandon itself if it gets lost.
* Major Refactor tribal hut event handling and introduce interfaces for better separation of concerns and dependency injection.
  * Moved from BaseUnitLand to its own domain/namespace TribalHuts.
  * Cleaned and fixed strange code behavior.
* Prevent a destroyed unit to be shown for a short time in the place of the attacked unit or city (#105)
* Fix: Destroying an AI city does not show the hut on the tile anymore.
* Major Update in Unit Movement
  * Refactoring for better understanding of movement logic.
  * Separation of movement logic for land, air, and sea units (using OOP)
    * Caravans movement logic moved to Caravan.cs
  * Fixing Fuel value for air units, because its value was not being used with/in savegame files and therefore not restored.
  * Logic of air units fixed (disbanding now works correctly for bomber (2rounds), fighter, and nuclear).
  * If "End Turn" is disabled, the player can now continuing moving units without hitting Enter each end of turn.
* Settlers build roads/railroads like in the original CIV1 (<https://github.com/fire-eggs/CivOne/issues/149>)
  * WorkProgress is stored in save file
  * WorkProgress is stored for each settler separately
  * Roads/railroads can be built on ocean (always)
  * Waking up a settler will not reset its progress (civ bug)
* Fixed hit key down in a game menu to not allow going beyond the last item.
* Added a new debug option to load a saved game.
* Added loading a saved game immediately when starting the program (using `--load-slot` option).
* Added README.md
* Fixed NPE at the end of game.
* Fixed sound playback.

## Known Issues

* JCivEdit crashes with NPE if a city is conquered and the saved game is loaded in the editor.
