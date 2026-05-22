# Changes

## Comment

I did not browse all issues on github at first, so I did not recognize that some of my fixes have an issue.

## History

* Debug Option:
  * Added a debug option to trigger an instant government change.
* Fix: Stabilized city resource caching to prevent stale values after resource tile changes.
  * `FoodRaw` cache is now validated via tile/city state hash and recalculated when needed.
  * Added `ShieldRaw` cache using the same state-hash pattern to avoid repeated `ResourceTiles.Sum(t => ShieldValue(t))` scans.
  * `ShieldTotal` now consumes `ShieldRaw` before applying building multipliers (Factory, Nuclear Plant, Mfg Plant).
  * Cache invalidation now clears both food and shield raw caches on relevant city/resource tile mutation paths (owner/size/resource tile updates).
  * Added focused unit test coverage for shield cache recomputation in `CityEconomyServiceImplCalculateBreakdownTests.ShieldTotal_Recomputes_WhenWorkedTileShieldChangesWithinSameTurn`.
  * Keeps the performance optimization while ensuring correct `FoodIncome`, `FoodTotal`, and `ShieldTotal` values.
* Fix: Fix and Migration of [mwerneburg](https://github.com/ChrisWiGit/CivOne/pull/33)
  * Keep city home-unit cache and unit home reference consistent during removal.
  * `DestroyCity(...)` and `DisbandUnit(...)` now clear a unit's home via `SetHome(null)` before removing it from game unit lists.
  * Prevents stale `city.Units` cache entries and avoids mismatches where `unit.Home` still points to an old city after disband/removal.
* Refactoring: Added translation support for texts in dialogs and reports.
* Fix: Dialogs now render correctly when using "Expand Size" in the setup menu, instead of being stretched across the expanded canvas.
* Feature: Implementation of spaceship construction and victory condition
  * Added spaceship construction mechanics that let cities produce ship parts and assemble them through the spaceship screen.
  * Added end-game handling for a completed spaceship so it can participate in victory processing as a distinct late-game win path.
  * Debug: added spaceship construction menu with part placement and launch action to the debug options for testing purposes.
* Debug Option:
  * Introduce large 2d menu for large lists of items (e.g. cities, units, etc.) with keyboard navigation and selection.
  * Refactored existing civilization and city selection menus to use the new grid menu, allowing for more items and better navigation.
* Extended game menu
  * Menu items now wrap at the top and bottom, so moving past the first or last entry jumps to the other end.
  * When a submenu is already open, `Left` and `Right` now switch between the main menu groups (`Game`, `Orders`, `Advisors`, `World`, `Civilopedia`) in a loop.
  * Disabled menu items stay disabled for both mouse and keyboard input and no longer close the menu when selected.
  * The World -> SpaceShips entry is only shown when at least one civilization can actually access spaceship content.
* Feature: International font simulation for non-English languages
  * Players with an English-only `FONTS.CV` can now display translated text (e.g. German umlauts, French accented letters) without a modified font file.
  * Missing glyphs are synthesised at runtime by composing the base letter with the required diacritic mark.
  * Activated automatically in auto language mode; can also be set explicitly via **Shift+F1 → Game Options → Language**.
* Feature: Added Hall of Fame screen showing top historical civilizations based on the player's score
  * Shown during end-game flow after the score/ranking sequence for conquest, defeat, Alpha Centauri, and retire outcomes.
  * The Hall of Fame screen shows a ranked list of civilizations with their score, leader name, civilization name, and victory type.
  * The look does not match the original game.
* Feature: Added victory screen showing a victory message and the player's palace in the background when the player conquers the world. This is not original game behavior.
* Top Leader screen now shows a percentage-based rating bar for each leader, with the player's leader highlighted. The rating is calculated based on the player's score relative to the top leader's score, and is displayed as a horizontal bar with a percentage label.
  * TODO: Currently, only available through debug menu. Future integration into the original ranking screen trigger conditions is planned once they are identified.
* Feature: Translation system with multi-language support
  * In-game translation is now active.
  * Language can be changed in the setup menu via `Shift+F1` -> `Game Options` -> `Language`.
  * The selected language is applied through `TranslationServiceFactory` and reused by gameplay and UI services.
  * `civtranslate` CLI tool scans `*.cs` files for translation calls and creates/updates translation key-value files.
    * Scans for `.Translate("...")`, `.TranslateFormatted("...", ...)`, and `T("...")` patterns.
    * Normalizes keys to uppercase while keeping values as source text.
    * Writes `key=value` entries and escapes literal `=` as `[EQ]`.
    * Merges with existing output files, preserving comments and handling value overwrites.
    * Writes obsolete keys to `obsoletekeys.txt` in the output directory.
    * Translation files support comment headers (lines starting with `#`).
    * Existing comment headers are preserved when updating files.
  * Translation loader ignores `#` comment lines in language files.
  * `SaveMetaDataService` now resolves translation service via factory to always use the currently active language.
* Refactored palette handling
  * Extended the `Palette.Merge` method and used it to improve performance and code clarity.
  * Replaced all direct `Palette = Common.DefaultPalette` assignments with `using` blocks to ensure immediate disposal.
* Barracks are now obsolete when Gunpowder or Combustion is discovered, i.e. all existing Barracks are removed immediately when either of these technologies is researched.
  * The behaviour can be turned on or off in the setup menu under "Remove obsolete buildings".
* Settings (Shift-F1) shows a helpful description for each setting when it is selected.
* Migration:
  * Copied A* pathfinding (`GotoStep`) implementation from [mwerneburg/CivOne/`Common.cs`](https://github.com/mwerneburg/CivOne/commit/e33b3968ebceea45a5f046c99c166574a5dfa08f)
  * Copied behaviour of [mwerneburg/CivOne/`Ai.Barbarians.cs`](https://github.com/mwerneburg/CivOne/commit/5525827163b01996553b3b7a854c5d16b406a509) for better movement decisions of barbarian units, including ocean/land avoidance and goal tile bypass.
  * Added as new service `UnitGotoServiceImpl` in `Services/Pathfinding` and factory method in `UnitGotoServiceFactory` to not mix the A* implementation with the existing movement logic and to allow for future extension to other pathfinding algorithms if needed.
  * Unit tests added (`UnitGotoServiceImplTests`) for the A* pathfinding implementation, covering various terrain and unit scenarios.
* The game is now paused when
  * the window is minimized or hidden to waste less CPU resources when the game is not visible.
  * the user presses the Pause key to toggle pause state of the game.
  * Pressing the Pause key and minimizing and then restoring the window will not unpause the game.
* Feature: Added Civilization Ranking screen integration with turn-based trigger service.
  * The ranking screen is now triggered from a dedicated service checked on each human turn.
  * Temporary (non-original) trigger algorithm: show the ranking every random 300-500 years.
    * The next trigger year is randomized after each display.
    * This is an interim implementation until original game conditions are identified.
  * Palace preview rendering is integrated into the ranking screen, showing the palace corresponding to each civilization's current palace level.
  * Debug controls on the ranking screen:
    * `F1`: cycle ranking category.
    * `F2`: toggle known civilizations vs all civilizations.
  * Save-state note:
    * The current ranking screen trigger state/category rotation is not persisted.
    * There is an SVE/SAV `rank` field, but its exact original semantic meaning is currently unknown and is not yet used as source of truth for this feature.
* Feature: Added the new palace rendering and palace part composition.
  * The palace upgrade trigger currently uses `CivilizationScore >= 1 + n*n + n` (`HumanCivScorePalaceTrigger`), where `n` is the number of existing palace upgrades.
    This can be adjusted later if balancing changes are needed.
  * Preview of the palace is shown in the sidebar demographics panel.
  * TODO: AI still needs to be able to build the palace.
  * Some minor changes may apply:
    * Alignment and placement of palace parts may be slightly different from the original game.
    * The furthest left and right (key 1 and 7) towers are now fully visible in the palace view, while in the original game they were behind the wall.
  * Debug: when the palace screen is opened from the debug menu you can:
    * Press F1 to toggle noise on/off for easier debugging of the morph stages.
    * Place all parts of the palace manually.
    * Press Escape to exit the palace screen.
* Fix: Addressed multiple long-standing gameplay issues from issue tracker from [https://github.com/fire-eggs/CivOne/issues](https://github.com/fire-eggs/CivOne/issues).
  * Fixed city buy edge case where overpayment could lead to invalid negative-cost purchase handling ([#179](https://github.com/fire-eggs/CivOne/issues/179)).
  * Buying city improvements is now blocked while the city is in civil disorder. ([#153](https://github.com/fire-eggs/CivOne/issues/153)).
  * AI city production no longer defaults to excessive caravan production once basic unit thresholds are met ([#172](https://github.com/fire-eggs/CivOne/issues/172)).
  * Diplomat tech theft is now limited to one successful theft per city until ownership changes ([#121](https://github.com/fire-eggs/CivOne/issues/121)).
  * Tech-theft city state now resets correctly when a city changes owner (conquest/incite/flip) ([#121](https://github.com/fire-eggs/CivOne/issues/121)).
  * Barbarians no longer land/spawn on arctic polar tiles ([#122](https://github.com/fire-eggs/CivOne/issues/122)).
  * Open/under review: No code change included yet for "We love the president" population behavior and "Unit trying to leave city blocked"; current behavior appears rule-dependent or already covered by existing movement logic/tests, but both tickets stay open for targeted regression verification ([#182](https://github.com/fire-eggs/CivOne/issues/182), [#123](https://github.com/fire-eggs/CivOne/issues/123)).
* Feature: Added support for MCP (Multi Client Protocol) to allow external clients (e.g. VS Code extension) to connect and interact with the game for testing, debugging, and automation purposes. See [MCP.md](MCP.md) for details on how to use the MCP server and its current capabilities.
  * Added command line option `--mcp` to start the game in MCP server mode.
  * Added command line option `--mcp-artifacts <folder>` to specify a custom folder for MCP artifacts (e.g. screenshots).
  * Added command line option `--mcp-saves <folder>` to specify a custom folder for MCP `.cos` save listing, loading, and MCP save creation.
  * Added command line option `--mcp-no-auth` to disable session token authentication for easier testing without token handling.
  * Added manual MCP execution support via OpenAPI in [mcp/openapi.yml](mcp/openapi.yml) for HTTP mode (`--mcp-http`).
    * OpenAPI examples are prefilled with valid JSON-RPC `tools/call` envelopes.
    * In most cases only `id` and `params.arguments` need to be changed for manual testing.
    * Can be used directly in Visual Studio Code REST Client extension or any other OpenAPI-compatible client.
  * Added MCP tool `game_save` to write the current game state as a new `.cos` file in the configured MCP saves folder.
    * Save files are always created with a unique timestamp-based name: `savegame_mcp_<UTC yyyyMMddHHmmssfff>.cos`.
    * Existing files are never overwritten.
    * If the computed file name already exists, the tool returns a `FILE_EXISTS` error with the message `file exists, wait a second, till next try`.
    * The response returns both `fileName` and the newly generated `saveGuid`.
* Add end credit score screen after conquering the world, showing the final score and ranking of the player.
  * Shows the player's final score and rank compared to historical civilizations.
  * Uses the original game's scoring system and ranking thresholds (may currently show only 0)
  * Triggered after the conquest screen when the player wins by conquering the world.
  * TODO: implement the actual score calculation logic based on the player's achievements and game state at the end of the game.
* Feature: Added quick save and quick load hotkeys with profile-based COS slots.
  * `Ctrl+F1` to `Ctrl+F10` saves quick slots to `fastsave_f1.cos` to `fastsave_f10.cos`.
  * `Alt+F1` to `Alt+F10` loads quick slots from the same files.
  * `Alt+F11` opens a modal quick load slot dialog.
  * In the quick load dialog, `F1` to `F10` can be used as direct slot shortcuts.
  * Invalid `.cos` slots are listed as `Invalid savegame`, disabled, and cannot be selected.
  * Quick slot files are stored in `saves` under the runtime profile storage directory (`%LOCALAPPDATA%\CivOne\saves` on Windows and `~/CivOne/saves` on Linux/macOS).
  * Hotkeys are handled globally and work in gameplay, credits, and end screens.
  * Missing or invalid quick slot loads now show a simple user-facing error message and log the technical error details.
  * After YAML quick load, gameplay screen is rebuilt so map centering is refreshed correctly.
  * Report hotkeys are now restricted to F1-F10 (without modifiers) to prevent conflicts with quick save/load hotkeys.
* Fix: `Alt+Enter` fullscreen toggle now persists the new state to the profile.
* Feature: Window placement persistence improved.
  * Window position is now stored in the profile and restored on startup.
  * On restore, position is validated against currently available displays; invalid/off-screen positions fall back to top-left (`0,0`).
  * Window maximized state is persisted and restored (windowed mode).
  * Refactor: window placement handling in `GameWindow.Update(...)` was split into smaller helper methods with clearer names.
* Feature: Added multiple standard screen and window resolutions to the setup menu (e.g. `1920x1080`, `2560x1440`, `3840x2160`).
  * Added preset options for "Window Size" and "Expand Size" settings in the setup menu.
  * Updated the "Expand Size" setting to allow for larger resolutions up to `7680x4320` (8K UHD).
  * Use "Auto" option for "Expand Size" to stretch the canvas to fill the window, otherwise the game will render at the selected resolution and may be cropped if the window is smaller.
* Feature: Ongoing migration from [`mwerneburg/CivOne`](https://github.com/mwerneburg/CivOne) (single consolidated entry; extend sub-points over time)
  * Migrated: `CivilizationIdentity` save/load bitmask fix in `SaveDataAdapter` (bit-shift mapping aligned with source fork behavior).
  * Migrated: macOS SDL native resolver registration in startup (`Program`).
    * Adds explicit fallback search paths for SDL2 framework/Homebrew installs on macOS.
    * Reduces manual environment setup requirements for native SDL loading.
  * Migrated: map centering and horizontal wrapping fix in `GameMap.CenterOnPoint(...)`.
    * Replaced hard-coded X offset with dynamic viewport-based centering.
    * Added explicit X wrapping for negative/overflow values.
    * Replaced hard-coded Y clamp window with `_tilesY`-based clamp.
  * Migrated: map generation grassland assignment fix in `Map.Generate` climate adjustment pass.
    * Ensures transformed `Terrain.Plains` tiles are assigned back to `_tiles[x, y]`.
  * Migrated: continent/ocean counting flood-fill in `Map.Generate`.
    * Replaced recursive traversal with iterative queue-based traversal.
    * Added horizontal X-axis wrapping during connectivity checks.
    * Reduces stack overflow risk on larger maps.
    * Implementation extracted to `ContinentTraversalDelegate` for better separation of concerns and testability.
  * Migrated: Expand settings load fix in `Settings`.
    * `ExpandWidth`/`ExpandHeight` now load into dedicated fields instead of `_scale`.
    * Allowed Expand ranges updated to `320..2560` and `200..1600`.
  * Migrated: Expand canvas/scaling adjustments in `GameWindow.Graphics`.
    * Expand default canvas fallback changed to `640x360` when no explicit Expand size is configured.
    * Sub-canvas bytemaps in Expand mode now keep integer scaling and centered placement.
    * Expand canvas hard cap raised from `512x384` to `2560x1600`.
    * Allowed Expand ranges updated to `320..7680` and `200..4320`.
  * War-time trade route purge now uses the existing city `TradingCities` model.
  * On `SetAtWar(...)`, trade links between both parties are removed bilaterally; third-party links remain unchanged.
  * added `IsAtWar(Player)` and `SetAtWar(...)` methods to `Player` to check runtime war state without consulting legacy diplomacy flags.
    * These methods are not yet integrated into gameplay and are not yet used for any game logic. They are intended to be used in future diplomacy mechanics implementation.
  * Implemented A* for computer player AI movement.
    * This can be reset to old behaviour in settings.
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
  * See example save file in [docs/SAVEGAME_EXAMPLE.cos](docs/SAVEGAME_EXAMPLE.cos).
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
