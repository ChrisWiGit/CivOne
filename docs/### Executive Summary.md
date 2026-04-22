### Executive Summary

- Ziel bleibt: CVL-Emulation für reproduzierbare Musik-Callbacks, aber Runtime nutzt primär `*.sound.json`.
- Namespace: `CivOne.Sound.Cvl` (optional Tooling: `CivOne.Tools.Sound.Cvl`).
- Kein separater Konverter-CLI: Auto-Import beim Spielstart, falls keine `*.sound.json` vorhanden sind.
- Ein Sound-Pack enthält mehrere Tunes (`tunes[]`) mit `title` und `endlessLoop`.
- `endlessLoop` ersetzt `loop` konsistent im Schema.
- Mehrere Packs gleichzeitig möglich; UI wählt Pack + Tune.
- Kein Hash-Zwang, Dateien dürfen editierbar bleiben.
- Event-Modell bleibt deterministisch (`OnPortWrite`, `OnInterrupt`, `OnWorkerCall`, optional `OnNoteEvent`).
- Phase 1: Event-Tap/Replayschicht, Phase 2: OPL/AdLib, Phase 3: weitere Treiber.
- Ablage im Profil unter CivOne/Sounds (bestehender Sound-Ort).

---

## Phase 1 — Event-Tap + Sound-Pack-Replay (ohne Audio-Genauigkeit)

### Ziel
Deterministische Laufzeitbasis schaffen: CVL nur als Quelle zur Erzeugung von `*.sound.json`, danach Replay aus Packs.

### Implementierungsschritte
1. `SoundPack`-Schema (`schemaVersion`, `displayName`, `id`, `format`, `tickRate`, `tunes[]`).
2. Tune-Schema mit `tuneId`, `title`, `endlessLoop`, `events[]`.
3. Startup-Flow:
   - Scanne CivOne/Sounds auf `*.sound.json`.
   - Falls leer: Auto-Import aus vorhandenen CVL-Dateien erzeugen.
4. `EventSink`/`EventPlayer`:
   - schreibe Events beim Import.
   - spiele Events deterministisch ab.
5. Auswahllogik:
   - Pack-Liste laden.
   - pro Pack verfügbare Tunes anzeigen.

### Dateien/Klassen
- `CivOne.Sound.Cvl.SoundPack`
- `CivOne.Sound.Cvl.SoundPackLoader`
- `CivOne.Sound.Cvl.SoundPackRegistry`
- `CivOne.Sound.Cvl.EventPlayer`
- `CivOne.Sound.Cvl.AutoImportOnStartup`

### Tests
- Keine Pack-Datei vorhanden -> Auto-Import läuft einmalig.
- Mehrere `*.sound.json` werden erkannt.
- Ein Pack mit mehreren Tunes wird vollständig geladen.
- `endlessLoop=true` wiederholt, `false` stoppt.
- Zwei Replays derselben Tune erzeugen identische Event-Reihenfolge.

### Done-Kriterien
- Spiel kann ohne manuelle Konvertierung starten.
- Mindestens ein Pack mit mehreren Tunes nutzbar.
- Pack/Tune-Auswahl funktional und stabil.

---

## Phase 2 — OPL/AdLib-Wiedergabe aus Events

### Ziel
Aus `format=opl2/opl3` Events hörbare, stabile Ausgabe erzeugen.

### Implementierungsschritte
1. `AudioBackendOpl` an `EventPlayer` anbinden.
2. Port-Event-Mapping (z. B. OPL Address/Data) implementieren.
3. Timing aus `tickRate` + Eventzeit ableiten.
4. `endlessLoop` in Audio-Pipeline beachten.

### Dateien/Klassen
- `CivOne.Sound.Cvl.Audio.AudioBackendOpl`
- `CivOne.Sound.Cvl.Audio.OplRegisterState`
- `CivOne.Sound.Cvl.Playback.PlaybackSession`

### Tests
- Tunes 3, 4, 34, 35 aus einem OPL-Pack abspielbar.
- Stop/Start/Wechsel zwischen Tunes ohne Hänger.
- `endlessLoop` funktioniert bei Titelmusik, Jingles stoppen korrekt.

### Done-Kriterien
- OPL-Pack klingt reproduzierbar und taktstabil.
- Keine Abhängigkeit von CVL-Dateien im Normalbetrieb.

---

## Phase 3 — Weitere Treiberformate (Speaker, Tandy, Roland)

### Ziel
Mehrere Ausgabeformate über eigene Packs parallel unterstützen.

### Implementierungsschritte
1. `format`-Dispatcher (`speaker`, `tandy`, `mt32`, optional `gm`).
2. Je Format eigenes Backend + Event-Interpretation.
3. UI-Filter nach kompatiblen Packs/Backends.
4. Fallback-Strategie bei unbekanntem Format.

### Dateien/Klassen
- `CivOne.Sound.Cvl.Audio.AudioBackendSpeaker`
- `CivOne.Sound.Cvl.Audio.AudioBackendTandy`
- `CivOne.Sound.Cvl.Audio.AudioBackendMt32`
- `CivOne.Sound.Cvl.Audio.AudioBackendFactory`

### Tests
- Pro Format mindestens ein Pack ladbar.
- Pack-Wechsel zur Laufzeit ohne Neustart.
- Gleiche Tune-ID in verschiedenen Formaten separat auswählbar.

### Done-Kriterien
- Mehrformat-Betrieb stabil.
- Auswahlmodell „Pack + Tune“ vollständig integriert.

---

## Callback-Modell (final)

- `OnWorkerCall(kind, tick, virtualTimeNs, context)`
- `OnPortWrite(port, value, tick, virtualTimeNs, context)`
- `OnInterrupt(intNo, tick, virtualTimeNs, context)`
- optional `OnNoteEvent(channel, note, velocity, tick, virtualTimeNs)`

Zeitbasis: rein virtuell/deterministisch aus Tick-Scheduler, keine Wall-Clock.

---

## Nächste 5 konkrete Tasks

1. `SoundPack`-Schema v1 fixieren (`title`, `endlessLoop`, `tunes[]`, `events[]`).
2. Loader + Validierung für `*.sound.json` implementieren.
3. Startup-Auto-Import-Hook einbauen (nur wenn keine Packs existieren).
4. Pack/Tune-Auswahllogik im Spielmenü integrieren.
5. `EventPlayer` mit `endlessLoop` und deterministischem Tick-Playback fertigstellen.