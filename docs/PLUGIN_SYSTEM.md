# CivOne Plugin System

## Overview

The plugin system loads external DLL files from the CivOne plugins directory and applies a small set of runtime modifications to the game.

It is based on reflection. CivOne scans assemblies, finds plugin entry points, and then reads plugin-defined modification classes from the loaded assemblies.

## How It Works

### 1. Plugin discovery

At startup, CivOne looks in `Settings.PluginsDirectory` for `*.dll` files.

Each DLL is validated and then loaded into memory. Invalid assemblies are skipped.

### 2. Plugin entry point

A plugin assembly must contain exactly one class named `Plugin` in the `CivOne` namespace.

That class must implement `IPlugin` and provide:

- `Name`
- `Author`
- `Version`

The entry-point class is created with a parameterless constructor.

### 3. Enabled and disabled state

Plugins are enabled unless their filename is listed in `Settings.DisabledPlugins`.

The enabled state is stored by filename, not by package id or assembly identity.

### 4. Runtime application

When plugins are loaded or changed, CivOne reapplies plugin modifications through `Reflect.ApplyPlugins()`.

The current game systems that are refreshed are:

- civilizations
- leaders
- units

### 5. Menu changes

The UI can also read `MenuModification` classes.

These are used to change menu item text and shortcuts for matching menu ids.

## Sounds

Sounds are addressed by name, never by number. The names the game itself uses are the constants in
`SoundNames`; they describe the situation a sound belongs to, such as `combat_win_strong`.

A plugin may pass any string to `PlaySound`. A name nothing knows is not an error, it is simply
silent. With **Wave files** selected as the sound source, a plugin that ships `my_sound.wav` into the
profile's `sounds/` folder can play it with `PlaySound("my_sound")` without registering anything.

Only the selected source plays. A plugin sound is not reachable while a converted sound pack is
selected, because that pack does not carry it and nothing falls back to the wave files; a redirect
is the way around that.

Replacing a sound the plugin does not own works by redirecting its name rather than by overwriting a
file. `ISoundAliasRegistry` holds those redirects and is applied in front of whatever plays the
sound, so a redirect may also send a tune of a converted sound pack to a plain wave file. The
registry is not reachable from a plugin assembly yet - exposing it is the remaining piece of this
feature.

## What Plugins Can Modify Today

The public API currently supports these modification base classes:

- `CivilizationModification`
- `LeaderModification`
- `UnitModification`
- `MenuModification`

The first three affect core game data.
`MenuModification` only changes menu labels and shortcuts.

## User Workflow

In the Setup screen, the player can:

- browse for a folder that contains plugin DLLs
- copy valid plugins into the CivOne plugins directory
- enable or disable installed plugins
- delete plugins from disk

If a plugin file already exists, CivOne shows an overwrite dialog.

## Current Limitations

This is the part that is still limited or incomplete.

- There is no general plugin API for adding brand new screens, systems, or engine hooks.
- Plugins are not discovered through packages or manifests. Only raw DLL files are supported.
- The entry point is strict: only one `CivOne.Plugin` class implementing `IPlugin` is accepted.
- The plugin metadata is minimal. Only name, author, and version are exposed by `IPlugin`.
- Menu plugins can only change text and shortcuts. They cannot add or remove menu items.
- Core runtime modifications are limited to civilizations, leaders, and units.
- Plugin state is tracked by filename, so renaming a DLL changes how CivOne sees it.
- Overwrite handling is interactive, but there is no advanced dependency or version conflict management.

## Code Paths

- `src/Plugin.cs` handles validation, loading, and plugin state.
- `src/Reflect.cs` loads plugins and reapplies modifications.
- `src/IO/FileSystem.cs` copies plugin DLLs into place.
- `src/Screens/Setup.cs` exposes plugin management in the Setup screen.
- `src/UserInterface/MenuItemCollection.cs` applies menu modifications.
- `api/src/IPlugin.cs` defines the public plugin metadata interface.

## Summary

The plugin system is a reflection-based DLL loader with a narrow modding surface.

It is useful for data and UI text changes, but it is not yet a full extension platform.