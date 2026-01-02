# Changes

## Comment

I did not browse all issues on github at first, so I did not recognize that some of my fixes have an issue.

## History

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
  * RedShirts citizens
  * Base performance tests for future changes and optimizations
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
  * [ ] TODO: store and load from save files.
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
