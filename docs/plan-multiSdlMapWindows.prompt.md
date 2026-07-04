## Plan: Multi-SDL-Mapfenster mit Fokus-Input

Ziel ist eine Architektur, in der ein Hauptfenster (vollständige UI/City/Screens) plus mehrere zusätzliche SDL-Fenster (nur Karte) parallel laufen. Interaktion (Unit wählen/bewegen) ist in Kartenfenstern erlaubt, aber nur im fokussierten Fenster aktiv. So bleiben Modal-Screens und City-Ansichten konsistent im Hauptfenster.

**Steps**
1. Phase 1 - SDL-Multi-Window-Basics (Basis, blockiert Folgephasen)
1.1. Bestandsverhalten kapseln: dokumentieren, welche Eventtypen bereits eine WindowId tragen und wo sie derzeit ignoriert werden (Window/Event-Handler).
1.2. Fenster-Identität pro SDL-Window einführen (WindowId erfassen/speichern).
1.3. Event-Routing nach WindowId ergänzen, sodass Eingaben nur das Ziel-Fenster erreichen.
1.4. Fokus-Tracking im SDL-Layer ergänzen, um Eingaben nur für das fokussierte Fenster zuzulassen.
1.5. Fenster-Shortcut-Routing festlegen: `Ctrl+N` öffnet nur aus dem fokussierten Fenster ein neues Kartenfenster, `Ctrl+1` bis `Ctrl+9` wirken nur im fokussierten Fenster als Eingabequelle.
2. Phase 2 - Fensterrollen trennen (depends on 1)
2.1. Hauptfenster bleibt unverändert als voller Screen-Host (City, Menüs, Modal).
2.2. Kartenfenster-Rolle definieren: nur Karten-Rendering plus mapbezogene Interaktionen (Tile/Unit/Move).
2.3. Sicherstellen, dass city-/screen-auslösende Aktionen aus Kartenfenstern an das Hauptfenster delegiert werden (z. B. City-Klick öffnet City nur dort).
2.4. Gemeinsame Kartenpositions-Semantik definieren: `Ctrl+1` bis `Ctrl+9` laden/speichern weiterhin globale Kartenpositionen, und eine Navigation darüber synchronisiert Hauptfenster und alle Zusatzfenster auf denselben Zielausschnitt.
3. Phase 3 - Map-Interaktion pro Fensterkontext (depends on 2)
3.1. Pro Zusatzfenster eigenen View-Kontext einführen (Viewport-Position, Zoom, Hover), getrennt von globalem UI-Stack.
3.2. Wiederverwendbare Map-Interaktionslogik aus den bestehenden GameMap-Delegates nutzen (Center, MoveTo, Hit-Testing), aber mit fensterspezifischem Kontext verbinden.
3.3. Globalen Konfliktpunkt "aktive Unit" absichern: Änderungen nur aus fokussiertem Fenster akzeptieren und über zentrale Queue in den Game-State übernehmen.
3.4. Zoom-Entkopplung: Human.MapZoomBasisPoints nicht mehr ungewollt zwischen Fenstern koppeln, sondern Fenster-Zoom explizit verwalten.
3.5. Erzeugungsregeln für Zusatzfenster einbauen: neue Fenster übernehmen beim Öffnen die aktuelle Kartenposition des Hauptfensters, starten aber immer mit 100 % Zoom.
4. Phase 4 - Lebenszyklus & Stabilität (parallel zu 3.3/3.4 möglich)
4.1. Erzeugen/Schließen mehrerer Kartenfenster robust machen (Dispose, Renderer, Texture-Caches) und auf maximal 10 Zusatzfenster begrenzen.
4.2. Fensterstil für Zusatzfenster festlegen: schließbar und resizable, aber ohne Minimieren- und Maximieren-Schaltflächen.
4.3. Größenregeln durchsetzen: Default-Größe 400x400 Pixel, Mindestgröße 200x200 Pixel, neue Fenster übernehmen Größe und Position des zuletzt geschlossenen Zusatzfensters, sonst Default-Größe.
4.4. Resize/Minimize/Restore pro Fenster korrekt behandeln (Redraw-Invalidierung pro Fenster), wobei die Position initial vom OS bestimmt werden darf.
4.5. Fail-safe: wenn Zusatzfenster geschlossen wird, darf Hauptfenster inklusive laufendem Spielzustand stabil bleiben.
4.6. Persistenz für Zusatzfenster-Lebenszyklus ergänzen: offene Fenster, gespeicherte Positionen und Größen müssen sauber in Save/Load ein- und ausgelesen werden.
5. Phase 5 - Tests & Nachweis (depends on 1-4)
5.1. Unit-Tests für Fenster-Ereignisrouting (WindowId/Fokus).
5.2. Unit-Tests für mapbezogene Eingaben mit separaten Fenster-Kontexten (Hit-Test, MoveTo-Dispatch, Zoom-Scope).
5.3. Unit-Tests für Fensterregeln: Maximum 10 Zusatzfenster, Mindestgröße 200x200, neues Fenster übernimmt letzten geschlossenen Bounds oder 400x400 als Default.
5.4. Save/Load-Tests für Zusatzfenster: Position, Größe und Zoom werden im SaveGame persistiert, aber nicht in Profileinstellungen gespiegelt.
5.5. Manuelle End-to-End-Prüfung mit 2-3 Zusatzfenstern: Unit wählen/bewegen, City-Klick -> Hauptfenster, Fokuswechsel, Schließen einzelner Fenster.
5.6. Manuelle Prüfung der Shortcuts: `Ctrl+N` öffnet Zusatzfenster mit Hauptfenster-Kartenposition; `Ctrl+1` bis `Ctrl+9` reagieren nur im fokussierten Fenster und synchronisieren danach alle Fenster.

**Relevant files**
- /home/christian/projekte/CivOne/runtime/sdl/src/SDL/Window.cs - zentrale Event-Loop, Routing, Lebenszyklus
- /home/christian/projekte/CivOne/runtime/sdl/src/SDL/Window.WindowEvent.cs - Fokus/Resize/WindowState-Ereignisse
- /home/christian/projekte/CivOne/runtime/sdl/src/SDL/Window.MouseEvent.cs - Maus-Input-Zuordnung pro Fenster
- /home/christian/projekte/CivOne/runtime/sdl/src/SDL/Window.KeyboardEvent.cs - Tastatur-Routing pro Fenster
- /home/christian/projekte/CivOne/runtime/sdl/src/SDL/Extern.cs - SDL-Bindings für WindowId/Fensterfunktionen
- /home/christian/projekte/CivOne/runtime/sdl/src/GameWindow.cs - Rollenaufteilung Hauptfenster vs Kartenfenster
- /home/christian/projekte/CivOne/runtime/sdl/src/Runtime.cs - Eventweitergabe in die Engine
- /home/christian/projekte/CivOne/runtime/sdl/src/Program.cs - Start/Lifecycle mehrerer Fenster
- /home/christian/projekte/CivOne/src/RuntimeHandler.cs - Screen/Input-Dispatch am Engine-Einstieg
- /home/christian/projekte/CivOne/src/Common.cs - globaler Screen-Stack/TopScreen (Konfliktzone)
- /home/christian/projekte/CivOne/src/Screens/GamePlayPanels/GameMap.cs - Karteninteraktion (Klick/Unit/Move)
- /home/christian/projekte/CivOne/src/Screens/GamePlayPanels/GameMapPositionDelegate.cs - Viewport/Center/MoveTo
- /home/christian/projekte/CivOne/src/Screens/GamePlayPanels/GameMapZoomDelegate.cs - Zoom-Verhalten

**Verification**
1. Build SDL-Runtime mit Linux-Debug-Konfiguration und prüfen, dass Multi-Window-Startpfad fehlerfrei kompiliert.
2. Automatisierte Tests für Event-Routing: gleiche Eingabe-Events mit verschiedenen WindowIds nur an jeweils ein Ziel.
3. Automatisierte Tests für Fokusregel: nicht-fokussierte Fenster dürfen keine Move-Aktion in den Game-State schreiben.
4. Automatisierte Tests für Persistenz: offene Zusatzfenster, Bounds und fensterspezifische Zoomwerte werden im SaveGame gespeichert und korrekt geladen.
5. Manuelle Prüfung: `Ctrl+N` öffnet aus dem fokussierten Fenster ein Zusatzfenster mit Hauptfenster-Kartenposition, 100 % Zoom und 400x400 Default-Größe, wenn kein zuletzt geschlossenes Fenster vorliegt.
6. Manuelle Prüfung: `Ctrl+1` bis `Ctrl+9` reagieren nur im fokussierten Fenster und synchronisieren anschließend die globale Kartenposition in Hauptfenster und Zusatzfenstern.
7. Manuelle Prüfung: 1 Hauptfenster + 2 Kartenfenster öffnen, in jedem Fenster unterschiedliche Kartenbereiche anzeigen, nur fokussiertes Fenster steuert.
8. Manuelle Prüfung: City-Klick im Kartenfenster führt zur City-Anzeige im Hauptfenster; Kartenfenster bleibt map-only.
9. Manuelle Prüfung: Zusatzfenster schließen/öffnen, Resize ohne Unterschreiten von 200x200, kein Minimieren/Maximieren, und Wiederverwendung der zuletzt geschlossenen Größe/Position ohne Verlust des Hauptfenster-Status.

**Decisions**
- In scope: mehrere zusätzliche Kartenfenster.
- In scope: Eingaben nur im fokussierten Fenster.
- In scope: Zusatzfenster map-only; City/andere Screens verbleiben im Hauptfenster.
- In scope: `Ctrl+N` öffnet neue Zusatzfenster, begrenzt auf 10 gleichzeitig.
- In scope: `Ctrl+1` bis `Ctrl+9` bleiben globale Kartenpositions-Slots, ausgelöst nur aus dem fokussierten Fenster.
- In scope: Zusatzfenster-Bounds und fensterspezifische Zoomwerte werden im SaveGame gespeichert.
- In scope: neue Zusatzfenster starten immer mit 100 % Zoom.
- Out of scope: vollständige Multi-UI-Fähigkeit in Zusatzfenstern (Modal/City direkt dort).
- Out of scope: netzwerk-/multiplayer-spezifische Synchronisationsmodelle.

**Further Considerations**
1. Empfehlung: Rollout in zwei Schritten - zuerst read-only Kartenfenster (nur Navigation), danach schreibende Aktionen (Unit Move), um Regressionen früh zu isolieren.
2. Empfehlung: Explizite Konfliktregel definieren, falls während laufender Move-Animation der Fensterfokus wechselt (Aktion abschließen vs. abbrechen).
3. Empfehlung: SaveGame-Format früh festziehen, damit Fensterliste, Bounds und Zoomwerte versionierbar gespeichert werden können.