# CVL Tune Numbers (from CIVPLAY)

This note identifies what each of the tune numbers `3..44` that `CIVPLAY.C` accepts is actually used
for in DOS Civilization, and how a few of them are arranged. What CivOneX built on top of this - the
sound pack pipeline and the runtime engine - is described separately in
[cvl.soundengine.md](cvl.soundengine.md), not repeated here.

## Attribution

The driver ABI itself (the six exported function pointers a `*.cvl` module provides, their offset in
the export table, and the `INT 8` scheduler that drives a driver's worker functions) was first
reverse-engineered and published by
[rajko-horvat's CivPlay project](https://github.com/rajko-horvat/CivPlay) (MIT licensed). That is his
finding, not repeated here; see his repository for the ABI itself.

No code from CivPlay was copied into CivOne. Only the fact that a driver is called with a tune number
`0..44` through `PlayTuneFn(tune, 3)` / `PlayTuneFn(0)` was used as the starting point below.

The tune number catalogue and the arrangement mechanism in this document were reverse-engineered
independently for CivOneX by analysing the actual game binary and its call sites, and are not part of
CivPlay.

## Tune Numbers

`CIVPLAY.C` allows inputs `3..44` (with `0` to stop/exit).

Known mappings (triggers confirmed against the original, see the notes below):

- `3`  Title Music
- `4`  Evolution Music
- `5`–`18`  Leader Themes, Long (Audience, Palace Intro, Civilization Founding, Dynasty End, Replay):
  `5` Lincoln, `6` Montezuma, `7` Ramesses, `8` Shaka Zulu, `9` Napoleon, `10` Caesar, `11` Stalin,
  `12` Alexander the Great, `13` Elizabeth, `14` Hammurabi, `15` Mao, `16` Genghis Khan, `17` Gandhi,
  `18` Frederick
- `19`–`32`  the same leader themes, short (event jingles: technology discovered, city conquered,
  wonder built, short audience ended), in the same order as `5`–`18`
- `33` Hostile-ultimatum sting over an already open audience (tribute or technology demanded,
  provocation, rejection, units ordered out, mobilisation). Not the audience itself - the leader
  themes cover that.
- `34` Win Music - also the start of a "We love the \<government\>" day in a city
- `35` Lose Music - also a cancelled celebration in a city
- `36` Alarm sting (famine, civil unrest, overthrow of government, nuclear disaster) – also serves as the
  barbarian theme. The barbarian half is indirect: the nation table gives the barbarians `36` as both
  their short and their long theme, where every real civilization has `19`–`32` and `5`–`18`.
- `37`–`44` short effects: `37` illegal move / error beep (possibly also "unit arrived"),
  `38`–`41` battle outcome, `42` nuclear blast, `43` bomber, `44` city completes a building or
  wonder (the showcase pass of the city view, not opening it)

Note: Not all numbers are labeled, but the allowed range in this player is `3..44`.

## Arrangements

A few tunes are not one fixed sequence but several interchangeable ones, so that replaying the same
tune number does not always sound identical.

**The standard form**, present in both drivers: the handler does not jump straight to a sequence.
Instead it takes the play argument, shifts it and masks it with `6`, and uses the result as an index
into a table of four 16-bit stream pointers - a table lookup, not a random pick, so it is the caller's
argument that decides which of the four plays. This is not specific to AdLib: the PC speaker driver
uses the identical shift-and-mask-with-`6` pattern for tunes `33` and `36`, and the AdLib driver uses it
for each of the fourteen long leader themes (`5`–`18`). A pack keeps all four arrangements and picks
between them at playback; see `ArrangementCount` in the pack index.

**One exception**: on the AdLib driver, tune `33` (the ultimatum sting) does not use this table-lookup
form at all. Its handler ducks the eight running voices, plays the sting on a ninth, then restores
them; the arrangement it plays is chosen by a persistent counter that increments and wraps after
seven entries, so it cycles through its own arrangements a play at a time rather than being selected
by argument. That table therefore holds seven entries, not four, and had to be recognised by its own
handler shape.

How CivOneX renders, stores, and plays these tunes back - including the AdLib voice bytecode, loop
point detection, and the runtime mixer - is described in [cvl.soundengine.md](cvl.soundengine.md).
