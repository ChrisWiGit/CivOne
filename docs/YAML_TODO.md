# YAML Persistence – Offene Punkte (TODO)

Stand: April 2026

---

## 🔴 Hohe Priorität – Offene Lücken in der YAML-Pipeline

### 1. ReplayData – fehlende Subklassen (7 Typen)

In `api/src/ReplayData.cs` existieren nur 3 Subklassen:
- `CityBuilt`
- `CityDestroyed`
- `CivilizationDestroyed`

Im `ReplayDataDtoMapper.cs` sind 7 weitere Typen auskommentiert, aber noch nicht als Domain-Klassen implementiert:

| Typ | Status |
|-----|--------|
| `WarDeclared` | ❌ Fehlt |
| `PeaceMade` | ❌ Fehlt |
| `AdvanceDiscovered` | ❌ Fehlt |
| `UnitFirstBuilt` | ❌ Fehlt |
| `GovernmentChanged` | ❌ Fehlt |
| `WonderBuilt` | ❌ Fehlt |
| `ReplaySummary` | ❌ Fehlt |
| `CivRankings` | ❌ Fehlt |
| `CityCaptured` | ❌ Fehlt |

**Problem:** Beim Laden einer YAML-Datei, die aus einem Binary-Save stammt (und komplexere Partien enthält), wirft `ReplayDataDtoMapper.FromDto()` eine `NotYetImplementedException`. Das blockiert echten Roundtrip.

**Lösung:**
1. Fehlende Subklassen in `api/src/ReplayData.cs` ergänzen.
2. Auskommentierte Blöcke in `ReplayDataDtoMapper.ToDto()` und `FromDto()` aktivieren.

---

### 2. ReplayData in GameStateHandler – Heap Corruption

In `src/Persistence/GameStateHandler.cs` ist folgende Zeile auskommentiert:

```csharp
// ReplayData = [.. game.ReplayData]  // TODO: CW: Produces Heap Corruption. App stops 0xc0000374
```

**Problem:** ReplayData wird beim YAML-Save gar nicht geschrieben – YAML-Saves verlieren alle Replay-Informationen.

**Aktion:** Ursache der Heap Corruption analysieren (vermutlich unsafe struct / fixed buffer Zugriff in `SaveData`). Nach Fix aktivieren und mit Roundtrip-Test absichern.

---

## 🟡 Mittlere Priorität – Qualität / Vollständigkeit

### 3. Spaceship – Roundtrip-Tests und Clamp-Randfälle

Laut `YAML_LEGACY_TRACEABILITY_CHANGES.md` fehlen noch Tests für:

- SpaceShip YAML → Runtime → YAML Roundtrip
- Clamp-Verhalten bei `Population` (`ushort`-Grenze) und `LaunchYear` (`short`-Grenze)
- Legacy-Kompatibilität: Grid-Symbol `0` wird als `E` (Empty) interpretiert

**Dateien:** `src/Persistence/Model/SpaceShipDto.cs`, `src/Persistence/Mapper/PlayerDtoMapper.cs`

---

### 4. `MovesSkip` im Binary-Save (Settlers)

In `src/Extensions.cs` steht:

```csharp
// TODO need to save (Settlers.)MovesSkip value to savefile
```

**Problem:** YAML roundtripped `MovesSkip` korrekt (über `UnitDto.MovesSkip`), aber Binary-Save nicht. Settlers verlieren beim Binary-Reload ihren Warte-Zustand.

**Aktion:** `MovesSkip`-Wert in den `UnitData`-Export für den SVE-Pfad einbauen (z.B. via `SpecialMoves`-Feld oder separates Mapping).

---

### 5. Manueller Smoke-Test `.cos` → UI

Laut `YAML_LOAD_IMPLEMENTATION.md` wurde dieser Test noch nicht durchgeführt:

> *Manual smoke test – Load 12.cos via UI, validate display*

**Aktion:**
1. Eine `.cos`-Datei in `{SavesDirectory}/{drive}/` ablegen.
2. Über LoadGame-UI laden.
3. Prüfen: Karte, Einheiten, Städte, Spieler korrekt angezeigt.
4. Ein paar Züge spielen und erneut speichern (YAML-Roundtrip).

---

### 6. `BinarySaveGameStateWriter` – komplett unimplementiert

`src/Persistence/BinarySaveGameStateWriter.cs` ist vollständig auskommentiert. Falls ein Binary-Export aus dem `GameState`-Modell geplant ist (z.B. für YAML→SVE Konvertierung), fehlt diese Implementierung.

**Priorität:** Nur relevant, wenn Binary-Export aus YAML-State benötigt wird.

---

## 🟢 Niedrige Priorität – Optional / Rekonstruierbar

| Item | Beschreibung |
|------|-------------|
| `ScoreChart[8*150]` / `PeaceChart[8*150]` | Historische Score-/Peace-Kurven für Endscreen |
| `ContinentStrategy/Defense/Attack/Cities` | KI-Daten, rekonstruierbar |
| `StrategicLocation*` (X/Y/Status/Policy) | KI-Strategiepunkte, rekonstruierbar |
| `LandPathFinding[260]` | Pathfinding-Cache, rekonstruierbar |
| `DebugSwitches` | Development-only |

---

## Empfohlene Reihenfolge

1. **ReplayData Heap Corruption** (#2) – zuerst analysieren, da es das Save blockiert
2. **ReplayData Subklassen** (#1) – dann die fehlenden Typen implementieren
3. **Manueller Smoke-Test** (#5) – validiert die komplette Pipeline
4. **Spaceship Roundtrip-Tests** (#3) – Qualitätssicherung
5. **MovesSkip Binary-Save** (#4) – kleine Lücke im Binary-Pfad
