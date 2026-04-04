# Analyse: YAML Load-Pipeline (Lesen)

Stand: 2026-04-05  
Branch: `yaml`

---

## Überblick

Die Save-Pipeline (`Game → GameStateHandler → GameStateDtoMapper.ToDto → YamlWriter`) ist fertig und getestet.  
Die **Load-Pipeline** (`YamlReader → GameStateDto → GameStateDtoMapper.FromDto → GameState → Game`) fehlt noch vollständig in der Runtime-Schicht.

Die Mapper-Logik in `GameStateDtoMapper.FromDto` ist bereits vorhanden und getestet – aber alle Factories darin sind `NotSupportedXxxFactory`-Stubs, die beim Laden eine `NotSupportedException` werfen würden.

---

## Was bereits vorhanden ist

| Komponente | Datei | Status |
|---|---|---|
| `YamlReader` (Deserialisierung YAML→DTO) | `src/Persistence/Yaml/YamlReader.cs` | ✅ fertig |
| `MapDtoTileDtoYamlConverter` inkl. `LandValues` | `src/Persistence/Yaml/MapDtoYamlConverter.cs` | ✅ fertig |
| `GameStateDtoMapper.FromDto(GameStateDto)` | `src/Persistence/Model/GameStateDtoMapper.cs` | ✅ fertig, aber nicht aufgerufen |
| `PlayerDtoMapper.FromDto` | `src/Persistence/Model/PlayerDtoMapper.cs` | ✅ fertig, aber braucht echte Factory |
| `UnitDtoMapper.FromDto` | `src/Persistence/Model/UnitDtoMapper.cs` | ✅ fertig, aber braucht echte Factory |
| `CityDtoMapper.FromDto` | `src/Persistence/Model/CityDtoMapper.cs` | ✅ fertig |
| `MapDtoMapper.FromDto` | `src/Persistence/Model/MapDtoMapper.cs` | ✅ fertig, aber braucht echte Factory |
| `YamlMapperDependenciesFactory` (nur SavePfad) | `src/Persistence/Model/YamlMapperDependenciesFactory.cs` | ⚠️ nur ToDto, alle FromDto-Factories sind Stubs |
| `Game(IGameData)` – Binär-Lade-Konstruktor | `src/Game.LoadSave.cs` | ✅ Referenzimplementierung |
| `IGameSnapshotSource` + `Game.GameStateSource.cs` | `src/Persistence/Game.GameStateSource.cs` | ✅ fertig |

---

## Was noch fehlt (vollständige Liste)

### 1. `RuntimePlayerFactory` — erstellt echte `Player`-Instanzen

**Datei (neu):** `src/Persistence/Model/RuntimePlayerFactory.cs`

```csharp
public sealed class RuntimePlayerFactory : IPlayerFactory
{
    public IPlayerRestorable Create(ICivilization civilization, PlayerDto dto)
    {
        // Player.Game muss VORHER gesetzt sein (analog Game.LoadSave.cs: "Player.Game = this")
        return new Player(civilization, dto.LeaderName, dto.TribeName, dto.TribeNamePlural);
    }
}
```

**Problem:** `Player.Game` ist ein statisches Feld (`static IPlayerGame Game`).  
→ Es muss gesetzt werden **bevor** `PlayerDtoMapper.FromDto` aufgerufen wird.  
→ Entweder im `Game(GameState)`-Konstruktor vor dem Mapper, oder durch einen Pre-Init-Schritt.

**Abhängigkeit:** `Player(ICivilization, string, string, string)` – Konstruktor bereits vorhanden.

---

### 2. `RuntimeUnitFactory` — erstellt echte `IUnit`-Instanzen per Klassenname

**Datei (neu):** `src/Persistence/Model/RuntimeUnitFactory.cs`

```csharp
public sealed class RuntimeUnitFactory : IUnitFactory
{
    public IUnitRestorable Create(string className, byte player, Guid? homeCityGuid)
    {
        // Analog Game.CreateUnit(UnitType type) per Reflection
        var type = Reflect.GetUnits().FirstOrDefault(u => u.GetType().Name == className)
            ?? throw new InvalidOperationException($"Unit type '{className}' not found.");
        var unit = (IUnitRestorable)Activator.CreateInstance(type.GetType());
        // HomeCityGuid wird später in Game(GameState) aufgelöst (nach City-Aufbau)
        return unit;
    }
}
```

**Referenz:** `Game.CreateUnit(UnitType)` in `src/Game.cs` Zeile 472 – macht dasselbe via `Reflect`.

**HomeCityGuid-Auflösung:** Kann erst nach City-Aufbau passieren → spätere Nachverdrahtung in `Game(GameState)`.

---

### 3. `RuntimeMapFactory` — initialisiert `Map.Instance` ohne Generierung

**Datei (neu):** `src/Persistence/Model/RuntimeMapFactory.cs`

```csharp
public sealed class RuntimeMapFactory : IMapFactory
{
    public IMapTiles CreateMap(int width, int height, uint terrainSeed)
    {
        Map.Instance.InitializeForYamlLoad(width, height, (int)terrainSeed);
        return Map.Instance;
    }
}
```

**Wichtig:** Kein `Generate()`, kein `PlaceHuts()`, kein `CalculateLandValue()` – diese Daten kommen aus dem YAML (Hut im TileCodec Bit 10, LandValue im `LandValues`-Array).

---

### 4. `Map.InitializeForYamlLoad` + `RuntimeTileDtoMapper`

**Datei (neu):** `src/Map.LoadYaml.cs` (Partial)

```csharp
internal void InitializeForYamlLoad(int width, int height, int terrainSeed)
{
    _terrainMasterWord = terrainSeed;
    _tiles = new ITile[width, height];
    Ready = false;
}

internal void FinalizeYamlLoad() => Ready = true;
```

**Datei (neu):** `src/Persistence/Model/RuntimeTileDtoMapper.cs`

Implementiert `ITileDtoMapper.SetTileFromDto(TileDto dto, int x, int y)`:
- Erzeugt den richtigen `ITile`-Subtyp anhand `dto.Terrain` (analog `Map.LoadSave.cs` Zeile 25–47)
- Setzt `Map.Instance._tiles[x, y]` direkt
- Kopiert `LandValue`, `Hut`, `Road`, `RailRoad`, `Irrigation`, `Mine`, `Fortress`, `Pollution` aus dem `TileDto`

**Terrain-Mapping** (aus `Map.LoadSave.cs`):

| Terrain-Enum | Klasse |
|---|---|
| Grassland | `Grassland(x, y)` |
| Plains | `Plains(x, y, special)` |
| Tundra | `Tundra(x, y, special)` |
| Forest | `Forest(x, y, special)` |
| Ocean | `Ocean(x, y, special)` |
| Swamp | `Swamp(x, y, special)` |
| River | `River(x, y)` |
| Jungle | `Jungle(x, y, special)` |
| Hills | `Hills(x, y, special)` |
| Mountains | `Mountains(x, y, special)` |
| Desert | `Desert(x, y, special)` |
| Arctic | `Arctic(x, y, special)` |

`TileIsSpecial(x, y)` – Methode bereits in `Map` vorhanden.

---

### 5. `YamlLoadMapperDependenciesFactory`

**Datei (neu):** `src/Persistence/Model/YamlLoadMapperDependenciesFactory.cs`

Analogon zur `YamlMapperDependenciesFactory`, aber mit echten Factories:

```csharp
public sealed class YamlLoadMapperDependenciesFactory
{
    public static YamlMapperDependencies Create(IYamlReadValueSanitizer sanitizer)
    {
        var unitMapper   = new UnitDtoMapper(new RuntimeUnitFactory(), sanitizer);
        var mapMapper    = new MapDtoMapper(new RuntimeMapFactory(), new RuntimeTileDtoMapper(), 0);
        var cityMapper   = new CityDtoMapper(new ProductionDtoMapper(new GameReflect()), new CityDefinitionResolver(), sanitizer);
        var playerMapper = new PlayerDtoMapper(
            null,  // IPlayerGame: noch nicht vorhanden vor Game-Konstruktor → siehe Hinweis unten
            new IndexBasedPlayerOwnerResolver(),  // NEU – löst per Index auf, nicht per Game-Instanz
            new RuntimePlayerFactory(),
            new CivilizationDtoMapper(Common.Civilizations),
            new PalaceDtoMapper(sanitizer),
            cityMapper,
            unitMapper,
            new RuntimeAdvanceResolver(),
            new RuntimeGovernmentResolver(),
            sanitizer);

        return new YamlMapperDependencies(playerMapper, unitMapper, mapMapper, sanitizer);
    }
}
```

**Problem `IPlayerGame` im `PlayerDtoMapper`:**  
`PlayerDtoMapper` bekommt `IPlayerGame gameInstance` für `ToDto` (um Units zu filtern). Beim `FromDto` wird `gameInstance` **nicht** genutzt. → Kann `null` übergeben werden oder ein Dummy-`IPlayerGame`.

**`IndexBasedPlayerOwnerResolver`** (neu, klein):  
Löst Owner-ID per Array-Index auf – ohne laufende `Game`-Instanz.

---

### 6. `Game(GameState)` — neuer Lade-Konstruktor

**Datei (neu):** `src/Game.LoadYaml.cs` (Partial)

Analogon zu `Game(IGameData)` in `src/Game.LoadSave.cs`:

```csharp
private Game(GameState state)
{
    _difficulty  = state.Difficulty;
    _competition = state.Players.Length - 1;

    // WICHTIG: Player.Game muss VOR Mapper gesetzt werden
    Player.Game = this;

    // Direkt übernehmen (Mapper hat bereits fertige Objekte gebaut)
    _players = state.Players.Cast<Player>().ToArray();
    _cities  = state.Cities;
    _units   = state.Units;

    // Verdrahten: Player.Destroyed-Events
    foreach (var player in _players.Skip(1))
        player.Destroyed += PlayerDestroyed;

    // HomeCityGuid → City-Referenz auflösen
    var cityById = _cities.ToDictionary(c => c.Id);
    foreach (var unit in _units)
    {
        // unit hat HomeCityGuid aus UnitDto; Nachverdrahtung hier
        if (unit is IUnitWithHomeCity u && u.HomeCityGuid.HasValue
            && cityById.TryGetValue(u.HomeCityGuid.Value, out var homeCity))
        {
            unit.SetHome(homeCity);
        }
    }

    GameTurn        = (ushort)state.GameTurn;
    HumanPlayer     = (Player)state.HumanPlayer;
    _currentPlayer  = Array.IndexOf(_players, (Player)state.CurrentPlayer);
    CityNames       = Common.AllCityNames.ToArray(); // oder aus GameState ergänzen
    _anthologyTurn  = state.AnthologyTurn;
    Common.SetRandomSeed(state.RandomSeed);

    // Game-Options
    ApplyGameOptions(state.GameOptions);

    // GlobalWarmingService aufbauen (wie in Game.LoadSave.cs)
    globalWarmingService = GlobalWarmingServiceFactory.CreateGlobalWarmingService(...);
    globalWarmingScourgeService = ...;

    // Extinction berechnen (wie in Game.LoadSave.cs)
    _players.ToList().ForEach(player => player.HandleExtinction(false));

    // Aktive Unit setzen
    SetInitialActiveUnit();
}
```

---

### 7. `static Game.LoadYamlGame(string cosFile)` — Entry Point

**In:** `src/Game.LoadYaml.cs`

```csharp
public static bool LoadYamlGame(string cosFile)
{
    // 1. Vorbedingung: Map.Instance.InitializeForYamlLoad wird durch RuntimeMapFactory aufgerufen
    //    (passiert innerhalb MapDtoMapper.FromDto → RuntimeMapFactory.CreateMap)

    // 2. Player.Game temporär auf null – wird im Konstruktor gesetzt

    var sanitizer = new YamlReadValueSanitizer(new RuntimeLogger());
    var deps      = YamlLoadMapperDependenciesFactory.Create(sanitizer);
    var mapper    = new GameStateDtoMapper(deps.PlayerMapper, deps.UnitMapper, deps.MapMapper, deps.Sanitizer);

    var dto = YamlReader
        .OfFile(cosFile)
        .WithStandard()
        .WithTypeConverter(new MapDtoTileDtoYamlConverter())
        .As<GameStateDto>();

    var state   = mapper.FromDto(dto);
    _instance   = new Game(state);

    Map.Instance.FinalizeYamlLoad();

    return true;
}
```

---

## Besondere Stolperstellen

### A) `Player.Game` – statisches Feld, zirkuläre Abhängigkeit

`Player.Game` muss gesetzt sein, damit `Player`-Methoden wie `HandleExtinction()` funktionieren.  
→ **Reihenfolge:** `Player.Game = this` **vor** dem ersten `PlayerDestroyed`-Event, aber der `Game`-Konstruktor selbst ruft ja schon `player.HandleExtinction(false)` am Ende.  
→ Lösung: `Player.Game = this` als erste Zeile im `Game(GameState)`-Konstruktor, **bevor** `_players` gesetzt wird.

### B) HomeCityGuid-Auflösung bei Units

`UnitDtoMapper.FromDto` setzt `HomeCityGuid` noch nicht direkt als `City`-Referenz (City existiert zu dem Zeitpunkt noch nicht).  
→ `IUnit` braucht entweder ein temporäres `HomeCityGuid`-Feld, oder die Auflösung passiert komplett im `Game(GameState)`-Konstruktor nach City-Aufbau.  
→ `IUnitRestorable` müsste ggf. um `Guid? PendingHomeCityGuid { get; set; }` erweitert werden.

### C) `CityNames` im GameState

`GameState` hat kein `CityNames`-Feld (war auskommentiert). Im Binär-Pfad kommen `CityNames` aus `IGameData.CityNames`.  
→ Entweder `GameState` um `string[] CityNames` ergänzen und in `GameStateHandler.Create` setzen, oder beim Laden aus `Common.AllCityNames` neu aufbauen (verliert den aktuellen Index-Stand).

### D) `Map.Instance` – Singleton vs. neues Map-Objekt

`Map.Instance` ist ein Singleton. `RuntimeMapFactory.CreateMap` greift darauf direkt zu.  
→ Es darf nicht mehrfach initialisiert werden (kein gleichzeitiger Load+GenerateNew).

### E) `TileIsSpecial(x, y)` – Position-basiert, kein YAML-Feld

`special` für Tile-Konstruktoren wird durch `Map.TileIsSpecial(x, y)` berechnet.  
Diese Methode basiert nur auf `x, y` und `_terrainMasterWord` → kann aufgerufen werden, sobald `InitializeForYamlLoad` den `_terrainMasterWord` gesetzt hat.

---

## Datentransport: Hut und LandValue

| Datum | Kodierung im YAML | Lese-Pfad |
|---|---|---|
| **Hut** | TileCodec Bit 10 in den Tiles-Zeilen (`QG` = Tundra mit Hut) | `TileCodec.Decode` → `TileDto.Hut = true` → `RuntimeTileDtoMapper.SetTileFromDto` → `tile.Hut = dto.Hut` |
| **LandValue** | Separates `LandValues`-Array, Hex-Bytes kommagetrennt je Zeile | `MapDtoTileDtoYamlConverter.ApplyLandValues` → `TileDto.LandValue` → `RuntimeTileDtoMapper.SetTileFromDto` → `tile.LandValue = dto.LandValue` |

**Wichtig:** `PlaceHuts()` und `CalculateLandValue()` dürfen beim YAML-Laden **nicht** aufgerufen werden – die Daten kommen bereits fertig aus dem YAML.

---

## Vollständige Komponentenübersicht

```
YAML-Datei (.cos)
    ↓
YamlReader.OfFile(...).WithStandard().WithTypeConverter(MapDtoTileDtoYamlConverter).As<GameStateDto>()
    ↓  (MapDtoTileDtoYamlConverter dekodiert TileCodec + LandValues)
GameStateDto
    ↓
GameStateDtoMapper.FromDto(dto)
    ├── PlayerDtoMapper.FromDto  → RuntimePlayerFactory → Player-Instanzen
    ├── UnitDtoMapper.FromDto    → RuntimeUnitFactory   → IUnit-Instanzen
    └── MapDtoMapper.FromDto     → RuntimeMapFactory    → Map.Instance initialisiert
                                   RuntimeTileDtoMapper → Map.Instance._tiles befüllt
    ↓
GameState  (IPlayer[], List<IUnit>, List<City>, ITile[,], Seeds, Options, ...)
    ↓
Game(GameState state)   [Game.LoadYaml.cs, Partial]
    ├── Player.Game = this
    ├── _players, _cities, _units zuweisen
    ├── HomeCityGuid → City-Referenz auflösen
    ├── Events verdrahten (Destroyed)
    ├── GameOptions anwenden
    ├── GlobalWarmingService aufbauen
    └── HandleExtinction aufrufen
    ↓
Map.Instance.FinalizeYamlLoad()   → Ready = true
    ↓
Spielbereit ✅
```

---

## Empfohlene Implementierungsreihenfolge

1. **`Map.LoadYaml.cs`** – `InitializeForYamlLoad` + `FinalizeYamlLoad` (keine Logik, ~10 Zeilen)
2. **`RuntimeTileDtoMapper`** – `SetTileFromDto` mit Terrain-Switch, setzt `Map.Instance._tiles[x,y]` (~40 Zeilen)
3. **`RuntimeUnitFactory`** – per Reflection aus Klassenname, analog `Game.CreateUnit` (~15 Zeilen)
4. **`RuntimePlayerFactory`** – `new Player(civ, ...)` (~10 Zeilen)
5. **`RuntimeMapFactory`** – ruft `Map.Instance.InitializeForYamlLoad` auf (~10 Zeilen)
6. **`IndexBasedPlayerOwnerResolver`** – Owner-ID per Index, kein `Game`-Bezug (~15 Zeilen)
7. **`YamlLoadMapperDependenciesFactory`** – baut Mapper-Chain mit echten Factories (~40 Zeilen)
8. **`Game.LoadYaml.cs`** – `Game(GameState)` + `static LoadYamlGame()` (~60 Zeilen)
9. **`IUnitRestorable` erweitern** um `Guid? PendingHomeCityGuid` (falls nötig)
10. **`GameState` erweitern** um `string[] CityNames` (falls nötig)
11. **Integrationstest** – YAML-Datei laden, Units/Cities/Map/Players/Seeds prüfen

---

## Akzeptanzkriterien

- `Game.LoadYamlGame("12.cos")` lädt fehlerfrei
- `_players.Length` korrekt, alle Cities und Units pro Player vorhanden
- `Map.Instance._tiles` vollständig befüllt inkl. Hut-Flags und LandValues
- `HumanPlayer` und `CurrentPlayer` korrekt gesetzt
- `GameRandomSeed` (Gameplay-RNG) und `TerrainMasterWord` (Map-Seed) korrekt und getrennt gesetzt
- Tests grün: bestehende Unit-Tests weiterhin grün, neuer Integrationstest für YAML-Load-Roundtrip

---

## Schrittweise Prompts zur Implementierung

Die Umsetzung sollte in **6 separaten Prompts** erfolgen, da es viele voneinander abhängige neue Dateien gibt und jeder Schritt einzeln build- und testbar ist.

---

### Prompt 1 — Map-Infrastruktur (keine Abhängigkeiten)

```text
Bitte implementiere die Map-Infrastruktur für den YAML-Load-Pfad.

Kontext (aus YamlLoad_Analyse.md):
- Map.LoadSave.cs zeigt die Referenz: LoadMap(Bitmap) befüllt _tiles, PlaceHuts(), CalculateLandValue()
- Beim YAML-Laden dürfen PlaceHuts() und CalculateLandValue() NICHT aufgerufen werden
  (Hut = TileCodec Bit 10, LandValue = separates LandValues-Array, beide kommen bereits fertig aus YAML)
- Map._tiles ist private; _terrainMasterWord ist private
- TileIsSpecial(x, y) ist bereits in Map vorhanden und braucht nur _terrainMasterWord gesetzt

Aufgaben:
1) Erstelle src/Map.LoadYaml.cs als neues partial class Map:
   - internal void InitializeForYamlLoad(int width, int height, int terrainSeed)
     → setzt _terrainMasterWord, alloziert _tiles = new ITile[width, height], setzt Ready = false
   - internal void FinalizeYamlLoad()
     → setzt Ready = true

2) Erstelle src/Persistence/Model/RuntimeTileDtoMapper.cs als ITileDtoMapper:
   - void SetTileFromDto(TileDto dto, int x, int y)
     → Terrain-Switch analog Map.LoadSave.cs Zeile 25-47 (Forest/Swamp/Plains/Tundra/River/Grassland/Jungle/Hills/Mountains/Desert/Arctic/Ocean)
     → special = Map.Instance.TileIsSpecial(x, y)
     → setzt Map.Instance._tiles[x, y] = neuer Tile
     → kopiert dto.Road, dto.RailRoad, dto.Irrigation, dto.Mine, dto.Fortress, dto.Pollution, dto.Hut, dto.LandValue direkt auf den Tile
   - FromDto / ToDto Stub-Implementierungen (werden nicht verwendet)

3) Erstelle src/Persistence/Model/RuntimeMapFactory.cs als IMapFactory:
   - IMapTiles CreateMap(int width, int height, uint terrainSeed)
     → ruft Map.Instance.InitializeForYamlLoad(width, height, (int)terrainSeed) auf
     → gibt Map.Instance zurück

Hinweis: Map.Instance ist das Singleton aus BaseInstance.
_tiles und _terrainMasterWord sind private – prüfe ob internal sichtbar ist oder ob Map.LoadYaml.cs
als partial class Map direkten Zugriff hat.

Akzeptanzkriterien:
- Build grün
- Tests grün (keine bestehenden Tests brechen)
```

---

### Prompt 2 — Runtime Player- und Unit-Factories

```text
Bitte implementiere die Runtime-Factories für Player und Unit im YAML-Load-Pfad.

Kontext (aus YamlLoad_Analyse.md):
- IPlayerFactory.Create(ICivilization, PlayerDto) → IPlayerRestorable
- IUnitFactory.Create(string className, byte player, Guid? homeCityGuid) → IUnitRestorable
- Player(ICivilization, string leaderName, string tribeName, string tribeNamePlural) – Konstruktor vorhanden
- Game.CreateUnit(UnitType) in src/Game.cs Zeile 472 – macht Reflection über Reflect.GetUnits()
- Player.Game ist static IPlayerGame – muss VOR Player-Erstellung gesetzt werden (passiert später in Game(GameState))
- HomeCityGuid-Auflösung (City-Referenz) kann erst nach City-Aufbau im Game-Konstruktor erfolgen

Aufgaben:
1) Erstelle src/Persistence/Model/RuntimePlayerFactory.cs als IPlayerFactory:
   - Create(ICivilization civilization, PlayerDto dto) → new Player(civilization, dto.LeaderName, dto.TribeName, dto.TribeNamePlural)
   - Kein Player.Game-Zugriff hier (wird außerhalb gesetzt)

2) Prüfe IUnitRestorable: Hat es ein Feld/Property für HomeCityGuid (zum späteren Auflösen)?
   Falls nicht: ergänze Guid? PendingHomeCityGuid { get; set; } in IUnitRestorable
   und setze es in UnitDtoMapper.FromDto aus dto.HomeCityGuid

3) Erstelle src/Persistence/Model/RuntimeUnitFactory.cs als IUnitFactory:
   - Create(string className, byte player, Guid? homeCityGuid) → IUnitRestorable
     → per Reflection: Reflect.GetUnits().FirstOrDefault(u => u.GetType().Name == className)
     → Activator.CreateInstance auf dem gefundenen Typ
     → setzt PendingHomeCityGuid = homeCityGuid auf dem erzeugten Unit
     → wirft InvalidOperationException wenn className nicht gefunden

4) Erstelle src/Persistence/Model/IndexBasedPlayerOwnerResolver.cs als IPlayerOwnerResolver:
   - Wird für den Load-Pfad gebraucht (keine laufende Game-Instanz)
   - TryResolveOwnerId → gibt immer false zurück (beim Laden keine ToDto-Aufrufe nötig)

Akzeptanzkriterien:
- Build grün
- Tests grün
- RuntimeUnitFactory wirft bei unbekanntem className eine aussagekräftige Exception
```

---

### Prompt 3 — YamlLoadMapperDependenciesFactory

```text
Bitte implementiere die YamlLoadMapperDependenciesFactory für den YAML-Load-Pfad.

Kontext (aus YamlLoad_Analyse.md):
- YamlMapperDependenciesFactory (src/Persistence/Model/YamlMapperDependenciesFactory.cs) ist das Vorbild
  → dort sind aber alle FromDto-Factories NotSupportedXxxFactory-Stubs
- Für den Load-Pfad brauchen wir dieselbe Struktur, aber mit echten Runtime-Factories
- PlayerDtoMapper bekommt IPlayerGame für ToDto (Units filtern) – beim Laden wird ToDto nicht aufgerufen
  → null oder ein NullPlayerGame-Dummy kann übergeben werden
- RuntimeTileDtoMapper, RuntimeMapFactory, RuntimePlayerFactory, RuntimeUnitFactory aus Schritt 1+2

Aufgaben:
1) Erstelle src/Persistence/Model/YamlLoadMapperDependenciesFactory.cs:
   - public static YamlMapperDependencies Create(IYamlReadValueSanitizer sanitizer)
     → UnitDtoMapper   mit RuntimeUnitFactory
     → MapDtoMapper    mit RuntimeMapFactory + RuntimeTileDtoMapper, terrainSeed=0 (wird von Factory überschrieben)
     → CityDtoMapper   mit ProductionDtoMapper(new GameReflect()), CityDefinitionResolver, sanitizer
     → PlayerDtoMapper mit:
          IPlayerGame  = null (oder NullPlayerGame-Dummy)
          IPlayerOwnerResolver = new IndexBasedPlayerOwnerResolver()
          IPlayerFactory       = new RuntimePlayerFactory()
          CivilizationDtoMapper(Common.Civilizations)
          PalaceDtoMapper(sanitizer)
          CityDtoMapper
          UnitDtoMapper
          RuntimeAdvanceResolver (analog YamlMapperDependenciesFactory)
          RuntimeGovernmentResolver (analog YamlMapperDependenciesFactory)
          sanitizer
   - InitializeDocLists() aufrufen (analog YamlMapperDependenciesFactory – ist bereits static, ggf. auslagern)

2) Falls PlayerDtoMapper null für IPlayerGame nicht akzeptiert:
   Erstelle eine kleine sealed class NullPlayerGame : IPlayerGame { ... }
   mit sinnvollen Default-Rückgaben (leere Arrays, 0-Werte) für alle Interface-Member.

Akzeptanzkriterien:
- Build grün
- Tests grün
- YamlLoadMapperDependenciesFactory.Create() kann ohne Game-Instanz aufgerufen werden
```

---

### Prompt 4 — `Game(GameState)` Konstruktor und `LoadYamlGame`

```text
Bitte implementiere den YAML-Lade-Konstruktor für Game und den statischen Entry Point.

Kontext (aus YamlLoad_Analyse.md):
- Referenz: Game(IGameData gameData) in src/Game.LoadSave.cs
- GameState enthält bereits fertige IPlayer[], List<IUnit>, List<City>, ITile[,], Seeds, Options
- Player.Game ist static IPlayerGame – muss als ERSTE Zeile im Konstruktor gesetzt werden
- _players ist Player[] – GameState.Players ist IPlayer[] → Cast zu Player[] nötig
- HomeCityGuid-Auflösung: units haben PendingHomeCityGuid (aus Prompt 2), Cities haben Id (Guid)
- GlobalWarmingService-Aufbau: analog Game.LoadSave.cs mit GlobalWarmingServiceFactory
- HandleExtinction(false) aufrufen wie in Game.LoadSave.cs am Ende

Aufgaben:
1) Erstelle src/Game.LoadYaml.cs als partial class Game:

   a) private Game(GameState state):
      - Player.Game = this  (ERSTE Zeile!)
      - _difficulty  = state.Difficulty
      - _competition = state.Players.Length - 1
      - _players  = state.Players.Cast<Player>().ToArray()
      - _cities   = state.Cities
      - _units    = state.Units
      - Player.Destroyed-Events auf alle außer _players[0] registrieren
      - HomeCityGuid → City-Referenz: Dictionary<Guid, City> aufbauen, alle units mit PendingHomeCityGuid auflösen via unit.SetHome(city)
      - GameTurn = (ushort)state.GameTurn  (löst auch AnthologyTurn-Check aus)
      - HumanPlayer = (Player)state.HumanPlayer
      - _currentPlayer = Array.IndexOf(_players, (Player)state.CurrentPlayer)
      - _anthologyTurn = state.AnthologyTurn
      - CityNames = Common.AllCityNames.ToArray()
      - Common.SetRandomSeed(state.RandomSeed)
      - GameOptions anwenden (analog Game.LoadSave.cs Zeile ~295 ff.)
      - GlobalWarmingService + GlobalWarmingScourgeService aufbauen (analog Game.LoadSave.cs)
      - Aktive Unit setzen: ersten bewegungsfähigen Human-Unit suchen (analog Game.LoadSave.cs ~315)
      - _players.ToList().ForEach(p => p.HandleExtinction(false))

   b) public static bool LoadYamlGame(string cosFile):
      - Player.Game = null  (Reset vor Load)
      - sanitizer = new YamlReadValueSanitizer(new RuntimeLogger())  [RuntimeLogger ist inner class, analog YamlMapperDependenciesFactory]
      - deps = YamlLoadMapperDependenciesFactory.Create(sanitizer)
      - mapper = new GameStateDtoMapper(deps.PlayerMapper, deps.UnitMapper, deps.MapMapper, deps.Sanitizer)
      - dto = YamlReader.OfFile(cosFile).WithStandard().WithTypeConverter(new MapDtoTileDtoYamlConverter()).As<GameStateDto>()
      - state = mapper.FromDto(dto)
      - _instance = new Game(state)
      - Map.Instance.FinalizeYamlLoad()
      - return true

2) Falls CityNames im GameState fehlt: ergänze string[] CityNames in GameState und setze es in GameStateHandler.Create aus game.CityNames

Hinweis: RuntimeLogger ist bereits als inner class in YamlMapperDependenciesFactory definiert.
Entweder wiederverwenden (internal machen) oder duplizieren.

Akzeptanzkriterien:
- Build grün
- Tests grün
- LoadYamlGame("e:/temp/12.cos") wirft keine Exception
```

---

### Prompt 5 — Integrationstest YAML-Load-Roundtrip

```text
Bitte erstelle einen Integrationstest für den vollständigen YAML-Load-Pfad.

Kontext:
- Testdatei: e:/temp/12.cos (oder eine im xunit-Projekt mitgelieferte Test-YAML)
- Save-Pipeline ist bereits getestet in xunit/src/persistence/YamlSaveGameStateWriterTest.cs
- Bestehende Mapper-Tests sind in xunit/src/persistence/Model/
- Die Load-Pipeline (Prompt 1-4) ist jetzt vollständig

Aufgaben:
1) Erstelle xunit/src/persistence/YamlLoadGameStateReaderTest.cs:

   Test A – GameStateDto-Deserialisierung (ohne Runtime-Abhängigkeiten):
   - Liest eine bekannte Test-YAML-Datei
   - Prüft: dto.Players.Count korrekt, dto.Players[1].Units.Count > 0
   - Prüft: dto.Map.Tiles != null, dto.Map.MapSeed gesetzt
   - Prüft: dto.HumanPlayer und dto.CurrentPlayer gesetzt

   Test B – GameStateDtoMapper.FromDto Roundtrip (mit Mock-Factories):
   - Verwendet YamlLoadMapperDependenciesFactory.Create(sanitizer)
   - GameStateDtoMapper.FromDto(dto) → GameState
   - Prüft: state.Players.Length korrekt
   - Prüft: state.Units nicht leer
   - Prüft: state.Cities nicht leer
   - Prüft: state.MapTiles[x,y] != null für bekannte Koordinaten
   - Prüft: state.HumanPlayer != null, state.CurrentPlayer != null

   Test C – LandValue und Hut korrekt übertragen:
   - Prüft nach FromDto: state.MapTiles enthält mindestens einen Tile mit Hut=true (falls in Test-YAML vorhanden)
   - Prüft: LandValue für bekannte Koordinaten korrekt (Wert aus LandValues-Array)

2) Lege eine kleine Test-YAML-Datei an (z.B. xunit/resources/test_small.cos) mit:
   - 2 Spielern, 1 City, 2 Units, 10x10 Map
   - Einem Tile mit Hut und bekanntem LandValue
   Alternativ: 12.cos nach xunit/resources/ kopieren falls Dateigröße akzeptabel.

Hinweis: Wenn Game(GameState) Runtime-Abhängigkeiten wie Map.Instance hat,
müssen diese Tests ggf. als Integrationstests markiert sein oder Map.Instance mocken.
Dokumentiere welche Tests Unit-Tests vs. Integrationstests sind.

Akzeptanzkriterien:
- Alle neuen Tests grün
- Bestehende Tests weiterhin grün
- Test B beweist: Kein Datenverlust zwischen YAML → GameStateDto → GameState
```

---

### Prompt 6 — Verdrahtung mit der Runtime (SDL-Einstiegspunkt)

```text
Bitte verdrahte LoadYamlGame mit dem bestehenden Lade-Einstiegspunkt der Runtime.

Kontext:
- Bestehender Binär-Load: Game.LoadGame(sveFile, mapFile) in src/Game.LoadSave.cs
- YAML-Load: Game.LoadYamlGame(cosFile) in src/Game.LoadYaml.cs (aus Prompt 4)
- Die Runtime (SDL oder andere) ruft LoadGame auf; zu prüfen wo und wie

Aufgaben:
1) Finde alle Aufrufstellen von Game.LoadGame(...) in der Codebasis (runtime/, src/)
   und dokumentiere, wo der Aufruf herkommt (z.B. Menü-Screen, Dateidialog).

2) Entscheide anhand der Dateiendung:
   - .sav / .sve → LoadGame (Binär)
   - .cos → LoadYamlGame (YAML)

3) Implementiere die Verzweigung an der Aufrufstelle:
   if (Path.GetExtension(file).Equals(".cos", StringComparison.OrdinalIgnoreCase))
       Game.LoadYamlGame(file);
   else
       Game.LoadGame(sveFile, mapFile);

4) Stelle sicher, dass nach erfolgreichem LoadYamlGame der Game-State korrekt aktiv ist:
   - Game.Instance != null
   - UI/Screen-Übergang zum Spiel funktioniert (analog Binär-Load)

5) Manuellen Smoketest durchführen: 12.cos laden, Spielstand erscheint korrekt auf dem Bildschirm.

Akzeptanzkriterien:
- Build grün (DebugWindows-Konfiguration)
- Tests grün
- 12.cos kann über die normale Lade-UI geöffnet werden
- Spielstand zeigt korrekte Map, Player, Units und Cities
```
