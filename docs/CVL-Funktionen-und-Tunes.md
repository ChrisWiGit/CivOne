# CVL-Funktionen und Tunes (aus CIVPLAY)

Diese Notiz beschreibt, wie die `*.cvl`-Soundmodule aus DOS Civilization vom Host (`CIVPLAY.C`) [Origin found here](https://github.com/rajko-horvat/CivPlay) verwendet werden.

## 1) Exportierte Modul-Funktionen (ABI)

`CIVPLAY.C` lädt pro Modul 6 Funktionszeiger aus der Export-Tabelle (Offset `0x32`) und ruft sie so auf:

1. `InitSoundFn(...)`
2. `PlayTuneFn(...)`
3. `CloseSoundFn()`
4. `SoundWorkerFn()`
5. `FastSoundWorkerFn()`
6. `SoundTimerFn()`

Praktische Bedeutung im Host:

- `InitSoundFn(0,0,0,0,0,0,0)`
  - Return `0` => OK
  - Return `!= 0` => Gerät/Treiber nicht verfügbar
- `PlayTuneFn(tune, 3)` startet Musik/Sound anhand der Nummer.
- `PlayTuneFn(0)` stoppt.
- `CloseSoundFn()` räumt beim Beenden auf.
- `SoundWorkerFn()` und `FastSoundWorkerFn()` werden zyklisch vom Timer-Interrupt getrieben.
- `SoundTimerFn()` wird geladen, aber in `CIVPLAY.C` hier nicht aktiv verwendet.

## 2) Scheduler/Timing in CIVPLAY

`CIVPLAY.C` hookt `INT 8` und betreibt einen schnelleren Tick (PIT), um die Treiber-Worker aufzurufen.

- Basis-Tick erhöht (ca. ~300 Hz)
- `SoundWorkerFn()` etwa alle 5 Ticks
- `FastSoundWorkerFn()` optional auf jedem Tick (wenn Treiber es verlangt)
- Alter BIOS-Timer-Interrupt wird periodisch weitergekettet

Das ist wichtig für Emulation: Der Treiber erwartet diese Worker-Aufrufe und nicht nur `PlayTuneFn` allein.

## 3) Welche Treiber/Module gibt es?

Laut Menü in `CIVPLAY.C`:

- `ASOUND.CVL` => AdLib / kompatibel (FM/OPL)
- `GSOUND.CVL` => General MIDI Treiber
- `ISOUND.CVL` => IBM Speaker
- `PSOUND.CVL` => Pro cards (OPL-3)
- `RSOUND.CVL` => Roland MT-32 / LAPC-1
- `TSOUND.CVL` => Tandy

## 4) Tune-Nummern

`CIVPLAY.C` erlaubt Eingaben `3..44` (mit `0` zum Stop/Exit).

Bekannte Zuordnung (aus vorhandenem Readme):

- `3`  Title Music
- `4`  Evolution Music
- `5`  Lincoln
- `6`  Montezuma
- `7`  Ramses
- `8`  Shaka Zulu
- `9`  Napoleon
- `10` Caesar
- `11` Stalin
- `12` Alexander the Great
- `13` Elizabeth
- `14` Hammurabi
- `15` Mao
- `16` Genghis Khan
- `17` Gandhi
- `18` Frederick
- `34` Win Music
- `35` Lose Music

Hinweis: Nicht alle Nummern sind benannt, aber der erlaubte Bereich in diesem Player ist `3..44`.

## 5) Bedeutung für dieses Testprojekt

Im `civonesound`-Toy-Projekt wird aktuell:

- Header/Export-Tabelle der CVL gelesen,
- I/O-Signatur statisch getraced,
- und der CIVPLAY-Scheduler vereinfacht simuliert.

Nächster Schritt wäre eine echte Callback-Schicht, z. B.:

- `OnPortWrite(port, value, tick)`
- `OnInterrupt(intNo, tick)`
- `OnWorkerCall(kind, tick)`

Darauf kann später ein Audio-Backend (z. B. OPL-Emu oder Software-Synth) aufsetzen.
