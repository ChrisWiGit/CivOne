## Plan: DTO Diagnostics + Overflow Safety

Ein zweigeteilter Ansatz minimiert Risiko und liefert schnell Nutzen: Teil 1 implementiert einen regelbasierten Diagnose-Service auf DTO-Ebene (inkl. diagnostics.txt), Teil 2 ergänzt technische Overflow-/Underflow-Absicherung in Rechenpfaden und Build-Konfiguration. So können ungültige Spielzustände früh sichtbar gemacht werden, ohne direkt Gameplay-Logik zu blockieren.

**Steps**
1. Phase A - Diagnosemodell und Regeln definieren
2. Definiere ein neutrales Diagnosemodell mit Severity, Code, Feldpfad, Ist-Wert, Erwartung und Nachricht; Ergebnisobjekt enthält Summary (Errors/Warnings/Info) und Einzelbefunde. *Voraussetzung für alle weiteren Schritte*
3. Definiere Regelprofile:
4. `DefaultDiagnosticsProfile` (nur Analyse, keine Blockierung) und `SveCompatibilityProfile` (später als Save-Gate nutzbar). *parallel zu Schritt 2 möglich*
5. Implementiere erste harte Regeln (Error): Map-Größe muss 80x50 für SVE-Profil, Player-Index-Grenzen (HumanPlayer/CurrentPlayer), Array-/Listenkonsistenz (z. B. Diplomacy-Ziele innerhalb gültiger Range), null-kritische Felder.
6. Implementiere erste weiche Regeln (Warning): ungewöhnlich große, aber typkompatible Werte (z. B. Gold/Science/FutureTech) als Plausibilität statt harter Fachgrenze.

7. Phase B - Integration in Save/Export-Flow
8. Füge einen Service (z. B. `IGameStateDiagnosticsService`) in den Persistenzfluss ein, direkt nach DTO-Erzeugung und vor finalem Schreiben. *depends on 1-6*
9. Ergänze einen Writer für diagnostics.txt, der in den Save-Pfad schreibt (über bestehenden Pfadprovider), inkl. Header (Zeitpunkt, Profil, Ergebniszusammenfassung) und detaillierten Findings.
10. Halte das Verhalten zunächst strikt diagnostisch: Save wird nicht blockiert; Findings werden protokolliert und als Datei ausgegeben.
11. Plane einen Feature-Flag/Setting-Hook, um später bei `SveCompatibilityProfile` und Error-Findings das SVE-Speichern gezielt zu verhindern. *depends on 8-10*

12. Phase C - Overflow-/Underflow-Absicherung (technische Sicherheit)
13. Aktiviere Overflow-Checks für Debug-Builds (Projektkonfiguration), damit arithmetische Überläufe in kritischen Pfaden früh auffallen.
14. Identifiziere und härte kritische numerische Rechen-/Cast-Pfade (insb. int->short/byte/ushort) durch `checked`-Kontext oder zentrale Safe-Cast-Helfer mit Logging.
15. Nutze vorhandenes Sanitizer-Logging als Diagnose-Signal (Clamping-Ereignisse aggregieren), um potenziell bereits beschädigte Werte sichtbar zu machen.
16. Definiere klar: DTO-Diagnose erkennt Zustandsverletzungen im Snapshot; sie kann historische Wrap-Around-Effekte nicht vollständig rekonstruieren. Deshalb ist diese Phase verpflichtend zusätzlich zu Phase A/B.

17. Phase D - Tests und Einführung
18. Ergänze Unit-Tests für Diagnoseregeln (harter Fehler bei Map != 80x50 im SVE-Profil, Warnings bei Plausibilitätsgrenzen, keine False-Positives bei gültigen Zuständen).
19. Ergänze Integrationstest für Diagnoseausgabe (diagnostics.txt entsteht, enthält Summary + Findings).
20. Ergänze Tests für Overflow-Schutz (Debug/checked-Pfade, erwartete Exceptions oder dokumentiertes Fail-Verhalten).
21. Führe stufenweise ein: erst Report-only in bestehendem Save-Flow, danach optionales Gate nur für SVE.

**Relevant files**
- /home/christian/projekte/CivOne/src/Persistence/GameStateHandler.cs - Snapshotquelle, enthält Legacy-Indizien für 80x50 und ist zentral für Save-Vorbedingungen.
- /home/christian/projekte/CivOne/src/Persistence/YamlSaveGameStateWriter.cs - geeigneter Integrationspunkt direkt nach DTO-Erzeugung vor finalem Schreiben.
- /home/christian/projekte/CivOne/src/Persistence/Mapper/GameStateDtoMapper.cs - DTO-Form als Basis für regelbasierte Diagnosen.
- /home/christian/projekte/CivOne/src/Persistence/Model/GameStateDto.cs - Top-Level DTO-Struktur für Validierungsregeln.
- /home/christian/projekte/CivOne/src/Persistence/Model/MapDto.cs - Map-Shape-Prüfungen (80x50-Regelprofil).
- /home/christian/projekte/CivOne/src/Persistence/Util/IValueSanitizer.cs - bestehendes Clamping/Logging, in Diagnosebericht integrierbar.
- /home/christian/projekte/CivOne/src/Services/ISaveGamePathProvider.cs - Zielpfad für diagnostics.txt.
- /home/christian/projekte/CivOne/src/Services/SaveGamePathProvider.cs - konkrete Pfadauflösung für Save-/AutoSave-Kontext.
- /home/christian/projekte/CivOne/CivOne.csproj - Build-Einstellungen für Overflow-Checks (Debug-first).
- /home/christian/projekte/CivOne/xunit/src/persistence/Model/ValueSanitizerTest.cs - Referenzmuster für Logging-/Grenzwerttests.
- /home/christian/projekte/CivOne/xunit/src/persistence/YamlSaveGameStateWriterTest.cs - geeigneter Ort für Integrationsprüfungen der Diagnoseausgabe.

**Verification**
1. Unit-Test: DTO mit Map 81x50 liefert im SVE-Profil mindestens einen Error mit eindeutiger Code-ID; im Default-Profil höchstens Warning/Info gemäß Entscheidung.
2. Unit-Test: gültiger 80x50-Zustand erzeugt keine Error-Findings.
3. Integrationstest: Save-Flow erzeugt diagnostics.txt im Save-Verzeichnis, Datei enthält Summary und deterministische Reihenfolge der Findings.
4. Unit-Test: Extremwerte an Sanitizer-Grenzen erzeugen nachvollziehbare Clamping-Logs und erscheinen optional in der Diagnosezusammenfassung.
5. Build-/Test-Check: Debug-Build mit aktivierten Overflow-Checks, gezielte Überlauf-Tests verifizieren erwartetes Verhalten in kritischen Rechenpfaden.

**Decisions**
- Entscheidung: Plan bewusst in zwei Teile getrennt.
- Teil 1: Zustandsdiagnose auf DTO-Ebene (fachlich/strukturell).
- Teil 2: technische Overflow-/Underflow-Sicherheit (arithmetisch/typbezogen).
- In Scope jetzt: Diagnosebericht + Testbarkeit + Vorbereitung auf späteres SVE-Gating.
- Out of Scope jetzt: sofortige harte Blockierung aller Save-Formate; automatische Reparatur inkonsistenter Spielzustände.

**Further Considerations**
1. Schwellwerte für Plausibilitäts-Warnings: konservativ starten (wenig Noise) oder aggressiv (mehr Hinweise, mehr False Positives).
2. Ausgabeort diagnostics.txt: immer neben Save-Datei oder optional zusätzlich globales Log-Verzeichnis.
3. Gate-Einführung: nur für SVE-Profil aktivieren, YAML/COS vorerst report-only belassen.