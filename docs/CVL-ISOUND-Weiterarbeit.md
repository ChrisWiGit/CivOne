# CVL ISOUND – Weiterarbeit und nächste Prompts

Diese Notiz hält den aktuellen Stand der ISOUND-Analyse fest und beschreibt die sinnvollsten nächsten Schritte für die folgenden Prompts.

## Aktueller Stand

Bereits umgesetzt:

- Direkte ISOUND-Tune-Erkennung über die Dispatch-Tabelle bei Image-Offset `0x0588`
- Handler-Scan nach `LEA BX, ...`-Mustern, um tune-spezifische Datenbereiche zu finden
- Direkte Extraktion von `note,duration`-Paaren aus den Tune-Daten statt generischer statischer CPU-Portspur
- Ausgabe echter Speaker/PIT-Ports:
  - `0x43` = PIT mode/control
  - `0x42` = PIT channel 2 data
  - `0x61` = PC speaker gate
- Integrationstest prüft jetzt zusätzlich:
  - nur Speaker-Ports bei ISOUND
  - nicht mehr für alle Tunes identische Event-Streams

Der aktuelle Stand ist funktional besser als der alte OPL-Fallback, aber die Note→PIT-Abbildung ist noch heuristisch und sollte als Nächstes präzisiert werden.

## Wichtige Analyseergebnisse

### Dispatch-Tabelle

- ISOUND verwendet eine Tune-Dispatch-Tabelle bei Image-Offset `0x0588`
- Beispielhaft beobachtete Handler:
  - Tune `3` → Handler `0x0623`
  - Tune `4` → Handler `0x05E2`
  - Tune `34` → Handler `0x071F`
  - Tune `35` → Handler `0x0726`

### Beobachtetes Verhalten

- Tune `4` scheint im ISOUND-Modul effektiv sofort zurückzukehren und kann daher absichtlich still/leer sein
- Tunes `3`, `34`, `35` verweisen auf unterschiedliche Datenbereiche
- In diesen Datenbereichen wurden wiederkehrende Byte-Paare erkannt, die plausibel als `note,duration` interpretiert werden können
- Die aktuellen Speaker-Events sind damit tune-spezifisch, aber tonal noch nicht als "exakt originalgetreu" verifiziert

## Note-PIT-Abbildung

### Aktuell implementierte Heuristik

Der momentane Code behandelt den Notenwert als chromatischen Schritt und berechnet daraus eine Frequenz relativ zu einem Basiston:

- Basis-Note: `0x62`
- Basis-Frequenz: `220 Hz`
- Formel:

```text
frequency = 220 * 2^((note - 0x62) / 12)
pitDivisor = round(1193182 / frequency)
```

Dabei ist `1193182` die PIT-Clock des PC-Speakers.

### Beispielwerte der aktuellen Heuristik

| Note-Code | Frequenz (ca.) | PIT-Divisor (ca.) |
|---|---:|---:|
| `0x62` | 220.00 Hz | 5424 |
| `0x69` | 329.63 Hz | 3620 |
| `0x6C` | 392.00 Hz | 3044 |
| `0x7E` | 1108.73 Hz | 1076 |
| `0x8C` | 2489.02 Hz | 479 |
| `0x94` | 3951.07 Hz | 302 |

### Einschätzung

Diese Abbildung ist eine brauchbare erste Näherung, aber vermutlich nicht exakt das originale Lookup-Verhalten von `ISOUND.CVL`.

Sehr wahrscheinlich besitzt der Treiber intern eine Lookup-Tabelle oder Oktav-/Halbton-Transformation, die aus dem Notencode den echten PIT-Divisor bestimmt. Diese exakte Tabelle sollte noch aus dem Modul rekonstruiert werden.

## Empfohlene nächste Prompts

### Prompt 1 – Exakte Note→PIT-Tabelle aus dem Binary ableiten

Ziel:

- Die tatsächliche Umrechnung des ISOUND-Treibers vom Notencode zum PIT-Divisor aus dem Code bzw. den Datenstrukturen rekonstruieren

Arbeitsauftrag:

- Setup-/Worker-Routinen im Bereich um die bereits gefundenen ISOUND-Routinen weiter analysieren
- Verdächtige Lookup-Tabellen für Frequenzen/PIT-Divisoren identifizieren
- Verifizieren, welche Rolle Werte wie `0x62`, `0x7E`, `0x8C` usw. tatsächlich spielen
- Die Heuristik im Code durch eine exakte Lookup- oder Transformationsfunktion ersetzen

Erfolgskriterium:

- Für reale Tunes entstehen nachvollziehbare, konsistente Divisoren ohne willkürliches musikalisches Mapping

### Prompt 2 – Duration-/Rest-Semantik sauber modellieren

Ziel:

- Verstehen, wie `duration` im Treiber tatsächlich in Tick-Längen bzw. Gate-Verhalten umgesetzt wird

Arbeitsauftrag:

- Prüfen, ob `duration` direkt Tick-Anzahl ist oder noch skaliert / akkumuliert wird
- Unterscheiden zwischen:
  - Ton halten
  - Rest/Stille
  - Gate kurz toggeln
  - Note-Wechsel ohne volles Abschalten
- Event-Zeitachsen in `CvlRecordRunner` realistischer modellieren, statt jede Write einfach auf den nächsten Tick zu legen

Erfolgskriterium:

- Die erzeugten Speaker-Events bilden Rhythmus und Pausen deutlich realistischer ab

### Prompt 3 – Tune 3 / 34 / 35 gezielt gegeneinander validieren

Ziel:

- Sicherstellen, dass die drei realen Musik-Tunes nicht nur unterschiedlich aussehen, sondern plausibel unterschiedliche Melodien/Rhythmen erzeugen

Arbeitsauftrag:

- Für Tune `3`, `34`, `35` jeweils die ersten N extrahierten Noten/Dauern dumpen
- PIT-Divisoren vergleichen
- Prüfen, ob Muster aus dem Binary mit dem erwarteten musikalischen Verlauf grob zusammenpassen
- Falls nötig Testfälle ergänzen, die auf unterschiedliche Sequenzstarts prüfen

Erfolgskriterium:

- Unterschiedliche Tunes haben eindeutig unterschiedliche Startsequenzen und nicht nur abweichende spätere Details

### Prompt 4 – Fallbacks und Tests härten

Ziel:

- Den Parser robuster gegen unbekannte oder leicht anders strukturierte CVL-Dateien machen

Arbeitsauftrag:

- Fallback-Regeln dokumentieren
- Tests für Sonderfälle ergänzen:
  - leerer Handler
  - Handler ohne brauchbaren `LEA BX`-Pointer
  - Tune mit nur Rest-/Silent-Daten
  - unvollständige Byte-Sequenzen
- Optional Logging-/Debug-Hilfen für erkannte Handler und Datensequenzen einbauen

Erfolgskriterium:

- Parser bleibt stabil, auch wenn künftige CVLs leicht anders aufgebaut sind

## Konkrete Aufgabenliste für den nächsten Implementierungsschritt

1. Rund um die ISOUND-Setup-/Worker-Routinen die echte Frequenzumrechnung entschlüsseln
2. Die aktuelle Formel-basierte `NoteCodeToPitDivisor(...)`-Implementierung ersetzen
3. Dauer- und Restbehandlung realistischer auf Event-Zeitpunkte abbilden
4. Reale ISOUND-Tests um assertions auf frühe Sequenzunterschiede erweitern
5. Optional: Analyseergebnisse in die bestehende CVL-Doku zurückspiegeln

## Sinnvolle Folge-Prompts

Beispiel 1:

> Analysiere jetzt die ISOUND-Routinen weiter und leite die exakte Note-zu-PIT-Tabelle aus dem Binary ab. Ersetze danach die heuristische Abbildung im Code.

Beispiel 2:

> Untersuche die Duration-Semantik von ISOUND und baue die Speaker-Events so um, dass Pausen und Notenlängen realistischer modelliert werden.

Beispiel 3:

> Ergänze gezielte Tests für Tune 3, 34 und 35, damit die ersten extrahierten Notenfolgen explizit voneinander unterschieden werden.

## Summary – gesammeltes Reverse-Engineering-Wissen

Dieser Abschnitt fasst das Wissen zusammen, das während der bisherigen Analyse entstanden ist und für spätere Reverse-Engineering-Schritte nützlich bleibt.

### 1) Allgemeines CVL-/Treiberwissen

- CVL-Dateien enthalten einen DOS-ähnlichen Image-Bereich; der eigentliche Code/Data-Block beginnt bei `imageStart = cparhdr * 16`
- Die Export-Tabelle liegt im Image bei Offsets `0x32..0x3C`
- Für Analysen ist die Unterscheidung wichtig:
  - **File-Offset** = Position in der Datei
  - **Image-Offset** = Position relativ zum geladenen Modul-Image
- Viele nützliche Routinen und Tabellen sind nur sinnvoll interpretierbar, wenn man diese beiden Offsets sauber auseinanderhält

### 2) Methodik, die sich bewährt hat

Folgende Heuristiken waren bisher besonders nützlich:

- Erst die **Dispatch-Tabelle** finden, bevor einzelne Handler disassembliert werden
- Bei kleinen Treibern zuerst nach typischen Mustern suchen:
  - `8D 1E ?? ??` → `LEA BX, <data>`
  - `E6 42`, `E6 43`, `E6 61` → OUT zu Speaker/PIT-Ports
  - `C3` / `CB` → sehr kurze Rückkehrpfade / leere Handler
- Nicht nur Code disassemblieren, sondern auch nach **datenähnlichen Strukturen** suchen:
  - wiederkehrende 2-Byte- oder 4-Byte-Muster
  - klar erkennbare Tabellenblöcke
  - Sequenzen, die musikalisch plausibel aussehen
- Bei unsicheren Kandidaten hilft ein **Scoring-Ansatz** besser als eine harte Annahme

Diese Methodik sollte auch für `GSOUND`, `TSOUND` oder andere CVL-Treiber wiederverwendbar sein.

### 3) Konkretes Wissen zu ISOUND

Bereits sicher bzw. mit hoher Wahrscheinlichkeit erkannt:

- Die Tune-Dispatch-Tabelle von `ISOUND.CVL` liegt bei Image-Offset `0x0588`
- Die Tabelle enthält 16-Bit-Handler-Offets für Tunes `0..44`
- Beobachtete Beispiele:
  - Tune `3` → `0x0623`
  - Tune `4` → `0x05E2`
  - Tune `34` → `0x071F`
  - Tune `35` → `0x0726`
- Tune `4` scheint im ISOUND-Treiber effektiv leer/still zu sein oder sofort zu terminieren
- Die wirklich interessanten Tunes verweisen über `LEA BX, ...` auf tune-spezifische Datenbereiche

Wichtiger Lerneffekt:

- Eine rein statische Portspur-Suche findet zwar `OUT`-Instruktionen, aber nicht automatisch die tune-spezifischen Unterschiede
- Für musikalische Daten ist **direkte Datenextraktion aus Treiberstrukturen** oft deutlich ergiebiger als reine Opcode-Verfolgung

### 4) Hinweise zur Struktur der ISOUND-Tunes

Was bisher beobachtet wurde:

- Einige Handler verweisen auf Bereiche, die wie Sequenzdaten aussehen
- Dort treten wiederkehrende Byte-Paare auf, die plausibel als `note,duration` interpretiert werden können
- Null-Paare (`00 00`) wirken häufig wie Separatoren, Pausen oder Sequenzgrenzen
- Nicht jeder `LEA BX, ...`-Kandidat ist automatisch die eigentliche Musikdatenquelle; daher war ein Auswahl-/Scoring-Mechanismus sinnvoll

Für spätere Arbeit wichtig:

- Es ist wahrscheinlich, dass es im Modul **mehr als eine Ebene von Indirektion** gibt:
  - Tune-Handler
  - Setup-/State-Routine
  - Datenzeiger / Tabellen
  - Frequenz-/Dauerlogik im Worker
- Bei künftiger Analyse sollte man also immer prüfen, ob ein gefundener Pointer echte Musikdaten oder nur Hilfs-/Initialisierungsdaten referenziert

### 5) Wissen zur Speaker-/PIT-Seite

Für ISOUND relevant sind insbesondere diese Ports:

- `0x43` → PIT mode/control
- `0x42` → PIT channel 2 data
- `0x61` → Speaker gate / enable

Das war eine wichtige Korrektur gegenüber dem alten Zustand:

- Früher wurden fälschlich OPL-Fallback-Ports `0x388/0x389` verwendet
- Jetzt ist klar: Für ISOUND müssen Speaker-/PIT-Ports im Fokus stehen

Das ist auch für zukünftiges RE nützlich, weil man damit Dumps, CPU-Spuren und Tests viel schneller auf Plausibilität prüfen kann.

### 6) Was beim Reverse Engineering wahrscheinlich noch fehlt

Die größten offenen Punkte sind:

- die **exakte** Note→PIT-Abbildung des Originaltreibers
- die echte Dauer-/Tick-Semantik
- die Rolle möglicher Zustandsvariablen im Worker / Timer-Callback
- die Frage, ob bestimmte Note-Codes Sonderbedeutungen haben (Rest, Repeat, Endmarker, Gate-only, Sustain)

Für spätere RE-Arbeit ist deshalb wichtig:

- nicht zu früh annehmen, dass alle Byte-Paare einfache Musiknoten sind
- immer auch Kontrollcodes, Rests, Delays und Endmarker in Betracht ziehen
- Worker-Routinen bevorzugt zusammen mit den Datenbereichen lesen, nicht isoliert

### 7) Was man künftig wiederverwenden kann

Die folgenden Erkenntnisse lassen sich direkt in spätere Prompts oder andere Treiberanalysen mitnehmen:

- **Offset-Disziplin**: File-Offset und Image-Offset immer separat dokumentieren
- **Dispatch-first**: zuerst Tabellen und Einsprunglogik, dann Handler, dann Daten
- **Pattern-Suche**: `LEA`, `OUT`, `RET`, Tabellenblöcke, wiederkehrende Paare
- **Scoring statt starre Annahme** bei mehreren Datenkandidaten
- **Tests auf Unterschiedlichkeit** sind wertvoll: nicht nur "irgendwelche Events", sondern explizit verschiedene Tunes gegeneinander prüfen
- **Fallbacks getrennt nach Treiberklasse** behandeln: OPL-Fallbacks für FM-Treiber, Speaker-Fallbacks für ISOUND

### 8) Praktischer Merksatz für spätere Prompts

Wenn in einem CVL-Treiber alle Tunes gleich aussehen, ist das oft ein Zeichen dafür, dass man nur generischen Initialisierungscode oder statische `OUT`-Instruktionen sieht – nicht die eigentliche tune-spezifische Datenlogik.

## RE-Checkliste für CVL-Treiber

Diese Checkliste ist bewusst allgemeiner gehalten und soll auch für spätere Arbeiten an `ASOUND`, `GSOUND`, `TSOUND`, `PSOUND` oder anderen CVL-Treibern nutzbar sein.

### 1) Grunddaten zuerst erfassen

- Dateigröße notieren
- `imageStart` aus dem Header bestimmen
- Export-Tabelle bei `imageStart + 0x32 .. 0x3C` auslesen
- File-Offsets und Image-Offsets getrennt dokumentieren
- Früh notieren, welcher Treibertyp vorliegt:
  - OPL/FM
  - Speaker/PIT
  - MIDI/MT-32
  - Tandy / andere Spezialhardware

### 2) Einsprungpunkte identifizieren

- Exportierte Funktionen den typischen Rollen zuordnen:
  - Init
  - PlayTune
  - Close
  - Worker
  - FastWorker
  - Timer
- Prüfen, welche Exporte echte Logik enthalten und welche nur Stubs sind
- Früh markieren, welche Funktionen direkt tune-spezifisch sein könnten

### 3) Dispatch-Tabellen suchen

- Nach indirekten Aufrufen suchen, z. B.:
  - `CALL [table + index]`
  - `JMP [table + index]`
  - `SHL BX,1` / `ADD BX,BX` vor Tabellenzugriffen
- Tabellen möglichst vollständig extrahieren
- Testweise einige bekannte Tune-IDs mappen und prüfen, ob die Handler plausibel aussehen

### 4) Code-Patterns systematisch scannen

Besonders nützliche Muster:

- `8D 1E ?? ??` → `LEA BX, data`
- `E6 xx`, `EE`, `E7`, `EF` → `OUT`-Instruktionen
- `CD 08` → Timer-/Interruptbezug
- `C3`, `CB` → Rückkehr / Stub / kurze Handler
- wiederkehrende `MOV`-/`CALL`-/`JMP`-Folgen rund um State-Updates

Faustregel:

- Wiederkehrende Port- oder Interrupt-Muster zeigen oft nur generische Infrastruktur
- Wirklich interessante Unterschiede sitzen häufig in Datenpointern, Tabellen und State-Variablen

### 5) Datenbereiche separat behandeln

- Nicht alles als Code interpretieren
- Verdächtige Bereiche auf Muster prüfen:
  - 2-Byte-Paare
  - 4-Byte-Paare
  - Worttabellen
  - wiederkehrende Marker
  - Nullblöcke / Separatoren
- Prüfen, ob ein Pointer auf:
  - echte Musikdaten
  - Initialisierungswerte
  - Lookup-Tabellen
  - temporäre State-Strukturen
  verweist

### 6) Hardware-spezifisch denken

Immer zuerst die Zielhardware klären, bevor man die Portzugriffe interpretiert.

Beispiele:

- `ASOUND` / OPL:
  - typischer Fokus auf `0x388..0x38B`
  - Instrumente, Registerblöcke, Operator-Daten
- `ISOUND` / Speaker:
  - `0x42`, `0x43`, `0x61`
  - PIT-Divisoren, Gate-Verhalten, Timer-Logik
- MIDI-/Roland-Treiber:
  - eher Kommandoströme, Statusbytes, Geräteschnittstellen

Wichtig:

- Falsche Hardware-Annahme führt oft zu scheinbar plausiblen, aber inhaltlich falschen Ergebnissen

### 7) State und Worker ernst nehmen

- Viele CVL-Treiber erzeugen die Musik nicht direkt in `PlayTune`, sondern initialisieren nur den Zustand
- Die eigentliche Tonerzeugung entsteht dann im Worker / Timer-Callback
- Deshalb bei RE immer zusammen analysieren:
  - Tune-Startfunktion
  - Setup-Routine
  - Worker-Routine
  - Timer-Routine
  - State-Variablen / Tabellen

### 8) Unterschiede zwischen Tunes aktiv prüfen

- Nicht nur schauen, ob überhaupt Events entstehen
- Immer mindestens 2–4 Tunes direkt vergleichen
- Gute Prüfpunkte:
  - erster Datenpointer
  - erste 16–32 Bytes der Tune-Daten
  - erste Portwrites / ersten Notenwerte
  - Länge der Sequenz
  - Sonderfälle wie Stille / sofortiger Return

Wenn alles identisch aussieht, prüfen:

- wird nur generischer Setup-Code gelesen?
- wird nur ein Fallback erzeugt?
- wurde die falsche Tabelle interpretiert?
- fehlt noch eine indirekte Datenebene?

### 9) Mit Hypothesen arbeiten, aber sauber markieren

Für spätere Arbeit sehr hilfreich ist die Trennung in drei Kategorien:

- **sicher beobachtet**
- **starke Hypothese**
- **noch offen**

Dadurch bleibt auch nach mehreren Sessions nachvollziehbar, welche Erkenntnisse belastbar sind und welche nur vorläufige Arbeitshypothesen waren.

### 10) Tests früh auf Aussagekraft trimmen

Gute Tests prüfen nicht nur "irgendwas kommt raus", sondern z. B.:

- richtige Portfamilie je Treiber
- keine unpassenden Fallback-Ports
- unterschiedliche Event-Streams für unterschiedliche Tunes
- sinnvolle Reaktion auf leere / stub-artige Handler
- stabile Verarbeitung bei unklaren oder verkürzten Datenblöcken

### 11) Praktische Mini-Checkliste pro neuer Analyse

Für einen neuen CVL-Treiber lohnt sich fast immer diese Reihenfolge:

1. Header + `imageStart` prüfen
2. Export-Tabelle extrahieren
3. Hardwaretyp bestimmen
4. Dispatch-Tabelle(n) suchen
5. Einige bekannte Tune-IDs auf Handler mappen
6. `LEA`-/Tabellen-/`OUT`-Muster suchen
7. Datenkandidaten extrahieren und gegeneinander vergleichen
8. Worker-/Timer-State nachvollziehen
9. erste Test-Assertions bauen
10. erst dann feinere Semantik wie Frequenz, Dauer, Gate, Instrumente auflösen

### 12) Kurzfassung als Merksätze

- Erst Tabellen, dann Handler, dann Daten
- Hardware vor Portanalyse klären
- File-Offset und Image-Offset nie vermischen
- Unterschiedliche Tunes aktiv gegeneinander testen
- Wenn alles gleich aussieht, fehlt meist noch die echte Datenebene
