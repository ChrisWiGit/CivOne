# YAML Persistierung – Vollständigkeitsanalyse

Vergleich des YAML-Speicherformats mit dem originalen binären Civ1-Savegame-Format (`SaveData`).

Stand: April 2026 (Branch `yaml-review`)

---

## 🔴 Kritische Lücken – Spielzustand geht verloren

### `CityDto` fehlt: `Food` und `Shields`

| Feld | Binär (`CityData`) | YAML (`CityDto`) | Auswirkung |
|------|--------------------|-----------------|------------|
| `Food` | ✅ vorhanden | ❌ fehlt | Stadtwachstum-Timer auf 0 zurückgesetzt |
| `Shields` | ✅ vorhanden | ❌ fehlt | Produktionsfortschritt geht verloren |

`Food` und `Shields` werden zwar in `GameStateDtoMapper.CreateCity()` bei der City-Instanzierung
gesetzt (`Food = sourceCity.Food`), aber da `CityDto` keine entsprechenden Felder hat, liefert
`RestorableCity.Food/Shields` immer `0`. Auch `CityDtoMapper.ToDto()` speichert sie nicht.

**Behebung:** `Food` und `Shields` in `CityDto` hinzufügen und in beiden Mapper-Richtungen
(`ToDto` / `FromDto`) übertragen.

---

## 🟡 Kleiner Verlust – UI betroffen, kein Gameplay-Bug

### `PlayerDto` fehlt: `StartX`

| Feld | Binär (`SaveData.StartingPositionX`) | YAML (`PlayerDto`) | Auswirkung |
|------|--------------------------------------|--------------------|------------|
| `StartX` | ✅ vorhanden | ❌ fehlt | Karten-Scroll-Position nach Laden falsch |

`Player.StartX` wird nur in `WorldMap.cs` für das initiale Karten-Scrolling genutzt.
Nach dem Laden aus YAML scrollt die Weltkarte ggf. an die falsche Ausgangsposition.

**Behebung:** `StartX` (als `short`) in `PlayerDto` ergänzen und in `PlayerDtoMapper`
beidseitig mappen.

---

## 🔵 Im Binary vorhanden, aber im Spiel **generell nicht implementiert**

Diese Felder existieren im Binärformat (`SaveData`), werden aber beim Laden **nicht**
ausgelesen und auch nicht vom Spiel aktiv genutzt. Es handelt sich also nicht um ein
YAML-spezifisches Problem, sondern um einen generellen Feature-Gap.

| Feld in `SaveData` | Zustand im Spiel-Code |
|--------------------|-----------------------|
| `Diplomacy[8*8]` | Nicht ausgelesen; nur `Embassies` pro Spieler vorhanden |
| `HumanContactTurns[8]` | Nicht implementiert |
| `PeaceTurns` | Nicht implementiert |
| `SpaceShips[1462]` + `SpaceShipPopulation` + `SpaceShipLaunchYear` | TODO-Kommentar in `City.cs`; Raumschiff-Bau ohne Persistenz |
| `PlayerFutureTech` | Nicht implementiert |
| `ScoreChart[8*150]` / `PeaceChart[8*150]` | Nicht ausgelesen |
| `EpicRanking[8]` | Nicht ausgelesen |
| `UnitsDestroyedBy[8*16]` / `UnitsLost[8*28]` | Nicht ausgelesen |
| `ContinentStrategy`, `ContinentDefense`, `ContinentAttack`, `ContinentCities` | KI-Daten, nicht ausgelesen |
| `StrategicLocation*` (Status/Policy/X/Y) | Nicht ausgelesen |
| `LandPathFinding[260]` | Nicht ausgelesen |
| `DebugSwitches` | Nicht ausgelesen |

---

## 🟢 YAML hat mehr als das Binary

Das YAML-Format speichert einige Daten, die im Binärformat **nicht direkt** vorhanden
sind oder daraus abgeleitet werden müssen:

| Feld | Binary | YAML | Anmerkung |
|------|--------|------|-----------|
| `Player.Anarchy` | ❌ nicht direkt | ✅ `PlayerDto.Anarchy` | Im Binary aus Government-Zustand abgeleitet |
| `Player.LuxuriesRate` | ❌ abgeleitet | ✅ `PlayerDto.LuxuriesRate` | Binary: `10 - TaxRate - ScienceRate` |
| `Player.CityNamesSkipped` | ❌ fehlt | ✅ `PlayerDto.CityNamesSkipped` | Zähler für nächsten Stadtnamen |
| `CityDto.VisibleSizes[]` | 1 Byte (nur Human) | ✅ Array pro Spieler | Feinere Kontrolle pro Spieler |
| `CityDto.WasInDisorder` | ❌ fehlt | ✅ vorhanden | Spielrunden-Zustandsflag |
| `CityDto.Specialists` | ❌ aus ResourceTiles | ✅ explizit | Direkter Zustand statt Bitmask |
| `GlobalWarmingDto` | 3 Felder | ✅ strukturiert | Klarer als Binary-Rohdaten |

---

## ✅ Vollständig implementiert (Binary ↔ YAML)

| Bereich | Status |
|---------|--------|
| `GameTurn`, `HumanPlayer`, `CurrentPlayer` | ✅ |
| `Difficulty`, `OpponentCount` | ✅ |
| `RandomSeed` / `TerrainSeed` (getrennt) | ✅ |
| `AnthologyTurn` | ✅ |
| Spieler: Gold, Science, TaxRate, ScienceRate, Government | ✅ |
| Spieler: Advances, Embassies, CurrentResearch | ✅ |
| Spieler: LeaderName, TribeName, TribeNamePlural | ✅ |
| Spieler: Palace | ✅ |
| Spieler: Explored / Visible (Tile-Sichtbarkeit) | ✅ |
| Städte: Position, Besitzer, Name, Größe | ✅ |
| Städte: Produktion, Gebäude, Wunder | ✅ |
| Städte: ResourceTiles, Status-Flags | ✅ |
| Städte: TradingCities, ContinentId | ✅ |
| Einheiten: Typ, Position, Goto, Status, Moves | ✅ |
| Einheiten: Veteran, Sentry, Fortify, Order | ✅ |
| Einheiten: HomeCity (via GUID) | ✅ |
| Karte: Terrain-Tiles, MapSeed | ✅ |
| `AdvanceOrigin` (Erstentdecker je Advance) | ✅ |
| `GameOptions` (8 Flags) | ✅ |
| `CityNames` (256 Einträge) | ✅ |
| `Wonders` (22 Einträge, Besitzer-City) | ✅ (in Binary/GameState, in YAML über City.Wonders) |
| `ReplayData` | ✅ (teilweise – nur CivilizationDestroyed vollständig) |
| `GlobalWarming` (Count, Pollution, Indicator) | ✅ |

---

## Umsetzungsplan: YAML-Vollständigkeit

Ziel: Alle Felder des binären Savegames in YAML speichern und laden,
auch wenn die Spiellogik dafür noch nicht implementiert ist.
Funktionen können später auf die gespeicherten Daten aufbauen.

---

### Stufe 1 – Kritisch (Datenverlust beim Speichern)

#### 1.1 `CityDto`: `Food` und `Shields` ergänzen

**Dateien:** `Model/CityDto.cs`, `Model/CityDtoMapper.cs`

- `CityDto`: zwei neue Properties `int Food` und `int Shields` hinzufügen
- `CityDtoMapper.ToDto()`: `Food = domain.Food, Shields = domain.Shields` eintragen
- `CityDtoMapper.FromDto()`: `RestorableCity` hat bereits `Food`/`Shields` –
  nur noch aus `dto.Food` / `dto.Shields` befüllen

---

### Stufe 2 – Einfach (einzelne Properties, ein paar Zeilen)

#### 2.1 `PlayerDto`: `StartX` ergänzen

**Dateien:** `Model/PlayerDto.cs`, `Model/PlayerDtoMapper.cs`

- `PlayerDto`: `short StartX { get; set; }` hinzufügen
- `PlayerDtoMapper.ToDto()`: `StartX = player.StartX`
- `PlayerDtoMapper.FromDto()`: `player.StartX = dto.StartX`  
  *(benötigt `StartX` in `IPlayerRestorable`)*

#### 2.2 `PlayerDto`: `HumanContactTurn` ergänzen

**Dateien:** `Model/PlayerDto.cs`, `Model/PlayerDtoMapper.cs`

- Entspricht `SaveData.HumanContactTurns[p]` (Countdown bis KI den Spieler kontaktiert)
- `PlayerDto`: `ushort HumanContactTurn { get; set; }` hinzufügen
- Mapper: Wert speichern/laden; `Player` braucht ein gleichnamiges internes Feld
- Spiellogik kann später implementiert werden

#### 2.3 `GameStateDto`: `PeaceTurns` ergänzen

**Dateien:** `Model/GameStateDto.cs`, `Model/GameStateDtoMapper.cs`, `src/Game.cs`

- Entspricht `SaveData.PeaceTurns`
- `GameStateDto`: `ushort PeaceTurns { get; set; }` hinzufügen
- `GameState`: gleiches Feld ergänzen
- `Game.GameStateSource.cs`: aus `_peaceTurns` (oder neuem Feld) befüllen
done 

#### 2.4 `PlayerDto`: `FutureTechCount` ergänzen

**Dateien:** `Model/PlayerDto.cs`, `Model/PlayerDtoMapper.cs`, `Model/GameStateDto.cs`, `Model/GameStateDtoMapper.cs`

- Neue Hauptquelle in YAML: `PlayerDto.FutureTechCount`
- Legacy-Kompatibilität: `GameStateDto.PlayerFutureTech` bleibt als Alias für den Human-Player erhalten
- Beim Einlesen alter YAML-Dateien kann `PlayerFutureTech` in `Players[HumanPlayer].FutureTechCount` übernommen werden
- Damit ist die Struktur schon heute für mehrere Human-Player vorbereitet

done
---

### Stufe 3 – Mittel (neue Felder mit Struktur, kein neues DTO nötig)

#### 3.1 `PlayerDto`: Diplomatischer Status (`Diplomacy`)

**Dateien:** `Model/PlayerDto.cs`, `Model/PlayerDtoMapper.cs`, `src/Player.cs`

- Entspricht `SaveData.Diplomacy[p * 8 + target]` – 8 Werte pro Spieler
- `PlayerDto`: Liste mit strukturierten Einträgen statt Dictionary, z. B.:

  - `List<DiplomacyEntryDto> Diplomacy`
  - `DiplomacyEntryDto`
    - `ushort TargetPlayerId`
    - `ushort RawFlags` (1:1 SaveData-Wert)
    - `DiplomacyDecodedDto Decoded` (optional; vorerst leer)
  - `DiplomacyDecodedDto`
    - **jetzt nur Platzhalter/Kommentar**, später für dekodierte Bit-Flags
    - geplanter Inhalt (später): `IsAtWar`, `HasPeaceTreaty`, `HasCeaseFire`, `HasContact`, etc.

  Vorteil: `RawFlags` bleibt verlustfrei kompatibel, `Decoded` kann später ohne Breaking Changes ergänzt werden.
- `Player`: internes `ushort[] _diplomacy = new ushort[8]` ergänzen
- Mapper: speichern/laden; Logik (Krieg, Frieden, Vendetta) folgt später

#### 3.2 `PlayerDto`: Statistik-Felder

**Dateien:** `Model/PlayerDto.cs`, `src/Player.cs`

Folgende Felder als einfache Arrays hinzufügen (je 28 Einträge für Einheitentypen):

| Property in `PlayerDto` | Entspricht | Typ |
|--------------------------|------------|-----|
| `UnitsLost` | `SaveData.UnitsLost[p*28..+28]` | `List<long>` (Mapper clamp → `ushort[28]`) |
| `UnitsDestroyedBy` | `SaveData.UnitsDestroyedBy[p*16..+16]` | `List<long>` (Mapper clamp → `byte[16]`) |
| `EpicRanking` | `SaveData.EpicRanking[p]` | `long` (Mapper clamp → `ushort`) |
| `MilitaryPower` | `SaveData.MilitaryPower[p]` | `long` (Mapper clamp → `ushort`) |
| `CivilizationScore` | `SaveData.CivilizationScore[p]` | `long` (Mapper clamp → `ushort`) |

DTO-seitig bewusst `long`/`List<long>` verwenden; Validierung und Bereichsgrenzen passieren zentral im Mapper per Clamp.

Spiellogik kann die Werte später befüllen und auswerten.

---

### Stufe 4 – Aufwändig (benötigt neue DTOs)

#### 4.1 SpaceShip-Daten pro Spieler

**Dateien:** `Model/SpaceShipDto.cs` (neu), `Model/PlayerDto.cs`, `Model/PlayerDtoMapper.cs`

- Entspricht `SaveData.SpaceShips[1462]` + `SpaceShipPopulation[8]` + `SpaceShipLaunchYear[8]`
- Neue Klasse `SpaceShipDto` mit strukturierten Feldern (Komponenten-Slots, Population, Startjahr)
- `PlayerDto`: `SpaceShipDto SpaceShip { get; set; }` hinzufügen
- Vorerst nur speichern/laden; Spiellogik (Apollo-Programm, Sieg) folgt separat

#### 4.2 Score- und Peace-Charts

**Dateien:** `Model/PlayerDto.cs` oder `Model/GameStateDto.cs`

- Entspricht `SaveData.ScoreChart[p*150..+150]` und `SaveData.PeaceChart[p*150..+150]`
- 150 Einträge pro Spieler (historische Punkteentwicklung über 150 Runden)
- `PlayerDto`: `byte[] ScoreHistory { get; set; }` und `byte[] PeaceHistory { get; set; }`
- Wird für den Endbildschirm und die Highscore-Kurven benötigt

---

### Stufe 5 – Optional (KI-Interna, niedriger Mehrwert)

Diese Daten werden ausschließlich von der KI intern berechnet und könnten
bei Bedarf rekonstruiert werden. Persistieren ist möglich, aber nachrangig.

| Feld | Beschreibung |
|------|--------------|
| `ContinentStrategy/Defense/Attack/Cities[p*16]` | KI-Kontinentalstrategie pro Spieler |
| `StrategicLocation*` (X/Y/Status/Policy) | Strategische Punkte pro Spieler |
| `LandPathFinding[260]` | Interner Pfadfindungs-Cache (rekonstruierbar) |
| `DebugSwitches` | Nur für Entwicklungszwecke relevant |

---

### Reihenfolge der Umsetzung

```
1.1  CityDto Food + Shields          ← sofort, Datenverlust-Fix
2.1  PlayerDto StartX                ← schnell, 5 Minuten
2.2  PlayerDto HumanContactTurn      ← schnell
2.3  GameStateDto PeaceTurns         ← schnell
2.4  GameStateDto PlayerFutureTech   ← schnell
3.1  PlayerDto Diplomacy             ← mittel, Player braucht neues Feld
3.2  PlayerDto Statistik-Felder      ← mittel, aber nur Arrays
4.1  SpaceShipDto                    ← aufwändig, eigenes DTO
4.2  Score/Peace-Charts              ← aufwändig, viele Daten
5.*  KI-Interna                      ← optional, bei Bedarf
```
