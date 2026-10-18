# DOSBox-X Audio Reproduction Prompt

You are implementing audio synthesis for a modern Windows application that wants to sound like DOSBox-X when it runs DOS games. The goal is not to translate port writes into raw tones. The goal is to reproduce the same final audible result that DOSBox-X produces after hardware emulation, timing reconstruction, filtering, mixing, and host audio output.

DOSBox-X can use SDL2 for the host audio device, so SDL2 is the output layer, not the sound-generation layer. The emulation still happens before SDL2 sees the final PCM stream.

Use DOSBox-X as the reference model for behavior, especially these code paths: [PC speaker emulation](../src/hardware/pcspeaker.cpp#L574), [PC speaker mixer channel registration](../src/hardware/pcspeaker.cpp#L735), [PC speaker lowpass filter setup](../src/hardware/pcspeaker.cpp#L739), [Sound Blaster callback](../src/hardware/sblaster.cpp#L3288), [Sound Blaster filter model](../src/hardware/sblaster.cpp#L2614), [Sound Blaster mixer channel registration](../src/hardware/sblaster.cpp#L4273), [Mixer initialization](../src/hardware/mixer.cpp#L1096), [SDL host audio device setup](../src/hardware/mixer.cpp#L1150), [SDL audio start](../src/hardware/mixer.cpp#L1176), [Mixer callback into host audio](../src/hardware/mixer.cpp#L809), and [Sound Blaster filtering note in config help](../src/dosbox.cpp#L3875).

## Core principle

Do not treat the JSON port log as the final sound source. In DOSBox-X, port writes are only the beginning. The actual audible result is produced by device emulation and mixer rendering at a target sample rate, then sent to the host audio device as stereo signed 16-bit PCM.

Your implementation should therefore model the hardware first, then render sound from the resulting state over time.

## Reference audio pipeline

The DOS game writes to PIT, PC speaker, or Sound Blaster registers. The emulated device updates its internal state, timers, counters, DMA position, or mixer registers. A per-device callback renders audio into an internal mixer channel. The DOSBox mixer resamples, interpolates, lowpass-filters, and mixes all channels into a stereo PCM stream. SDL2 opens the host audio device and plays the mixed PCM stream on Windows.

Your app should mimic this flow.

## PC speaker behavior to reproduce

The PC speaker in DOSBox-X is not a fixed square wave generator. It is a time-based reconstruction of the PIT-driven output.

Important behaviors include modeling PIT timing and speaker gate/output state, supporting the relevant PIT modes used by games especially mode 3 square wave behavior and the transient transitions when the counter changes, preserving event timing because DOSBox-X uses timestamped output-level changes and reconstructs the signal across the current audio slice, converting the state changes into sample values over time instead of a single tone frequency, applying smoothing between output levels because DOSBox-X ramps the speaker volume over a short time instead of switching instantly, applying a lowpass filter to the speaker channel because DOSBox-X sets the speaker channel lowpass around 10 kHz with a 3-stage filter, handling ultrasonic or silencing tricks used by games because high PIT counters are often used to mute the speaker and DOSBox-X treats that as effectively silent or hiss-like behavior rather than a clean audible tone, and keeping the speaker channel alive only while it is actually needed before disabling it after idle time.

Practical implication: if the game toggles the speaker control bit or PIT counter, your renderer should build a waveform over time from those transitions. Use a continuous timeline and interpolate between speaker states inside each render block. Do not emit a new PCM tone immediately for every port write.

## Sound Blaster behavior to reproduce

Sound Blaster is also stateful and timing-sensitive. The audible result depends on card type, DMA mode, sample rate, stereo state, and filtering.

Important behaviors include emulating the DSP and DMA playback model rather than just port writes, rendering DMA playback from buffered guest samples at the configured SB sample rate, supporting the different SB families and their different output characteristics so SB 1.x and 2.x sound more raw and rough, SB Pro gets a stronger lowpass character and stereo behavior matters, and SB16 has its own filtering behavior and rate handling, preserving the current sample until new DMA data arrives in direct DAC mode because DOSBox-X keeps the last DAC sample to avoid pops, respecting stereo, mono, 8-bit, and 16-bit mode changes, applying the card-specific lowpass and slew behavior through `updateSoundBlasterFilter()`, and allowing a configuration equivalent to disabling filtering while keeping the default DOSBox-like filtering because that is part of the expected sound.

Practical implication: a pure event log of `out 220h, al` style writes is not enough. You need a device emulator that turns register state and DMA data into rendered PCM, then mix that PCM into the final host stream.

## Mixer and host audio requirements

Model the final output like DOSBox-X: internal mixer rate is configurable, device channels are resampled into the mixer rate, channels are mixed to stereo, and the host audio backend plays the mixer output as signed 16-bit stereo PCM.

If you are writing a modern Windows app, use the Windows audio stack you already have, or SDL2 if that is already part of your application, but keep the DOSBox-style architecture: one or more emulated device channels, a central mixer, a host audio callback or pull-based render loop, and explicit resampling and filtering.

## Implementation guidance

When reconstructing sound from your JSON port log, group events by device and by time, rebuild the device state machine in timestamp order, render audio in small blocks using the current emulated time as the source of truth, render the PC speaker wave from PIT transitions and speaker gate state, render the Sound Blaster sample stream from DSP/DMA state and card-specific filtering, and mix all channels together after they have been rendered to the same sample rate.

If you need a correctness target, the goal is this: when a game sounds correct in DOSBox-X, your app should produce a similar timbre, timing, filter behavior, and stereo image. The output should feel like an emulated ISA sound card, not like synthesized beeps based only on port values.

## What to avoid

Do not map each port write directly to a note or waveform without state reconstruction. Do not ignore timing between writes. Do not mix raw device output straight to the host without a central resampling/mixing stage. Do not skip filtering if you want the DOSBox-like sound. Do not assume PC speaker is just one square wave frequency at a time.

## Suggested design target

That architecture is the closest match to how DOSBox-X actually produces its sound.
