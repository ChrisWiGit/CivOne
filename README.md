<!-- Please use a single sentence each line. -->
# CivOne# (Civ One Sharp)

## Introduction

A civilization game clone written in C#.
It was originally developed several years ago by other authors, and then abandoned.
This project is a continuation of that work, with the goal of completing the game and making it fully playable, but with some tweaks and improvements that
make it more enjoyable for modern players.

## First Steps

### Requirements

* This program requires the .NET 9 Runtime to be installed on your system.

### Running the Program

To run the program, navigate to the directory where the `CivOne.dll` file is located and use the following command:

```sh
dotnet CivOne.SDL.dll
```

### Install graphics from original game

When starting the game for the first time, you will need to install graphics from the original Civilization game.
The game will prompt you to select the directory where the original game's data files are located.
Select the directory containing the original Civilization game files to proceed.

If you do not have the original game files, you can use a free package of graphics files instead.

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

## Building from source

### Prerequisites

* .NET 9 SDK

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
