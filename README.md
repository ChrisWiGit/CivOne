<!-- Please use a single sentence each line. -->
# CivOne# (Civ One Sharp)

A civilization game clone written in C#.

TODO: - Add more details about the game, and its original version.

## Changes

See [CHANGES.md](CHANGES.md) for a detailed list of changes and updates.

## Programm parameters

### Loading a saved game immediately

To load a saved game immediately when starting the program, you can use the `--load-slot` option followed by a drive letter and a slot number.
The drive letter should be between 'a' and 'z', and the slot number should be between 0 and 15 (inclusive).
These correspond to the saved game files that are stored in the `SaveGames` directory (`~/CivOne/saves/c`)

If you want to load the saved game from drive 'c' and slot 0, you would use:

```sh
civone --load-slot c0
```

If you omit the slot number, a loading screen will be shown, allowing you to select a saved game interactively.
