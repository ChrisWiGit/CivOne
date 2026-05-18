# civtranslate

civtranslate is a small C# CLI tool for CivOne.
It scans C# files for translation calls and creates or updates a key-value translation file.

## What it scans

The scanner searches `*.cs` files in a folder recursively.
It extracts keys from these call patterns:

- `.Translate("...")`
- `.TranslateFormatted("...", ...)`
- `T("...")`

It supports multiple matches in one line.

## Interpolation behavior

Interpolated strings are ignored on purpose.
This includes patterns like `$"..."`, `@$"..."`, and `$@"..."`.
The tool prints a warning with file path and line number and continues scanning.

Placeholders like `{0}` inside normal string literals are allowed.

## Output format

Output uses `key=value` lines.

Rules:

- Key matching is case-insensitive by normalizing keys to uppercase.
- Output keys are always uppercase.
- If a key is new, value is set to the original source text.

Separator escape rule:

- Separator is `=`.
- If `=` appears inside key or value, it is replaced with `[EQ]`.

## Existing output file behavior

If the output file already exists:

- Existing entries are loaded into a dictionary by uppercase key.
- Found keys reuse existing values.
- New keys are added with the original source text as the value.
- Keys not found in current scan are kept.
- A warning is printed for each kept key that is no longer found.

## Usage

Run from repository root or any working directory.

```sh
# Show help
 dotnet run --project ./civtranslate/civtranslate.csproj -- --help
```

```sh
# Scan ./src and write default output translate_all.txt
 dotnet run --project ./civtranslate/civtranslate.csproj -- ./src
```

```sh
# Scan ./src and write custom output file
 dotnet run --project ./civtranslate/civtranslate.csproj -- ./src --output ./translate_de.txt
```

## Build

```sh
dotnet build ./civtranslate/civtranslate.csproj
```

## Example output

```
Keys reused: 14
Keys added: 0
Keys overwritten: 0
Obsolete keys kept: 1
Output written: C:\Users\Christian\Documents\Projekte\CivOne-Chris\translation\all.txt
PS C:\Users\Christian\Documents\Projekte\CivOne-Chris> .\translate.ps1
Warning: Key not found in current scan but kept in output: FAST SAVE1 IS NOT AVAILABLE RIGHT NOW.

Warning: Key not found in current scan but kept in output: FAST SAVE1 IS NOT AVAILABLE RIGHT NOW.
```

## Example all.txt

```txt
BC=BC
AD=AD
FAST SAVE IS NOT AVAILABLE RIGHT NOW.=Fast save is not available right now.
COULD NOT SAVE FAST SAVE SLOT.=Could not save fast save slot.
FAST SAVE SLOT IS EMPTY.=Fast save slot is empty.
COULD NOT LOAD FAST SAVE SLOT.=Could not load fast save slot.
QUICK SAVE/LOAD=Quick Save/Load
IN {3} LEADER {0} {1} OF THE {2}=In {3} leader {0} {1} of the {2}
LORD=Lord
PRINCE=Prince
KING=King
EMPEROR=Emperor
DEITY=Deity
CHIEF=Chief
FAST SAVE1 IS NOT AVAILABLE RIGHT NOW.=Fast save1 is not available right now.
```