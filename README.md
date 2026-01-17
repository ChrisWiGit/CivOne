<!-- Please use a single sentence each line.
use sh or cmd blocks for commands.
Do not use en-dash or em-dash, use simple sentences only.
 -->
# CivOne# (Civ One Sharp)

## Introduction

A civilization game clone of Civilization (1991) written in C#.
It was originally developed several years ago by other authors, and then abandoned.
This project is a continuation of that work, with the goal of completing the game and making it fully playable, but with some tweaks and improvements that
make it more enjoyable for modern players.

## First Steps

### Requirements

* This program requires the .NET 9 Runtime to be installed on your system.
* Also SDL2 is required for graphics and sound support see [SDL2 Installation](#sdl2-installation-linux-windows-macos) for instructions.

### Running the Program

To run the program, navigate to the directory where the `CivOne.SDL` or `CivOne.SDL.exe` file is located and use the following command:

Windows Command Prompt:

```cmd
CivOne.SDL.exe
```

Linux / macOS Terminal:

```sh
./CivOne.SDL
```

### Install graphics from original game

When starting the game for the first time, you will need to install graphics from the original Civilization game.
The game will prompt you to select the directory where the original game's data files are located.
Select the directory containing the original Civilization game files to proceed.

If you do not have the original game files, you can use a free package of graphics files instead.

#### Data directory

The data files will be copied to

* on Linux/macOS: `~/CivOne/data`
* on Windows: `C:\Users\<YourUsername>\AppData\Local\CivOne\data` (also `%LOCALAPPDATA%\CivOne\data`)

### Program parameters

There are some command line parameters that can be used to modify the behavior of the program.

| Parameter | Description |
| --------- | ----------- |
| `--seed <number>` | Sets the random seed for the game. Replace `<number>` with an integer value. |
| `--skip-credits` | Skips the credits sequence at the start of the game. |
| `--skip-intro` | Skips the intro cinematic at the start of the game. |
| `--no-sound` | Disables sound in the game. |
| `--no-data-check` | Skips the data integrity check at startup. |
| `--load-slot <drive><slot>` | Loads a saved game from the specified drive and slot. Replace `<drive>` with a letter (a-z) and `<slot>` with a number (0-15) as if you were in the game |

### Loading a saved game immediately

To load a saved game immediately when starting the program, you can use the `--load-slot` option followed by a drive letter and a slot number.
The drive letter should be between 'a' and 'z', and the slot number should be between 0 and 15 (inclusive).
These correspond to the saved game files that are stored in the `SaveGames` directory (`~/CivOne/saves/c`)

If you want to load the saved game from drive 'c' and slot 0, you would use:

```sh
civone --load-slot c0
```

If you omit the slot number, a loading screen will be shown, allowing you to select a saved game interactively.

You can find your saves in the following locations:

* on Linux/macOS: `~/CivOne/saves/`
* on Windows: `C:\Users\<YourUsername>\AppData\Local\CivOne\saves\` (also `%LOCALAPPDATA%\CivOne\saves\`)

### The debug menu (in game)

You can activate the debug menu in-game by hitting `Shift + F1` and in the menu choosing `Patches`, then enabling `Debug Menu` by hitting `Enter` and selecting `Yes`.

The menu provides multiple options for testing and debugging the game, including:

| Option | Description |
| ------ | ----------- |
| Load a Game | Opens the in-game load screen to load a saved game. |
| Set Game Year | Opens a dialog to set the current game year. |
| Set Player Gold | Opens a dialog to set the selected player's gold. |
| Set Player Science | Opens a dialog to set the selected player's research/science. |
| Set Player Advances | Opens a screen to add technology advances to the selected player. |
| Set City Size | Opens a dialog to set a city's population/size. |
| Cause City Disaster | Triggers a city disaster (opens the Cause Disaster screen). |
| Add building to city | Opens the Add Building screen to add a building to a selected city. |
| Change Human Player | Switches which player is controlled by the human (Change Human Player screen). |
| Spawn Unit | Allows to spawn unit to create a unit for a player. Click left mouse button to place the unit or right mouse button to place multiple units of the same type. |
| Meet With King | Opens the Meet With King screen for diplomatic/events. |
| Toggle Reveal World | Toggles the "reveal world" cheat (shows the entire map). |
| Build Palace | Instantly builds a palace for the current player. |
| Instant Conquest | Instantly removes AI units and cities, shows Conquest screen, then quits the game. |
| Instant Global Warming | Pollutes all tiles and triggers the global warming scourge, then refreshes the map. Kind of algorithm depends on settings. |
| Settings | Opens the game's setup/settings screens (Shift+F1). |

> You can hit the starting letters of the options to quickly access them (e.g. `L` for Load a Game, `S` for Set Game Year, etc.).
> Hit multiple times to cycle through options starting with the same letter.

The debug menu visible flag is stored in `default.profile` in the CivOne directory in your user folder.

### Settings screen (CivOne Setup)

You can access the settings screen by hitting `Shift + F1` when starting the game or in-game through the debug menu (see [The debug menu (in game)](#the-debug-menu-in-game)).
The settings contains multiple options to configure the game, including:

#### Settings

These settings affect the overall game behavior and used graphics/sound options.

| Option | Description |
| ------ | ----------- |
| Window Title | Set the window title text shown by the game window. |
| Graphics Mode | Choose the graphics rendering mode (e.g. 256-colour or 16-colour). |
| Aspect Ratio | Select how the game handles aspect ratio (Auto, Fixed, Scaled, ScaledFixed, Expand). |
| Full Screen | Toggle fullscreen mode on or off (`Alt+Enter`). |
| Window Scale | Set the UI scale multiplier (1x to 8x) for window size. |
| In-game sound | Browse for and enable in-game sound files. |

#### Patches

This screen allows to change additional modification options for the game.

| Option | Description |
| ------ | ----------- |
| Reveal world | Toggle the "reveal world" cheat so the entire map is visible. |
| Side bar location | Choose the side for the in-game sidebar (left or right). Changing this may require a restart. |
| Debug menu | Enable the in-game debug menu (only available when the game is not running). |
| Cursor type | Select mouse cursor type (Default, Builtin, or Native). |
| Destroy animation | Choose destroy animation style (Sprites or Noise). |
| Enable Deity difficulty | Enable the Deity difficulty option. |
| Enable (no keypad) arrow helper | Toggle an arrow helper for users without a numeric keypad. |
| Custom map sizes (experimental) | Enable experimental custom map sizes. |
| Game behavior menu | Open the behavior submenu to change gameplay-related toggles and cheats. |
| (Gbm) Use smart PathFinding for "goto" | Enable smart pathfinding for unit movement. |
| (Gbm) Use auto-settlers-cheat | Enable automatic settlers (cheat) to place settlers automatically. |
| (Gbm) Use fast river movement | Movements on rivers behave like roads (faster movement). |
| (Gbm) No movement penalty for sea units in city | Sea units suffer no movement penalty when in a city. |
| (Gbm) Extended global warming | Open extended global warming options (needs savegame load). |
| (Gbm+Egw) Sea level rise | Extended game play with sea level rise instead of only land changing. |

#### Plugins

This screen allows you to manage plugins for the game.

#### Game Options

These options affect the gameplay mechanics and rules and can also be changed in-game via the options menu.

| Option | Description |
| ------ | ----------- |
| Instant Advice | Configure whether the game shows instant advice prompts to the player. |
| AutoSave | Enable or disable automatic saving during play. |
| End of Turn | Configure end-of-turn behavior (e.g. immediate end or prompts). |
| Animations | Toggle in-game animations (on/off) to improve performance or visuals. |
| Sound | Toggle sound effects and music for the game. |
| Enemy Moves | Show or hide enemy move animations while processing turns. |
| Civilopedia Text | Select Civilopedia text display mode (affects in-game encyclopedia text). |
| Palace | Toggle palace-related behaviour or display options in cities. |
| Tax Rate | Set the tax rate which splits commerce between gold and science (0%–100%). |


#### Launch Game / Return to Game

Closes the settings screen and launches or returns to the game.

## SDL2 Installation (Linux, Windows, macOS)

This game depends on **SDL2** (Simple DirectMedia Layer 2) for windowing, input, audio, and graphics.  
You must install the **native SDL2 library** for your operating system before running the game.

> **Note:** This project uses SDL2 via C# bindings (e.g. SDL2-CS). Even when using NuGet, the native SDL2
> runtime must be present on your system.

### Linux

On most Linux distributions, SDL2 is available via the system package manager.

**Ubuntu / Debian:**

```sh
sudo apt update
sudo apt install libsdl2-2.0-0 libsdl2-dev
```

**Fedora:**

```sh
sudo dnf install SDL2 SDL2-devel
```

**Arch Linux:**

```sh
sudo pacman -S sdl2
```

After installation, the SDL2 shared library should be available automatically.

### Windows

1. Download the **SDL2 Development Libraries for Windows** from:
   [https://www.libsdl.org/download-2.0.php](https://www.libsdl.org/download-2.0.php)

2. Extract the archive (e.g. `SDL2-2.0.x`).

3. Copy the correct `SDL2.dll` to a location where your game can find it:

   * Next to your compiled `.exe`, **or**
   * Into a directory listed in your `PATH`

   Use:

   * `x64/SDL2.dll` for 64-bit builds
   * `x86/SDL2.dll` for 32-bit builds

4. Make sure your C# project targets the same architecture (x64 or x86).

### macOS (currently untested)

The recommended way to install SDL2 on macOS is via **Homebrew**.

1. Install Homebrew (if not already installed):
   [https://brew.sh](https://brew.sh)

2. Install SDL2:

```bash
brew install sdl2
```

Homebrew installs SDL2 system-wide, and the dynamic library will be available automatically at runtime.

### Troubleshooting

* Ensure the SDL2 **native library** is installed, not only C# bindings.
* Architecture mismatches (x86 vs x64) are a common cause of startup errors.
* If the game fails to start, check that `SDL2.dll` (Windows) or `libSDL2-2.0` (Linux/macOS) is discoverable by the runtime.

## Building from source

### Prerequisites

* .NET 9 SDK
* SDL2 runtime installed (see [SDL2 Installation](#sdl2-installation-linux-windows-macos))

### Using Visual Studio Code

The project provides a `launch.json` file for Visual Studio Code, which can be used to run and debug the project.
To use it, open the project in Visual Studio Code, go to the Run and Debug view and use one

### Tests

To run the tests, you can use the following command:

```sh
dotnet test
```

Extended console output will be shown during the test run, providing more insights into the test execution and results.

```sh
dotnet test --logger "console;verbosity=detailed"
```

Some tests require the original data files to be present in order to run successfully.
To skip them use

```sh
dotnet test --filter "FullyQualifiedName!~ZOCTests&FullyQualifiedName!~IrrigateTest"
```

## Changes (Log)

See [CHANGES.md](CHANGES.md) for a detailed list of changes and updates.
