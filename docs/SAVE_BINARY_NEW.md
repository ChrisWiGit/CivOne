# Binary Save Writer Plan

Stand: April 2026

## Ziel

`BinarySaveGameStateWriter` soll einen `GameState` wieder in das Legacy-SVE-Binaryformat schreiben.

Das Ziel ist nicht, die YAML-DTOs direkt als Binaryformat zu verwenden, sondern den vorhandenen Persistenz-Stack sauber zu erweitern:

1. Runtime `Game`
2. `GameState`
3. optional `GameStateDto` fuer YAML
4. `IGameData` / `SaveDataAdapter` fuer Binary

## Kurzfazit zur Architektur

Die vorhandenen YAML-DTOs koennen als fachliches Zwischenmodell auch fuer Legacy-Export dienen, aber sie sind kein 1:1-Abbild des Binaryformats.

Warum nicht direkt DTO -> Binary:

- DTOs enthalten GUID-basierte Referenzen, das Binaryformat arbeitet mit Slots und Indizes.
- DTOs sind semantisch und variabel lang, das Binaryformat ist fest gepackt und groessenbegrenzt.
- DTOs modellieren einige Daten reicher als das Binaryformat, zum Beispiel Trading-Cities, Home-City-Referenzen, Replay-Eintraege und Statusinformationen.
- Einige Binaryfelder existieren nur als flache Bitfelder oder kompakte Arrays und muessen aus Runtime-/DTO-Zustand abgeleitet werden.

Deshalb sollte der Binarywriter primaer auf `GameState` arbeiten. DTOs bleiben ein YAML-Transportmodell.

## Empfehlung

`BinarySaveGameStateWriter` sollte `GameState -> IGameData` abbilden.

Nicht empfohlen:

- `GameStateDto -> IGameData` als primarer Pfad
- Rueckkehr zu `GameState2`
- ein zweites paralleles Legacy-Snapshotmodell

Begruendung:

- `GameState` ist bereits das gemeinsame fachliche Snapshotmodell.
- YAML nutzt heute schon `GameState -> GameStateDto`.
- Binary kann denselben `GameState` als Eingabe verwenden und dann gezielt in `SaveDataAdapter` uebertragen.
- Damit bleibt die Trennung klar: fachlicher Zustand vs. Serialisierungsformat.

## Bestehende Bausteine

Bereits vorhanden:

- `GameStateHandler.Create(...)` fuer Runtime -> `GameState`
- `YamlSaveGameStateWriter` fuer `GameState` -> YAML
- `SaveDataAdapter` als konkrete `IGameData`-Implementierung fuer SVE-Binary
- `SveSaveCompatibilityService` fuer harte Binary-Grenzen und Inkompatibilitaeten

Noch unvollstaendig:

- `BinarySaveGameStateWriter`

## Zielbild fuer den Binary-Pfad

Gewuenschter Datenfluss:

1. `Game` oder YAML-Load liefert `GameState`
2. `BinarySaveGameStateWriter.Write(Stream, GameState)` validiert den Snapshot fuer SVE
3. Writer mappt `GameState` nach `IGameData` (`SaveDataAdapter`)
4. `SaveDataAdapter.GetBytes()` liefert den finalen SVE-Blob
5. Blob wird in den Stream geschrieben

## Umsetzungsphasen

### Phase 1: Writer-Skelett produktiv machen

Ziel:

- `BinarySaveGameStateWriter` darf nicht mehr nur aus auskommentiertem Code bestehen.

Aufgaben:

- Null-Checks fuer `stream` und `snapshot`
- `SaveDataAdapter` instanziieren
- die bereits klaren Scalar-Felder direkt setzen
- Ergebnis ueber `GetBytes()` in den Stream schreiben

Felder mit direkter, risikoarmer Abbildung:

- `GameTurn`
- `Difficulty`
- `PeaceTurns`
- `PlayerFutureTech`
- `ReplayData`
- `GlobalWarmingCount`
- `PollutedSquaresCount`
- `WarmingIndicator`

Hinweis:

- `HumanPlayer`, `CurrentPlayer`, `Players`, `Cities`, `Units` muessen vorher auf Legacy-Indizes reduziert werden.

### Phase 2: `GameState` -> `IGameData` Mapping explizit definieren

Ziel:

- Alle Binary-relevanten Felder werden aus `GameState` systematisch abgeleitet.

Aufgaben:

- Hilfsmethode fuer Human-Player-Index erstellen
- Hilfsmethode fuer per-Player Arrays erstellen
- Hilfsmethode fuer Wonder-Ownership erstellen
- Hilfsmethode fuer Advance-Origin in `ushort[72]`
- Hilfsmethode fuer Legacy-GameOptions-Reihenfolge

Kernmappings:

- `GameState.HumanPlayer` -> `IGameData.HumanPlayer`
- `GameState.RandomSeed` -> `IGameData.RandomSeed`
- `Players[]` -> Leader-/Citizen-/Civilization-Namen, Gold, Science, Rates, Government, StartX, ContactTurn
- `Cities` -> `CityData[]`
- `Units` -> `UnitData[][]`
- `MapTiles` bzw. Sichtbarkeiten -> `TileVisibility`
- `AdvanceOrigin` -> `AdvanceFirstDiscovery`
- `ReplayData` -> `ReplayData[]`

### Phase 3: Legacy-spezifische Reduktionen absichern

Ziel:

- Alles, was im DTO/Runtime reicher modelliert wird als im Binaryformat, wird bewusst reduziert und dokumentiert.

Explizit zu behandeln:

- GUID-Referenzen fuer Staedte und Units muessen auf Legacy-Slots/Indices gemappt werden
- Trading-Cities muessen auf maximal 3 Eintraege begrenzt bleiben
- Fortified Units muessen in die Legacy-City-Slots passen
- Players muessen im Bereich `0..7` bleiben
- City-Count `<= 128`
- ReplayData `<= 4096` Bytes
- Map `80 x 50`

Diese Regeln existieren fachlich bereits im `SveSaveCompatibilityService` und sollten vor dem eigentlichen Schreiben verwendet oder gespiegelt werden.

### Phase 4: Integration mit Kompatibilitaetspruefung

Ziel:

- Binary-Export faellt frueh und klar mit Fehlertexten aus, statt implizit Daten zu verlieren.

Aufgaben:

- Binarywriter soll vor dem Schreiben einen Kompatibilitaets-Snapshot aus `GameState` bewerten
- Bei Inkompatibilitaet `InvalidOperationException` mit klarer Ursache werfen

Wichtige bestehende Regeln:

- YAML-geladene Spiele sind aktuell nicht SVE-kompatibel
- mehr als 8 Spieler sind nicht erlaubt
- ReplayData ueber 4096 Bytes ist nicht erlaubt
- ungueltige City-/Unit-Referenzen sind nicht erlaubt
- Out-of-Bounds-Koordinaten sind nicht erlaubt

### Phase 5: Tests zuerst auf Writer-Ebene

Ziel:

- Der neue Writer wird ueber kleine, isolierte Snapshot-Tests abgesichert.

Empfohlene Testfaelle:

1. Minimaler Snapshot schreibt erfolgreich einen nicht-leeren Binary-Stream
2. `ReplayData.CivilizationDestroyed` wird korrekt geschrieben
3. ReplayData > 4096 Bytes fuehrt zu sauberem Fehler
4. Mehr als 8 Spieler fuehren zu sauberem Fehler
5. Trading-City-Overflow fuehrt zu sauberem Fehler
6. Ungueltige Unit-HomeCity-Referenz fuehrt zu sauberem Fehler
7. Binary-Write -> `SaveDataAdapter.Load(...)` roundtrip fuer Kernfelder

### Phase 6: Optionaler DTO-gestuetzter Binary-Export

Ziel:

- Nur wenn wirklich benoetigt: ein zusaetzlicher Pfad `GameStateDto -> GameState -> Binary`.

Empfehlung:

- Kein separater DTO -> Binary-Writer
- Stattdessen YAML-Load nutzt weiter `GameStateDtoMapper.FromDto(...)` nach `GameState`
- Binary-Export startet immer ab `GameState`

Damit bleibt genau eine Binary-Schreiblogik im System.

## Feldgruppen: direkt nutzbar vs. Transformationsbedarf

### Direkt oder fast direkt nutzbar

- Difficulty
- GameTurn
- HumanPlayer-Index
- GameRandomSeed/RandomSeed, sofern auf `ushort` validiert
- AnthologyTurn
- PeaceTurns
- PlayerFutureTech
- ReplayData
- GlobalWarming

### Mit strukturellem Mapping

- Players -> per-Player Arrays
- Cities -> `CityData[]`
- Units -> `UnitData[][]`
- Wonders -> Wonder-Owner-City-Array
- AdvanceOrigin -> `ushort[72]`
- Visibility -> `bool[8][80,50]`
- GameOptions -> Legacy-Positionsarray

### Mit Reduktions-/Verlustentscheidung

- GUID-basierte Referenzen
- DTO-Felder, die das Binaryformat nur implizit oder kompakter kennt
- DTO-Daten, die es im Binaryformat gar nicht gibt

## Offene Architekturentscheidungen

1. Seed-Semantik:
   Binary kennt nur den Legacy-Seed-Slot. Es muss festgelegt sein, ob dort `GameRandomSeed` oder `TerrainSeed` landet. Aktuell ist der Runtime-SVE-Pfad an den Legacy-Seed gebunden.

2. CurrentPlayer:
   `IGameData` kennt nur `HumanPlayer`. Wenn `CurrentPlayer != HumanPlayer` in YAML/Runtime erlaubt ist, muss fuer Binary klar sein, wie dieser Zustand degradiert wird.

3. YAML-Quelle -> SVE:
   Der Kompatibilitaetsservice verbietet das aktuell. Falls Binary-Export aus YAML wirklich unterstuetzt werden soll, muss diese Policy bewusst geaendert werden.

4. ReplayData-Typen:
   Lesen unterstuetzt mehrere Typen, Schreiben derzeit nur `CivilizationDestroyed`. Vor einem vollwertigen Binarywriter muss klar sein, welche ReplayData-Typen wirklich exportiert werden sollen.

## Konkreter Implementierungsplan fuer den naechsten Schritt

1. `BinarySaveGameStateWriter` entkommentieren und mit Null-Checks plus `GetBytes()` aktivieren.
2. Kleine private Mapper-Helfer direkt in `BinarySaveGameStateWriter` einfuehren.
3. Snapshot-Kompatibilitaet vor dem Schreiben evaluieren.
4. Fokus zuerst auf die Felder legen, die der aktuelle Runtime-SVE-Save schon direkt ueber `Game.Save(...)` schreibt.
5. Danach Writer-Tests bauen.
6. Erst danach optional den Writer an hoehere Services anbinden.

## Nicht empfohlen

- `GameState2` wieder einfuehren
- DTOs zum zweiten fachlichen Kernmodell machen
- Binarywriter direkt an YAML-DTOs koppeln
- inkompatible Snapshot-Zustaende stillschweigend zu truncaten oder zu erraten

## Zusammenfassung

Ja, die DTOs koennen auch fuer das Legacyformat genutzt werden, aber nur indirekt.

Die richtige Struktur ist:

- `GameState` als gemeinsames fachliches Snapshotmodell
- `GameStateDto` nur fuer YAML-Serialisierung
- `SaveDataAdapter` als Binary-Backend
- `BinarySaveGameStateWriter` als expliziter Adapter `GameState -> IGameData`

Damit wird kein altes Legacy-Modell mehr benoetigt, und YAML- sowie Binary-Export teilen sich denselben fachlichen Ausgangspunkt.