# civtranslate

civtranslate is a small C# CLI tool for CivOne.
It scans C# files for translation calls and creates or updates a key-value translation file.

## What it scans

The scanner searches `*.cs` files in a folder recursively.
It extracts keys from these call patterns:

- `.Translate("...")`
- `.TranslateFormatted("...", ...)`
- `TranslateFormattedArray("...", ...)`
- `TranslateArray("...", ...)`
- `.TranslateFormatted("...", ...)`
- `TF("...", ...)`
- `T("...")`

It supports multiple matches in one line.

### Adding new translation function names

To scan for additional translation function names:

1. Open `civtranslate/Program.cs` and find the `EnumerateInvocationCandidates` function.
2. Add a new `if` block following the existing pattern:

```csharp
if (TryMatchInvocation(content, index, "YourFunctionName", out int openParenX))
{
    yield return new InvocationCandidate("YourFunctionName", openParenX);
    index = openParenX;
    continue;
}
```

3. Rebuild civtranslate and re-run the scan.

**Note:** The scanner extracts the **first string argument** from the function call. For functions with multiple string arguments, only the first one is used as the translation key.

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

## Recommended translation workflow

Use this end-to-end flow when creating a new language file.

### 1. Generate or update `all.txt`

From repository root, generate the source translation file.

```sh
./translate.sh
```

```powershell
.\translate.ps1
```

This writes or updates `translation/all.txt`.

### 2a. Create your language file from `all.txt`

> only if you intend to create a new language file or want to reset an existing one with all keys from `all.txt`.

Copy or rename `translation/all.txt` to `translation/civ_<mylang>.txt`.

Examples:

- `civ_german.txt`
- `civ_french.txt`

If your language also uses other text files, rename them with the same language suffix pattern.
For example, rename `story.txt` to `story_<mylang>.txt`.

> **Use lowercase file names** that follow the documented pattern, for example `civ_german.txt` and generally `civ_<mylang>.txt`. Files with uppercase letters may be skipped by language discovery and tests. When copying translations into place, prefer `.\ copy-translations.ps1` or `./copy-translations.sh`.

### 2b. Manual updating

1. Call `.\translate.ps1` or `./translate.sh` to update `all.txt`
2. Merge new keys from `all.txt` into your language file using `.\translate-mergekeys.ps1 all.txt civ_<mylang>` or `./translate-mergekeys.sh all.txt civ_<mylang>` (no folder `translation/` needed in arguments)
3. Edit your language file and translate values manually in a text editor.
4. Copy the final language file to the active CivOne profile translations folder with `.\copy-translations.ps1` or `./copy-translations.sh`
5. Test with CivOne and repeat from step 1 until you are satisfied.

### 3. Translate values using the interactive roundtrip

Use `civtranslate-interactive` with your language file.

**In short this does:**

Roundtrip behavior:

1. Creates a values work file next to your language file, for example `civ_german.values.txt`.
2. Waits for Enter so you can translate values in that work file.
3. Reads translated values back and writes them into the original `key=value` language file.
4. Preserves key order and comment lines.

Safety behavior:

- The values work file must not exist before start.
- If it already exists, the tool fails and leaves the language file unchanged.

```sh
dotnet run --project ./civtranslate-interactive/civtranslate-interactive.csproj -- --language german
```

Or use helper scripts from repository root.

```powershell
.\translate-interactive.ps1
.\translate-interactive.ps1 -Language german
```

```sh
./translate-interactive.sh
./translate-interactive.sh --language german
```

The language postfix resolves to `translation/civ_<postfix>.txt`.
For example, `--language german` resolves to `translation/civ_german.txt`.

### 4. Copy final files to the active CivOne profile

Copy generated translation `.txt` files from repository `translation` to the active CivOne translations folder.

```sh
./copy-translations.sh
```

```powershell
.\copy-translations.ps1
```

`copy-translations` excludes `all.txt` and copies all other `.txt` files.

### All the other files like KING, BLURB, CREDITS

The original source text for these is not in the C# code but in the existing translation files.

1. To update these, simply edit the existing language file and translate values manually in a text editor.
2. Remove the binary part at the top and end of the file.
3. Rename the file with the same language suffix pattern if needed, for example `KING_<mylang>.txt`.

> **Use lowercase file names** that follow the documented pattern, for example `KING_german.txt`. Files with uppercase letters may be skipped by language discovery and tests. When copying translations into place, prefer `.\ copy-translations.ps1` or `./copy-translations.sh`.

## Example output

```txt
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
