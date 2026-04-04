# Analyse: GameState ↔ DTO Mapping (für Prompting)

Stand: 2026-04-04  
Branch: `yaml`

## Kurzfazit

Der `GameStateDtoMapper` ist **nicht allein das Problem**. Die eigentlichen Datenverluste entstehen vor allem in der Save-Pipeline.

## Hauptbefunde

### 1) Units gehen schon vor dem Mapper verloren

**Datei:** `src/Persistence/GameStateHandler.cs`  
In `Create(IGameSnapshotSource game)` wird `GameState` gebaut, aber `Units` nicht gesetzt.

- Vorhanden: `Cities = game.Cities`
- Fehlend: `Units = game.Units`

**Folge:** `GameState.Units` ist `null`/leer, daher kommen in `GameStateDtoMapper.ToDto(...)` pro Player keine Units an.

---

### 2) Der produktive Save-Pfad nutzt nicht den GameStateDtoMapper

**Datei:** `src/Persistence/YamlSaveGameStateWriter.cs`  
`Write(...)` ruft ein lokales `toDto(snapshot)` auf. Diese Methode ist aktuell nur ein Stub und mappt nur wenige Felder.

- `Players` wird nicht gemappt
- damit indirekt auch keine `Cities`/`Units` in den Playern

**Folge:** Selbst wenn `GameStateDtoMapper` korrekt wäre, wird er im aktuellen Save-Pfad nicht verwendet.

---

### 3) CurrentPlayer-Konzept nicht im DTO abgebildet

**Datei:** `src/Persistence/Model/GameStateDto.cs`  
`GameState` kennt `CurrentPlayer`, `GameStateDto` aber nicht.

**Folge:** Nach Roundtrip ist nicht eindeutig reproduzierbar, welcher Spieler am Zug war.

---

### 4) Seed-Zuordnung wahrscheinlich inkonsistent

**Datei:** `src/Persistence/GameStateHandler.cs`  
`RandomSeed` und `TerrainSeed` werden beide aus `game.TerrainMasterWord` befüllt.

**Folge:** Mögliche Vermischung von RNG-Seed und Terrain-Seed.

---

## Bereits korrigiert

**Datei:** `src/Persistence/Model/GameStateDtoMapper.cs`  
In `ToDto(GameState gameState)` werden `player.Units` jetzt aus `gameState.Units` gefiltert (`Owner == player.Id`) gesetzt.

**Wichtig:** Dieser Fix hilft nur, wenn `gameState.Units` vorher korrekt befüllt wurde und der Mapper auch wirklich im Save-Pfad verwendet wird.

---

## Empfohlene Reihenfolge für Umsetzung

1. `GameStateHandler.Create(...)` um `Units = game.Units` ergänzen.  
2. `YamlSaveGameStateWriter` auf `GameStateDtoMapper` umstellen (statt lokalem Stub-`toDto`).  
3. Entscheiden, ob `CurrentPlayer` ins `GameStateDto` aufgenommen wird (z. B. `CurrentPlayerId`).  
4. Seed-Semantik klären (`RandomSeed` vs. `TerrainSeed`) und konsistent mappen.  
5. Integrationstest ergänzen: `Game -> GameStateHandler -> GameStateDtoMapper -> YAML` mit Assert auf Player-Units.

---

## Prompt-Vorlage (Copy/Paste)

```text
Bitte behebe die Save-Pipeline für YAML so, dass GameState und GameStateDto vollständig konsistent sind.

Kontext:
- In GameStateHandler.Create(...) fehlen derzeit die Units im GameState.
- YamlSaveGameStateWriter nutzt aktuell einen lokalen toDto-Stub statt GameStateDtoMapper.
- CurrentPlayer ist im Domain-Modell vorhanden, aber nicht im DTO.
- RandomSeed/TerrainSeed-Zuordnung ist zu prüfen.

Aufgaben:
1) Ergänze in GameStateHandler.Create(...): Units = game.Units.
2) Ersetze in YamlSaveGameStateWriter den Stub durch Nutzung von GameStateDtoMapper.
3) Falls sinnvoll: erweitere GameStateDto um CurrentPlayerId und mappe in beide Richtungen.
4) Prüfe und korrigiere Seed-Mapping (RandomSeed vs TerrainSeed).
5) Ergänze/aktualisiere Tests für den kompletten YAML-Save-Pfad inkl. Units pro Player.

Akzeptanzkriterien:
- YAML enthält Players mit korrekten Units.
- Roundtrip verliert keine Units.
- Tests sind grün.
```
