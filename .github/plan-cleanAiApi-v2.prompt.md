# Plan: Clean AI-API v2 — Action Sink + Realtime-Ready

## Ziel

Aufbau einer sauberen, testbaren AI-API im `api/`-Projekt, die vollständig unabhängig von den internen Singletons (`Game.Instance`, `Map.Instance`, `Settings.Instance`) ist. Externe oder alternative KI-Implementierungen sollen nur diese Interfaces sehen — kein Zugriff auf interne CivOne-Typen.

**Kerneigenschaft gegenüber v1:** Aktionen werden nicht als Return-Wert zurückgegeben, sondern über einen **Action Sink** eingereicht. Das hält die API offen für zukünftige asynchrone oder Realtime-KI, ohne das Interface zu brechen.

Die interne `AI`-Klasse wird zum Adapter. Default-KI-Verhalten bleibt erhalten (Regressionsschutz).

---

## Kontext

### Problem: BaseInstance koppelt AI an Singletons

```
AI : BaseInstance
  └── Game.Instance   (Singleton)
  └── Map.Instance    (Singleton)
  └── Settings.Instance (Singleton)
```

Jede externe KI würde diese Kopplung erben — oder direkt gegen interne Typen kompilieren.

### Bestehende Grundlagen

- `api/src/IPlugin.cs` — Stil-Referenz für API-Interfaces
- `api/src/IGameData.cs` — Granularität der Spieldaten
- `api/src/IModification.cs` — leer, Ausgangspunkt
- `src/IGame.cs` — `IGame`, `IGameUnitsQuery`, `IGamePlayerQuery` etc.
- `src/Services/Pathfinding/IAiGotoExecutor.cs` — bereits sauberes Interface-Muster
- `src/BaseInstance.cs` — Singleton-Hub, darf von AI-API **nicht** genutzt werden

### Current runtime entry points

1. **Unit turn** — `Tasks/Turn.Step()` ruft `Game.CurrentPlayer.AI.Move(_unit)`
2. **City production** — `City.NewTurn()` ruft `Player.AI.CityProduction(this)` nach Rundenabrechnung
3. **Research choice** — `Tasks/ProcessScience.Run()` ruft `_player.AI.ChooseResearch()`
4. **Player turn hook (optional)** — `Game.EndTurn()` → `Turn.New(CurrentPlayer)` → `Player.NewTurn()` — Kandidat für `OnTurnStart`

### Warum Action Sink statt Return-Wert

| Ansatz | Turn-based | Realtime | Async/LLM |
|---|---|---|---|
| `IReadOnlyList<Action> OnUnitTurn(ctx)` | ✅ | ❌ | ❌ |
| `void OnUnitTurn(ctx, IAiActionSink sink)` | ✅ | ✅ | ✅ (später mit CancellationToken) |

Der Sink entkoppelt **wann** die KI entscheidet von **wann** die Engine ausführt. Für Turn-based wird der Sink am Ende des Calls ausgewertet. Für Realtime läuft er kontinuierlich.

---

## Phase 1 — Read-only Context-Views + Events (`api/src/AI/`)

### Daten-Interfaces (readonly, keine internen Typen)

| Interface | Inhalt |
|---|---|
| `IAiUnitView` | Position, UnitType, Owner, MovesLeft, PartMoves, Goto, UnitClass, Role, Fortify, HasAction, Veteran |
| `IAiCityView` | Position, Owner, Size, CurrentProduction, Buildings, Food, Shields, Trade |
| `IAiTileView` | TerrainType, IsVisible, Road, Railroad, Irrigation, Mine, Pollution, Hut, SpecialResource (enum), ContinentId, Units (als `IAiUnitView`), City (als `IAiCityView`) |
| `IAiMapView` | `IAiTileView GetTile(int x, int y)`, Width, Height |
| `IAiPlayerView` | PlayerId, CivilizationId, Gold, ScienceRate, TaxRate, LuxuryRate, Government, KnownAdvances, Cities, Units |

### Context-Interfaces (Snapshots — immer immutable, nie live-Referenz)

| Interface | Zweck |
|---|---|
| `IAiTurnContext` | Player-Snapshot für den gesamten KI-Zug: `OwnPlayer`, `MovableUnits`, `OwnCities`, `Map`, `GameTurn`, `Events`, `FogOfWar` |
| `IAiUnitTurnContext : IAiTurnContext` | Zusätzlich: `Unit` (die zu bewegende Einheit) |
| `IAiCityTurnContext : IAiTurnContext` | Zusätzlich: `City` (die entscheidende Stadt) |
| `IAiResearchContext : IAiTurnContext` | Zusätzlich: `AvailableAdvances` (wählbare Technologien) |

`MovableUnits` ist ein Convenience-Filter auf `OwnPlayer.Units` — enthält nur Einheiten mit `HasAction == false` und `MovesLeft > 0`. Fortifizierte Einheiten (Fortify oder Sentry) tauchen hier nicht auf, es sei denn, die KI wählt sie explizit via `WakeUpAction` über den Sink.

**Performance-Strategie: materialisierte Turn-Snapshots** — Context-Objekte bleiben echte, immutable Snapshots. `AiContextFactory` baut pro KI-Spieler und Zug einen Snapshot-Root, den `IAiUnitTurnContext`, `IAiCityTurnContext` und `IAiResearchContext` wiederverwenden. Es gibt keine Live-Adapter über Engine-Objekte.

### Events

Events werden einmal pro KI-Zug im `IAiTurnContext` gesammelt. Die KI empfängt sie beim ersten Einstiegspunkt des Zugs.

```csharp
// api/src/AI/Events/
public abstract record AiEvent;

// Eigene Ereignisse
public sealed record CityProductionCompletedEvent(int X, int Y, AiProductionChoice Completed) : AiEvent;
public sealed record ResearchCompletedEvent(ushort AdvanceId) : AiEvent;
public sealed record UnitCreatedEvent(ushort UnitType, int X, int Y) : AiEvent;

// Feindliche / neutrale Ereignisse (nur sichtbare)
public sealed record WonderCompletedEvent(ushort WonderId, byte ByPlayer) : AiEvent;
public sealed record EnemyCityFoundEvent(int X, int Y, byte ByPlayer) : AiEvent;

// Verluste
public sealed record OwnUnitDestroyedEvent(ushort UnitType, int X, int Y, byte ByPlayer) : AiEvent;
public sealed record OwnCityLostEvent(int X, int Y, byte ToPlayer) : AiEvent;
```

Der `AiContextFactory` sammelt diese Events pro Spieler zwischen den Zügen. Engine-seitig werden sie in einem `AiEventAccumulator` gepuffert, der bei bekannten Engine-Ereignissen (Stadtproduktion fertig, Einheit zerstört etc.) befüllt wird.

---

## Phase 2 — Action Sink + Action-Modell (`api/src/AI/`)

### IAiActionSink

```csharp
// api/src/AI/
public interface IAiActionSink
{
    void Submit(AiUnitAction action);
}

public interface IAiCityActionSink
{
    void Submit(AiCityAction action);
}

public interface IAiResearchActionSink
{
    void Submit(AiResearchAction action);
}
```

Der Sink ist das einzige Kommunikationskanal von der KI zur Engine. Die KI schreibt nie direkt in Engine-Objekte.

### Action-Hierarchie

```csharp
// api/src/AI/Actions/

// Unit actions
public abstract record AiUnitAction;
public sealed record MoveAction(int Dx, int Dy) : AiUnitAction;
public sealed record SetGotoAction(int X, int Y) : AiUnitAction;
public sealed record ClearGotoAction : AiUnitAction;
public sealed record FortifyAction : AiUnitAction;
public sealed record WakeUpAction : AiUnitAction;         // reaktiviert Fortify UND Sentry — setzt beide Flags auf false
public sealed record DisbandAction : AiUnitAction;
public sealed record SkipTurnAction : AiUnitAction;
// Settler-spezifisch:
public sealed record FoundCityAction : AiUnitAction;
public sealed record BuildRoadAction : AiUnitAction;
public sealed record BuildIrrigationAction : AiUnitAction;
public sealed record BuildMineAction : AiUnitAction;

// City actions
public abstract record AiCityAction;
public sealed record ChooseProductionAction(AiProductionChoice Production) : AiCityAction;

// Research actions
public abstract record AiResearchAction;
public sealed record ChooseResearchAction(AiResearchChoice Research) : AiResearchAction;
```

`AiActionApplier` verarbeitet alle Sinks nach dem KI-Call. Unbekannte oder für die Einheitenklasse ungültige Aktionstypen machen den kompletten Call invalid; der Call wird verworfen und der Grund geloggt (z.B. `BuildRoadAction` für einen Krieger).

Der Sink bleibt auf Interface-Ebene bei `0..n` Actions pro Call. Die Runtime-Semantik ist trotzdem streng: jeder Call wird vor Ausführung vollständig validiert. Enthält ein Unit-Call widersprüchliche oder im aktuellen Zustand ungültige Aktionen (z.B. `MoveAction` + `FortifyAction`, `SetGotoAction` + `ClearGotoAction`, `FoundCityAction` + `BuildRoadAction`), verwirft `AiActionApplier` den kompletten Call atomar und loggt den Grund. Für City- und Research-Sinks ist mehr als eine Action pro Call immer invalid.

### Produktions- und Forschungs-Identifier

```csharp
// api/src/AI/
public readonly record struct AiProductionChoice(AiProductionKind Kind, ushort Id);
public enum AiProductionKind { Unit, Building, Wonder }

public readonly record struct AiResearchChoice(ushort AdvanceId);
```

Stabile numerische IDs, keine internen Typen, kein raw string. `AiActionApplier` mapped auf konkrete Engine-Typen intern.

---

## Phase 3 — AI-Haupt-Interface (`api/src/AI/`)

```csharp
// api/src/AI/
public interface IAiPlayer
{
    /// Called once per turn before any unit or city decisions.
    /// Informational — sink is available but optional here.
    void OnTurnStart(IAiTurnContext context, IAiActionSink sink);

    /// Called for each unit that needs a decision this turn.
    void OnUnitTurn(IAiUnitTurnContext context, IAiActionSink sink);

    /// Called when a city has completed production and needs a new choice.
    void OnCityTurn(IAiCityTurnContext context, IAiCityActionSink sink);

    /// Called when research is completed and a new technology must be chosen.
    void OnResearch(IAiResearchContext context, IAiResearchActionSink sink);
}
```

**Regeln:**
- Alle Methoden sind synchron in Phase 1
- Der Sink kann 0..n Aktionen entgegennehmen, aber `AiActionApplier` validiert jeden Call atomar; bei Konflikt wird nichts angewendet
- Kein `GameTask`, `IUnit`, `City`, `Player`, `Game` im public API
- Context-Objekte sind immer echte Snapshots — immutable DTOs, nie live Engine-Referenzen

### Upgrade-Pfad zu Async (Phase 5, nicht jetzt)

```csharp
// Später — ohne Interface-Break:
public interface IAiPlayerAsync : IAiPlayer
{
    Task OnUnitTurnAsync(IAiUnitTurnContext context, IAiActionSink sink, CancellationToken ct);
}
```

`AiPlayerAdapter` prüft zur Laufzeit ob `IAiPlayerAsync` implementiert ist und verwendet den async Pfad wenn verfügbar.

---

## Phase 4 — Adapter im Spielkern (`src/`)

| Klasse | Aufgabe |
|---|---|
| `AiEventAccumulator` | Wird von Engine-Ereignissen befüllt (Stadtproduktion, Einheit zerstört etc.); wird pro Spieler beim nächsten Zug geleert und in `IAiTurnContext.Events` übergeben |
| `AiContextFactory` | Einzige Klasse, die Singletons kennt; baut immutable Turn-Snapshots aus `Game.Instance` / `Map.Instance`; konsultiert `AiEventAccumulator` |
| `AiPlayerProfileResolver` | Löst `IAiPlayerView` auf Civilization-/Profil-Key auf; entkoppelt KI-Auswahl von `playerId` |
| `AiActionSinkImpl` | Interne Implementierung von `IAiActionSink` — puffert Actions bis der Applier läuft |
| `AiPlayerAdapter` | Nutzt Resolver + Factory, baut Context via Factory, ruft korrekten Entry Point, übergibt Sink |
| `AiActionApplier` | Validiert Sink-Inhalt atomar, übersetzt gültige Actions in `MoveTo`, `SetProduction`, `CurrentResearch`, `Orders.*`, `GameTask` |
| `DefaultAiPlayer` | Implementiert `IAiPlayer` mit bestehendem Verhalten aus `AI.cs` — submits Actions über Sink |
| `AI.cs` (refactor) | Kompatibilitätshim innerhalb des Big-Bang-Umbaus; delegiert direkt an `DefaultAiPlayer` und bleibt kein dauerhafter Parallelpfad |

### Pflicht-Verdrahtung im Runtime

| Datei | Änderung |
|---|---|
| `src/Tasks/Turn.cs` | `AI.Move(_unit)` → `AiPlayerAdapter.DispatchUnitTurn(unit)` |
| `src/City.cs` | `AI.CityProduction(this)` → `AiPlayerAdapter.DispatchCityTurn(city)` |
| `src/Tasks/ProcessScience.cs` | `AI.ChooseResearch()` → `AiPlayerAdapter.DispatchResearch(player)` |
| `src/Player.cs` | `Player.NewTurn()` bleibt Hook-Kandidat für `OnTurnStart`; KI-Auswahl läuft über Resolver statt `Player.AI` / `playerId` |
| `src/Game.cs` (neu) | Engine-Ereignisse → `AiEventAccumulator.Record(...)` |

### Event-Accumulator Verdrahtung (Beispiele)

```csharp
// Wo heute Einheit zerstört wird:
_aiEventAccumulator.Record(owner, new OwnUnitDestroyedEvent(unit.Type, unit.X, unit.Y, attackerOwner));

// Wo heute Stadtproduktion fertig wird (City.NewTurn):
_aiEventAccumulator.Record(owner, new CityProductionCompletedEvent(city.X, city.Y, completed));

// Wo heute Weltwunder fertiggestellt wird:
foreach (Player p in _players)
    _aiEventAccumulator.Record(p.Id, new WonderCompletedEvent(wonderId, builderOwner));
```

---

## Scope

**Eingeschlossen (Phase 1-4):**
- Unit-Movement (inkl. Settlers, Goto-Logik, Fortify/WakeUp)
- City-Production-Entscheidung
- Tech-Research-Entscheidung
- Event-Accumulation für die häufigsten Spielereignisse

**Ausgeschlossen vorerst:**
- Diplomatie / Kriegserklärung
- Barbaren-Logik (bleibt in `AI.Barbarians.cs`)
- Government-Wechsel
- Async/Realtime-Ausführung (Interface ist vorbereitet, wird nicht implementiert)

Barbarian-AI bleibt intern bis der normale Spieler-AI-Vertrag stabil ist.

---

## Execution model

Die Engine besitzt die Zugsequenz und schiebt State in die AI-Handler.

```
Game.EndTurn()
  └─ AiEventAccumulator befüllt (aus diesem Zug)
  └─ Turn.New(unit) für jede Einheit
       └─ Turn.Step()
                └─ AiPlayerAdapter.DispatchUnitTurn(unit)
                  ├─ turnSnapshot = AiContextFactory.BuildTurnSnapshot(player)
                  ├─ profile = AiPlayerProfileResolver.ResolveProfile(turnSnapshot.OwnPlayer)
                  ├─ ai = IAiPlayerFactory.CreateFor(profile)
                  ├─ ctx = turnSnapshot.CreateUnitContext(unit)
                  ├─ AiActionSinkImpl sink = new()
                  ├─ IAiPlayer.OnUnitTurn(ctx, sink)
                  └─ AiActionApplier.Apply(unit, sink)
                    ├─ validate whole call atomically
                    ├─ MoveAction        → unit.MoveTo(dx, dy)
                    ├─ FortifyAction     → unit.Fortify = true
                    ├─ WakeUpAction      → unit.Fortify = false; unit.Sentry = false
                    ├─ FoundCityAction   → GameTask.Enqueue(Orders.FoundCity(...))
                    └─ ...
```

City- und Research-Pfad analog. Der `AiEventAccumulator` wird nach `OnTurnStart` geleert.

---

## Verifikationskriterien

1. `IAiPlayer`-Implementierung hat **kein** `using CivOne;` auf interne Typen
2. `AiContextFactory` ist die **einzige** Klasse mit Singleton-Zugriff
3. Alle Context-Objekte sind materialisierte, immutable Snapshots — keine Live-Referenzen auf Engine-Objekte
4. Ein Turn-Snapshot wird pro KI-Spieler einmal gebaut und von Unit-/City-/Research-Contexts desselben Zugs wiederverwendet
5. `IAiActionSink` ist der einzige Ausgabekanal der KI — kein direktes Schreiben in Engine-Objekte
6. `AiActionApplier` validiert jeden Call atomar; Konflikte oder ungültige Kombinationen verwerfen den kompletten Call
7. Unit-turn flow: `Turn.Step()` → `AiPlayerAdapter` → Sink → `AiActionApplier`
8. City production: nur nach `City.NewTurn()` Abrechnung
9. Research: nur aus `ProcessScience`, Timing bleibt erhalten
10. KI-Auswahl läuft über Civilization-/Profil-Resolver, nicht hart über `playerId`
11. `DefaultAiPlayer` verhält sich wie die aktuelle Implementierung (Tests grün)
12. `WakeUpAction` ist im Action-Modell vorhanden — KI kann Fortify- und Sentry-Einheiten reaktivieren
13. `AiEventAccumulator` ist verdrahtet für: Einheit zerstört, Stadtproduktion fertig, Weltwunder fertig, Forschung abgeschlossen

---

## Designentscheidungen (festgelegt)

### Action Sink statt Return-Wert
`void OnUnitTurn(ctx, IAiActionSink sink)` statt `IReadOnlyList<AiUnitAction> OnUnitTurn(ctx)`. Der Sink entkoppelt Entscheidungszeitpunkt von Ausführungszeitpunkt. Phase 1 bleibt trotzdem synchron. `0..n` gilt nur auf Interface-Ebene; die Engine akzeptiert pro Call nur vollständig validierte, nicht widersprüchliche Action-Sets. Wenn später echte Mehrschritt-Sequenzen nötig werden, kommen dafür explizite Composite-Actions hinzu statt impliziter Konfliktkombinationen.

### Performance: materialisierte Turn-Snapshots
Context-Objekte sind **echte Snapshots**. Sie halten keine internen Referenzen und lesen nie live aus Engine-Objekten:

| Datenkategorie | Strategie |
|---|---|
| Skalare (GameTurn, Gold, MovesLeft) | Direkt kopiert — billig |
| Einheitenliste (`OwnPlayer.Units`) | Einmal pro Turn als kompakte Snapshot-Arrays materialisiert |
| Karte (`IAiMapView.GetTile(x,y)`) | Einmal pro Turn als immutable Tile-Snapshot-Buffer materialisiert; nicht sichtbare Felder werden redaktiert |
| Events | Aus `AiEventAccumulator` kopiert und danach vom Accumulator getrennt |
| `MovableUnits` | Aus Snapshot-Daten abgeleiteter Filter, keine Live-Query |

```csharp
// Intern — ein Snapshot-Root pro KI-Spieler und Zug:
internal sealed class AiTurnSnapshot
{
  public required AiPlayerSnapshot OwnPlayer { get; init; }
  public required AiMapSnapshot Map { get; init; }
  public required ImmutableArray<AiUnitSnapshot> Units { get; init; }
  public required ImmutableArray<AiCitySnapshot> Cities { get; init; }
}
```

**Konsequenz für Realtime/Async:** Weil die Contexts bereits echte Snapshots sind, bleibt derselbe Datenvertrag auch für spätere Async-Hosts gültig. Zusätzliche Async-Arbeit betrifft dann Scheduling und Cancellation, nicht Datenkonsistenz.

### Events als Context-Property
`IAiTurnContext.Events` enthält alle für diesen Spieler relevanten Ereignisse seit dem letzten Zug. Die KI empfängt sie beim ersten Entry Point des Zugs (`OnTurnStart` oder erstem `OnUnitTurn`). Der `AiEventAccumulator` wird danach geleert.

### MovableUnits als Convenience
`IAiTurnContext.MovableUnits` ist ein vorgefilterte Liste: Einheiten mit `HasAction == false && MovesLeft > 0`. Fortifizierte Einheiten sind nicht drin — sie müssen explizit via `WakeUpAction` reaktiviert werden.

### Kartensichtbarkeit
`IAiTurnContext.FogOfWar : bool`. Default `true`. Die `AiContextFactory` filtert Tiles entsprechend. Für Debug oder starke KI kann `false` gesetzt werden.

### SpecialResource als Enum
`IAiTileView.SpecialResource` wird als Enum modelliert, nicht als rohe numerische ID. Das hält das API für externe KI stabiler und lesbarer.

### Settler-Aktionen
`BuildRoadAction`, `BuildIrrigationAction`, `BuildMineAction`, `FoundCityAction` sind normale `AiUnitAction`-Typen. `AiActionApplier` verwirft den kompletten Call und loggt den Grund, wenn die Einheit kein Settler ist.

### Registrierung externer KI
```csharp
// api/src/AI/
public interface IAiPlayerProfileResolver
{
  string ResolveProfile(IAiPlayerView player);
}

public interface IAiPlayerFactory
{
    string Name { get; }
  IAiPlayer CreateFor(string profileKey);
}
```
Resolver vor Factory. Default-Resolver mappt `CivilizationId` oder explizite Profil-Konfiguration auf einen stabilen `profileKey`. `AiPlayerAdapter` fragt danach pro AI-Spieler die passende Factory an. `playerId` bleibt Identität, aber nicht Auswahlkriterium.

### Migrations-Strategie
Big Bang im Runtime-Pfad, nicht als lang laufender Mischbetrieb: Phase 1-4 bleiben Architekturschnitte, aber der Umschaltpunkt in `Turn.cs`, `City.cs` und `ProcessScience.cs` passiert gemeinsam in einer Branch/PR. Innerhalb dieser Branch wird zuerst Snapshot-/Resolver-Infrastruktur gebaut, dann `DefaultAiPlayer` fertiggestellt, dann werden die drei Einstiegspunkte gemeinsam umgelegt. `AI.Barbarians.cs` bleibt unberührt. Tests müssen vor und nach dem Umschaltpunkt grün sein.

---

## Offene Fragen

- Soll `OnTurnStart` ebenfalls einen `IAiActionSink` bekommen oder nur informational sein?
  - Aktuell: Sink vorhanden aber optional — KI muss keine Aktionen hier einreichen
- Soll `AiEventAccumulator` auch Ereignisse für Barbaren-Spieler akkumulieren?
  - Nein — Barbaren bleiben vollständig intern
