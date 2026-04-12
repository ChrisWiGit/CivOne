# Review: PlayerGuid + UnitsDestroyedBy-Umstellung

## Ziel

Diese Änderung bereitet die Persistenzstruktur auf stabilere, zukunftsfähige Referenzen vor:

1. **Stabile Spieler-Identität** über `PlayerGuid` (unabhängig von Listenpositionen).
2. **Beziehungsdaten zwischen Spielern** (`UnitsDestroyedBy`) zusätzlich **Guid-basiert** modellieren.
3. Legacy-/Indexstruktur weiterhin unterstützen, damit bestehende Pfade nicht brechen.

---

## Kurzfassung der Änderung

- `Player` hat jetzt eine persistierbare `PlayerGuid`.
- `UnitsDestroyedBy` wurde intern von `byte[16]` auf **`ushort[8]`** umgestellt (pro Zielspieler ein Zähler).
- `PlayerDto` enthält zusätzlich eine neue Guid-basierte Struktur:
  - `UnitsDestroyedByByPlayerGuid: Dictionary<Guid, long>`
- Beim Laden wird Guid-basiertes Mapping auf die interne Indexstruktur aufgelöst.
- Beim Speichern wird aus der internen Indexstruktur wieder zusätzlich die Guid-Map erzeugt.

---

## Was genau wurde geändert

## 1) DTO-Schicht

### Datei
- [src/Persistence/Model/PlayerDto.cs](../src/Persistence/Model/PlayerDto.cs)

### Änderungen
- Neues Feld `PlayerGuid` hinzugefügt.
- Neues Feld `UnitsDestroyedByByPlayerGuid` hinzugefügt.
- Bestehendes `UnitsDestroyedBy` beibehalten (kompatibler Indexpfad).

### Warum
- `Id` ist positionsabhängig (`Players`-Liste), `PlayerGuid` ist stabil.
- Für spätere flexible Spieleranzahl sind Guid-basierte Querverweise robuster.
- Parallelbetrieb (Index + Guid) reduziert Migrationsrisiko.

---

## 2) Domain-/Persistenz-Contracts

### Dateien
- [src/Persistence/Model/IPlayer.cs](../src/Persistence/Model/IPlayer.cs)
- [src/Persistence/Model/IPlayerRestorable.cs](../src/Persistence/Model/IPlayerRestorable.cs)

### Änderungen
- `PlayerGuid` in `IPlayer`/`IPlayerRestorable` ergänzt.
- `UnitsDestroyedBy` Typ von `byte[]` auf `ushort[]` geändert.

### Warum
- Einheitliche Verfügbarkeit der stabilen Identität im Mapping.
- `UnitsDestroyedBy` ist als pro-Spieler-Zähler modelliert; `ushort` ist der passende Zählertyp.

---

## 3) Runtime-Modell `Player`

### Dateien
- [src/Player.cs](../src/Player.cs)
- [src/Persistence/Game/Player.Persistence.cs](../src/Persistence/Game/Player.Persistence.cs)

### Änderungen
- Backing-Field `_playerGuid` ergänzt (`Guid.NewGuid()` als Default).
- `_unitsDestroyedBy` auf `ushort[8]` umgestellt.
- Explizite Getter/Setter über `IPlayer`/`IPlayerRestorable` angepasst.

### Warum
- `PlayerGuid` muss im Runtime-Modell vorhanden sein, um Persistenzrunden stabil zu halten.
- Beziehungszähler über Spielerpaare sollten nicht als rohe Byte-Slots geführt werden.

---

## 4) Player-Mapping

### Datei
- [src/Persistence/Model/PlayerDtoMapper.cs](../src/Persistence/Model/PlayerDtoMapper.cs)

### Änderungen
- `PlayerGuid` wird aus DTO übernommen (bei leerer Guid wird eine neue erzeugt).
- Beim Schreiben wird `PlayerGuid` in DTO zurückgegeben.
- `BuildUnitsDestroyedByArray(...)` auf `ushort[8]` umgestellt.
- Clamp für `UnitsDestroyedBy` jetzt auf `0..ushort.MaxValue`.

### Warum
- Identität muss beim Roundtrip stabil sein.
- Zählwerte sollten nicht unnötig früh durch `byte` limitiert werden.

---

## 5) GameState-Mapping (Guid ↔ Index)

### Datei
- [src/Persistence/Model/GameStateDtoMapper.cs](../src/Persistence/Model/GameStateDtoMapper.cs)

### Änderungen
- Neue Auflösung nach `MapPlayers(...)`:
  - `ResolveUnitsDestroyedByByPlayerGuid(dto, players)`
- Beim Lesen:
  - `UnitsDestroyedByByPlayerGuid` wird über `PlayerGuid` auf Zielindex gemappt.
  - Werte werden auf interne `ushort[]` geschrieben (mit Clamp).
- Beim Schreiben:
  - Für jeden Spieler wird zusätzlich `UnitsDestroyedByByPlayerGuid` erzeugt.

### Warum
- Die Guid→Index-Auflösung braucht **alle Spieler gleichzeitig**; daher gehört das in den GameState-Mapper.
- So bleibt das Player-Mapping lokal einfach und der Bezug über Spieler hinweg korrekt.

---

## 6) Tests und Mocks

### Dateien
- [xunit/src/Mocks/MockedIPlayer.cs](../xunit/src/Mocks/MockedIPlayer.cs)
- [xunit/src/persistence/Model/PlayerDtoMapperTest.cs](../xunit/src/persistence/Model/PlayerDtoMapperTest.cs)
- [xunit/src/persistence/Model/GameStateDtoMapperTest.cs](../xunit/src/persistence/Model/GameStateDtoMapperTest.cs)

### Änderungen
- Mock unterstützt `PlayerGuid` und `ushort[] UnitsDestroyedBy`.
- Player-Mapping-Test um `PlayerGuid` erweitert; `UnitsDestroyedBy`-Fixtures angepasst (8 statt 16, `ushort`).
- GameState-Mapping-Test um Guid-basierte `UnitsDestroyedByByPlayerGuid`-Daten erweitert.

### Warum
- Tests verifizieren, dass sowohl die neue Identität als auch die Guid-Map korrekt roundtript.

---

## Kompatibilität / Verhalten

- **Indexpfad bleibt vorhanden** (`UnitsDestroyedBy: List<long>`), damit bestehende YAML-Strukturen nicht sofort brechen.
- **Neue bevorzugte Struktur** ist Guid-basiert (`UnitsDestroyedByByPlayerGuid`).
- Bei koexistierenden Daten gilt:
  - Guid-basierte Auflösung wird auf die interne Struktur gemappt.
  - Clamping schützt vor Überläufen im Legacy-nahen Runtime-Modell.

---

## Warum diese Architektur gewählt wurde

1. **Stabilität:** Guid ist robust gegen Reordering der `Players`-Liste.
2. **Inkrementelle Migration:** Alter und neuer Pfad können parallel leben.
3. **Klare Verantwortlichkeit:** Cross-Player-Auflösung findet auf GameState-Ebene statt.
4. **Sicherheit:** Werte werden an den Grenzen geklemmt, intern aber weniger eng geführt.

---

## Teststatus

Ausgeführt:
- `dotnet test ... --filter "FullyQualifiedName~PlayerDtoMapperTest|FullyQualifiedName~GameStateDtoMapperTest"`

Ergebnis:
- **4/4 Tests erfolgreich**.

---

## Offene Folgepunkte (optional)

- Doku in [docs/YAML_SAVE_COMPLETENESS.md](YAML_SAVE_COMPLETENESS.md) ergänzen (Guid-Map als bevorzugtes Schema).
- Optional: zusätzliche Merge-Regel dokumentieren, falls Indexliste und Guid-Map gleichzeitig inkonsistent sind.
