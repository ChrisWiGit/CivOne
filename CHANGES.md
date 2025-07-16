# Changes

* Major Update in Unit Movement
  * Refactoring for better understanding of movement logic.
  * Separation of movement logic for land, air, and sea units (using OOP)
    * Caravans movement logic moved to Caravan.cs
  * Fixing Fuel value for air units, because its value was not being used with/in savegame files and therefore not restored.
  * Logic of air units fixed (disbanding now works correctly for bomber (2rounds), fighter, and nuclear).
* Settlers build roads/railroads like in the original CIV1.
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
