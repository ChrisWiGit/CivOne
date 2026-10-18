# Plugin-System

Stand: 2026-08-23. Zweck dieses Dokuments: festhalten, was am Plugin-System umgebaut wurde,
warum, und woran später angeknüpft werden kann.

---

## Ausgangslage

Das Plugin-System existierte, war aber in wesentlichen Teilen wirkungslos:

* **Modifikationen wurden nie gefunden.** `Reflect.PluginModifications` prüfte mit
  `x.GetInterfaces().Contains(typeof(Modification))`. `Modification` ist eine abstrakte **Klasse**,
  kein Interface — die Bedingung war immer `false`. `CivilizationModification`,
  `UnitModification`, `LeaderModification` und `MenuModification` hatten damit null Wirkung.
* **Plugin-Assemblies waren für die Typ-Discovery unsichtbar.** `Reflect.GetAssemblies` lieferte nur
  die CivOne-Assembly. Plugins konnten keine Inhalte beitragen.
* **Kein Fehlerschutz.** Eine korrupte DLL im Plugin-Ordner riss den Start mit.
* **Doppeltes Laden.** `Reflect.LoadPlugin` lud dieselbe Datei zweimal in den Default-Kontext.
* **Kein Entladen.** Deaktivieren filterte nur; die Assembly blieb bis Prozessende im Speicher.
* **`api/src/Plugin/` war eine tote API-Oberfläche.** Drei Capability-Interfaces ohne einen
  einzigen Consumer.
* **Plugin-Verwaltung in einer statischen Utility-Klasse** mit statischem Mutable-State — nicht
  testbar, gegen die DI-Regeln aus `.claude/CLAUDE.md`.

---

## Runde 1 — Die drei Blocker

| Fix | Datei |
| --- | --- |
| `typeof(Modification).IsAssignableFrom(x)` statt `GetInterfaces().Contains(...)` | `src/Reflect.cs` |
| Aktivierte Plugin-Assemblies in die Typ-Discovery aufnehmen | `src/Reflect.cs` |
| Fehlerschutz pro DLL, Verzeichnisprüfung, `ReflectionTypeLoadException` abfangen | `src/Reflect.cs` |

Begleitend nötig:

* **`Common.ResetContentCaches()`** — ohne Cache-Invalidierung hätten `Common.Advances`,
  `Buildings` und `Wonders` beim Ein-/Ausschalten eines Plugins veraltete Listen behalten.
* **`Reflect.IsInstantiable`** — Plugin-Assemblies enthalten konkrete Typen ohne parameterlosen
  Konstruktor. Ohne diesen Filter hätte `Activator.CreateInstance` die gesamte Enumeration
  gesprengt. Für die Kern-Assembly verhaltensneutral.

Statt `catch (Exception)` (CA1031 ist im Projekt ein Error) filtert eine `when`-Klausel auf die
realistischen Ladefehler. Bugs im Spielcode propagieren weiterhin.

---

## Runde 2 — Der Hauptumbau

### 2.1 Isoliertes Laden und echtes Entladen

**`src/Services/Plugins/PluginLoadContext.cs`** — ein `AssemblyLoadContext` mit
`isCollectible: true` pro Plugin.

Zwei Entwurfsentscheidungen, die nicht offensichtlich sind:

1. **Laden aus Bytes, nicht aus dem Pfad.** `LoadFromStream` über `File.ReadAllBytes` lässt kein
   Dateihandle offen. Das ist zwingend, weil `Plugin.Delete()` die Datei löscht und der
   Overwrite-Dialog sie überschreibt — bei einem Pfad-Load würde beides an der Sperre scheitern.
2. **`Load(AssemblyName)` gibt `null` zurück für alles, was schon im Default-Kontext liegt.**
   Das ist die kritischste Stelle des ganzen Umbaus. Lädt der Kontext eine eigene Kopie von
   `CivOne.API`, ist ein Plugin-Typ nicht mehr zu `IPlugin`, `Modification` oder `IUnit`
   zuweisbar — **ohne jede Fehlermeldung**. Das Plugin wirkt dann einfach wirkungslos.
   Deshalb gibt es dafür einen eigenen Test (`PluginAssembly_UsesTheHostContracts`).

Plugin-eigene Abhängigkeiten laufen über `AssemblyDependencyResolver`. Eine fehlende `.deps.json`
ist der Normalfall und kein Fehler — dann greift der Default-Fallback.

**`src/Plugin.cs`**

* `Unload()` / `Reload()` / `IsUnloaded`.
* Metadaten (`Name`, `Author`, `Version`) werden beim Laden **gesnapshottet**, damit das
  Einstellungsmenü sie weiter anzeigen kann, wenn die Assembly entladen ist.
* Die `IPlugin`-Instanz wird nach dem Auslesen der Metadaten **nicht** festgehalten — jede
  gehaltene Referenz würde das Entladen blockieren.
* Doppeltes Laden aufgelöst: `LoadPlugin` verlässt sich auf `Load` → `null`.
* `Validate(string)` bleibt bestehen, weil `FileSystem.CopyPlugins` eine **andere** Datei prüft
  als die später geladene (Quelle vor dem Kopieren vs. Ziel danach). Es lädt jetzt aber in einen
  Wegwerf-Kontext, der sofort entladen wird — vorher pinnte jede Prüfung eine Assembly dauerhaft.
* `_seed` auf `Interlocked.Increment`. `Id` wird vom Plugin-Menü gebraucht, ist also nicht
  entfernbar.

### 2.2 `IPluginService`

Neue Dateien unter `src/Services/Plugins/`:

| Datei | Aufgabe |
| --- | --- |
| `IPluginService.cs` | Vertrag: `Plugins`, `EnabledAssemblies`, Capability-Listen, `LoadPlugin`, `ApplyPlugins`, `OnPluginStateChanged` |
| `PluginService.cs` | Verzeichnis-Scan, Fehlerbehandlung, ALC-Verwaltung, Capability-Discovery |
| `PluginServiceFactory.cs` | Gecachter Singleton mit `Create()` / `Override()` / `Reset()` |
| `PluginFailure.cs` | Klassifiziert Plugin-Ladefehler für die `when`-Filter |
| `PluginAiRegistrationDelegate.cs` | Hält die Plugin-AIs mit der Agent-Registry synchron |

`Reflect` bleibt als **dünne Fassade** bestehen und delegiert. Alle rund 40 bestehenden
`Reflect.*`-Call-Sites kompilieren unverändert weiter. Das war eine bewusste Scope-Entscheidung:
nur der Plugin-Teil wurde herausgezogen, die Content-Enumeration bleibt statisch.

Zwei Eigenschaften, die leicht übersehen werden:

* **`EnabledAssemblies` löst nie eine Plugin-Suche aus.** Vor dem ersten expliziten Laden ist die
  Liste leer. Sonst würde jeder gewöhnliche Zugriff auf Spielinhalte `Settings.Instance` und den
  Plugin-Ordner nachziehen — genau das, was die Static-Initializer-Regel in `CLAUDE.md` verbietet.
* **`PluginService` löst `ISettings` und `ITranslationService` lazy auf.** Der Konstruktor darf
  keine Singletons anfassen, damit ein Unit-Test die Klasse ohne laufende Engine bauen kann.

### 2.3 Reihenfolge beim Zustandswechsel

In `PluginService.OnPluginStateChanged` ist die Reihenfolge nicht beliebig:

```
1. bei Aktivierung:  Reload()
2. immer:            ApplyPlugins()   <- baut Referenzen ab
3. bei Deaktivierung: Unload()
```

`ApplyPlugins()` **vor** `Unload()`, weil es die Modification-Dictionaries leert und die
Content-Caches verwirft. Umgekehrt bliebe der Ladekontext am Leben.

### 2.4 Referenzabbau

Damit der collectible ALC überhaupt greifen kann:

* **`src/Screens/Civilopedia.cs`** — sechs *eager* statische Arrays voller instanziierter
  Plugin-Typen wurden auf das Lazy-Muster plus `ResetCaches()` umgestellt. Das behebt zugleich
  einen direkten Verstoß gegen „Static Initializers Must Not Need a Runtime" aus `CLAUDE.md`:
  die Initializer riefen `Reflect.*` und damit `RuntimeHandler.Runtime`.
* **Modification-Dictionaries** in `BaseCivilization`, `BaseLeader`, `BaseUnit` — die
  `LoadModifications()` leeren ihr Dictionary bereits am Anfang, es zählt also nur die Reihenfolge.
* **`Common.ResetContentCaches()`** ruft zusätzlich `Civilopedia.ResetCaches()`.

Nebenbei: `BaseUnit.LoadModifications` enumerierte `GetModifications` zweimal — auf eine
Enumeration reduziert.

**Einschränkung, die bleibt:** Das Entladen ist *Best Effort*. Läuft ein Spiel, in dem Einheiten
oder Zivilisationen aus einem Plugin auf der Karte stehen, halten diese Instanzen den Kontext am
Leben. Der Speicher wird erst frei, wenn das Spiel beendet ist. Das steht so im XML-Doc von
`Plugin.Unload()`.

### 2.5 Descriptor-Identität: `string` → `Guid`

Vorher hatte `AiDescriptor` zwei Identitäten: `Id` (string, plugin-intern) und die Guid aus
`IAgentInformation.GetUuid()` — letztere ist die, die in `AgentRegistry` registriert wird und als
`Player.AiId` **im Spielstand landet**. Die Guid erfuhr der Host aber erst, *nachdem* er `CreateAi`
gerufen hatte. Um nur das Auswahlmenü zu bauen, hätte er jede AI-Variante instanziieren müssen.

Jetzt ist `Id` eine `Guid` — in allen drei Descriptors, konsistent. Ein Wert dient als
Varianten-Selektor gegenüber dem Plugin, als Registry-Schlüssel und als Spielstand-Schlüssel.
Bei Abweichung zu `GetUuid()` gewinnt der Descriptor (er ist der persistierte Wert), die
Abweichung wird geloggt.

Das Save-Format ist unberührt: `Player.AiId` war immer schon eine Guid.

### 2.6 AI-Provider verdrahtet

`AgentRegistry` konnte nur *eager* registrieren. Neu:

* **`RegisterLazy(Guid, AiDefinition, Func<IAgentRegistration>)`** — legt Definition und Factory ab.
  `TryMaterialize` erzeugt bei erstem Zugriff und cacht in `_agentsById`.
* **`Unregister(Guid)`** — fehlte vollständig. Ohne das bliebe eine deaktivierte Plugin-AI im
  Auswahlmenü stehen. Entfernt auch Spielerbindungen auf diese AI.

`GetRegisteredDefinitions()` liest `_definitionsById` und deckt lazy Einträge damit automatisch ab.
Der Descriptor liefert exakt die Felder, die `AiDefinition` braucht — **das Menü entsteht ohne jede
Instanziierung.**

Aufrufzeitpunkt ist `ApplyPlugins()`, also einmal beim Start über `RuntimeHandler` und erneut bei
jeder Plugin-Änderung. Bewusst **nicht** im statischen Konstruktor von `AgentLoaderEntry` — der
läuft zu früh und würde erneut gegen die Static-Initializer-Regel verstoßen.

### 2.7 Map-Generator und Image: Discovery ohne Consumer

Alle drei Provider-Interfaces werden entdeckt und instanziiert. Gescannt wird die ganze Assembly,
nicht nur der `CivOne.Plugin`-Einstiegspunkt — ein Plugin darf Capabilities bündeln oder trennen.

`MapGeneratorProviders` und `ImageProviders` werden exponiert, aber **von nichts aufgerufen**. Das
ist an zwei Stellen festgehalten: im XML-Doc der Interfaces und der Service-Properties, sowie als
Abschnitt in `TODO.md` mit den konkret fehlenden Andockpunkten.

---

## Runde 3 — `AiDifficulty` in die API

`AiDifficulty` lag in `src/Agents/AiProfiles.cs`, also in der CivOne-Assembly. Ein Plugin, das nur
`CivOne.API` referenziert, konnte es nicht benutzen — deshalb war `DefaultDifficulty` ein `int?`
und im Beispielcode stand eine kommentierte `2`.

Das Enum liegt jetzt in **`api/src/Agents/AiDifficulty.cs`**, Namespace bleibt `CivOne.Agents`.
Dadurch musste **kein einziges `using`** angepasst werden; alle 17 bestehenden Nutzungsstellen
kompilierten unverändert. In `AiProfiles.cs` steht ein Verweis auf den neuen Ort.

Folgeänderung: `AiDescriptor.DefaultDifficulty` ist jetzt `AiDifficulty` mit Default `Unspecified`.
Die Validierung bleibt — Plugin-Code kann auch in ein Enum-Feld einen undefinierten Wert casten,
deshalb prüft `PluginAiRegistrationDelegate` mit `Enum.IsDefined` und fällt sonst auf `Unspecified`
zurück.

---

## Runde 4 — Schwierigkeit pro Spieler

**Der Auslöser war eine berechtigte Rückfrage.** Ich hatte zunächst in `AiCreationContext`
geschrieben, eine Registrierung könne „keine per-Spieler-Schwierigkeit tragen". Das erweckte den
falschen Eindruck, alle KI-Spieler hätten dieselbe Schwierigkeit.

**Tatsächlich ist die Schwierigkeit pro Spieler:**

* `Player.AiDifficulty` (`src/Player.cs`) ist ein Feld je Spieler.
* `NewGameAiSelection` lässt pro Gegner-Zeile **AI und Schwierigkeit getrennt** wählen.
* `NewGameAiSetupDelegate` schreibt die Auswahl auf den jeweiligen Player.

Geteilt wird nur das **Registrierungs-Objekt**: `AgentRegistry` bildet Guid → *eine*
`IAgentRegistration` ab, die sich alle Spieler mit dieser AI teilen. Zum Erzeugungszeitpunkt gibt
es noch keinen Spieler.

Das eigentliche Problem war aber: `ITurnContext` bot **keine Schwierigkeit**. Ein Plugin konnte sie
also nirgends erfahren — weder beim Erzeugen noch pro Zug. Und was in `AiCreationContext.Difficulty`
durchgereicht wurde, war der Wert *aus dem Descriptor*, also das, was das Plugin selbst deklariert
hatte. Ein Rundlauf ohne Informationsgewinn.

**Lösung:**

* **`ITurnContext.Difficulty`** neu (`api/src/Agents/TurnSession.cs`). Die Implementierung in
  `TurnBasedAgentHost.TurnContext` ist ein Einzeiler: `player.AiDifficulty`. Der `Player` lag dem
  Kontext ohnehin schon im Konstruktor vor.
* **`AiCreationContext.Difficulty` entfernt.** Der Record trägt nur noch `TranslationService`,
  mit einem `<remarks>`, das auf `ITurnContext.Difficulty` verweist.

Damit sind zwei Dinge sauber getrennt:

| | Bedeutung | Zeitpunkt |
| --- | --- | --- |
| `AiDescriptor.DefaultDifficulty` | Empfehlung des Plugins, füllt das Menü vor | Registrierung |
| `ITurnContext.Difficulty` | tatsächliche Wahl für *diesen* Spieler | jeder Zug |

Zwei bestehende `FakeContext`-Testdoubles mussten die neue Property ergänzen — der einzige Bruch
durch die Interface-Erweiterung.

---

## Architektur jetzt

```
Plugin-Datei (.dll)
   └─ PluginLoadContext          collectible, lädt aus Bytes,
        │                        teilt CivOne.API mit dem Default-Kontext
        └─ Plugin                Metadaten gesnapshottet, Unload/Reload
             │
   PluginService                 Scan, Fehlerbehandlung, Capability-Discovery
        ├─ EnabledAssemblies ──> Reflect.GetAssemblies    (Inhalte)
        │                        Reflect.PluginModifications
        ├─ AiProviders ────────> PluginAiRegistrationDelegate
        │                             └─> AgentLoaderEntry.RegisterLazy
        │                                      └─> AgentRegistry
        ├─ MapGeneratorProviders   (entdeckt, kein Consumer)
        └─ ImageProviders          (entdeckt, kein Consumer)
```

**Ablauf einer Plugin-AI:**

```
GetAiDescriptors()  ──> AiDefinition ──> Auswahlmenü        (keine Instanziierung)
Spieler wählt       ──> Player.AiId = descriptor.Id         (im Spielstand)
erster Zug          ──> TryResolveAi ──> CreateAi()         (jetzt erst)
jeder Zug           ──> OnTurn(session)
                        session.Context.Difficulty          (pro Spieler)
```

---

## Verifikation

**1036 Tests grün**, davon neu:

| Testdatei | Deckt ab |
| --- | --- |
| `xunit/src/Plugins/PluginServiceTests.cs` | Discovery, Modification-Erkennung, Typidentität, Capabilities, lazy `CreateAi`, Fehlerpfade |
| `xunit/src/Plugins/PluginUnloadTests.cs` | Unload, Reload, Collection des Ladekontexts via `WeakReference` |
| `xunit/src/TurnContextDifficultyTests.cs` | Schwierigkeit pro Spieler |
| `xunit/src/Plugins/PluginTestFixture.cs` | Isoliertes Plugin-Verzeichnis pro Test |

**`xunit/TestPlugin/`** — ein echtes Plugin-Projekt, das nur `CivOne.API` referenziert. Es wird
gebaut, aber bewusst **nicht referenziert** (`ReferenceOutputAssembly="false"`): eine Referenz
würde seine Typen in den Default-Kontext holen und die Isolation untestbar machen. Die Tests
kopieren die DLL in ein temporäres Verzeichnis und lesen Zählerstände per Reflection zurück.

Damit sind die Fixes aus Runde 1 **erstmals nachgewiesen** — vorher gab es kein Test-Plugin und
damit keine Möglichkeit, sie zu verifizieren.

---

## Was als Nächstes drankommen könnte

### Nicht gefixt, bewusst stehen gelassen

1. **`DisabledPlugins`-Logik ist fragwürdig.** Unverändert aus dem Altbestand übernommen:

   ```csharp
   if (_plugins.Any(x => !disabledPlugins.Contains(x.Filename)))
       Settings.Instance.DisabledPlugins = [.. _plugins.Where(x => !x.Enabled).Select(x => x.Filename)];
   ```

   Der Guard hat keine erkennbare Semantik, und der Effekt ist: `DisabledPlugins` wird auf die
   *aktuell vorhandenen* Dateien reduziert. Ein temporär fehlendes Plugin **verliert seinen
   Disabled-Status** und ist nach der Rückkehr wieder aktiv. Sollte auf „nur bekannte Einträge
   entfernen, unbekannte behalten" umgestellt werden.

2. **`DisabledPlugins` ist ein `;`-getrennter String ohne Escaping** (`src/Settings.cs`).
   Ein Dateiname mit Semikolon zerlegt die Liste. Steht außerdem nicht auf `ISettings`, deshalb
   greift `PluginService` dafür weiterhin direkt auf `Settings.Instance` zu.

3. **Keine Warnung, wenn eine gespeicherte AI fehlt.** Wird ein Plugin entfernt, dessen `AiId` in
   einem Spielstand steht, fällt `AgentBindingResolver.Resolve` **stillschweigend** auf
   `BuiltInTurnBasedAgentRegistration` zurück. Der Spieler merkt nur, dass sich der Gegner anders
   verhält. Ein Log oder ein Hinweis beim Laden wäre angebracht.

4. **`Reflect` ist weiterhin eine statische Fassade.** Die Content-APIs (`GetUnits`, `GetAdvances`,
   …) sind unverändert statisch und damit nicht testbar. Das war die gewählte Scope-Grenze; eine
   Vollmigration der ~40 Call-Sites steht aus. Die Konvention im Projekt wäre dabei nicht ein
   breites `IReflect`, sondern schmale Resolver pro Fähigkeit — wie bereits `IGovernmentResolver`,
   `IUnitFactory`, `IAdvanceResolver`.

5. **Keine Thread-Sicherheit.** `PluginService`, `PluginServiceFactory` und `AgentRegistry` sind
   nicht synchronisiert. Relevant, weil `RuntimeHandler` die Civilopedia in `Task.Run` vorlädt und
   dieser Pfad über `GetAssemblies` auf `EnabledAssemblies` zugreift. Ein paralleler Plugin-Vorgang
   wäre ein Race. War vorher genauso, ist aber jetzt an einer klar benennbaren Stelle.

6. **`Plugin.Validate` führt weiterhin Plugin-Code aus.** Der Wegwerf-Kontext behebt das Pinnen,
   aber die Assembly wird geladen — Modul-Initialisierer können laufen. Für eine reine Prüfung wäre
   ein `MetadataLoadContext` korrekt, der nur Metadaten liest und nichts ausführt.

7. **Keine API-Versionsprüfung.** Ein Plugin, das gegen eine ältere `CivOne.API` gebaut wurde, lädt
   möglicherweise und scheitert erst später an einem fehlenden Member. Eine deklarierte
   Mindestversion in `IPlugin` samt Prüfung beim Laden würde das früh und verständlich abfangen.

8. **Keine Sicherheitsgrenze.** Plugins laufen mit vollen Rechten, ohne Signaturprüfung. Für ein
   Einzelspieler-Spiel vertretbar, sollte aber bewusst dokumentiert sein.

### Offene Features

1. **Map-Generator-Provider verdrahten** — Auswahlmenü in `CustomizeWorld`, Verzweigung in
   `Map.Generate`, `SupportedSizes` an das Größenmenü koppeln. Details in `TODO.md`.

2. **Image-Provider verdrahten** — `ImageStore`-Abfrage im `Picture`-Indexer von `Resources`,
    Cache-Invalidierung beim Wechsel, Auswahl-UI, Laden der Ressourcen aus der Plugin-Assembly.
    Details in `TODO.md`.

3. **Schwierigkeit pro Spieler auch bei der Erzeugung**, falls je gebraucht: `AgentRegistry`
    müsste nach (AI-Guid, Spieler) schlüsseln statt nur nach AI-Guid. Aktuell nicht nötig, weil
    `ITurnContext.Difficulty` den Bedarf deckt.

4. Mein Senf: KI Plugin nur für COS-Spielstände erlaubt, und dann muss noch vermutlich die KI-Variante im Spielstand gespeichert werden, damit sie beim Laden wiederhergestellt werden kann. Wenn die KI-Variante nicht mehr verfügbar ist, sollte das Spiel den Spieler darauf hinweisen und eine Fallback-KI auswählen (lassen) oder auf die Original-KI zurückfallen.

### Noch nicht erledigt

 1. **Manuelle Verifikation steht aus.** `Setup → Plugins`: listen, aktivieren, deaktivieren,
    löschen; danach Civilopedia auf Plugin-Inhalte prüfen; neues Spiel starten und die Plugin-AI im
    Gegner-Auswahlmenü suchen. Das lässt sich nicht durch Tests ersetzen.

 2. **`CHANGES.md` ist nicht aktualisiert.** Das Projekt pflegt eine Änderungshistorie; diese
    Umbauten stehen noch nicht darin.

 3. **Keine Tests für den Drag-&-Drop-Pfad** (`FileSystem.CopyPlugins` → `Plugin.Validate` →
    `Reflect.LoadPlugin`) und für den Overwrite-Dialog.

 4. **Kein Plugin-Entwicklerhandbuch.** `xunit/TestPlugin/` ist derzeit das einzige vollständige
    Beispiel für Einstiegspunkt, Modification und AI-Provider — als Vorlage brauchbar, aber nicht
    als Dokumentation gedacht.
