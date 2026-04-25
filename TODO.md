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
* Space victory
   * Data model for spaceship construction state
   * Victory screen

## Specific TODOs from Code

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
