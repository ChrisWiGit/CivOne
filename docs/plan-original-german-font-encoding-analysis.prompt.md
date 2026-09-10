# Prompt: Analyze German Original Civ Font/Text Encoding Issues

## Context
We need to investigate why German umlauts from original Civilization data files are displayed as empty glyphs in-game when translation in game is not active (default).

Current project behavior suggests two interacting layers:
1. Text decoding from original .TXT files.
2. Glyph rendering from FONTS.CV (non-wizard game font pipeline).

This prompt is for a follow-up deep analysis on a machine/environment where verified original German Civ files are available and untouched.

## What Is Already Known
- Runtime font loading currently resolves one font file name: FONTS.CV from the data directory.
- There is no language-specific runtime font file switch (for example no FONTS_DE / FONTS_FR selection path).
- Simulate International Font setting currently behaves as:
  - No -> plain Fontset
  - Auto/Yes -> InternationalSimulatedFontSet
- Text loader currently reads source text files using UTF-8 line decoding.
- Missing/unknown glyphs in Fontset fall back to an empty 8x8 bytemap, which appears as blank characters.
- In one inspected BLURB0.TXT sample dump, byte sequence EF BF BD appeared repeatedly, indicating replacement characters already present in file content or produced before rendering.

## Problem Statement
When original German game text files are copied and used without an active translation file, umlauts may render as blank glyphs in gameplay text.

We must determine whether the root cause is:
1. Wrong runtime setting (international simulation disabled),
2. Wrong decoding strategy for original text files (UTF-8 vs legacy encodings),
3. Corrupted source files produced by prior conversion/export,
4. A mismatch between decoded characters and available glyph mapping in FONTS.CV.

## Out Of Scope
- Wizard DOS font path (startup wizard rendering).
- Translation key-value language files under translations/.

## Inputs Required For Deep Analysis
Provide all of the following from a trusted original German Civilization installation:
1. Raw FONTS.CV.
2. Raw BLURB0.TXT through BLURB4.TXT.
3. Raw ERROR.TXT, HELP.TXT, KING.TXT, PRODUCE.TXT.
4. Optional: a known English original set for comparison.
5. Checksums (SHA-256) of all copied files to prove integrity.

## Investigation Tasks
1. Validate file integrity and provenance.
- Compute SHA-256 for each file before and after copy.
- Verify no text editor or script rewrote bytes.

2. Inspect text byte encodings.
- For each TXT file, inspect raw bytes around known umlaut words.
- Check whether umlauts are stored as:
  - CP437 high-bit bytes,
  - Latin-1 bytes,
  - Civ control-code placeholders,
  - UTF-8 multibyte sequences,
  - Replacement bytes (EF BF BD).

3. Compare decoding strategies.
- Decode same byte ranges as UTF-8, Latin-1, and CP437.
- Identify which decoding reproduces expected German words correctly.

4. Correlate decoded chars with render path.
- Track decoded character values entering text rendering.
- Verify whether each character:
  - maps via special international map,
  - composes via Unicode diacritics,
  - falls back to base letter,
  - or becomes blank.

5. Inspect FONTS.CV glyph availability.
- Verify whether low-code glyphs 0..31 exist and visually contain expected international letters.
- Confirm whether font blocks used by gameplay screens include these glyphs.

6. Configuration verification.
- Confirm runtime value of Simulate International Font during repro.
- Re-run with No, Auto, Yes and compare rendered output.

## Expected Deliverables
1. Root cause classification with evidence:
- Setting-only issue,
- Decode-only issue,
- Source file corruption,
- Mixed/multi-cause.

2. Minimal reproducible case:
- One source line + byte dump + decoded output + rendered result.

3. Recommended fix strategy (if needed):
- Loader decode fallback policy for original TXT files,
- Detection heuristic for damaged files,
- Optional diagnostics/logging improvements.

4. Regression tests to add:
- Preserve umlauts from raw original-style bytes,
- Prevent blank-glyph regressions for German/French special characters.

## Success Criteria
Analysis is complete when:
- We can explain exactly why umlauts become blank in the failing scenario,
- We can reproduce and toggle the behavior intentionally,
- We can define a deterministic fix path with test coverage.
