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
| `--console-log` | Enables console log output. This is the default. |
| `--no-console-log` | Disables console log output on the console. The log file is still written. |
| `--language <postfix>` | Loads `translations/civ_<postfix>.txt` at startup. Use `identity` for original text without translation. Language files must exist in the active CivOne profile. |
| `--load-slot <drive><slot>` | Loads a saved game from the specified drive and slot. Replace `<drive>` with a letter (a-z) and `<slot>` with a number (0-15) as if you were in the game |
| `--load-cos <path>` | Loads a savegame file directly from a file path. |
| `--mcp` | Enables the MCP server. See [MCP.md](MCP.md) for setup and usage. |
| `--mcp-no-auth` | Disables MCP session-token authentication. Useful for direct VS Code MCP client usage. |
| `--mcp-artifacts <path>` | Sets the directory where MCP screenshot artifacts are saved. Defaults to `temp/mcp-runs/` inside the storage directory. |
| `--mcp-saves <path>` | Sets the directory where MCP save tools read and write `.cos` files (`game_list_saves`, `game_load`, `game_save`). |

```sh
dotnet run --project ./runtime/sdl/CivOne.SDL.csproj -- --seed 12345 --language german
```

```powershell
dotnet run --project ./runtime/sdl/CivOne.SDL.csproj -- --seed 12345 --language german
```

```sh
dotnet run --project ./runtime/sdl/CivOne.SDL.csproj -- --seed 12345 --language identity
```

### Translation workflow

The repository includes translation helper tools in the repository root.
Use `translate.ps1` or `translate.sh` to generate or update `translation/all.txt`.
If you already have an existing language file with manual translations, it is often better to merge missing keys instead of generating the whole file again.
Use `civtranslate-mergekeys` to append keys from `translation/all.txt` that are missing in your `civ_<postfix>.txt` file without changing existing translated values.

```sh
dotnet run --project ./civtranslate-mergekeys/civtranslate-mergekeys.csproj -- ./translation/all.txt ./translation/civ_german.txt
```

You can also use the helper scripts from repository root and pass only file names.

```powershell
.\translate-mergekeys.ps1 all civ_german
```

```sh
./translate-mergekeys.sh all civ_german
```

Use `translate-interactive.ps1` or `translate-interactive.sh` to run the values-only translation roundtrip for a language postfix.
Use `copy-translations.ps1` or `copy-translations.sh` to copy final language files to the active CivOne profile.
For the full translation workflow and naming rules, see [civtranslate/README.md](civtranslate/README.md).
For merge helper details, see [civtranslate-mergekeys/README.md](civtranslate-mergekeys/README.md).

Examples:

```powershell
.\translate-interactive.ps1 -Language german
```

```sh
./translate-interactive.sh --language german
```

### Use translation in game

Translation is now active in the game UI and gameplay text.
You can select the language in the setup menu with `Shift + F1`.
Open `Game Options`, then select `Language`.
Choose `Identity (default)` to use original keys, or choose one of the available `civ_<postfix>.txt` language files.

Language files must be placed in your CivOne profile translation folder.
On Windows this is `%LOCALAPPDATA%\CivOne\translations`.
On Linux and macOS this is `~/.local/share/CivOne/translations`.

The CivOne profile root is the parent folder of those files.
On Windows this is `%LOCALAPPDATA%\CivOne`.
On Linux and macOS this is `~/.local/share/CivOne`.

Other common folders are:

* Windows: `%LOCALAPPDATA%\CivOne\data`, `%LOCALAPPDATA%\CivOne\saves`, `%LOCALAPPDATA%\CivOne\sounds`
* Linux and macOS: `~/.local/share/CivOne/data`, `~/.local/share/CivOne/saves`, `~/.local/share/CivOne/sounds`

To create or update language files, run the CLI scanner from repository root and copy the output file to your profile translation folder with a `civ_<postfix>.txt` name.

Example:

```sh
dotnet run --project ./civtranslate/civtranslate.csproj -- ./src --output ./translation/civ_german.txt
```

### Game menu hotkeys

The top gameplay menu supports translator-defined hotkeys.
Use `~` directly before the character that should be highlighted and used for the `Alt+<key>` shortcut.
The `~` marker is not shown in the UI.
The marked character is highlighted in the menu bar.
If no valid marker exists, the first visible character is used as fallback.
The translation keys in code must still be written as explicit `Translate("...")` calls so the translation tools can find them.

Example entries in a translation file:

```txt
GAME=~SPIEL
ORDERS=~BEFEHLE
ADVISORS=BE~RATER
WORLD=~WELT
CIVILOPEDIA=~CIVILOPEDIA
```

In this example, `Alt+S` opens `SPIEL`, `Alt+B` opens `BEFEHLE`, and `Alt+R` opens `BERATER`.

If no `~` marker is present, the first character is used as fallback.

### MCP savegame tools

When MCP is enabled, savegame automation tools can read and write `.cos` files in the configured MCP saves folder.

`game_save` creates a new save file and never overwrites an existing file.
The filename format is `savegame_mcp_<UTC yyyyMMddHHmmssfff>.cos`.
If a file with the computed name already exists, the tool returns a `FILE_EXISTS` error and asks the caller to retry.
On success, the response includes both the new `fileName` and a newly generated `saveGuid`.

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

### Loading a savegame from a file (new file format - YAML)

To load a savegame file directly when starting the game, use the `--load-cos` option followed by the file path.
The path can be absolute or relative to the current working directory.

Example:

```sh
civone --load-cos ./SaveGames/c/auto-save.cos
```

On Windows you can also use:

```cmd
CivOne.SDL.exe --load-cos "C:\\Users\\<YourUsername>\\AppData\\Local\\CivOne\\saves\\c\\auto-save.cos"
```

### Quick save and quick load hotkeys

You can use fast in-game hotkeys for ten quick save slots.

| Hotkey | Action |
| ------ | ------ |
| `Ctrl+F1` to `Ctrl+F10` | Save quick slot 1 to 10 |
| `Alt+F1` to `Alt+F10` | Load quick slot 1 to 10 |
| `Alt+F11` | Open quick load slot menu |

Quick slot files are stored in the `saves` subfolder of the profile storage directory.

* Windows: `%LOCALAPPDATA%\CivOne\saves`.
* Linux/macOS: `~/CivOne/saves`.

File names are:

* `fastsave_f1.cos`
* `fastsave_f2.cos`
* `fastsave_f3.cos`
* `fastsave_f4.cos`
* `fastsave_f5.cos`
* `fastsave_f6.cos`
* `fastsave_f7.cos`
* `fastsave_f8.cos`
* `fastsave_f9.cos`
* `fastsave_f10.cos`

Behavior notes:

* Hotkeys are handled globally and work in gameplay, credits, and end screens.
* If a slot does not exist or load/save fails, the game shows a simple error message and writes technical details to the log.
* After a YAML quick load, the gameplay screen is rebuilt so map centering is refreshed.
* `Alt+F11` opens a modal quick load dialog with all existing quick slots.
* In the `Alt+F11` dialog, `F1` to `F10` are direct slot shortcuts.
* Slots with invalid `.cos` content are shown as `Invalid savegame`, are disabled, and cannot be selected.
* If no quick save exists, the dialog shows `No fast savegames available. Use Ctrl+F1-F10 to save.` as a disabled entry.
* Report shortcuts are now plain `F1` to `F12` only.
   * Modified combinations (`Shift`, `Ctrl`, `Alt`) no longer open report screens.


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
| AutoSave format | Choose whether autosaves prefer legacy `SVE` output with automatic `COS` fallback or always write `COS`. |
| Save cast behavior | Select whether save/load casts use checked conversion or the legacy unchecked mode. |
| (Gbm) Use smart PathFinding for "goto" | Enable smart pathfinding for unit movement. |
| (Gbm) Use smart pathfinding for computer players | Enable smart pathfinding for computer controlled units only. Disable to use legacy AI movement behavior. |
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
| Language | Select active translation language (`Identity` or any valid `civ_<postfix>.txt` file from the profile translation folder). |


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

The SDL loader also includes a macOS native resolver with fallback search paths.
The resolver checks `/Library/Frameworks/SDL2.framework/Versions/Current/SDL2` first.
The resolver then checks Homebrew library paths in `/opt/homebrew/lib` and `/usr/local/lib`.
This reduces manual setup and usually avoids setting `DYLD_LIBRARY_PATH`.
This behavior applies only on macOS.

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

If you want to suppress console output from the game code during tests, run:

```sh
dotnet test xunit/CivOne.UnitTests.csproj -p:SuppressConsoleLogs=true
```

This keeps xUnit status output and hides log messages written by the code under test.

```sh
dotnet test --logger "console;verbosity=detailed"
```

Some tests require the original data files to be present in order to run successfully.
To skip them use

```sh
dotnet test --filter "FullyQualifiedName!~ZOCTests&FullyQualifiedName!~IrrigateTest"
```

The test suite uses two integration trait categories.

* `IntegrationEarthYaml` is for integration tests that rely only on bundled Earth YAML test data.
* `IntegrationLocalData` is for integration tests that require local proprietary game data files.

CI workflows run `IntegrationEarthYaml` tests.
CI workflows skip `IntegrationLocalData` tests.

Run all tests except local-data integration tests.

```sh
dotnet test --filter "Category!=IntegrationLocalData&TestCategory!=IntegrationLocalData"
```

Run only Earth YAML integration tests.

```sh
dotnet test --filter "Category=IntegrationEarthYaml|TestCategory=IntegrationEarthYaml"
```

Run only local-data integration tests.

```sh
dotnet test --filter "Category=IntegrationLocalData|TestCategory=IntegrationLocalData"
```

### Test coverage

This repository supports code coverage with Coverlet and ReportGenerator.
You can generate a text summary for the console and an HTML report for local inspection.

Run coverage from command line.

```sh
dotnet test xunit/CivOne.UnitTests.csproj --collect:"XPlat Code Coverage" --results-directory TestResults --filter "FullyQualifiedName!~CivOne.UnitTests.CityCitizenServiceImplPerformanceTests.Measure"
dotnet tool restore
dotnet tool run reportgenerator -- "-reports:TestResults/**/coverage.cobertura.xml" "-targetdir:CoverageReport" "-reporttypes:Html;TextSummary"
```

Read the text summary in console friendly format.

```sh
cat CoverageReport/Summary.txt
```

On Windows Command Prompt you can use.

```cmd
type CoverageReport\Summary.txt
```

Open the HTML report at [CoverageReport/index.html](CoverageReport/index.html).

If you use Visual Studio Code, run the task `test-coverage`.
This task runs tests with coverage, generates HTML, and prints the text summary in the terminal.

### Coverage in GitHub Actions

A workflow is available at [.github/workflows/coverage.yml](.github/workflows/coverage.yml).
It runs on push and pull request, prints the coverage summary in the job logs, writes the summary into the GitHub job summary, and uploads the HTML report as an artifact.

### Cleaning up

To clean build artifacts and coverage files:

Run `dotnet clean` to remove `bin/` and `obj/` directories.

```sh
dotnet clean
```

Run the `clean-all` VS Code task to remove build artifacts and coverage data (`TestResults/` and `CoverageReport/`).

Alternatively, run individual cleanup tasks in VS Code:

* `clean` – Runs `dotnet clean`
* `clean-coverage-folders` – Removes `TestResults/` and `CoverageReport/` directories

## FAQ

### The screen content is cut off after resizing the window

This happens when **Aspect Ratio** is set to `Expand` and a fixed window size was saved.
In `Expand` mode the game renders exactly as many tiles as fit the current window.
When the window is later made smaller, the rendered area stays the same size but no longer fits inside the window, so parts of the screen (e.g. the top or sides) are cropped.

To fix this, resize the window to match the size the game was configured for, or change the **Aspect Ratio** setting to `Fixed`, `Scaled`, or `ScaledFixed`.
Those modes always scale or letterbox the fixed 320x200 game surface to fit any window size.

If you want to use `Expand` mode, make sure to use `Auto` for the Expand size in the settings, which allows the game to automatically adjust the rendered area to fit the window size.


## Changes (Log)

See [CHANGES.md](CHANGES.md) for a detailed list of changes and updates.

## MCP Integration

CivOne includes a built-in MCP server for local automation and screenshot capture.

Start the game with `--mcp` to enable it.
Use `--mcp-artifacts <path>` to choose where screenshots are written.

```cmd
CivOne.SDL.exe --mcp
```

The server communicates over stdio and prints a session token to `stderr` on startup.
That token must be included in every request.

> There is a mcp.json file for Visual Studio Code MCP client integration. You can use `Ctrl+Shift+P` → "MCP: List Servers" to start/connect to `civone`'s MCP server and send requests directly from VS Code.

For activation, request examples, available tools, response format, and Visual Studio Code integration, see [MCP.md](MCP.md).
For internal architecture and implementation notes, see [docs/MCP.md](docs/MCP.md).
