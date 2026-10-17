# Plan: Civilization Score — Original Mode

Implementation plan for this repository.

The reverse-engineering notes in [plan-civilizationScore.prompt.md](plan-civilizationScore.prompt.md) and the
follow-up analyses are used here **only as a source of facts about the original DOS game**. Their file and line
references point into a separate decompilation repository and do not apply to CivOne. Every CivOne-side statement
below was verified against the current working tree.

**Decompilation repository:** `/home/christian/projekte/CivOne-rajko` (checked 2026-10-03). The score routine
there is `F20_0000_0ca9(playerID, flag)` in `Overlay_20.cs:1164-1700`; the per-city simulation it calls is
`F0_1d12_0045_ProcessCityState(cityID, -1)` (`Overlay_20.cs:1322`), implemented in `CityWorker.cs`. Line
references of that shape below are **rajko**, not CivOne.

**Second reference: OpenCivOne** (`/home/christian/projekte/OpenCivOne_codeberg`, checked 2026-10-10). Readable C#
translation of the same binary. References marked **OCO** point to `src/Game/CodeObjects/` there; the score routine
is `F20_0000_0ca9_ShowCivilizationScorePopup` in `Overlay_20.cs:1142-1650`. The check confirmed the formula in §1
but corrected the rating/leader logic, the palace trigger, the spaceship gate, the accumulation claim (§2), the
opponent count and the peace counter. All corrections are worked into the sections below and listed in §15.

## Startplan (Kurz)

Ziel: Original-Score-Logik sauber in CivOne abbilden und alle Verbraucher konsistent machen.

1. Phase 1 abschließen: Happiness-Modell aus dem separaten Plan aktiv schalten. -> Fertig!
2. Phase 2 umsetzen: Score-Service auf Originalformel umstellen (inkl. 40%-Luxury-Referenz = `ScoringLuxuryRate` 0 -> 4, Peace-Year-Gate, Spaceship-Term, Conquest-Regel) **und Rating/Famous Persons auf `score*(D+1)/50`**.
3. Phase 3 umsetzen: Palace-Trigger korrigieren (n = UpgradeCount + 1, Difficulty-Faktor, **Palace-Option, einmal pro Runde**, kein Spaceship-Gate).
4. Phase 4 umsetzen: Score als abgeleiteten Live-Wert nutzen, altes gespeichertes Score-Feld entfernen, SVE/YAML-Verhalten finalisieren.
5. Phase 5 (Q7 = A): Historian-Palastbild aus der Reichssumme wie im Original.
6. Phase 6: F9-Screen auf Original-Layout, über den Service.
7. Phase 7: Endspiel-Ablauf wie im Original.
8. Tests und manuelle Checks abschließen: Service-, Trigger-, Delegate-, Mapper-, SaveDataAdapter-Tests plus UI-Abgleich (F9/TopLeader/HallOfFame).

## TODO Fortschritt

- [X] Phase 1: Happiness-Korrektur aktiv (Voraussetzung für Score-Start)
- [X] Phase 2.0: `ScoringLuxuryRate` von `0` auf `4` (40 %-Referenz, §3 — in der Decompilation bestätigt)
- [X] Phase 2.1: CivilizationScoreService-Formel auf Original umgestellt -> `Calculate(IScoredCivilization, IScoreGameState)` liefert `CivilizationScoreBreakdown`; Live-Inputs über `IScoreInputSource`/`GameScoreInputSource` (`src/Services/Score/`). F9 zeigt die Breakdown-Werte (Layout bleibt Phase 6), neue Übersetzungen in `civ_german.txt`
- [X] Phase 2.2: Spaceship-Erfolgsrate als gemeinsam nutzbare Service-Funktion extrahiert -> `SpaceShipSuccessRateDelegate` + `ISpaceShipService.SuccessRate()` (gestartetes Schiff zählt als vollständig, kein 99-Deckel)
- [X] Phase 2.3: WorldConquestPredicate (latched + derived) eingebaut -> `src/Services/Score/`, Auswahl über `ISettings.WorldConquestMode` in `CivilizationScoreServiceFactory.CreateWorldConquestPredicate`; Eingabe `IConquestParticipant` (Adapter auf `Player` folgt mit 2.1; Achtung: `Player.IsDestroyed` ruft `HandleExtinction` mit Nebenwirkung). Setup-Menüeintrag unter Game behavior menu (2026-10-11). **2026-10-14 vereinfacht:** nur noch latched (`WorldConquestDelegate`), Schalter/Menü/`WorldConquestPredicate` entfernt.
- [X] Phase 2.4: Peace-Bonus nur bei Year > 0 (Score-Seite)
- [X] Phase 2.5: Wonder-Capture/Destroy-Verhalten einmal verifiziert -> Wunder bleiben am `City`-Objekt (Eroberung ändert nur `CityOwnerPlayerIndex`), `Game.DestroyCity` setzt Besitzer 0, `Player.Cities` filtert danach. Keine Änderung
- [X] Phase 2.6: Peace-Turns lesbar gemacht (Accessor auf `IGame`; heute nur privates `_peaceTurns`), siehe §14.3
- [X] Phase 2.7: Peace-Reset überspringt Barbaren-Beteiligung (Slot 0), siehe §14.4
- [X] Phase 2.8: Peace-Inkrement nur bei Year > 0; Nuke und Einnahme leerer Stadt setzen zurück (bewusste Abweichung, in XML-Doc von `IPeaceTurnService`), siehe §14.4 -> 2.6–2.8/2.10 umgesetzt in `src/Services/Peace/PeaceTurnService.cs` (`GameRulesServiceFactory`), Zugriff `IGame.PeaceTurns` (`IGamePeaceQuery`); Verteidiger = erste Einheit auf dem Feld, sonst Stadtbesitzer; Abweichungen in der XML-Doc von `PeaceTurnService`
- [X] Phase 2.9: Spaceship-Term: Gate = „Ankunft in diesem Zug erkannt“ (wie `CheckSpaceVitory`), Null-Vorbedingungen, 0-Klammerung, siehe §14.8/§15.3 -> `ISpaceShipArrivalService` (`ArrivedThisTurn` = Ankunftsjahr fällt in die Spanne dieses Zugs; `CheckSpaceVitory` nutzt `HasArrived`). Bevölkerung in CivOneX in Personen → `/ 10_000`
- [X] Phase 2.10: Peace-Reset + Inkrement im selben Rundenwechsel (kein Off-by-one), Reset nur bei echtem Kampf, siehe §15.7
- [X] Phase 2.11: Unhappy-Term = `unhappy + redShirt`, Size inkl. Spezialisten, siehe §15.6
- [X] Phase 2.12: Conquest-Bonus mit `Competition − 1` und Ersatz bei `bonus >= score`, siehe §15.5
- [X] Phase 2.13: `RatingPercent = score*(D+1)/50` + Famous-Persons-Auswahl `(i*i)/3 < rating` (20 Namen, `score==0`-Pfad), siehe §15.1 -> `ICivilizationRatingService`/`CivilizationRatingService` (`src/Services/Score/`), `LeaderOrderDelegate` mit Originalliste; TopLeader schließt sich ohne Person sofort, HallOfFame nutzt den Service. HoF sortiert nach Rating, bei Gleichstand bleibt der ältere Eintrag vorn → neuer Eintrag nur bei strikt höherem Rating (`HallOfFamePersistService.Normalize`, 2026-10-14)
- [X] Phase 3.1: Palace-Threshold korrigiert (n + Difficulty)
- [X] Phase 3.2: End-of-game/Spaceship-Block aus Trigger entfernt -> entfällt, war schon weg (§14.5)
- [X] Phase 3.3: `Difficulty` auf `IPlayerGameState` + `PlayerGameStateAdapter`, siehe §14.6
- [X] Phase 3.4: Trigger prüft Spieloption `Game.Palace` (Flag `0x80`), siehe §5/§15.2
- [X] Phase 3.5: Trigger nur einmal pro Runde (Rundenwechsel) und nur mit Palast-Stadt, siehe §5/§15.2 -> Schwelle in `PalaceThresholdDelegate`; `IPlayerGameState` + `Difficulty`, `PalaceOptionEnabled`, `HasPalaceCity`; `Game.EndTurn` prüft nur beim Rundenwechsel und nur den Menschen. Game-Level-Test „max. einmal pro Runde" folgt mit Phase 4 (Live-Score)
- [X] Phase 4.1: CivilizationScore als Live-Getter umgesetzt
- [X] Phase 4.2: _civilizationScore + Restorable-Member + FromDto-Write entfernt
- [X] Phase 4.3: SVE CivilizationScore[8] Lesen/Schreiben im Adapter ergänzt
- [X] Phase 4.4: SVE-Collapse-Regel umgesetzt (Human-Score in Slot 0, Rest 0) -> `SveCivilizationScoreDelegate`; Live-Getter in `Player` (0 außerhalb eines laufenden Spiels, wie `IPlayer.Cities`); kein Lesen beim Laden
- [X] Phase 4.5: Persistenz des Latched-Conquest-Flags geklärt -> Laufzeit-only, kein DTO-Feld (§14.7)
- [X] Phase 5: `IHistorianPalaceService` + `IHistorianPalaceLevelService` — Mensch echter Palast, KI generiert aus Reichssumme (Q7 = A), SVE-Slots 1..7 befüllen -> `src/Services/Historian/`; Generator `MiniPalaceGeneratorDelegate` mit DOS-16-Bit-Semantik (Low-Byte `% 9`, Zwangsschreiben nach 999 Versuchen; civ-porter-Analyse, rajko `Segment_1238.cs:1758-2000`), gegen Referenzsimulation getestet. Seed = `Map.TerrainMasterWord`. **Korrigiert 2026-10-16** ([plan-palace.impl.md](plan-palace.impl.md), 5b.1): Der Generator liefert jetzt alle 9 Slots auf der Original-Skala (kein „+1“, Level 0 unsichtbar), der Mini-Palast hat 9 Slots
- [X] Architektur: neue Services nach §16 (Interface, Konstruktor-Injection mit Factory-Fallback, keine statische Logik) -> Hinweis: `Player` bekommt den Score-Service nur als lazy Feld (kein Konstruktorparameter, zu viele Erzeugungsstellen); `Game` hält Peace-Service, Conquest-Prädikat und Arrival-Service lazy
- [X] Phase 5b: eigener Plan [plan-palace.impl.md](plan-palace.impl.md). Befund: Der echte Palast hat auch im Original 7 Sektionen, 9 Slots gibt es nur im Mini-Palast. Mini-Palast, Generator, SVE-Palast, Bauzähler und originalgetreuer Palast-Bildschirm (5b.4) umgesetzt 2026-10-16
- [X] Phase 5c: siehe [plan-palace.impl.md](plan-palace.impl.md). Das Original speichert keine KI-Paläste; kein Snapshot. Setting `HistorianAiPalace` (Generated / BuiltOrGenerated / BuiltOnly) umgesetzt 2026-10-16
- [X] Kalender: eigener Plan [plan-calendar.impl.md](plan-calendar.impl.md) (Q8) -> umgesetzt 2026-10-15
- [X] Weiterspielen nach Raumschiff-Ankunft + Spielende per Jahr: eigener Plan [plan-endOfGameContinue.impl.md](plan-endOfGameContinue.impl.md) -> umgesetzt 2026-10-15
- [X] Phase 6: F9-Screen (`CivilizationScore.cs`) auf Original-Layout, ohne eigene Formel, siehe §15.8 -> Positionen in `CivilizationScoreLayoutDelegate` (+ Tests), Screen zeichnet nur `CivilizationScoreBreakdown`; Score-Balken bei y 192. Annahme: Start-Zeilenhöhe 8 (Originalwert liegt im Datensegment, unbekannt). Icons in CivOneX-Reihenfolge (`Citizens`), Rothemden eingeschlossen
- [X] Phase 7: Endspiel-Ablauf wie im Original (`EndGameService`), siehe §8.15 -> Reihenfolge in `EndGameService` (Bildschirme über `IEndGameScreenFactory`, Tests `EndGameServiceTests`); neu `HandleRivalSpaceArrivalAsync`. „Weiterspielen?“ nach Raumschiff-Ankunft ist seit 2026-10-15 umgesetzt (`plan-endOfGameContinue.impl.md`). Offen nur noch: Replay-Bildschirm (fehlt in CivOneX) → [plan-replayHistory.prompt.md](plan-replayHistory.prompt.md), als Abweichung in der XML-Doc von `EndGameService`
- [X] Doku: toter Verweis auf `plan-cityCitizenService.impl.md` aufgelöst, siehe §14.1
- [X] Tests: CivilizationScoreServiceTests (+ `SpaceShipArrivalServiceTests`)
- [X] Tests: HumanCivScorePalaceTriggerTests
- [X] Tests: WorldConquestDelegateTests
- [X] Tests: PeaceTurnsTests erweitert (Barbaren-Ausnahme, BC-Inkrement, Reset-Runde zählt sofort wieder) -> `xunit/src/Services/Peace/PeaceTurnServiceTests.cs` + `PeaceTurnsTests`
- [X] Tests: LeaderOrderDelegate-Schwellen + Rating-Formel (`CivilizationRatingServiceTests`)
- [X] Tests: PlayerDtoMapperTest angepasst -> keine Änderung nötig: der Test nutzt `MockedIPlayer`, `ToDto` liest dessen Wert; `FromDto` schreibt nicht mehr zurück und wird nicht verglichen
- [X] Tests: SaveDataAdapterTests für Score-Block + Collapse
- [X] Manueller Check: F9, TopLeader, HallOfFame zeigen identische Werte. Code-Abgleich 2026-10-14: alle drei rechnen über `CivilizationScoreService.Calculate(...).Total` bzw. `TotalScore` und `CivilizationRatingService`; Sichtprüfung im Spiel steht aus
- [X] Tests: `Game`-Level — ein voller Zug über der Schwelle reiht `Show.BuildPalace` genau einmal ein -> `PalaceUpgradeTurnTests` (+ Palast-Option aus → kein Builder)

## Goal

Replace CivOne's invented scoring formula with the original's, fix the two systems that consume the score
(palace upgrades, save files), and correct the city happiness model that the score depends on.

## Decisions taken

| # | Decision |
|---|---|
| D1 | **Port the original formula.** CivOne's own formula (advances ×10, wonders ×50, gold/25) is dropped. |
| D2 | **Recompute only for the score itself.** The per-turn accumulation is *not* dead (corrected 2026-10-10): the Historian reads it. It is reproduced for the Historian (D11). See §2. |
| D3 | **Happiness is corrected to match the original**, and the score consumes that one corrected model. Implemented in the code (Phase 1, §14.2); there is no separate document. |
| D4 | **The 40 % luxury reference is reproduced, side-effect free.** See §3. |
| D5 | **Spaceship success rate is read live**, not frozen. See §4. |
| D6 | Palace threshold is corrected to the original formula including difficulty scaling. See §5. |
| D7 | ~~**The world-conquest condition ships in both forms, switchable**~~ **Revised 2026-10-14: latched only**, as in the original (`WorldConquestDelegate`). The derived variant, the setting and the menu entry were removed; they differ only in an editor edge case. §10 is kept for the analysis. |
| D8 | **SVE stays byte-compatible with the original; YAML is free.** YAML may carry richer per-player data as long as it collapses back to the SVE layout on export. See §11. |
| D9 | **Rating and Famous Persons follow the original**: `rating = score × (D + 1) / 50`, leader by `(i × i) / 3 < rating`. CivOneX's `score / 137 × 100` and the wiki thresholds are dropped. See §15.1. |
| D10 | **Only the human player is scored**, as in the original. A live score for AI players is a CivOneX extension and listed as a deviation (§8). |
| D11 | **Historian mini palace follows the original** (Q7 = A): level derived from the per-player city sum, not from the real palace. Phase 5. |
| D12 | **The original calendar (1-year steps in 1750–1850 while a ship is in flight, truncated ETA) is adopted, in its own plan** — [plan-calendar.impl.md](plan-calendar.impl.md) (Q8). This plan does not wait for it. |
| D13 | **Every new rule lives in a replaceable service** (interface + factory with a replacement point), so a later service or plugin can change it. See §16. |

---

## 1. The formula

```
score  =  Σ over the player's cities: (Size + Happy − Unhappy)        // happiness at the 40 % luxury reference, D4
        + 20 × wonders currently held
        + (successRate × SpaceShipPopulation) / 2                     // only in the spaceship's arrival year
        − 10 × PollutedSquaresCount                                   // global counter, not per player
        + min(PeaceTurns × 3, 100)                                    // only while Year > 0 (AD), see below
        + 5  × FutureTechCount

score  =  clamp(score, 0, short.MaxValue)                            // original: 16-bit wrap, then < 0 → 0 (deviation 12)

if the player has eliminated every other civilization:
    bonus = 400 + 100 × AiOpponentCount + 2 × (550 − GameTurn)       // AiOpponentCount = setup opponents, not counting human/barbarians
    if bonus >= score: score = bonus                                  // ">=", label "Bonus Score:" (OCO Overlay_20.cs:1552-1562)
```

Every term was re-verified in the decompilation on 2026-10-03: the per-city term is
`ActualSize + Var_70e2 − Var_70e4` (`Overlay_20.cs:1292-1297`), the wonder weight is `0x14` = 20
(`Overlay_20.cs:1417`), and the peace bonus is `min(3 × Turns, 100)` (`Overlay_20.cs:1531-1546`). The pollution
and future-tech terms match §1 as written.

Notes on the terms:

- `successRate` is a percentage 0–100; `SpaceShipPopulation` is stored in units of ten thousand (~1–99). Both
  factors are pre-scaled, so the term stays in the low thousands and cannot overflow the 16-bit field.
- **The spaceship term is gated on year *equality*, against a stored ETA field.** Verified 2026-10-03: the
  product sits at `Overlay_20.cs:1463-1468`, and it is added only when
  `Players[HumanPlayerID].SpaceshipETAYear == Year` (`Overlay_20.cs:1481-1483`) — equality, not `>=`. In every
  other year the value is only *displayed*, suffixed `" possible"` (`:1488`). The original compares a stored ETA
  field here, it does not recompute launch year + flight time. Two further preconditions: the term is skipped
  entirely when `SpaceShipPopulation == 0` (`:1454-1456`) or `successRate == 0` (`:1460`).
- **The stored ETA cannot be replaced by an equality check against CivOneX's derived year** (OCO, 2026-10-10).
  The original stores `ETA = Year + flightTenths / 10` (truncated) at launch, and while any ship is in flight
  (`SpaceshipFlags & 0xfe`) the 1750–1850 phase advances by 1 year instead of 2 (OCO `GameEngine.cs:206-220`;
  the bit is cleared again when a ship is lost, `Actions.cs:292-296`). Every year is hit, so `ETA == Year` is
  always reached. CivOneX derives
  `LaunchYear + Ceiling(flight)` and keeps 2-year steps until 1850 ([Game.cs:551](../src/Game.cs#L551),
  `Common.TurnToYear`), so an equality test against its year can be skipped over and the term would never apply.
  **CivOneX gate: "the human ship's arrival was detected this turn"** — the same condition `CheckSpaceVitory`
  uses. See §15.3 and Q8.
- The spaceship term is clamped to `0` before it is added (`Overlay_20.cs:1469-1471`, verified) — a negative
  product contributes nothing.
- Pollution and peace turns are **global** values in the original, not per-player. CivOne matches this.
- **The peace bonus has a year gate, and so does the counter.** Both verified 2026-10-03: the score read is
  gated by `if (Year <= 0)` (`Overlay_20.cs:1524`), and the increment is gated the same way
  (`Segment_1238.cs:208-209`). Reproduce both, so the two cannot drift apart. CivOne currently gates **neither**
  — see §14.4.
- **The `min(…, 100)` cap sits on the score term, not on the counter.** The counter runs uncapped and a second
  consumer reads it with a different cap (`× 2`, max 60). Do not clamp the counter itself.
- **The peace counter resets on every attack, not on a declaration of war.** An earlier revision of this plan
  had this backwards; **corrected 2026-10-03.** The field is `CivState.PeaceTurnCount` (`CivState.cs:19`) and it
  has exactly two writers in the whole tree: the gated increment above, and one reset to `0` inside the combat
  routine `F0_29f3_000e` (`Segment_29f3.cs:798`), reached from the attack resolution
  (`Segment_25fb.cs:4589`, `Segment_1403.cs:2817`) on every path. No diplomacy and no nuke routine touches it.
  The declaration of war is merely adjacent: the war bit `Diplomacy |= 0x20` is set a few lines earlier
  (`:783`, `:793`).
- **Only real combat resets.** The estimate path of the attack routine (`flag = false`) returns before the reset
  (OCO `AttackActions.cs:120-122`, `:339-342`). Capturing an undefended city is no combat and does not reset.
- **Reset and increment are independent.** The reset happens during the attack; the increment runs at the start
  of the human slot of the *next* turn (OCO `GameEngine.cs:122-125`). A reset in turn T therefore already reads
  `1` in turn T+1. CivOneX resets and skips the increment in the same call, so it runs one behind (3 points). See
  §15.7.
- **The reset is skipped when either party is slot 0** (`Segment_29f3.cs:794-798`, verified). Fights against
  barbarians therefore do not break the peace streak — but the war bit is still set. This is the one part of the
  old claim that holds, and it is the part CivOne is missing (§14.4).
- The world-conquest bonus *replaces* the score when it is larger — it does not add to it. The condition is a
  latched flag in the original (DS variable `0xb884`), set when the active-civilization mask equals
  `(1 << HumanPlayerID) | 1` — "human plus barbarians only" — in `StartGameMenu.cs:1104-1112`, and read as the
  bonus gate at `Overlay_20.cs:1580`. **The latch lives in RAM only and is never persisted** (verified
  2026-10-03, see §10). CivOne ships it and a derived variant behind a switch (D7, §10), latched by default.
- **`SCORING COMPLETED` is reachable** (corrected 2026-10-14). The original guards it with `SpaceshipFlags & 0x100`.
  That bit is the barbarian slot of the "has spaceship parts" bits, which the space race never sets — but the
  end-of-game block sets it on purpose as a "scoring completed" latch after every end-game flow. From then on the
  score display shows `SCORING COMPLETED` and stops. Implemented with
  [plan-endOfGameContinue.impl.md](plan-endOfGameContinue.impl.md), Phase 4. See §5.

### Inputs, all available in CivOne today

| Term | Source |
|---|---|
| City size, happy, unhappy | `City.GetCitizenTypes()` → [CityCitizenService.cs](../src/Screens/Services/CityCitizenService.cs) — **corrected in Phase 1 (§14.2)**. Term = `city.Size + happy − (unhappy + redShirt)`: CivOneX counts red shirts as unhappy ([CityCitizenService.cs:350](../src/Screens/Services/CityCitizenService.cs#L350)); specialists count only through `Size` |
| Wonders held | `player.Cities.Sum(c => c.Wonders.Length)` |
| Spaceship population / arrival | `Player.SpaceShipPopulation`; gate = arrival detected this turn, same condition as `CheckSpaceVitory` ([Game.cs:547-556](../src/Game.cs#L547-L556)) |
| Spaceship success rate | `ISpaceShipService.SuccessRate()` ([SpaceShipService.cs](../src/Services/SpaceShip/SpaceShipService.cs)); formula in `SpaceShipSuccessRateDelegate` (Phase 2.2, done) |
| Polluted squares | `IGlobalWarmingService.PollutedSquaresCount` |
| Peace turns | `Game.PeaceTurns`, advanced in [Game.cs:593](../src/Game.cs#L593) |
| Future tech | `Player.FutureTechCount` |
| Difficulty, turn | `Game.Difficulty`, `Game.GameTurn` |
| AI opponent count | `Game.Competition − 1`. `Competition` is opponents + 1 ([Game.LoadSave.cs:150](../src/Game.LoadSave.cs#L150)). The YAML path sets it to `Players.Length − 1` ([Game.LoadYaml.cs:87](../src/Game.LoadYaml.cs#L87)), i.e. all slots minus barbarians — the same value, as long as the YAML player list holds every setup civilization (verify with a test). **Not** the live player list: at conquest time it holds no rivals. |

No new persistent game state is required.

---

## 2. Why recompute only (D2)

**Corrected 2026-10-10 against OCO.** An earlier revision called the per-turn accumulation dead weight. It is not.

The original keeps a `short Score` per player and accumulates into it every turn:

- `Players[i].Score = 0` is set **per slot, right before that slot is processed** — not for all eight at turn
  start (OCO `GameEngine.cs:72-75`).
- City processing adds `size + happy − unhappy` at the player's *real* luxury rate (OCO `CityWorker.cs:3406`,
  derived from control flow: the real turn path).
- After all players: `+25` per wonder (index 1..21, city < 128) for the wonder's owner, and `−10 × pollution` for
  the human player only (OCO `GameEngine.cs:298-318`).

The accumulation **is read**: the Historian report (OCO `Reports.cs:474`,
`F12_0000_09e2_PlayerHistoryRankingReport`) draws each civilization's mini palace from
`F0_1238_1c98_GetPalaceLevelFromScore(Players[sel].Score)` (OCO `GameEngine.cs:1196-1210`) — for AI players and
the human alike. CivOneX draws `player.Palace` there instead
([CivilizationRankingService.cs:94](../src/Screens/Reports/CivilizationRankingService.cs#L94)).

Timing detail (checked 2026-10-10): the Historian is called **after the player loop but before** the `+25`
wonder and `−10 × pollution` adjustments (OCO `GameEngine.cs:288-298`). It therefore sees only this turn's city
sums. The wonder and pollution adjustments are then applied, written to the SVE on save, and zeroed again before
the next read — they remain unobservable apart from the save bytes. Side effect of `Var_df60`: in the first turn
after a load, slots below the human player are zeroed but not processed, so the Historian sees `0` for them.

So there are **two different quantities** in the original:

| Quantity | Formula | Consumer |
|---|---|---|
| Civilization score (§1) | recompute at 40 % luxury, `+20` per wonder, peace, future tech, spaceship | F9, palace trigger, end of game, Hall of Fame |
| Accumulated turn score | `Σ (size + happy − unhappy)` at the **real** luxury rate | Historian mini palace (`level`: subtract 5, 10, 15, … until ≤ 0; `level/5 − 1`) |

The `+25`/`+20` difference is therefore not an inconsistency of one path but the difference between two values;
the `+25` itself only reaches the save file.

Decision for the score itself is unchanged: implement the recompute. Whether the Historian's accumulated score is
reproduced for the Historian — **D11** / Phase 5.

### Slot 0 is the barbarians, not the human player

An earlier revision of this plan claimed the routine's hard-coded write to `Players[0].Score` "coincides with the
human player". That is wrong on both sides. In the original, `HumanPlayerID` is forced into `1..7` at game start
(a rolled `0` is re-rolled), and civilization id `0` is the barbarian nation. CivOne has the same layout:
`_players[0]` is built from the civilization with `PreferredPlayerNumber == 0`
([Game.LoadSave.cs:163-167](../src/Game.LoadSave.cs#L163-L167)), with an explicit "don't need for barbarians"
guard.

So `Players[0].Score` is the **barbarian slot used as a scratch variable**. Two consequences:

- The score routine never writes the human player's `Score` field at all. What sits in
  `Players[HumanPlayerID].Score` is the per-turn accumulator described above.
- All eight slots are written to the SVE file, so a byte-compatible save has to pick a slot mapping rather than
  inherit one. Decided in D8 / §11: human score into slot 0, zeros elsewhere. **This does not mirror the
  original's bytes** — there slot 0 holds the barbarian accumulation or the last popup result, slots 1..7 the
  accumulation. It is still layout-compatible: the original reads the block back on load
  (OCO `LoadAndSave.cs:371`), but each slot is zeroed in the turn loop before the Historian or anything else reads
  it. Since Q7 = A, slots 1..7 carry the city sum instead of `0` (Phase 5).

The other original quirk we do not reproduce: the routine mutates real game state while measuring (see §3).

---

## 3. The 40 % luxury reference (D4)

**Verified against the decompilation on 2026-10-03. The 40 % figure is confirmed; the list of leaked side
effects had to be corrected.**

The original does not evaluate happiness with the player's actual tax and luxury sliders. It saves `TaxRate`,
overwrites it with `6 − ScienceTaxRate` (`Overlay_20.cs:1224-1226`) and restores it after the city loop
(`Overlay_20.cs:1327-1328`). No path leaves the function between those two points.

The luxury share is `−(ScienceTaxRate + TaxRate − 10)` (`CityWorker.cs:1468-1472`), so with `TaxRate = 6 − Sci`
it collapses to `10 − Sci − (6 − Sci) = 4` — **happiness is always measured as if the luxury slider were at
40 %**, independent of what the player has set. The city's luxuries are then
`clamp((4 × (Trade − Corruption) + 5) / 10, 0, Trade)` and the happy counter is `luxuries / 2`
(`CityWorker.cs:1574`). CivOne's [CityCitizenService.Luxuries()](../src/Screens/Services/CityCitizenService.cs#L113)
already has exactly this shape, so only the rate has to be supplied.

`ScienceTaxRate` itself is never written in the routine, only read (`Overlay_20.cs:1226`). **No difficulty,
government or flag gate skips the assignment.** The one early return sits *before* it
(`flag && SpaceshipFlags & 0x100`, `Overlay_20.cs:1189-1208`) and abandons the whole evaluation — that is the
`SCORING COMPLETED` display after the end of the game (§5, corrected 2026-10-14). It only affects the displaying
call; the rate itself is unchanged. The non-displaying call site (`flag = false`, `Segment_1238.cs:581`)
runs the assignment and the simulation as well.

Therefore the reference rate is **`4` in tenths**, and it applies to every score evaluation without exception.

### Which side effects the original really leaks

An earlier revision of this plan claimed the routine leaves "trade values, growth and production thresholds"
changed. Only the first third of that is true.

| Leaked, not restored | Where | Note |
|---|---|---|
| `city.BaseTrade` | `CityWorker.cs:1436` | recomputed at the 40 % rate and left behind |
| `city.WorkerFlags`, including the specialist/worker distribution | `CityWorker.cs:739` | the city can come back with reassigned workers |
| `city.ShieldsCount = 0` on an out-of-range clamp | `CityWorker.cs:140-142` | only on the clamp path |

All three are gated by `StatusFlag != 0xff`.

**Growth and production are *not* leaked.** `FoodCount`, `ActualSize`, the shields accumulation,
`CurrentProductionID`, `Coins` and `ResearchProgress` all sit in `flag == 0` blocks, or behind
`if (flag != 0) goto L68cc` (`CityWorker.cs:3586`), and the call passes `flag = -1`, so they are skipped. The
side-effect problem in the original is therefore smaller than assumed — but it is still real, and a leaked
`WorkerFlags` is worse than a leaked threshold would have been.

Open residue, not blocking: the interactive block from `CityWorker.cs:2561` (`StatusFlag ^= 0x10` and
neighbours) is gated in that translation only by `PlayerID == HumanPlayerID`, not visibly by `flag`. Whether it
is reachable at `flag = -1` could not be established from the control flow. See Q5 — it does not affect our
implementation, which runs no simulation at all.

### Implementation in CivOne

Make the happiness computation accept the luxury rate as a parameter instead of reading it from the player.
**Done in Phase 1:** `City.GetCitizenTypes(int? luxuryRate)` ([City.cs:1046](../src/City.cs#L1046)) forwards to
`CityCitizenService`, whose `LuxuryRate` ([:102](../src/Screens/Services/CityCitizenService.cs#L102)) falls back
to `10 − Science − Taxes` when the caller supplies nothing. The scoring path passes the fixed reference rate,
every other caller passes nothing and behaves exactly as before.

What is left for Phase 2.1: `CivilizationScoreService.ScoringLuxuryRate`
([:20](../src/Services/CivilizationScoreService.cs#L20)) is still **`0`**, i.e. 0 % luxuries. It has to become
**`4`**. At `0` the main term `Σ (Size + Happy − Unhappy)` is systematically too low, because no city gets
luxury-derived happiness. The constant's current value was harmless for CivOne's own formula and becomes a
defect the moment §1 lands.

Nothing in CivOne mutates game state to obtain the rate, so none of the leaked fields above apply here
(deviation 3, §8).

---

## 4. Spaceship success rate (D5)

The original stores the success rate in a player field that is only rewritten when the player opens the spaceship
screen, so between visits it holds a stale value. CivOne computes the same quantity live in
`SpaceShipScreenDataFactory`.

We use the live value. Reproducing the frozen field would mean a new persistent player field plus a save-format
entry, for a difference that is only observable if the player opens the spaceship screen and then dismantles
parts without reopening it. Recorded as a known deviation (§8).

**Done (Phase 2.2).** The formula moved into `SpaceShipSuccessRateDelegate` (inputs: `SpaceShipMissionStatus`), used by
`SpaceShipScreenDataFactory`. `ISpaceShipService.SuccessRate()` reads it live for the score. A launched ship counts
as complete there, so a perfect ship in flight reports 100; the spaceship screen still caps it at 99 after the
launch, because `CanLaunch()` turns `false` then (unchanged UI behaviour).

---

## 5. Palace upgrade threshold (D6)

Original: with `n = PalaceLevel + 1` and `D = Difficulty` (0 easiest … 4 hardest in the original; CivOneX adds
Deity = 5, which continues the curve with coefficient `2`)

```
threshold(n) = floor((7 − D) × n × (n + 1) / 2 / 2) + 1
```

CivOne today ([HumanCivScorePalaceTrigger.cs:34](../src/Services/Palace/HumanCivScorePalaceTrigger.cs#L34)):

```csharp
int n = player.Palace.UpgradeCount;
long threshold = 1L + ((long)n * n) + n;
```

At `D = 3` the original reduces to exactly `n² + n + 1`. So CivOne already has the right curve, hard-coded to
difficulty 3 and with `n = UpgradeCount` instead of `UpgradeCount + 1`. Two small corrections, not a rewrite:

1. `n = UpgradeCount + 1`.
2. difficulty coefficient `(7 − D)`, requiring `Difficulty` on `IPlayerGameState` (or a second interface member
   supplied by `Game.PlayerGameStateAdapter`).

The original does **not** cap the palace level at 37 — the counter keeps incrementing; only the redraw is skipped
beyond 37. That is harmless there because the threshold grows with `n`. CivOne needs no cap either:
`PalaceData.UpgradeCount` is `7 levels × 4 + 3 gardens × 3 = 37` by construction, and `CanUpgrade`
([PalaceData.cs:79](../src/PalaceData.cs#L79)) already returns `false` at the maximum.

### The three flags in the palace check

All three are now understood; none is a mystery gate.

| Flag | Meaning | CivOne |
|---|---|---|
| `ImprovementFlags0 & 0x1` | Not a gate at all — it is the Palace *improvement* bit. The loop uses it to find the city that holds the palace. | CivOne locates the palace city directly; nothing to port. |
| `GameSettingFlags & 0x80` | **The game option "Palace"** (`GameSettingFlags.BuildPalace`, OCO `GameSettings.cs:49-51`). Default on (`0xfa`), toggled by XOR in the options menu (OCO `Menus.cs:185`), tested by the trigger (OCO `GameEngine.cs:344`). Corrected 2026-10-10 — an earlier revision called it "never cleared". | `Game.Palace` ([GameOptions.cs:85](../src/Screens/GameOptions.cs#L85)) exists but the trigger ignores it. **Must be passed in** via `IPlayerGameState`. |
| `SpaceshipFlags & 0x100` | **"Scoring completed" latch** (corrected 2026-10-14). Set by the end-of-game block after every end-game flow; blocks further palace upgrades. | A per-game latch, see [plan-endOfGameContinue.impl.md](plan-endOfGameContinue.impl.md), Phase 4. The trigger returns `false` while it is set. |

The `0x100` case needs spelling out, because an earlier revision of this plan (until 2026-10-14) called it dead
code. The high spaceship bits are `1 << (playerID + 8)`, so `0x100` is slot 0 — the barbarians, who never build
a spaceship. The space race therefore never sets it; the place that tests for spaceship parts uses `0xfe00`, i.e.
bits 9–15 for players 1–7. **But the end-of-game block reuses the free bit:** after the end-game flow (power
graph, replay, score, Famous Persons, Hall of Fame) it sets `SpaceshipFlags |= 0x100`, right before the "keep
playing?" dialog. Verified 2026-10-14 by civ-porter in the disassembled `CIV.EXE`; the OCO rewrite misses the
write, the early pseudo-assembly translation has it.

Therefore: **after "keep playing", palace upgrades are blocked and the score display shows `SCORING COMPLETED`.**
This is not a "player has a spaceship" predicate; it is a per-game "scoring completed" latch, implemented with
[plan-endOfGameContinue.impl.md](plan-endOfGameContinue.impl.md), Phase 4. The latch is stored in SVE saves
(bit 8 of `SpaceshipFlags`).

### When the check runs (added 2026-10-10)

The original checks **exactly once per turn**, at the end of the turn after all players, and only if the human
player owns a city with a palace. `PalaceBuildCount++` happens immediately, before the dialog
(OCO `GameEngine.cs:338-356`):

```
for each city: if owner == human && has palace:
    if option Palace && score >= threshold(PalaceBuildCount + 1):
        PalaceBuildCount++
        if PalaceBuildCount < 38: show upgrade dialog
    break
```

CivOneX checks in `EndTurn` on **every player change** ([Game.cs:511-524](../src/Game.cs#L511-L524)). Today that is
harmless because the stored score never changes. Once the score is live (Phase 4), `Show.BuildPalace` would be
queued several times per turn until `UpgradeCount` rises. Required:

1. Check only on the turn wrap (`_currentPlayer` wraps to the first slot), not per player.
2. Precondition: the human player owns a palace city.
3. Test: one turn above the threshold queues the dialog exactly once.

---

## 6. Current state and defects being fixed

| Area | Where | State |
|---|---|---|
| Score formula | [CivilizationScoreService.cs](../src/Services/CivilizationScoreService.cs) | CivOne's own formula — replaced by §1 |
| Consumers | [CivilizationScore.cs](../src/Screens/Reports/CivilizationScore.cs) (F9), [TopLeaderScreen.cs](../src/Screens/Reports/TopLeaderScreen.cs), [HallOfFameEntryComposerService.cs](../src/Services/HallOfFame/HallOfFameEntryComposerService.cs) | all resolve the service through `CivilizationScoreServiceFactory.CreateDefault()` and pass the human player |
| Stored field | `Player._civilizationScore` (`ushort`), read via `IPlayer.CivilizationScore` ([Player.cs:752](../src/Player.cs#L752)) | written **only** by `IPlayerRestorable.CivilizationScore` ([Player.Persistence.cs:240](../src/Persistence/Game/Player.Persistence.cs#L240)), whose only caller is [PlayerDtoMapper.cs:87](../src/Persistence/Mapper/PlayerDtoMapper.cs#L87) (YAML load) |
| YAML save | [PlayerDtoMapper.cs:165](../src/Persistence/Mapper/PlayerDtoMapper.cs#L165) | writes the stored field straight back |
| Palace trigger | [Game.cs:434](../src/Game.cs#L434) → the trigger | reads the stored field |
| SVE struct | [SaveData.cs:99](../src/IO/SaveData.cs#L99), `fixed ushort CivilizationScore[8]`, offset 1864:1879 | reserved, never read or written |
| Tests | [PlayerDtoMapperTest.cs](../xunit/src/persistence/Model/PlayerDtoMapperTest.cs) round-trips `CivilizationScore = 333` | none for the score service, the report or the trigger |
| Happiness tests | [CityCitizenServiceImplTests.cs](../xunit/src/CityCitizenServiceImplTests.cs) (35), [CityHappy.cs](../xunit/src/CityHappy.cs) (11) | substantial and green — the baseline for the separate happiness plan |

Defects that follow:

1. **The palace upgrade trigger is dead in any game that was never loaded from YAML.** `_civilizationScore`
   defaults to `0`, nothing in gameplay ever sets it, and the lowest threshold is `1`.
2. **YAML saves store a stale score** — whatever the last load produced, `0` for a fresh game, while the reports
   on screen show a different number.
3. **The SVE score block is dead space.**
4. **The score and the reports can never agree**, because nothing connects the service to the field.

After this plan the score is a derived value again — but derived by the *original's* formula, so the number in
the save file is meaningful to the original binary as well.

---

## 7. Phases

### Phase 1 — Correct the happiness model (D3)

**Done (§14.2).** It was planned as a separate document, which was never written; the work is in the code.

The score's largest term is `Σ (Size + Happy − Unhappy)`, so it is only as correct as `CityCitizenService` —
but correcting that model turned out to be a larger piece of work than the score port itself: it touches ten
consumers and changes how the game plays. It therefore ships as its own deliverable, with its own decisions
(H1–H7), its own open points and its own tests. This plan simply consumes the result.

What the score needs from it:

| Need | Covered by |
|---|---|
| A per-city `Size + Happy − Unhappy` that matches the original | the whole of that plan |
| No positive contribution from a negative unhappy count | its §3 — per-stage normalisation and clamp ordering |
| The luxury rate as a caller-supplied parameter, for the 40 % reference (§3 here) | its H7 / step 9 |

**Phase 2 must not start before that plan's switch is flipped on** (its §7). Until then the running game still
uses the old happiness model, and a score computed against it would be wrong in exactly the way this plan exists
to fix.

### Phase 2 — The score service

- Rewrite `CivilizationScoreService.TotalScore` to §1. Drop the citizen/city/advance/wonder/gold weights and the
  `GoldPerScorePoint` constant with them.
- The signature currently takes the concrete `Player`, which is hard to construct in a test (the existing
  `MockedIPlayer` is an `IPlayer`, not a `Player`). Narrow it to the state the formula reads — cities, wonders,
  spaceship fields, future tech — and inject the global inputs (`IGlobalWarmingService`, peace turns, difficulty,
  turn, opponent count) rather than reaching for `Game.Instance` inside the service. Resolve any singleton
  fallback lazily, per `CLAUDE.md`.
- Lift `CalculateSuccessProbability` out of `SpaceShipScreenDataFactory` into the spaceship service (§4).
- **`RatingPercent` changes** (D9, §15.1): `rating = score × (D + 1) / 50`, integer division, no rounding. The
  `topLeaderThreshold` parameter goes away. Rewrite `LeaderOrderDelegate` to the original selection and name list;
  adapt `TopLeaderScreen` and `HallOfFameEntryComposerService` (the Hall of Fame prints the same formula).
- Per-city term uses `unhappy + redShirt` as "unhappy" and `city.Size` (§1 inputs table).
- Conquest bonus: `Competition − 1` as opponent count; replace when `bonus >= score`.
- Implement the peace bonus's `Year > 0` gate (§1). There is no spaceship gate in the formula. The `0x100` test
  only stops the *display* with `SCORING COMPLETED` after the end of the game (§5, corrected 2026-10-14); it
  belongs to [plan-endOfGameContinue.impl.md](plan-endOfGameContinue.impl.md), Phase 4.
- Take the world-conquest condition as an injected `WorldConquestPredicate` (D7, §10); the service must not know
  which of the two variants it holds.
- **Wonder counting is already correct.** The original reads the wonder→city table directly, so obsolete wonders
  count, captured cities count for the captor, and destroyed cities drop out. CivOne's
  `player.Cities.Sum(c => c.Wonders.Length)` gives the same three answers — *provided* CivOne's wonder list
  travels with a captured city and disappears with a destroyed one. Verify that once (Q9); no formula change.
- Tests `xunit/src/Services/CivilizationScoreServiceTests.cs`: every term in isolation; the arrival-year gate on
  the spaceship term (arrival detected this turn); the BC/AD gate on the peace term; the `0` floor; the
  world-conquest bonus replacing rather than adding, also when equal; `short.MaxValue` saturation (deviation 12);
  `ArgumentNullException` on a null player; wonder counting across capture and destruction; red shirts counted as
  unhappy.
- Tests for the rating: `score × (D + 1) / 50` at several difficulties; leader thresholds
  `rating > 0, 1, 3, 5, 8, 12, 16, 21, 27, 33, 40, 48, 56, 65, 75, 85, 96, 108, 120, 133`; rating `0` shows no
  window; index capped at 19.

### Phase 3 — Palace trigger (D6)

- Apply the two corrections from §5; expose `Difficulty` through `IPlayerGameState`. `n = UpgradeCount + 1` is
  confirmed: the original's counter starts at 0, is incremented by exactly one per upgrade, and is queried as
  `PalaceLevel + 1`. The coefficient `(7 − D)` runs `7…3` over the original's difficulties and continues to `2`
  at CivOne's Deity level.
- **No spaceship gate.** The current end-of-game condition is removed. Corrected 2026-10-14: the original does
  block upgrades after the end of the game through the "scoring completed" latch `0x100` (§5). That gate is added
  with [plan-endOfGameContinue.impl.md](plan-endOfGameContinue.impl.md), Phase 4, not here.
- **Palace option gate**: the trigger returns `false` when `Game.Palace` is off (§5, flag `0x80`).
- **Once per turn**: move the check from the per-player `EndTurn` path to the turn wrap, with "human owns a palace
  city" as precondition (§5 "When the check runs").
- Side note for whoever touches the Hall of Fame: the original derives its rank from the same counter, as
  `(PalaceLevel >> 1) − 1`. Out of scope here; recorded so it is not re-derived from scratch.
- Make the trigger read the live score. The adapter at [Game.cs:434](../src/Game.cs#L434) needs no change for
  that, but the call site in `EndTurn` does (previous bullet).
- Tests `xunit/src/Services/Palace/HumanCivScorePalaceTriggerTests.cs`: the threshold at `n = 0, 1, 2` across
  difficulties 0–5, exact-threshold boundary triggers, AI players never trigger, null player / null palace /
  `!CanUpgrade` return `false`, Palace option off returns `false`, and the regression case — a human player in a
  game that was never loaded from a save, above the threshold, triggers. That case fails on today's code, which
  is the proof the defect existed. Plus a `Game`-level test: one full turn queues `Show.BuildPalace` at most once.

### Phase 4 — Persistence

**Derived getter.** `IPlayer.CivilizationScore` ([Player.cs:752](../src/Player.cs#L752)) computes the live score
instead of returning `_civilizationScore`, clamped into `ushort`, with the service resolved lazily:

```csharp
private readonly ICivilizationScoreService? _civilizationScoreService;   // ctor parameter, default null
private ICivilizationScoreService CivilizationScoreService =>
    _civilizationScoreService ??= CivilizationScoreServiceFactory.CreateDefault();
```

Every producer — palace trigger, YAML save, SVE save — becomes correct through this one change, with no new
constructor parameter in `PlayerDtoMapper` (already 10 dependencies behind a `CA1707` suppression), none in
`Game.PlayerGameStateAdapter`, none in `Game.LoadSave`.

**Remove the stored field (D-Q3).** Once the getter is derived, nothing reads `_civilizationScore` and neither
save format needs it back on load (§11). Delete all three of it:

1. the `_civilizationScore` field on `Player` ([Player.cs:752](../src/Player.cs#L752) region),
2. `IPlayerRestorable.CivilizationScore` ([Player.Persistence.cs:240](../src/Persistence/Game/Player.Persistence.cs#L240)),
3. its only caller, the `FromDto` write in [PlayerDtoMapper.cs:87](../src/Persistence/Mapper/PlayerDtoMapper.cs#L87).

Keeping it as a write-only inbox was the alternative; it buys nothing and leaves a field that looks authoritative
and is not.

**YAML.** `PlayerDtoMapper.ToDto` needs no edit — it reads `player.CivilizationScore`, now live. The DTO keeps
its `CivilizationScore` member: YAML is unconstrained (D8) and a per-player score is useful for reports and
debugging. `FromDto` simply stops writing it back.

The existing round-trip assertion (`CivilizationScore = 333`,
[:341](../xunit/src/persistence/Model/PlayerDtoMapperTest.cs#L341)) **will fail**, because `ToDto` no longer
echoes what `FromDto` stored. That failure is the intended signal. Rewrite it: build a player with known state,
`ToDto`, assert the DTO carries the computed score.

**SVE binary**, following the existing `TaxRate` pattern end to end:

1. [SaveDataAdapter.Get.cs](../src/SaveDataAdapter.Get.cs) —
   `private ushort[] GetCivilizationScore() => GetArray<ushort>(nameof(SaveData.CivilizationScore), 8);`
2. [SaveDataAdapter.Set.cs](../src/SaveDataAdapter.Set.cs) —
   `private void SetCivilizationScore(ushort[] values) => SetArray(nameof(SaveData.CivilizationScore), values);`
3. [SaveDataAdapter.cs](../src/SaveDataAdapter.cs) — public `ushort[] CivilizationScore` property mirroring
   `TaxRate` (`:192`), plus the member on `IGameData`.
4. [Game.LoadSave.cs](../src/Game.LoadSave.cs) — save side only, among the other per-player arrays (`:70-90`).
   **There is no load side.** The value is derived, and `IPlayerRestorable.CivilizationScore` is gone; reading
   the block back would only reintroduce the stale-value defect this plan exists to remove. Use the
   `CVS.CheckedUInt16` guard the neighbouring fields use on the way out.

**Which slot gets what (D8, §11).** SVE writes the human player's computed score into **slot 0** — the barbarian
scratch slot the original's score routine writes — and `0` in the other seven. This is layout-compatible, not
byte-identical (§2): the original would hold the accumulation in slots 1..7. It costs nothing, because the
original zeroes each slot in its turn loop before reading it. Since Q7 = A, slots 1..7 carry the city sum
instead of `0` (Phase 5).

YAML is not bound by this: it may carry a score per player. `Game.LoadSave`'s SVE writer is the single place that
collapses that to the slot-0 layout, so a YAML-originated game saved as SVE produces the same bytes as one that
was SVE all along.

Tests: `SaveDataAdapterTests` round-trips the block at offset 1864 per slot with synthesised bytes; a separate
test pins the collapse — a game with distinct per-player scores writes them all to `0` except slot 0, which
carries the human player's.

### Phase 5 — Historian mini palace (decided 2026-10-10: Q7 = A)

Earlier dropped on the claim that the accumulation is unobservable. It is observable: the Historian draws each
civilization's mini palace from it (§2). Option B (keep the real palace) was rejected in favour of the original.

**How the original draws the mini palace** (OCO `F0_1238_14a3_DrawMiniPalace`, `GameEngine.cs:837-925`, checked
2026-10-10):

- **Human player:** the real palace, piece by piece (`PalaceLevels`/`PalaceTypes`) — what CivOneX already does.
- **AI players:** AIs never build a palace. The original *generates* one from a single number, the "build count":
  `GetPalaceLevelFromScore(citySum) + 3`, clamped to 3..36. It places that many pieces on the 9 palace sections,
  chosen by a deterministic pseudo-random sequence (seed `playerID × RandomSeed`), each section at most
  `count / 5` and at most level 3, in style `playerID % 3`. The same civilization with the same city sum always
  gets the same palace.

So only AI rows change. CivOneX shows `player.Palace` for them today, i.e. an unbuilt palace.

**New service `IHistorianPalaceLevelService`** (§16), new MIT file under `src/Services/Historian/`:

```csharp
public interface IHistorianPalaceLevelService
{
    /// Sum of (size + happy − unhappy) over the player's cities at the player's real luxury rate.
    int CitySum(IPlayer player);

    /// Original GetPalaceLevelFromScore: subtract 5, 10, 15, … while > 0; result = level / 5 − 1.
    int PalaceLevel(int citySum);
}
```

- `CitySum` calls `City.GetCitizenTypes()` **without** a luxury override (the real rate), red shirts counted as
  unhappy as in §1. No stored accumulator: the Historian reads the value in the same turn the original builds it.
- `PalaceLevel` is a pure function; put it in a `HistorianPalaceLevelDelegate` if the service grows.
- **The palace shown per player is itself a service** (decided 2026-10-10): `IHistorianPalaceService` with
  `IPalaceData PalaceFor(IPlayer player)`. The ranking screen asks only this service and never decides where a
  palace comes from. Default implementation, as in the original:
  - human player → `player.Palace` (the real palace);
  - AI player → generated: `PalaceLevel(CitySum(player)) + 3`, clamped to 3..36, placed by a
    `MiniPalaceGeneratorDelegate` that reproduces the original generator and returns CivOneX's `IPalaceData`.
  A later implementation can return a really built AI palace instead (like the human's) without touching the
  screen or the generator. The ranking screen keeps drawing `IPalaceData`; no new renderer.
- [CivilizationRankingService.cs:94](../src/Screens/Reports/CivilizationRankingService.cs#L94) calls
  `IHistorianPalaceService.PalaceFor(player)` for every row.
- To map when implementing: the original's 9 sections × level 0..3 onto `PalaceData`'s layout (7 levels × 4 +
  gardens). The seed is the game's `RandomSeed` ([SaveDataAdapter.cs:90](../src/SaveDataAdapter.cs#L90)).
- SVE: slots 1..7 of `CivilizationScore[8]` carry `CitySum` (clamped to `short`) instead of `0`; slot 0 stays the
  human score (§11). This is closer to the original's bytes and still never read back by CivOneX.
- Not reproduced: the original shows `0` for slots below the human player in the first turn after a load
  (deviation 13). Open check Q9 applies (accumulation site proven only by control flow).
- Tests: `PalaceLevel` at sums 0, 5, 6, 15, 16, 50, 150 against the original loop; `CitySum` uses the real luxury
  rate (differs from the score's 40 % reference); the generator is deterministic and respects the per-section
  limits; human row unchanged; the SVE writer fills slots 1..7.

No per-turn hook is needed.

### Phase 5b — Palace with 9 sections as in the original (moved, 2026-10-16)

> **Moved to [plan-palace.impl.md](plan-palace.impl.md).** The premise below is wrong: the real palace has 7 sections
> in the original too; only the mini palace has 9 slots. Kept for history.

The original palace has **9 sections** (0..8); CivOneX has **7** (`PalaceData`, `PalaceDto` with `LeftTower` …
`RightTower`, `PalaceView`). The Historian generator therefore drops original sections 0 and 8 (Phase 5).

Observation from an original game screenshot: the palace is wider than CivOneX draws it. Behind the wall at
CivOneX index 1 and 6 an additional end tower is visible. So the 7-section model is probably not just narrower but
arranges the parts differently.

Current CivOneX model (for comparison):

- `PalaceData`: `PalaceLevel[7]`, `PalaceStyle[7]`, `GardenLevel[3]`; `IsSlotUnlocked` opens 2/3/4 first, then
  1/5, then 0/6.
- `PalaceView.GetPalacePartPreview`: 0 = `LeftTower`, 1/2 = `Wall` or `LeftTowerWall`, 3 = `Center`,
  4 = `WallShadow` or `RightTowerWallShadow`, 5 = `Wall` or `RightTowerWall`, 6 = `RightTower`.
- `PalacePart` has 8 parts; towers are drawn only on the outermost sections.

Next steps:

1. civ-porter: find out how the original arranges its 9 section bitmaps (x positions, which part per section and
   level, when an end tower is drawn, mirror rules, gardens) — in the palace screen and in the Historian mini palace.
   Also check whether level 0 is drawn as the visible base (assumed in Phase 5, not proven).
2. Check the SVE layout: `SaveData.Palace[8 * 6]` (offset 37256) and `PalaceLevel` (37816) — what they hold and
   whether 9 sections fit.
3. Extend `PalaceData` to 9 sections, adapt `PalaceView` (build screen, morph, upgrade selection), the mini palace
   renderer, `SideBar` preview and `VictoryScreen` background.
4. `PalaceDto`: add the two outer sections; loading an old YAML with 7 sections must still work (map 7 → 9).
5. `MiniPalaceGeneratorDelegate`: return all 9 sections; remove deviation "sections 0 and 8 dropped".
6. Upgrade threshold (`PalaceData.UpgradeCount`, max 37) and `CanUpgrade` change with 9 sections; check §5 and the
   palace trigger.

### Phase 5c — Persist AI palaces in YAML (moved, 2026-10-16)

> **Moved to [plan-palace.impl.md](plan-palace.impl.md).** Decided: no snapshot; a setting chooses the AI palace.

`PlayerDtoMapper` already writes `PlayerDto.Palace` for **every** player and reads it back. For AI players this is
always an empty `PalaceData`, because AIs never build, and `HistorianPalaceService.PalaceFor` ignores it and
generates the palace instead.

Option: store the generated (or later really built) AI palace in YAML, so the Historian can show it without
regenerating, and a later "AI builds its palace" rule (Phase 5 notes this as a possible later implementation of
`IHistorianPalaceService`) has a place to live. SVE stays as in the original (only the human palace; collapse rule
like D8).

To decide before implementing:

- Store the generated palace (snapshot per turn) or keep generating and store only real AI palaces?
- When is it written (end of turn, on save)? A snapshot must not drift from the live city sum.
- `HistorianPalaceService`: prefer a stored non-empty AI palace over the generator?

### Phase 6 — F9 screen to the original layout

[CivilizationScore.cs](../src/Screens/Reports/CivilizationScore.cs) keeps its own copy of the old weights
(cities / population / advances / treasury). It must use the service and draw the original layout (OCO
`Overlay_20.cs`, see §15.8). No formula may be duplicated in the screen; the service has to expose the parts
(citizens per city, wonders, spaceship, pollution, peace, future tech, total/bonus) so the screen only draws.

### Phase 7 — End-of-game flow as in the original (decided 2026-10-10)

[EndGameService.cs](../src/Services/EndGame/EndGameService.cs) is already a service (`IEndGameService`); only its
sequences change. Original (OCO `F0_1238_08a0_EndGame`, `GameEngine.cs:440-462`): power graph → replay (not for
end type 2) → score → Famous Persons → Hall of Fame.

| End | CivOneX today | Target |
|---|---|---|
| Retire | score, Hall of Fame | power graph, score, Famous Persons (TopLeader), Hall of Fame |
| Human defeated | defeat, score, TopLeader | defeat, **no** score |
| AI spaceship arrives | defeat, score, TopLeader | full flow incl. Hall of Fame entry and "continue playing?" |
| Conquest, own spaceship | victory, score, TopLeader, Hall of Fame | check power graph and replay against OCO |

Verify each row against OCO when implementing (end types, replay condition). Tests per handler: screen order.

Update 2026-10-14: the replay is skipped only for *Retire*; "keep playing?", the year end and the "scoring
completed" latch are planned in [plan-endOfGameContinue.impl.md](plan-endOfGameContinue.impl.md).

---

## 8. Known deviations from the original

Documented deliberately, not hidden:

1. **No per-turn accumulation** (§2). Corrected 2026-10-10: the accumulation *is* observable through the
   Historian mini palace, and it is reproduced there (D11, Phase 5).
2. **Spaceship success rate is live, not frozen at the last screen visit** (§4). Note that the original stores
   this value in the SVE file, so CivOne's SVE output differs from the original's at that field. The original
   also always writes it into `Players[HumanPlayerID]`, even when the screen is opened for an AI player — a
   genuine bug we do not reproduce.
3. **The 40 % luxury reference is computed without mutating game state** (§3). The original leaves `BaseTrade`,
   `WorkerFlags` (including the specialist/worker distribution) and, on one clamp path, `ShieldsCount` changed
   until the next real end of turn — verified 2026-10-03; growth and production thresholds are *not* affected,
   contrary to an earlier revision. We leak none of them, because we run no city simulation to measure.
4. **The runtime barbarian-slot scratch write is not reproduced** (§2). CivOne computes each player's score live for runtime and YAML consumers; SVE export writes the human score to slot 0 and zeroes the remaining slots (D8 / §11) — layout-compatible, not byte-identical (§2).
5. **One original bug is not reproduced:** the spaceship success rate is always written into the human
   player's slot even when the screen is opened for an AI player. Corrected 2026-10-14: `SpaceshipFlags & 0x100`
   is no bug but the "scoring completed" latch (§5); its palace block and `SCORING COMPLETED` are implemented
   with [plan-endOfGameContinue.impl.md](plan-endOfGameContinue.impl.md).
6. **The `+25` per wonder of the accumulation is not reproduced** (§2). It is a different quantity, not an
   inconsistency, and it only reaches the save file — the Historian reads the accumulation before the `+25` is
   added.
7. **The spaceship gate is "arrival detected this turn", not `ETA == Year`.** The original stores a truncated ETA
   at launch and, while a ship is in flight, advances 1750–1850 in 1-year steps, so equality always hits. CivOneX
   uses `LaunchYear + Ceiling(flight)` and 2-year steps until 1850 ([Game.cs:551-555](../src/Game.cs#L551-L555)),
   so equality could be skipped. Corrected 2026-10-10 — the earlier claim "the two agree unless flight time
   changes" was wrong. The arrival can still fall on a different turn than in the original (rounding and step
   width). Removed by the calendar plan (D12).
8. **Peace counter timing.** CivOne latches `_hostileActionOccurred` and applies it in `AdvancePeaceTurns`
   ([Game.cs:593](../src/Game.cs#L593)); the original zeroes the counter inside the combat routine. The timing
   difference itself is unobservable, **but** CivOne's `AdvancePeaceTurns` returns after the reset without
   incrementing, so it runs one turn behind (3 score points). That part is a defect, fixed in Phase 2.10 (§15.7),
   not a deviation.
9. **Nuclear attacks reset the counter in CivOne** ([Nuclear.cs:37](../src/Units/Nuclear.cs#L37)), where the
   original has no write from any nuke path (§1). **Kept deliberately (decided 2026-10-10)** — a nuclear strike
   ends a peace streak. Documented in the XML doc of `IPeaceTurnService` (§14.4).
10. **Palace level is capped at 37 in CivOne**, where the original lets the counter run past it and only stops
   redrawing. Equivalent for the trigger, since the threshold grows with the level. Not equivalent for the
   Famous Persons fallback at `score == 0` (`PalaceBuildCount / 2 − 1`, OCO `GameEngine.cs:456`), and CivOneX
   does not read `PalaceBuildCount` from an original SVE (offset 37816, `SaveData.PalaceLevel` is unused) but
   derives `UpgradeCount` from the palace levels.
11. **Hostile-action registration happens before the capture check** ([BaseUnit.cs:385](../src/Units/BaseUnit.cs#L385)).
   Capturing an undefended city therefore resets the peace counter in CivOne, not in the original (only real
   combat resets). **Kept deliberately (decided 2026-10-10)** — taking a city is a hostile act. Documented in the
   XML doc of `IPeaceTurnService`. The barbarian exception still applies.
12. **Saturation instead of wrap-around.** The original sums in 16 bit, can wrap, and then floors at `0`.
   CivOne clamps to `short.MaxValue`. Only reachable with extreme scores.
13. ~~Historian mini palace shows the real palace.~~ Resolved by D11 (Phase 5). Remaining difference: the
   original shows `0` for slots below the human player in the first turn after a load (`Var_df60`); not reproduced.
14. **AI players get a live score** (D10). The original scores only the human player: spaceship, future tech,
   citizens and achievements are always read from `HumanPlayerID` (OCO `Overlay_20.cs:1304, 1331, 1387, 1424,
   1518`). Any per-player score in YAML or reports is a CivOneX extension.
15. ~~End-of-game flow differs.~~ **Aligned with the original (decided 2026-10-10), Phase 7.** Original flow
   (OCO `F0_1238_08a0_EndGame`, `GameEngine.cs:440-462`):
   - Retire shows power graph, score, Famous Persons and Hall of Fame; CivOneX skips TopLeader on retire
     ([EndGameService.cs:56-63](../src/Services/EndGame/EndGameService.cs#L56-L63)).
   - A human defeat (`GameEndType −1`) shows no score.
   - An AI spaceship arrival runs the full flow, including the Hall of Fame entry and "continue playing?";
     CivOneX shows only defeat + score + TopLeader.
   - Done 2026-10-15: "keep playing?", the year end and the "scoring completed" latch, see
     [plan-endOfGameContinue.impl.md](plan-endOfGameContinue.impl.md). Still missing: the replay screen.
16. **Conquest latch semantics.** The original sets the latch only when a destroyed civilization cannot respawn
   (`TurnCount > 350 − 50 × D` or no room) and sets `GameEndType = 1` at the same time (OCO `StartGameMenu.cs:559-570`),
   so the game ends that turn. The latched/derived switch (D7) therefore matters little in practice; the derived
   variant has to account for CivOneX's own respawn rules.
17. **Original quirk not reproduced:** after a load the original increments the peace counter again in the first
   human slot.

The happiness model's own deviations are documented in the code of `CityCitizenService` (Phase 1, §14.2).

---

## 9. Open points

| # | Point | Blocks |
|---|---|---|
| Q1 | ~~Gate flag `0x80` semantics.~~ **Resolved, corrected 2026-10-10**, see §5: it is the game option "Palace" (`BuildPalace`), toggled in the options menu — not "never cleared". The trigger must test `Game.Palace`. `0x100` **corrected 2026-10-14**: it is the "scoring completed" latch, set after the end-game flow — not dead code. Moved to [plan-endOfGameContinue.impl.md](plan-endOfGameContinue.impl.md). | — |
| Q2 | ~~Keep `_civilizationScore` and `IPlayerRestorable.CivilizationScore` as a loaded-value inbox?~~ **Decided: remove them.** Nothing reads the field once the getter is derived, and both save formats ignore it on load (§11). Phase 4 deletes the field, the `IPlayerRestorable` member, and the `FromDto` write. | — |
| Q3 | ~~Is the world-conquest condition tracked, or derived at score time?~~ **Decided: latched only** (D7, revised 2026-10-14, §10). The two forms agree when the condition first becomes true and diverge only if a civilization appears afterwards — reachable in CivOne through the editor, not in the original. The derived form and its setting were removed. | — |
| Q4 | ~~SVE slot mapping.~~ **Decided: SVE mirrors the original, YAML is free** (D8, §11). SVE writes the human player's score into slot 0 and zeroes the rest; YAML may carry per-player scores and collapses to that layout on SVE export. Cheap, because the original treats the whole block as volatile scratch. | — |

| Q5 | Is the interactive block at `CityWorker.cs:2561` reachable while the score routine measures? Gated only by `PlayerID == HumanPlayerID` in the rajko translation, with no visible `flag` test (§3). **Not blocking** — CivOne reproduces the rate, not the simulation, so no reachability answer changes our code. Reopen only if we ever decide to emulate the original's side effects. | — |

| Q6 | The latch address `0xb884` is also used as an indexed array base at `StartGameMenu.cs:1890` (`SI + 0xb884`). Whether that writes the same byte could not be established from the decompilation. **Not blocking** — if it does, the original has a latch that can be clobbered, which is a bug we would not reproduce anyway. | — |

| Q7 | ~~Historian mini palace: original or CivOneX?~~ **Decided 2026-10-10: A** (D11) — derive the level from `Σ (size + happy − unhappy)` at the real luxury rate. Phase 5. | — |

| Q8 | ~~Adopt 1-year calendar steps in 1750–1850 while a spaceship is in flight, and a truncated ETA?~~ **Decided 2026-10-10: yes, in its own plan** (D12, [plan-calendar.impl.md](plan-calendar.impl.md)). The score gate "arrival detected this turn" works with both calendars. | — |

| Q9 | `CityWorker.cs:3406` (OCO) as the accumulation site lies on the real turn path (`flag == 0`) by control flow only, not proven. Matters only if Q7 = A. **Resolved 2026-10-16:** confirmed in the original game — the Historian shows the AI palace from the per-turn sum (screenshot, see [plan-palace.impl.md](plan-palace.impl.md)). In the first turn after loading, players before the human keep score 0. | — |

The happiness model carries its own open points — the pending unhappiness refill reading, the luxury chain overlap and
wonder ownership — with the Phase 1 code (§14.2).

## 10. World-conquest condition, both forms (D7)

> **Superseded 2026-10-14:** only the latched form ships (`WorldConquestDelegate`). The derived delegate,
> `WorldConquestPredicate`, `WorldConquestMode` and the menu entry were removed. The analysis below is kept for
> reference; the "As built" paragraph and the test list describe the removed two-variant state.

The original latches a flag when the active-civilization mask equals `(1 << HumanPlayerID) | 1` — the human plus
barbarians, i.e. every other civilization destroyed. Deriving the same condition from the player list at score
time gives the same answer **at the moment it first becomes true**, but the two diverge afterwards:

| | latched | derived |
|---|---|---|
| last rival destroyed | true | true |
| a new civilization appears later (civil war, respawn, editor) | stays true | falls back to false |

CivOne has a map editor and may grow civil-war-style mechanics, so the divergence is reachable here even though
it is not in the original. Ship both, selectable, with the original's behaviour as the default.

Per `CLAUDE.md`'s delegate chapter this is interchangeable behaviour, so it becomes two delegate classes behind
one native delegate type:

```csharp
// Predicate over the current player set; no service, no locator.
public delegate bool WorldConquestPredicate(IPlayerGameState state);

internal sealed class LatchedWorldConquestDelegate { ... }   // sets once, never clears
internal sealed class DerivedWorldConquestDelegate { ... }   // recomputed from the player list each call
```

**As built (2026-10-10):** the latch must live as long as the game, so `Game` owns the predicate
(`Game.IsWorldConquered()`, created by `CivilizationScoreServiceFactory.CreateWorldConquestPredicate`) and evaluates it
at every turn wrap. `CivilizationScoreService` reads only `IScoreGameState.IsWorldConquered` and never decides which
variant it is. Setup > Patches > Game behavior menu > "World conquest bonus" selects `WorldConquestMode`
(added 2026-10-11). `Game` reads it once, so a change applies to the next started or loaded game.
The selection sits in `CivilizationScoreServiceFactory`, driven by one `ISettings` entry
(`WorldConquestMode`, default = latched, matching the original).

**The latch is runtime state, not saved state — decided 2026-10-03 on the evidence.** In the original the flag
is the DS variable `0xb884`, set at exactly one site (`StartGameMenu.cs:1104-1112`), never cleared, and **absent
from the save format**: the save and load routines walk `CivState` fields only, and `0xb884` does not appear in
`GameLoadAndSave.cs` — there is no raw dump of the data segment. What *is* saved is `ActiveCivilizations`
(`:769-773`, `:1366-1370`), so after a load the mask condition has to become true again before the latch is
re-set.

So CivOne keeps the `bool` in the latched delegate instance and adds **no** DTO field and **no** save entry.
The consequence has to be stated, because it is the whole difference between the two variants: **after a
save/load, "latched" behaves like "derived"** until the condition holds once more. That matches the original
exactly, and it is cheaper than the persistence Phase 4 would otherwise have needed.

Tests: both delegates against the same player sequence — destroy all rivals (both true), then add a
civilization (latched still true, derived false); the factory returns the latched one by default; a fresh
latched delegate starts `false` even for a player set that already satisfies the condition is **not** required —
the condition is evaluated on the first call, so assert that instead.

## 11. Save formats (D8)

**SVE is the constraint, YAML is not.** The rule: an SVE file CivOne writes must be loadable by the original, and
any YAML file must collapse to that same SVE layout when the player saves as SVE.

That is cheaper than it looks, because the original's score block is effectively **volatile scratch**:

- the turn loop zeroes each `Score` field right before that slot is processed (§2, OCO `GameEngine.cs:72-75`), and
- the achievements routine recomputes and overwrites slot 0 during the human player's palace check, every turn.

So whatever CivOne writes into the block is discarded by the original before anything reads it — the Historian
reads it only after the slot has been zeroed and rebuilt. SVE compatibility here means matching the *layout*, not
the values.

Concretely:

| Format | What we write |
|---|---|
| SVE | The original's shape: the human player's computed score in **slot 0** (the barbarian scratch slot the original's score routine uses), `0` in the other seven (slots 1..7: the city sum, Phase 5). 8 × `ushort` at offset 1864, unchanged. |
| YAML | Whatever is useful — a per-player score for every slot, computed live. No compatibility constraint. |
| YAML → SVE | Collapse: take the human player's score, write it to slot 0, zero the rest. One function, tested. |

On **load**, both formats ignore the stored value entirely: `IPlayer.CivilizationScore` is derived (Phase 4), so
nothing needs to round-trip. This is what makes the whole thing safe — we never depend on a field the original
treats as scratch.

## 12. Happiness model — see the separate plan

How the corrected `CityCitizenService` lands (parallel subclass, factory switch, the `virtual` commit, the
pending-unhappiness traps and the time-boxed cleanup) is documented in
the Phase 1 code (§14.2). Nothing in it is score-specific.

## 13. Test plan (summary)

- Happiness tests: [CityCitizenServiceImplTests.cs](../xunit/src/CityCitizenServiceImplTests.cs), [CityHappy.cs](../xunit/src/CityHappy.cs) (Phase 1).
- `CivilizationScoreServiceTests` — every term, arrival-year gate, BC/AD peace gate, `0` floor, conquest bonus
  semantics, saturation, null argument, wonder counting across capture and destruction (Phase 2).
- Rating / `LeaderOrderDelegate` — `score × (D + 1) / 50`, the 20 thresholds, no window at rating 0, the
  `score == 0` fallback (Phase 2.13).
- `HumanCivScorePalaceTriggerTests` — threshold across difficulties 0–5 (0–4 original, 5 = Deity),
  `n = UpgradeCount + 1`, boundaries, AI exclusion, null guards, Palace option off, at most one dialog per turn,
  and the never-loaded-game regression (Phase 3).
- `PeaceTurnsTests` — barbarian exception, no BC increment, reset turn counts again in the next turn, capture of
  an undefended city (decide per deviation 11).
- F9 screen — values come from the service only; layout per §15.8 (Phase 6).
- `WorldConquestDelegateTests` — latched vs. derived across "all rivals destroyed, then a civilization appears";
  factory default is latched (§10).
- `PlayerDtoMapperTest` — rewritten score assertion; `FromDto` no longer writes the score back (Phase 4).
- `SaveDataAdapterTests` — score block at offset 1864 round-trips per slot; the SVE collapse is pinned (human
  score in slot 0, zeros elsewhere), and nothing reads the block back on load (Phase 4, §11).
- Manual: F9 report, `TopLeaderScreen` and `HallOfFameScreen` show the same number for the same state; city
  screens still behave sensibly once the happiness plan's switch is on.

## 14. Code-Abgleich (Stand 2026-10-03)

Für die spätere Fortsetzung: das ist der Unterschied zwischen dem, was dieser Plan beschreibt, und dem, was im
Arbeitsbaum steht. Phase 1 ist erledigt und verifiziert; Phase 2–4 sind unberührt
([CivilizationScoreService.cs](../src/Services/CivilizationScoreService.cs) trägt noch CivOnes eigene Formel).
Alle Inputs aus der Tabelle in §1 existieren im Tree — mit der Ausnahme in 14.3.

**14.1 Toter Verweis — erledigt 2026-10-10.** `docs/plan-cityCitizenService.impl.md` wurde nie geschrieben, die
Arbeit ist im Code umgesetzt (14.2). Alle Links im Plan verweisen jetzt auf 14.2 bzw. den Code.

**14.2 Phase 1 ist real erledigt.** Verifiziert im Tree: `luxuryRate` ist von
[City.GetCitizenTypes](../src/City.cs#L1046) bis [CityCitizenService.LuxuryRate](../src/Screens/Services/CityCitizenService.cs#L102)
durchgezogen, `PendingUnhappinessRefill` existiert als Delegate, die Luxus-Kette entspricht der
Original-Reihenfolge. Phase 2 ist damit nicht mehr blockiert.

**14.3 `Game.PeaceTurns` gibt es nicht.** §1 führt es als Input, im Code existiert nur das private
`_peaceTurns` ([Game.cs:593](../src/Game.cs#L593)) und `SaveDataAdapter.PeaceTurns`. Der Score-Service braucht
einen Zugang über `IGame`/`IGameTurnQuery`, kein Feld auf dem Player.

**14.4 Peace-Zähler — geklärt, drei konkrete Korrekturen.** Die Prüfung vom 2026-10-03 hat die alte Annahme
des Plans umgedreht: das Original setzt den Zähler **bei jedem Angriff** zurück (Combat-Routine
`Segment_29f3.cs:798`), nicht erst bei einer Kriegserklärung. CivOnes `RegisterHostileAction` am `Confront`
([BaseUnit.cs:385](../src/Units/BaseUnit.cs#L385)) ist damit im Kern **richtig**. Was fehlt:

1. **Barbaren-Ausnahme.** Das Original überspringt den Reset, wenn einer der Beteiligten Slot 0 ist
   (`Segment_29f3.cs:794-798`). CivOne bricht die Friedensserie auch bei Barbarenkämpfen.
   `Game.RegisterHostileAction()` ([Game.cs:310](../src/Game.cs#L310)) ist parameterlos und kann die Ausnahme
   heute nicht treffen — es braucht die beiden beteiligten Spieler (oder mindestens „ist einer davon
   Barbar"). Aufrufer sind [BaseUnit.cs:354](../src/Units/BaseUnit.cs#L354) und
   [Nuclear.cs:37](../src/Units/Nuclear.cs#L37).
2. **Jahres-Gate auf dem Inkrement.** Das Original zählt nur bei `Year > 0` hoch
   (`Segment_1238.cs:208-209`); `AdvancePeaceTurns` ([Game.cs:593](../src/Game.cs#L593)) zählt immer.
3. **Nuke-Pfad und Einnahme einer leeren Stadt — entschieden 2026-10-10: beide setzen zurück.** Das Original
   setzt bei beiden nicht zurück; fachlich ergibt der Reset mehr Sinn (Deviation 9 und 11, §8). Die Regel lebt
   im `IPeaceTurnService` (§16) und **muss dort in der XML-Doc als bewusste Abweichung vom Original
   dokumentiert sein**, mit Verweis auf diesen Plan. Ein späterer Service kann das Original-Verhalten
   wiederherstellen.

Nicht zu korrigieren: der verzögerte Reset am Rundenende (Deviation 8, §8) und das Setzen des Diplomatie-
Kriegsbits, das im Original am selben Ort passiert und CivOne anders löst.

[PeaceTurnsTests.cs](../xunit/src/PeaceTurnsTests.cs) muss für 1 und 2 mitgezogen werden; neue Fälle: Angriff
gegen Barbaren bricht die Serie nicht, Inkrement in BC-Jahren findet nicht statt.

**14.5 Phase 3.2 ist gegenstandslos.** [HumanCivScorePalaceTrigger](../src/Services/Palace/HumanCivScorePalaceTrigger.cs)
hat keinen End-of-Game- und keinen Spaceship-Block mehr. Nichts zu entfernen; der Punkt ist abgehakt, damit er
nicht erneut gesucht wird. Offen bleiben dort nur die beiden Korrekturen aus §5.

**14.6 `IPlayerGameState` braucht `Difficulty`.** Die Schnittstelle
([IPlayerGameState.cs](../src/Services/Palace/IPlayerGameState.cs)) führt heute nur `CivilizationScore`,
`Palace`, `IsHuman`. Mit ihr muss `PlayerGameStateAdapter` ([Game.cs:430](../src/Game.cs#L430)) erweitert
werden — eine CC0-Datei, also so klein wie möglich halten. `Game.Difficulty`
([Game.cs:179](../src/Game.cs#L179)) ist bereits geklammert vorhanden. Zusätzlich braucht die Schnittstelle die
Spieloption `Palace` (§5). **Korrektur 2026-10-10:** Die Trigger-Schleife ([Game.cs:516](../src/Game.cs#L516))
braucht doch eine Änderung — sie läuft bei jedem Spielerwechsel, das Original nur einmal pro Runde (§5).

**14.7 Latched-Conquest-Flag — geklärt, keine Persistenz.** Die Prüfung vom 2026-10-03 hat die Frage
entschieden: der Latch des Originals liegt ausschließlich im RAM und steht in keinem Savegame (§10). CivOne
braucht also **kein** DTO-Feld und keinen Save-Eintrag — das `bool` bleibt in der latched Delegate-Instanz.
§10 ist entsprechend umgeschrieben, einschließlich der Folge, dass „latched" nach einem Laden wie „derived"
arbeitet, bis die Bedingung erneut zutrifft. Phase 4 wird dadurch kleiner, nicht größer.

**14.8 Spaceship-Term — geklärt.** Das Jahres-Gate ist eine **Gleichheit** gegen ein gespeichertes
ETA-Feld, keine `>=`-Prüfung, und es gibt zwei Null-Vorbedingungen sowie die `≥ 0`-Klammerung (§1, alle
belegt). CivOne leitet das Ankunftsjahr stattdessen aus Startjahr + Flugzeit ab
([Game.cs:551-555](../src/Game.cs#L551-L555)); das ist als Deviation 7 (§8) aufgenommen. **Korrektur
2026-10-10:** Ein Gleichheitsvergleich gegen das abgeleitete Jahr kann wegen `Ceiling` und 2-Jahres-Schritten
übersprungen werden. Für Phase 2.1 heißt das: Gate = „Ankunft des menschlichen Schiffs in diesem Zug erkannt“
(dieselbe Bedingung wie `CheckSpaceVitory`), `Population == 0` und `successRate == 0` früh aussteigen, Ergebnis
vor der Addition auf `0` klammern. Die `" possible"`-Anzeige des Originals in den anderen
Jahren ist eine UI-Eigenschaft und nicht Teil dieses Plans.

### Empfohlene Reihenfolge für die Fortsetzung

Phasen 1–7, Kalender und Weiterspielen sind umgesetzt (Stand 2026-10-16). Rest:

1. Manueller Check: F9, TopLeader und HallOfFame zeigen im Spiel identische Werte.

## 15. OpenCivOne check (2026-10-10)

Comparison of this plan against OpenCivOne (OCO, `src/Game/CodeObjects/`). The formula in §1 holds. The findings
below are worked into the sections above; this chapter keeps the evidence in one place.

**15.1 Rating and Famous Persons — high.**
- Rating = `score × (D + 1) / 50`, integer division (OCO `HallOfFame.cs:126, 219, 277`; `HallOfFameEntry.cs:17`).
- Leader selection `F3_0000_065d_ShowFamousPersonsWindow` (OCO `HallOfFame.cs:212-290`): for `i = 1..20`,
  `personIndex = i − 1` if `(i × i) / 3 < rating`. Thresholds: rating > 0, 1, 3, 5, 8, 12, 16, 21, 27, 33, 40, 48,
  56, 65, 75, 85, 96, 108, 120, 133. Capped at 19. No window when `personIndex < 0`.
- `score == 0` path: index `PalaceBuildCount / 2 − 1` (OCO `GameEngine.cs:456`), text "Sire, your fame now /
  exceeds even that of". Otherwise "Sire, your Civilization / Rating of N% exceeds even".
- Names, ascending (OCO `HallOfFame.cs:200-205`): Dan Quayle, Emperor Nero, Ferdinand Marcos, Neville Chamberlain,
  Louis the XVI, Kaiser Wilhelm, Eric the Red, Otto von Bismarck, **Simon Bolivar**, Vladimir Lenin,
  Charles De Gaulle, Cleopatra, Winston Churchill, Suleyman the Magnificent, Franklin Roosevelt, Shogun Tokugawa,
  Thomas Jefferson, King Charlemagne, Emperor Augustus, Solomon the Wise.
- Hall of Fame inserts an entry only when its rating is strictly greater (OCO `HallOfFame.cs:160-195`).
- CivOneX today: `score / 137 × 100` with rounding ([CivilizationScoreService.cs:69](../src/Services/CivilizationScoreService.cs#L69)),
  wiki thresholds and 19 names without Bolivar ([LeaderOrderDelegate.cs:49-67](../src/Screens/Reports/LeaderOrderDelegate.cs#L49-L67)).

**15.2 Palace trigger — high.** Flag `0x80` is the "Palace" game option; the check runs once per turn and only
with a palace city (§5).

**15.3 Spaceship gate — high.** Truncated ETA plus 1-year steps (1750–1850, while a ship is in flight) in the original; `Ceiling` plus
2-year steps in CivOneX. Gate on "arrival detected this turn" (§1, deviation 7, D12).

**15.4 Accumulation — medium-high.** Not dead; read by the Historian (§2, Phase 5, D11).

**15.5 Conquest bonus — medium.** `AIOpponentCount` is the setup opponent count (2–6, SVE field `OpponentCount`
@37820, OCO `StartGameMenu.cs:83-84`) → `Game.Competition − 1`. Replace when `bonus >= score` (OCO
`Overlay_20.cs:1552-1562`).

**15.6 Unhappy — medium.** Original `ActualSize + happy − unhappy` (OCO `Overlay_20.cs:1279-1284`); CivOneX
counts red shirts as unhappy, so the term is `Size + happy − (unhappy + redShirt)`.

**15.7 Peace counter — medium.** CivOneX `AdvancePeaceTurns` ([Game.cs:593-606](../src/Game.cs#L593-L606)) returns
after the reset. Fix: reset, then increment in the same wrap if `Year > 0`. Reset only on real combat (OCO
`AttackActions.cs:120-122, 339-342`); capture of an empty city does not reset (deviation 11).

**15.8 F9 screen — medium.** Original layout (OCO `Overlay_20.cs`):
- "<Nationality> Citizens (N)" with icons per city, in the order happy, content, unhappy, specialists.
- "<Nationality> Achievements (N)" with wonder icon and name in two columns (x += 152).
- Optional "SpaceShip: (N[ possible])".
- One combined line "Pollution: (−N)   Peace: (+N) Future Tech: (+N)", each part only when ≠ 0.
- "Total Score: N" or "Bonus Score: N".
- Score bar at y = 192, x = 35, 250 px grey (colour 8), cyan fill (colour 11), width `clamp(score / 4, 0, 250)`
  (`:1613-1623`).
- On overflow (y ≥ 186) the line height `Var_5116` is decremented and the screen redrawn (`:1600-1606`).

**15.9 Confirmed by OCO.** 40 % luxury reference (`TaxRate = 6 − Sci`, restored afterwards); wonders `+20` per
`WonderCityID[1..21]` with valid city, owner = player, destroyed city → 128; pollution `−10 × count` global;
peace `min(3n, 100)` only when `Year > 0 && n != 0`; future tech `+5`; order sum → floor at 0 → bonus;
`0x100` = "scoring completed" latch (corrected 2026-10-14, not dead); success rate always written to the human slot; latch RAM-only; SVE `Score[8]` after
`TaxRate[8]` matches `SaveData.CivilizationScore` @1864; palace threshold formula and `(short)score >= threshold`.

## 16. Architecture: replaceable rule services (D13)

Every rule this plan adds or corrects lives in a **service behind an interface**, created in a **factory** and injected through the constructor. The default implementation behaves like the original. A later service, a game-rule setting or
a plugin can swap one implementation without touching the callers.

### Services

| Rule | Interface | Default implementation | State |
|---|---|---|---|
| Score with its parts (for F9, palace, end of game) | `ICivilizationScoreService` → returns a `CivilizationScoreBreakdown` record | original formula §1 | exists, reworked (Phase 2) |
| Rating and Famous Persons | `ICivilizationRatingService` | `score × (D + 1) / 50`, `LeaderOrderDelegate` with the original list | new (Phase 2.13) |
| World conquest | `WorldConquestPredicate` (native delegate type) | latched / derived delegates | planned (§10) |
| Spaceship arrival and success rate | `ISpaceshipArrivalService` — `ArrivedThisTurn(player)`, `SuccessRate(player)` | today's CivOneX rule; switches to the truncated ETA with the calendar plan | new; takes the logic out of `Game.CheckSpaceVitory` and `SpaceShipScreenDataFactory` (Phase 2.2, 2.9) |
| Peace counter | `IPeaceTurnService` — `RegisterCombat(attacker, defender)`, `AdvanceTurn(year)`, `PeaceTurns` | barbarian exception, reset + increment in the same wrap, AD gate; **deviation:** nuke and capture of an undefended city reset too (XML doc) | new; takes the logic out of `Game.cs` (Phase 2.6–2.10) |
| Palace upgrade | `IPalaceUpgradeService` / `IPalaceUpgradeTrigger` | §5 threshold, Palace option, once per turn | exists, corrected (Phase 3) |
| Historian mini palace | `IHistorianPalaceService` (`PalaceFor(player)`) plus `IHistorianPalaceLevelService` (`CitySum`, `PalaceLevel`) | human: real palace; AI: generated from the city sum as in the original | new (Phase 5) |
| Calendar | `IGameCalendarService` | see [plan-calendar.impl.md](plan-calendar.impl.md) | exists, extended there |

### Rules for these services

- **New files, MIT.** `Game.cs`, `Player.cs`, `City.cs` (CC0) only call the services; no rule logic is added there.
- **Constructor injection, lazy fallback** — the pattern from `CLAUDE.md`, nothing more. Consumers take the
  interface as an optional constructor parameter and fall back lazily to the factory:

  ```csharp
  public PeaceTurnConsumer(IPeaceTurnService? peaceTurnService = null)
  {
      _peaceTurnService = peaceTurnService;
  }
  private IPeaceTurnService PeaceTurnService => _peaceTurnService ??= GameRulesServiceFactory.CreatePeaceTurnService();
  ```

  Unit tests pass a mock or a real instance through the constructor and never need a static class to build
  objects.
- **`new` for services only inside factory methods.** Delegates may be created with `new()` directly
  (`CLAUDE.md`, Delegate Pattern).
- **Static factories are allowed** (corrected 2026-10-10), like the existing `GameCalendarServiceFactory` or
  `PalaceUpgradeServiceFactory`. One factory serves related services (`CLAUDE.md`, Factories) — e.g. a
  `GameRulesServiceFactory` with `CreateRatingService()`, `CreatePeaceTurnService()`, … — or the existing
  factory of that area is extended. No interface for the factory and no composition root are introduced.
- **No static logic.** Rules and formulas are not put in static utility classes or static methods (`CLAUDE.md`,
  Code Smells); they live in service or `*Delegate` instances.
- **Inputs as interfaces, not singletons.** Services read `IGame` / `IPlayerGameState` / `ISettings` passed in,
  not `Game.Instance`, so they are testable without a running engine.
- **Replacing behaviour later** means the factory returns a different implementation (or a decorator around the
  default). Tests do not need this — they inject directly.
- **Pure parts as delegates.** Formulas without state (threshold, level function, rating) sit in `*Delegate`
  classes inside the service, so a replacement can reuse them.
- **Deliberate deviations are documented at the service.** When a default implementation departs from the
  original on purpose (e.g. the peace counter, §14.4), its XML doc says so in a `<remarks>` block: what the
  original does, what this service does, why, and a link to this plan. A replacement can then restore the
  original.
- **YAML stays structured and readable.** Every new value in YAML is a named field with a meaningful type
  (numbers, lists, small records, enums as names) and a `[Doc]` attribute — never a raw byte array, a bitmask
  copied from SVE or an opaque blob. SVE-only encodings (flag bits, fixed-size arrays, scratch slots) are
  translated in the SVE adapter and do not leak into YAML. Derived values are not stored — except as information for readers that is ignored on load, like
  `CivilizationScore` (Phase 4). If a field's
  YAML shape is unclear, it is settled in the persistence part of the plan that introduces the field.
- **Composition over subclassing.** A variant wraps the default (decorator) instead of inheriting from it, e.g. a
  calendar with different step rules wraps `GameCalendarService`.

### Plugins (out of scope here)

Today a plugin cannot replace a service: `Reflect.ApplyPlugins()` only applies civilization, leader and unit
modifications, and `IPlugin` carries metadata only. [PLUGIN_EXTENSION_PLAN.md](PLUGIN_EXTENSION_PLAN.md) already
defines the pattern — optional capability interfaces on top of `IPlugin`. A later `IPluginRulesProvider` can hand
out replacement services, which the factory then returns. This plan only guarantees that every rule sits
behind an interface and is created in one factory.
