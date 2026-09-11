# civtext-hashgen

## What this tool is

`civtext-hashgen` is a small .NET CLI tool that generates the default text-language hash definitions used by CivOne.

It reads known original Civilization text files (for example `BLURB0.TXT`, `HELP.TXT`, `ERROR.TXT`) from a data directory, computes SHA-256 hashes for configured segments, and emits C# code for:

- `OriginalTextLanguageValidationFileDefinition[]`
- `OriginalTextLanguageValidationSegmentDefinition`

The generated output is intended for `OriginalTextLanguageValidationDefaultDefinitions.cs`.

## Why this exists

Maintaining many hardcoded hashes by hand is error-prone.

This generator gives you a reproducible way to refresh hash definitions when original source texts change, while keeping the segment configuration centralized in one place.

## Input and output

Input:

- A directory path containing the original `.TXT` files.
- The file names and segment selectors are defined inside `Program.cs`.

Output:

- By default: generated C# code is written to stdout.
- With `--output`: generated C# code is written directly to a file (create or overwrite).

## Segment sources

The generator supports two source types:

- `MarkerBodyLine`: hashes one configured body line inside a marker section.
- `ByteWindow`: hashes a fixed raw byte window (`Offset` + `Length`).

For marker-based segments, normalization matches runtime behavior:

1. Replace `^` with line breaks.
2. Collapse whitespace.
3. Trim.
4. Convert to uppercase using invariant casing.

## Usage

Build:

```bash
dotnet build civtext-hashgen/civtext-hashgen.csproj
```

Show help:

```bash
dotnet run --project civtext-hashgen -- --help
```

Generate to console:

```bash
dotnet run --project civtext-hashgen -- /path/to/civ_orig
```

Generate directly to the definitions file:

```bash
dotnet run --project civtext-hashgen -- /path/to/civ_orig --output src/IO/Text/OriginalTextLanguageValidationDefaultDefinitions.cs
```

Generate with trace output:

```bash
dotnet run --project civtext-hashgen -- /path/to/civ_orig --trace
```

You can combine both:

```bash
dotnet run --project civtext-hashgen -- /path/to/civ_orig --trace --output src/IO/Text/OriginalTextLanguageValidationDefaultDefinitions.cs
```

## Typical workflow

1. Update original `.TXT` files in your source folder.
2. Run the generator with `--output` targeting `src/IO/Text/OriginalTextLanguageValidationDefaultDefinitions.cs`.
3. Review the diff.
4. Build and run relevant tests.

## Exit codes

- `0`: success
- `1`: no args / help path requiring input
- `2`: invalid arguments or input directory missing
- `3`: generation failed (missing files or unreadable segments)

## Notes

- This tool currently expects `.TXT` source files.
- The segment list is intentionally explicit in `Program.cs` to keep changes reviewable.
