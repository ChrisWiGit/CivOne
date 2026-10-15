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

## Open TODOs for LLM Agents

This chapter lists open work found while porting the original game's behavior.
Each item names the files and plans to read first.
Agents: search this chapter for open work before starting a new feature; keep items short and update or remove
them when they are done.

### Spaceship

1. Spaceship parts are not stored in SVE files.
   - `SaveData.SpaceShipParts[1440]` (8 x 180 bytes) is neither read nor written; CivOneX keeps its own 12x12 grid.
   - A ship in flight survives an SVE round trip (launch year, arrival year, population, flags), its parts do not.
   - Needs the mapping between the original part slots and `SpaceShipComponentType` in the grid.

### SVE (on hold, waiting for a decision)

1. Verify the new SVE spaceship fields against a real original savegame.
   - Offsets are derived from the reference code, not checked: `SpaceshipFlags` @37234 (bit `0x100` = "scoring
     completed"), success rates @37236/@37238, `SpaceshipEtaYear[8]` @37240-37255.
   - Check with an original save that has a ship in flight and one saved after "keep playing".
   - Code: `src/IO/SaveData.cs`, `src/Services/SpaceShip/SveSpaceRaceDelegate.cs`, `src/Game.SveSpaceRace.cs`.
   - Plans: `docs/plan-calendar.impl.md` (K1, Phase 5), `docs/plan-endOfGameContinue.impl.md` (Phase 4).
2. Verify the rebuild of the calendar history from an SVE file (`SveCalendarDelegate`): only the current year is
   exact; check that past years in the power graph and replay look plausible.

### Keep Playing and End of Game

1. Replay screen is missing.
   - The original shows the replay in the end-game flow (not after *Retire*), the conqueror screen after a
     conquest, and unlocks the menu entry "View Replay" once scoring is completed.
   - Plan: `docs/plan-replayHistory.prompt.md`; hooks in `src/Services/EndGame/EndGameService.cs`.
2. "No, I'm done" leads to the credits; the original shows an advertisement picture and exits to DOS.
   - Deliberate deviation; keep unless decided otherwise (`docs/plan-endOfGameContinue.impl.md`, §5).
3. After a world conquest the original ends the conqueror's unit moves at once and skips the unit phase of the
   remaining players of that turn; CivOneX lets them continue until the end-of-turn check.
   - Code: `src/Game.cs` (`PlayerDestroyed`), `src/Game.EndOfGame.cs`.

### Respawn and Destruction of Civilizations

1. The respawn rule differs from the original.
   - CivOneX: a destroyed civilization respawns while the year is BC and `AllowedToRespawn` holds
     (`src/Game.cs`, `PlayerDestroyed`).
   - Original: respawn while `TurnCount <= 350 - 50 x difficulty` and a start position is found (at most 2000
     tries, both nationality variants of the slot); otherwise the slot is disabled.
   - This also decides when world conquest is reached.
2. The destruction criterion differs from the original.
   - CivOneX counts a civilization as alive while it has cities or settlers without a home city
     (`Player.HandleExtinction`, `src/Player.cs`).
   - Original: only the number of cities counts.
