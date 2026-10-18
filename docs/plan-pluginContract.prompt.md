## Plan: Plugin Contract for AI, Visuals, Behavior, Mapgen

Ziel ist ein einheitlicher Plugin-Contract mit klaren Capabilities, strikt-null bei unsupported Features und einem eingebauten Default-Plugin-Pfad, sodass der Übergang ohne neue Screens und ohne externe DLL-Pflicht starten kann.

**Steps**
1. Phase 1, Contract-Seam im API-Layer: neues Root-Plugin-Contract-Modell definieren (Plugin-Metadaten, Capability-Probes, Provider-Listen für KI und Map-Generatoren, optionale Single-Provider für Visual/Behavior).
2. Phase 1, Default-Provider im Core: bestehende Legacy-Implementierungen als Builtin-Provider kapseln, damit ohne externe Plugins immer ein funktionaler Default existiert.
3. Phase 1, zentrale Registrierung: eine Runtime-Registry einführen, die beim Start immer zuerst Builtin-Provider registriert und optional externe Plugins ergänzt; Auswahl erfolgt nur über Registrierung/Config, ohne UI-Änderung.
4. Phase 1, Routing-Umweg aktivieren: KI- und Mapgen-Aufrufe auf Registry-Auflösung umstellen, dabei Verhalten der bisherigen Defaults beibehalten; Unsupported-Features werden über null signalisiert und auf Builtin fallback geroutet. depends on 1-3.
5. Phase 1, Exklusivitätsregeln durchsetzen: Visual und Behavior global exklusiv halten (genau ein aktiver Provider je Kategorie), KI und Mapgen als Mehrfach-Liste führen (mehrere Einträge pro Plugin möglich). depends on 3.
6. Phase 2, Auswahl-Metadaten erweitern: Descriptor-Strukturen für KI/Mapgen/Visual/Behavior finalisieren (Name, Beschreibung, Version, Difficulty-Bereich, Plugin-Zuordnung), damit spätere Auswahl-Screens direkt konsumieren können. parallel with 7.
7. Phase 2, Startup-/Settings-Integration vorbereiten: Konfigurationsschlüssel für gewählte KI/Mapgen/Visual/Behavior einführen und auf Registry-Lookups mappen; weiterhin ohne neue Screens. parallel with 6.
8. Phase 3, UI-Später-Anschluss vorbereiten: nur technische Read-Modelle und Sortierungsvorgaben liefern (Plugin-Header + Einträge), damit künftige AoE-artige Selektionsscreens ohne Contract-Änderung gebaut werden können. depends on 6-7.

**Relevant files**
- [api/src/IPlugin.cs](api/src/IPlugin.cs) — bestehender Plugin-Basisvertrag, Startpunkt für Contract-Erweiterung.
- [src/Plugin.cs](src/Plugin.cs) — Loader-Verhalten, Aktivierung und Validierung externer Assemblies.
- [src/Reflect.cs](src/Reflect.cs) — vorhandene Plugin-Discovery/Reflection-Mechanik als Basis für Registrierung.
- [src/AI.cs](src/AI.cs) — Legacy-KI als Builtin-Provider kapseln.
- [src/AI.Barbarians.cs](src/AI.Barbarians.cs) — Barbarian-Sonderfall in Builtin-Provider-Strategie aufnehmen.
- [src/Map.Generate.cs](src/Map.Generate.cs) — Standard-Mapgen als Builtin-Generator kapseln.
- [src/RuntimeHandler.cs](src/RuntimeHandler.cs) — zentraler Initialisierungspunkt für Registry-Bootstrap.
- [src/Tasks/Turn.cs](src/Tasks/Turn.cs) — KI-Routing-Aufrufe auf Registry-basierte Auflösung umstellen.
- [src/City.cs](src/City.cs) — CityProduction-KI-Routing prüfen und umstellen.
- [src/Tasks/ProcessScience.cs](src/Tasks/ProcessScience.cs) — Research-KI-Routing prüfen und umstellen.
- [docs/AI/README.md](docs/AI/README.md) — API-Dokumentation um neues Plugin-Contract-Modell ergänzen.

**Verification**
1. Contract-Tests: prüfen, dass Capability-Methoden bei unsupported strikt null zurückgeben und keine Exception werfen.
2. Default-Routing-Tests: ohne externe Plugins müssen KI und Mapgen identisches Default-Verhalten liefern wie bisher.
3. Exklusivitäts-Tests: bei Visual/Behavior darf immer nur ein aktiver Provider gleichzeitig aufgelöst werden.
4. Mehrfach-Provider-Tests: ein Plugin mit mehreren KI- und Mapgen-Einträgen wird vollständig gelistet und korrekt auflösbar.
5. Smoke-Test Spielstart: Start ohne Plugin-DLL, mit Builtin-Registrierung, KI-Zug und Kartengenerierung funktionieren.
6. Kompatibilitäts-Test: bestehende Modification-basierte Plugins bleiben ladbar und werden nicht regressiv beeinflusst.

**Decisions**
- Visual und Behavior sind global exklusiv (genau ein aktiver Provider je Kategorie).
- Unsupported wird strikt mit null signalisiert (kein Empty-Object-Ersatz).
- Phase 1 arbeitet ohne neue UI-Screens; Auswahl zunächst nur über Registrierung/Config.
- Builtin-Default ist verpflichtend und Teil des Projekts (kein DLL-Zwang für Debugging/Übergang).

**Further Considerations**
1. Capability-Granularität klein halten (zuerst grob: KI, Mapgen, Visual, Behavior), Detail-Capabilities erst ergänzen, wenn der erste reale Plugin-Fall sie benötigt.
2. Für spätere UI direkt stabile Sortierregeln festlegen: zuerst Plugin-Name als Gruppenkopf, darunter Provider-Einträge.
3. Contract-Version früh einführen, auch wenn zunächst nur v1 existiert, um harte Brüche später zu vermeiden.