# TODO

## Generic TODOs

* Diplomacy
  * King Screens
  * Data model for diplomacy state
* Make sure original save format is fully compatible with original Civ1 DOS (e.g. bitfield packing, array shapes, etc.)
* Computer player AI 
   * Data model for AI state
* Replay recording
   * Replay menu at the end
   * Replay screen


## Specific TODOs from Code

### Plugin capability providers (api/src/Plugin/)

Three plugin capability interfaces exist. The host discovers and instantiates all three
(`PluginService.AiProviders`, `.MapGeneratorProviders`, `.ImageProviders`), but only the AI provider
is actually consumed.

1. `IPluginMapGeneratorProvider` is loaded but never called.
   - World generation is hard-wired: `Map.Generate(LandMass, Temperature, Climate, EarthAge)` in
     `src/Map.Generate.cs` always runs the built-in pipeline via `LandElevationGeneratorDelegate`.
   - `src/Screens/CustomizeWorld.cs` offers size, land mass, temperature, climate and age, but no
     choice of generator.
   - To activate: add a generator selection menu in `CustomizeWorld`, persist the chosen
     `MapGeneratorDescriptor.Id`, and branch in `Map.Generate` to call
     `IMapGenerator.Generate(MapGenerationParameters)` instead of the built-in pipeline.
     `MapGenerationParameters` already uses the same enums the setup screen collects.
   - `MapGeneratorDescriptor.SupportedSizes` / `SupportsCustomSize` are meant to restrict the map
     size menu, which currently uses the hardcoded presets in `CustomizeWorld.GetMapSizePreset`.

2. `IPluginImageProvider` is loaded but never called.
   - Sprite lookup happens in `src/Graphics/Resources.cs` through the `Picture this[string filename]`
     indexer and its internal caches. There is no override step.
   - To activate: let `Resources` consult an `ImageStore` (from `IImagePackFactory.Create()`) before
     falling back to the game data files, and invalidate the bitmap caches when the selected image
     pack changes. `ImageStore.TryGetOverride` and `ImageAssetReference` (resource name plus crop
     rectangle) already describe what the lookup needs.
   - Also needs a place for the user to pick an image pack, and a way to load the referenced
     resources out of the plugin assembly.

### SaveDataAdapter.cs

1. Bitfield compatibility with original CivDOS still needs verification.
   - The adapter currently packs and unpacks three SVE-backed bitfields manually:
   - `ActiveCivilizations`
   - `CivilizationIdentity`
   - `GameOptions`
   - All three still carry the same open comment: `TODO fire-eggs: is bit order compatible with CivDOS?`

### Game.LoadSave.cs

1. Unit restore still assumes an 8-player shaped SVE array.
   - The loop in `src/Game.LoadSave.cs` still has the open note `TODO fire-eggs: wrong when playing with fewer than 7?`.
   - This likely needs a dedicated check whether the binary unit arrays and active-civilization handling behave correctly for smaller player counts.

### Extensions.cs

1. Settler-specific `MovesSkip` state still appears unsaved in the SVE conversion path.
   - `src/Extensions.cs` still contains `TODO need to save (Settlers.)MovesSkip value to savefile` in the `UnitData` export path.
   - That suggests a remaining gap between in-memory unit state and the legacy binary save representation.

2. Unit fortify status mapping may still be ambiguous in SVE export.
    - `src/Extensions.cs` still contains `TODO not the same as _fortify?` in `GetUnitStatus`.
    - This suggests `Fortify`/`FortifyActive` semantics might not map 1:1 to legacy status bits.
