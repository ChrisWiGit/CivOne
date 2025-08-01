# Changes

## Comment

I did not browse all issues on github at first, so I did not recognize that some of my fixes have an issue.

## History

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
Moved from BaseUnitLand to its own domain/namespace TribalHuts.
Cleaned and fixed strange code behavior.
* Prevent a destroyed unit to be shown for a short time in the place of the attacked unit or city.
* Fix: Destroying an AI city does not show the hut on the tile anymore.
* Major Update in Unit Movement
  * Refactoring for better understanding of movement logic.
  * Separation of movement logic for land, air, and sea units (using OOP)
    * Caravans movement logic moved to Caravan.cs
  * Fixing Fuel value for air units, because its value was not being used with/in savegame files and therefore not restored.
  * Logic of air units fixed (disbanding now works correctly for bomber (2rounds), fighter, and nuclear).
  * If "End Turn" is disabled, the player can now continuing moving units without hitting Enter each end of turn.
* Settlers build roads/railroads like in the original CIV1 (https://github.com/fire-eggs/CivOne/issues/149)
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
