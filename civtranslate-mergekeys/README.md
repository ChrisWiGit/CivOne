# civtranslate-mergekeys

`civtranslate-mergekeys` compares two `key=value` files and appends keys that exist in the first file but not in the second file.

Use this when you already have a language file with manual translations and only want to add newly introduced keys.
This is often better than recreating the whole file because existing translated values stay untouched.

## Usage

```sh
dotnet run --project ./civtranslate-mergekeys/civtranslate-mergekeys.csproj -- ./translation/all.txt ./translation/civ_german.txt
```

## Helper scripts

Run from repository root.

You can pass plain file names without folders.
They are resolved from the repository `translation` folder automatically.

```powershell
.\translate-mergekeys.ps1 all civ_german
```

```sh
./translate-mergekeys.sh all civ_german
```

You can also pass `.txt` file names.

```powershell
.\translate-mergekeys.ps1 all.txt civ_german.txt
```

## Behavior

- Keys are compared case-insensitively.
- Missing keys are appended to the second file in the order they appear in the first file.
- Existing entries in the second file are preserved.
- The second file is created when it does not exist.

## Notes

- Lines starting with `#` are treated as comments and ignored for comparison.
- Empty lines are ignored.
- The tool keeps `=` escaping via `[EQ]` to match the existing translation file format.
- If a file argument contains a path separator, that path is used as-is.
