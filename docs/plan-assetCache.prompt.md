# Plan: Asset Cache (resource-derived bitmaps with an owner)

## Scope

Give every bitmap and palette that is derived from game resources a single owner that knows when the
data is stale. The trigger for this plan is the graphics mode switch (16 ↔ 256 colours) during a running
game: after the switch, many screens keep drawing graphics from the previous mode, and some of them crash
because a palette has the wrong length.

**Not** in scope: changing how resource files (`.PIC`, `.GIF`) are decoded, the plugin modification
system, or the screen/layer rendering pipeline.

**Interim measure (done):** a graphics mode change only takes effect after a restart.
`Settings.GraphicsMode` returns the mode the session started with; the setup menu writes
`Settings.ConfiguredGraphicsMode`, which is saved for the next start.
Locking the menu only while a game runs would not be enough: `Reflect.PreloadCivilopedia()` creates every
unit, building and wonder at program start, so their static icon caches are filled before the setup
screen can be opened. This plan removes the reason for the restart.

---

## Problem

A cache is a stored copy of data, so the data does not have to be loaded again.
Resource-derived graphics are cached in many places, and no single place decides how long a cached
copy stays valid.

### Symptoms

- After a graphics mode switch, the map, units, icons and leader portraits still show the old mode.
- Code that assumes a 256-entry palette crashes when it meets a 16-entry palette, or the other way round
  (`Conquest`, `SpaceShipView`, `Nuke` needed individual guards).
- Memory leaks, because it is unclear whether a returned bitmap must be disposed (`Nuke` copied `NUKE1`
  29 times per strike and never disposed the copies).

### Root causes

1. **No owner for cache lifetime.**
   Every class builds its own cache. `Resources.ClearInstance()` only clears the caches it knows about,
   and that list falls behind with each new cache.
2. **Domain objects hold graphics.**
   `BaseUnit.Icon`, `BaseBuilding.Icon`/`SmallIcon` and `BaseWonder.SmallIcon` are assigned in
   constructors. A unit is a game object; it lives longer than the graphics mode it was created in.
3. **The graphics mode is hidden global state.**
   `GFX256` reads `Settings.Instance` statically in 12 files. The 16/256 decision is made once, on first
   access, and then cached along with the bitmap. The mode is not part of any cache key.
4. **No change notification.**
   Settings set `Common.ReloadSettings = true`, but nothing reads that flag.
   `Resources.CacheGeneration` (added as a stop-gap) makes every consumer poll for changes by itself.
5. **Unclear ownership of bitmaps.**
   `Resources[...]` returns an owned copy the caller must dispose. Cached sprites belong to the cache.
   `Free.*` returns shared bitmaps. The types (`IBitmap`, `Picture`, `Bytemap`) do not show which case
   applies.
6. **Palette size assumptions spread across the code.**
   Direct reads such as `palette[128]` or the range 64–143 assume 256 entries. Each call site needs its
   own guard.

---

## Current state

| Cache | Location | Scope | Invalidated on mode switch |
|---|---|---|---|
| Map tile sprites | `Graphics/Sprites/MapTile.cs` (`CachedSprite`, `CachedSpriteCollection`) | static | yes (stop-gap via `ResourceGenerationDelegate`) |
| Unit sprites | `Graphics/Sprites/Unit.cs` | static | yes (same stop-gap) |
| `Generic`, `Pattern` sprites | `Graphics/Sprites/` | static | yes (same stop-gap) |
| Cursor sprites | `Graphics/Sprites/Cursor.cs` | static | yes (`Cursor.ClearCache()` in `Resources.ClearInstance`) |
| Tile palette | `Tiles/TileExtensions.cs` | static | yes (`CacheGeneration`) |
| Palace resources | `Graphics/PalaceResourcesDelegate.cs` | instance on `Resources` | yes (`ClearInstance`) |
| `PicFile` cache | `Graphics/ImageFormats/PicFile.cs` | static | yes (`ClearInstance`) |
| City and UI icons | `Graphics/Icons.cs` (~20 fields, citizens, lamps, suns, government portraits) | static | **no** |
| Terrain icons | `Tiles/BaseTile._icons` | static | **no** |
| Unit icons | `Units/BaseUnit._iconCache` + `Icon` set in constructor | static + per instance | **no** |
| Building icons | `Buildings/BaseBuilding._iconsCache`, `_iconsCacheGrass` + `Icon`/`SmallIcon` | static + per instance | **no** |
| Power plant icon | `Buildings/PowerPlant._iconCache` | static | **no** |
| Wonder small icons | `Wonders/BaseWonder.SmallIcon` set in constructor | per instance | **no** |
| Leader portraits | `Leaders/BaseLeader._picture`, `_portraitSmall` | per instance (`Common.Civilizations`) | **no** |
| Screen palettes | `GamePlay`, `GameMap`, `SideBar`, `MenuBar` | per screen | yes (stop-gap `GamePlay.ReloadGraphics`) |

Numbers for sizing: 168 `Resources[...]` call sites, 96 `SetIcon`/`SetSmallIcon` calls, 12 files that use
`GFX256`, 21 consumers of `.Icon`/`.SmallIcon` outside the domain classes.

---

## Target architecture

### 1. `IAssetCache`: one owner for derived graphics

An injected service that returns resource-derived bitmaps by key and owns them.

```csharp
internal interface IAssetCache
{
    /// Returns a cached bitmap. The cache owns it; callers must not dispose it.
    IBitmap Get(AssetKey key);

    /// Returns a palette with at least 256 entries for the current graphics mode.
    Palette GetPalette(string resource);
}
```

- `AssetKey` is a small `record struct` that identifies the asset, for example
  `AssetKey.UnitIcon(UnitType.Settlers)`, `AssetKey.TerrainIcon(Terrain.Hills)`,
  `AssetKey.CitizenIcon(Citizen.UnhappyMale)`.
- The graphics mode is **not** part of the public key. The cache combines it with the key internally, or
  clears itself on a mode change (see 2). Callers never read `GFX256` again.
- The factories that build each bitmap (the code now inside `Icons`, `BaseTile.Icon`,
  `BaseUnit.SetIcon`, …) move into small delegate classes grouped by area: `UnitIconDelegate`,
  `TerrainIconDelegate`, `CityIconDelegate`, `LeaderPortraitDelegate`.
  The cache calls them on a cache miss (when the key is not cached yet).
- Created through `AssetCacheFactory`; consumers get `IAssetCache` through constructor injection,
  following the lazy-resolution rule in `CLAUDE.md`.

### 2. `IGraphicsModeNotifier`: change notification instead of polling

- `Settings.GraphicsMode` raises an event after the value changed.
- `Resources.ClearInstance()` becomes a subscriber, not the entry point.
- `IAssetCache` subscribes and clears itself.
- Screens that must redraw (`GamePlay` and its panels) subscribe instead of polling
  `ResourceGenerationDelegate`.
- The unused `Common.ReloadSettings` flag is either removed or wired to the same event.

### 3. Domain objects hold keys, not bitmaps

- `IProduction.Icon` / `SmallIcon` and `IUnit.Icon` become thin getters that resolve through the cache:
  `public IBitmap Icon => AssetCache.Get(AssetKey.UnitIcon(Type));`
- Medium term: renderers ask the cache themselves, and the `Icon` properties are removed from the
  domain interfaces. This is a follow-up and not required to fix the mode switch.

### 4. Clear ownership rules

| Source | Ownership | Rule |
|---|---|---|
| `IAssetCache.Get` | borrowed | never dispose |
| `Resources[...]` | owned copy | always `using` |
| `Free.*` | shared singleton | never dispose; only used inside the asset delegates |

- Document the rules in `CLAUDE.md` (it already covers sprite caches).
- Optionally, make `Resources[...]` return a borrowed picture as well, and keep an explicit
  `Resources.Copy(name)` for the rare case that needs a mutable copy. Decide in phase 5.

### 5. Palette normalisation in one place

- `IAssetCache.GetPalette` always returns at least 256 entries (`PaletteExpansionDelegate`).
- Screens take their palettes from there, so the guards in `Conquest`, `SpaceShipView`, `Nuke` and
  `RuntimeHandler` can be removed.

---

## Phases

Each phase builds, passes all tests, and is shippable alone.

### Phase 0: Restart required (done)
- `Settings.GraphicsMode` is fixed for the session; `Settings.ConfiguredGraphicsMode` is saved for the
  next start. Setup menu and README say that a restart is required.
- The existing stop-gaps (`ResourceGenerationDelegate`, `GamePlay.ReloadGraphics`) stay as a safety net;
  they no longer fire for a mode change, because `Resources.ClearInstance()` is not called for it any more.

### Phase 1: Notification
- Add `IGraphicsModeNotifier` (event `GraphicsModeChanged`), implemented by `Settings`.
- Make `Resources.ClearInstance` and `GamePlay` subscribe.
- Tests: the event fires once per actual change and not when the same value is set again.

### Phase 2: `IAssetCache` skeleton
- Add `IAssetCache`, `AssetCache`, `AssetKey`, `AssetCacheFactory`.
- The cache clears itself on `GraphicsModeChanged`. Old bitmaps are dropped, not disposed, while a frame
  may still reference them (same reasoning as the stop-gap in `CachedSprite`); disposal can be deferred
  to the next frame.
- Tests with a fake resource provider: hit, miss, clearing on mode change, no disposal of borrowed
  entries.

### Phase 3: Move static caches
One area per commit, in this order (least risky first):
1. `Icons` → `CityIconDelegate`
2. `BaseTile._icons` → `TerrainIconDelegate`
3. `BaseUnit._iconCache` → `UnitIconDelegate`
4. `BaseBuilding._iconsCache`, `_iconsCacheGrass`, `PowerPlant._iconCache` → `BuildingIconDelegate`
5. `BaseWonder.SmallIcon` → `WonderIconDelegate`
6. `Graphics/Sprites/*` (`MapTile`, `Unit`, `Generic`, `Pattern`) → behind `IAssetCache`; then
   `ResourceGenerationDelegate` can be removed from `CachedSprite`/`CachedSpriteCollection`.

`Icons` keeps its static facade during the move and forwards to the cache, so the 21 callers do not
all change at once.

### Phase 4: Domain objects stop holding bitmaps
- Replace the constructor assignments of `Icon`/`SmallIcon` with getters that go through the cache.
- `BaseLeader`: portrait resolved through `LeaderPortraitDelegate`; `_picture` removed.
- Remove `GamePlay.ReloadGraphics` polling in favour of the event from phase 1.

### Phase 5: Palettes and ownership
- `IAssetCache.GetPalette` with normalisation to 256 entries.
- Remove the individual length guards.
- Decide on borrowed vs. owned `Resources[...]` (see target architecture §4) and fix the remaining
  undisposed copies found by a search for `Resources[` without `using`.

### Phase 6: Remove the restart requirement
- Merge `ConfiguredGraphicsMode` back into `GraphicsMode`, raise `GraphicsModeChanged` on a change, and
  remove the restart hints from the setup menu and the README.
- Manual test matrix: switch in both directions on the map, in a city view, in the civilopedia, on the
  spaceship screen, in a conquest screen, with `--free`, and with missing `DOCKER.PIC`.

---

## Licensing

Almost every class touched in phases 3 and 4 is listed in `.cc0-baseline.csv`.
The new types (`IAssetCache`, `AssetCache`, `AssetKey`, the `*IconDelegate` classes, the notifier) go into
**new files** and are therefore MIT. The CC0 files shrink to calls into those types, as `CLAUDE.md`
prefers.

---

## Risks

| Risk | Mitigation |
|---|---|
| A bitmap that is still referenced by the current frame gets disposed | Drop instead of dispose on clear; dispose deferred to the next frame |
| Static initializer touches the runtime (see `CLAUDE.md`, "Static Initializers Must Not Need a Runtime") | Cache and delegates are resolved lazily; no eager `static` fields |
| Plugin modifications (`UnitModification`, `LeaderModification`) bypass the cache | Treat a modification as one more input of the asset delegate; cover with a test |
| Background Civilopedia preload reads caches concurrently | The cache must be thread-safe (lock or `ConcurrentDictionary`) |
| Large diff across CC0 files | One area per commit (phase 3 order); `Icons` facade keeps callers stable |

---

## Done criteria

- Switching the graphics mode during a game updates every visible graphic without restarting.
- No class outside `AssetCache` and its delegates reads `GFX256` or `Settings.GraphicsMode` for
  graphics.
- No `static` bitmap or palette fields remain outside the cache.
- No palette length guards remain in screens.
- Unit tests cover the cache, the delegates and the notifier without a running game engine.
