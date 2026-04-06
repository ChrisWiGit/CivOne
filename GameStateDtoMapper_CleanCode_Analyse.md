# Clean-Code-Analyse: `GameStateDtoMapper`

**Datei:** `src/Persistence/Model/GameStateDtoMapper.cs`  
**Stand:** 6. April 2026

## Kurzfazit

Die Datei ist insgesamt **gut strukturiert** und bereits deutlich wartbarer als ein typischer großer Mapper. Positiv sind die sinnvolle Zerlegung in Hilfsmethoden, sprechende Namen und defensive Eingangsbehandlung. 

Das größte Clean-Code-Risiko liegt in **versteckten Seiteneffekten auf globalem Zustand** (`Player.Game`) und einer teils zu breiten Verantwortlichkeit der Orchestrierungsmethode `FromDto`.

---

## Stärken

1. **Lesbarer Ablauf in `FromDto`**
   - Reihenfolge ist nachvollziehbar (Map → Players → Validierung → Trading → Units → Cities → Build).
   - Guards und Sanitizing sind vorhanden.

2. **Sinnvolle Aufteilung der City-Mapping-Logik**
   - `MapCities`, `BuildCityNameCatalog`, `MaterializeCities`, `CreateCity`, `ApplyStatusFlags`, `ApplyProductionAndCollections`, `ApplyTradingLinks` sind klar benannt.

3. **Defensive Programmierung vorhanden**
   - `ArgumentNullException.ThrowIfNull(dto)`.
   - Werte-Clamping über `yamlReadValueSanitizer` reduziert Fehlerfolgen bei ungültigen Eingabedaten.

4. **Naming weitgehend konsistent**
   - Methoden und Variablen sind überwiegend selbsterklärend.

---

## Hauptprobleme (priorisiert)

## 1) Globaler Seiteneffekt in `FromDto` (hoch)

`Player.Game` wird temporär überschrieben und danach zurückgesetzt (`try/finally`).

### Warum kritisch?
- Unerwarteter globaler Nebeneffekt in einem Mapper.
- Erschwert Tests und Parallelität.
- Koppelt Mapping an impliziten Runtime-Kontext.

### Empfehlung
- Kapselung über ein injiziertes Kontext-Interface (Dependency Injection), statt direkten Zugriff auf globalen Zustand.

---

## 2) Zu viele Verantwortungen in `FromDto` (hoch)

Die Methode orchestriert Validierung, Mapping, Kontextmanagement, Sanitizing und Objektaufbau gleichzeitig.

### Risiko
- Änderungen werden fehleranfälliger.
- Schwerer isoliert zu testen.

### Empfehlung
- Pipeline-artige Zerlegung in fokussierte Schritte, z. B.:
  - `CreateMappingContext(...)`
  - `MapCoreState(...)`
  - `MapCitiesWithContext(...)`
  - `AssembleGameState(...)`

---

## 3) Inkonsistente Fehlersignalisierung (mittel-hoch)

`FindPlayerIndex(...)` wirft ein generisches `Exception`.

### Warum problematisch?
- Unscharfe Fehlersemantik.
- Schwerer gezielt behandelbar.

### Empfehlung
- `InvalidOperationException` oder domänenspezifische Exception nutzen.

---

## 4) `Debug.Assert` statt Laufzeitvalidierung in `MapMap` (mittel)

`Debug.Assert` schützt nur in Debug-Konfiguration.

### Empfehlung
- Für Input-Boundary-Prüfung echte Guards verwenden (`ArgumentNullException.ThrowIfNull(...)`, ggf. `ArgumentException`).

---

## 5) Redundante Null-Prüfung auf injizierte Dependency (mittel)

`CreateMapDto(...)` prüft `mapMapper != null`, obwohl `mapMapper` per Konstruktor injiziert wird.

### Empfehlung
- Invariants klar halten: Dependency einmalig am Konstruktorrand validieren und danach ohne Redundanz verwenden.

---

## Weitere Beobachtungen

- `BuildGameState(...)` verwendet ein Tupel für Map-Daten. Für Lesbarkeit/Erweiterbarkeit wäre ein kleines Value-Objekt (`record`/`struct`) klarer.
- In `ToDto(...)` wird pro Player `IndexOf` genutzt. Ein indexbasierter Loop ist direkter und effizienter.
- `ApplyProductionAndCollections(...)` ist noch verständlich, bündelt aber mehrere Teilverantwortungen.

---

## Konkrete Refactoring-Vorschläge (ohne Verhaltensänderung)

1. **Kontextzugriff abstrahieren**
   - Interface einführen, das den temporären Zugriff auf den benötigten Laufzeitkontext kapselt.

2. **`FromDto` weiter entkoppeln**
   - Kleine private Schritte mit klarer Verantwortlichkeit.

3. **Exception-Qualität verbessern**
   - Generisches `Exception` in `FindPlayerIndex` ersetzen.

4. **Boundary-Validation vereinheitlichen**
   - Assertions durch echte Guards ergänzen/ersetzen.

5. **Lesbarkeit steigern**
   - Map-Tupel durch benannten Typ ersetzen.
   - `ToDto`-Loop indexbasiert formulieren.

---

## Gesamtbewertung

- **Lesbarkeit:** gut
- **Wartbarkeit:** gut mit klaren Verbesserungshebeln
- **SOLID-/Clean-Code-Konformität:** solide Basis, mit Schwächen bei globalem Zustand und Responsibility-Schnitt

**Gesamt:** **7.5/10**
