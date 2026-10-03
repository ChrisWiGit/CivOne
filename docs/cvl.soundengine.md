# CVL Sound Engine

This note describes the sound engine CivOneX built on top of the `*.cvl` reverse engineering in
[cvl.analyze.md](cvl.analyze.md). Everything here is CivOneX's own work; nothing in this document
comes from CivPlay or any other external source. See cvl.analyze.md for the driver ABI and tune
number attribution.

## 1) Pipeline overview

CivOneX turns the original `*.cvl` modules into playable audio in three stages, run once per sound
pack rather than at game time:

1. **Parse** - `AsoundParser` (AdLib) and `IsoundParser` (PC speaker) read a `*.cvl` module and turn
   its tune data into driver-independent step/event lists.
2. **Convert to JSON** - each tune becomes a `*.sound.json` file (`TuneScore`, `TuneArrangement`,
   `TuneStep` for the PC speaker; the AdLib equivalents for FM). This is the archival, human-readable
   form of a tune: readable without a CVL file, a CPU, or a driver.
3. **Render to WAV** - `AdlibTuneRenderer` and `PcSpeakerTuneRenderer` synthesize the JSON scores into
   PCM samples, which `WaveFileWriter` writes to disk. Rendering happens once per pack and is cached;
   playback only ever touches wave data.

At runtime, `SoundPackPlaybackService` and the classes in `Sound/Engine` play the rendered waves; they
never touch a CVL file or the original bytecode again.

## 2) The AdLib voice bytecode

The ABI document only covers what `CIVPLAY.C` calls into a driver. What a driver's tune actually
*contains* - the byte language each AdLib voice stream is written in - is not part of that ABI and is
not documented anywhere CivOneX found. `AdlibBytecodeDecoderDelegate` decodes it from the module data
itself: values below `0xF3` are two-byte note records (note, duration; a duration of `0` ends the
voice), everything from `0xF3` upward is a control opcode with a fixed operand count
(`AdlibEventKind`):

- `0xFD` **Restart** - rewind to the start of the stream and clear all modifiers.
- `0xFF` / `0xFE` **LoopOuter** / **LoopInner** - backward repeats, nested, decremented on each pass.
- `0xFC` **SetInstrument** - select an instrument from the bank.
- `0xFB` **SetGate** - release lead time: the note is keyed off this many ticks before its recorded
  duration has elapsed, rather than always running the full length.
- `0xFA` **SetPitchSlide** - signed F-number delta added to the running pitch every tick (portamento).
- `0xF9` **SetVolume**, `0xF8` **VolumeEnvelope**, `0xF5` **SetVolumeOffset** - three layers of volume
  control that combine per voice.
- `0xF7` **SetDetune** - signed detune in F-number units.
- `0xF6` **RandomVariant** - pick one of several choices at random and patch it into a byte read later
  in the stream, so the same tune can vary between plays without extra arrangement copies.
- `0xF4` **SetPan**, `0xF3` **PanEnvelope** - stereo position and its envelope; the original driver
  only applies these on an OPL3 card, so they have no audible effect through the OPL2 renderer.

None of this instruction set is guessed: every opcode and operand count was confirmed against the
module's actual bytes and the driver's disassembly. It is what lets the renderer reproduce gate
timing, portamento, and volume/pan envelopes rather than flat notes.

## 3) PC speaker note effects

`IsoundParser` decodes a comparable, much smaller effect word per step (`SpeakerEffectKind`): a note
can be plain, oscillate as vibrato (`Divisor` moves by a fixed step within a range), or slide (a
signed delta added to the divisor every worker tick, wrapping like the real 16-bit timer register
would). This is what keeps the PC speaker renderer from sounding flatter than the original.

## 4) Loop point discovery

The original driver has no explicit "loop this tune" flag; whether a tune repeats falls out of
whether a voice ever hits opcode `0xFD` (Restart) while playing. `AdlibTuneRenderer` renders every
voice and watches for a restart; the first sample index at which any voice restarts becomes the wave
file's loop point (`LoadedWave.LoopStart` / `LoopEnd`).

This is an empirical finding, not something documented anywhere: of the fourteen long leader themes,
eleven restart a voice and therefore loop, while three play to the end and stop. `SoundLoop.WhenMarked`
encodes exactly that distinction, so a tune the original never looped does not get an artificial loop
point stitched onto its end.

## 5) Runtime playback engine

`Sound/Engine` is a small real-time mixer (`SoundSystem`, `SoundMixer`, `MixerVoice`) that plays the
rendered waves, independent of how they were produced:

- **Buses** (`SoundBus`) separate music (usually one piece, often looping) from effects (short sounds
  layered over it), each with its own master volume and replacement rule.
- **Loop crossfade** - a looping voice can overlap its turnaround by a configurable number of samples
  (`MixerVoice.LoopCrossFade`) instead of cutting hard at the loop point, and the tail left behind by
  the old iteration keeps playing itself out as an unreachable, handle-less voice.
- **Fades and voice lifecycle** (`VoiceFadeAction`) - a voice can fade to a target gain and then either
  keep playing, pause (used to duck music under an effect without losing its position), or stop and be
  removed. Gain moves a fixed step per sample rather than jumping, so transitions are click-free.

None of this - the bus split, the crossfade, or the pause/stop fade actions - exists in the original
driver; it is CivOneX's own addition to make tune changes and interruptions sound smooth instead of
the hard cuts `PlayTuneFn` produces in the original.
