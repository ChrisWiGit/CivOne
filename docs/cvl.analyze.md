# CVL Functions and Tunes (from CIVPLAY)

This note describes how the `*.cvl` sound modules from DOS Civilization are used by the host (`CIVPLAY.C`) [Source found here](https://github.com/rajko-horvat/CivPlay).

## 1) Exported Module Functions (ABI)

`CIVPLAY.C` loads 6 function pointers per module from the export table (offset `0x32`) and calls them as follows:

1. `InitSoundFn(...)`
2. `PlayTuneFn(...)`
3. `CloseSoundFn()`
4. `SoundWorkerFn()`
5. `FastSoundWorkerFn()`
6. `SoundTimerFn()`

Practical significance in the host:

- `InitSoundFn(0,0,0,0,0,0,0)`
  - Return `0` => OK
  - Return `!= 0` => Device/driver not available
- `PlayTuneFn(tune, 3)` starts music/sound based on the number.
- `PlayTuneFn(0)` stops it.
- `CloseSoundFn()` cleans up upon termination.
- `SoundWorkerFn()` and `FastSoundWorkerFn()` are driven cyclically by the timer interrupt.
- `SoundTimerFn()` is loaded but not actively used here in `CIVPLAY.C`.

## 2) Scheduler/Timing in CIVPLAY

`CIVPLAY.C` hooks `INT 8` and runs a faster tick (PIT) to call the driver workers.

- Base tick increased (approx. ~300 Hz)
- `SoundWorkerFn()` runs approximately every 5 ticks
- `FastSoundWorkerFn()` runs optionally on every tick (if the driver requires it)
- The old BIOS timer interrupt is periodically chained

This is important for emulation: The driver expects these worker calls and not just `PlayTuneFn` alone.

## 3) What drivers/modules are available?

According to the menu in `CIVPLAY.C`:

- `ASOUND.CVL` => AdLib / compatible (FM/OPL)
- `ISOUND.CVL` => IBM Speaker
- `RSOUND.CVL` => Roland MT-32 / LAPC-1
- `TSOUND.CVL` => Tandy

## 4) Tune Numbers

`CIVPLAY.C` allows inputs `3..44` (with `0` to stop/exit).

Known mappings (triggers in the code, see [CVL-ASOUND-AdLib.md](CVL-ASOUND-AdLib.md#open-items)):

- `3`  Title Music
- `4`  Evolution Music
- `5`–`18`  Leader Themes, Long (Audience, Palace Intro, Civilization Founding, Dynasty End, Replay):
  `5` Lincoln, `6` Montezuma, `7` Ramesses, `8` Shaka Zulu, `9` Napoleon, `10` Caesar, `11` Stalin,
  `12` Alexander the Great, `13` Elizabeth, `14` Hammurabi, `15` Mao, `16` Genghis Khan, `17` Gandhi,
  `18` Frederick
- `19`–`32`  the same leader themes, short (event jingles: technology discovered, city conquered,
  wonder built, short audience ended), in the same order as `5`–`18`
- `33` Sting during an audience with a foreign leader
- `34` Win Music
- `35` Lose Music
- `36` Alarm sting (famine, civil unrest, overthrow of government, nuclear disaster) – also serves as the
  barbarian theme
- `37`–`44` short effects (unit arrived, battle outcome ×4, nuclear accident, bomber shot down,
  city view opened) – see the AdLib documentation for details and open issues

Note: Not all numbers are labeled, but the allowed range in this player is `3..44`.
