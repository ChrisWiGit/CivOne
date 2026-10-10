
# Instructions

## Graphify

Code graphs live in two separate folders:

* `graphify-out/src/` - game and engine code (`GRAPH_REPORT.md`, `graph.json`).
* `graphify-out/api/` - public API (`GRAPH_REPORT.md`, `graph.json`).

Read only the top of the matching `GRAPH_REPORT.md` (summary, hubs, god nodes), not the whole file (about 67 KB).
To find code for a topic, grep the report for a keyword, then query the graph:

```sh
.venv/bin/graphify query "how does city production work" --graph graphify-out/src/graph.json
```

`graph.json` is not committed. Generate it locally first.
Regenerate with `./graphify-fast.sh --ai src` and `./graphify-fast.sh --ai api`.
Only `GRAPH_REPORT.md` is committed; see `.gitignore`.
The graph is only a map. Read the source files before drawing conclusions.

## Response Style

* Use Caveman Compression whenever possible.
* Do not narrate progress.
* If progress updates are necessary, keep them very short.

## Code Review: Side-Effect Preservation

Before removing or rewriting any statement, check whether it carries side effects beyond its visible result. A statement that looks "unused" may still mutate shared state.

## Reviews

### Powershell and Shell scripts

If a powershell script is written in a way that is not cross-platform, also provide a bash version of the script that achieves the same result.

In launch.json, if a command is provided for Windows, also provide a command for non-Windows platforms that achieves the same result.

```json
{
  "command": "dotnet '${workspaceRoot}/runtime/sdl/bin/Debug/net9.0/CivOne.SDL.dll' --debug & game_pid=$!; dotnet trace collect --process-id \"$game_pid\" --output '${workspaceFolder}/profiling/civone-profile-'\"$game_pid\"'.nettrace'; dotnet trace convert --format Speedscope '${workspaceFolder}/profiling/civone-profile-'\"$game_pid\"'.nettrace'",
   "windows": {
    "command": "$gameProcess = Start-Process dotnet -ArgumentList '${workspaceRoot}/runtime/sdl/bin/Debug/net9.0/CivOne.SDL.dll','--debug' -WorkingDirectory '${workspaceRoot}' -PassThru; dotnet trace collect --process-id $gameProcess.Id --output ${workspaceFolder}/profiling/civone-profile-$($gameProcess.Id).nettrace; dotnet trace convert --format Speedscope ${workspaceFolder}/profiling/civone-profile-$($gameProcess.Id).nettrace"
   }
}
```

### Structure

* Keep interfaces, classes and enums in its own file, unless they are very small and tightly coupled to the containing class.
* Use namespaces to organize code and avoid name collisions.

### High-risk patterns

* `var x = expr; index += 2;` — value unused, but index advancement on the **same line** must be preserved.
* `var x = reader.ReadXxx();` — the read advances a stream/index even if `x` is discarded.
* Post/pre-increment in expressions: `arr[i++]`, `_bytes[index++]`, `c++` inside a discarded expression.
* `out`/`ref` parameters on an "unused" call.
* Property getters or method calls with hidden mutation (logging, lazy init, caching, position advance).
* `Interlocked.*`, `Volatile.*`, `Dispose()`, event subscriptions hidden in initializers.

### Required checks when removing or simplifying

* If the right-hand side reads from a stream, buffer, or `ref`/index variable, it almost certainly advances state. **Keep the advancement explicitly** (e.g. replace `uint length = BitConverter.ToUInt16(_bytes, index); index += 2;` with `index += 2;`, never with nothing).
* When acting on an "unused variable" warning (CS0219, IDE0059, RCS1118, etc.), separate the side effect from the assignment instead of deleting the whole statement.
* When changing a `for`-loop that mutates the loop variable inside the body (e.g. `arr[x++] = ...`), confirm the new form writes to the **same indices in the same order** and advances by the **same step**.
* When removing a local that is passed to an `out`/`ref` parameter, verify the callee has no observable effect.

### Concrete example (do not regress)

```csharp
// BAD: removing the line drops the index advance that the next reader depends on.
uint length = BitConverter.ToUInt16(_bytes, index); index += 2;
// GOOD: side effect kept, dead value removed.
index += 2; // skip 2-byte length header
```

```csharp
// BAD: removing `byte bits = _bytes[index++];` because `bits` is unused
//      silently shifts every subsequent read by one byte.
// GOOD:
index++; // skip 1-byte bits header
```

### Review trigger

Whenever a diff deletes a line that contains any of `index++`, `++index`, `i++` (in subscripts), `ref`, `out`, `Read…(`, `Write…(`, `Dispose(`, `+=`, `-=`, treat it as suspicious and re-verify behaviour before accepting.

## Branding: CivOneX vs. CivOne

The product was renamed from CivOne to **CivOneX**. Both names are correct, each in its own place:

* **CivOneX** is the product name. Use it in player-facing text, documentation, comments and log messages.
  In code, take it from `ProductInfo.Name` instead of a string literal where possible.
* **CivOne** stays correct for namespaces (`CivOne.*`), assembly, project and executable names (`CivOne.SDL`),
  the CC0 license headers, `ProductInfo.LegacyName`, the profile folder of older versions, and references to
  the original upstream projects.
* The upper-case form `CIVONEX` is intended in DOS-style headers (for example the startup wizard).

Do not report findings that only swap one name for the other in these places.

## Documentation

* Always write documentation in English.
* Use concise and clear language.
* Avoid jargon unless necessary.
* Briefly explain technical terms when used.

* Use dependency injection only.
* Never instantiate services manually with `new`.
* Avoid Service Locator.
* Use constructor injection.
* Follow SOLID.
* Prefer interfaces and services.
* Do not include CC0 as a header comment in new source files. If already present in an existing file, keep it, but never add it to a new file.
* Never remove a CC0 header from a file listed in `.cc0-baseline.csv`.

## Licensing: CC0 vs. MIT

This repository is dual-licensed by file origin, and that has a direct consequence for
how new behavior should be added.

* Files listed in `.cc0-baseline.csv` came from the original CivOne project and are
  **CC0 1.0 Universal** (public domain). They stay CC0 even after being modified, so any
  code written *into* one of those files is contributed under CC0 as well.
* Every file not listed there is new to this repository and is **MIT** (`LICENSE.md`).

### Prefer new files for new behavior

When extending existing functionality, prefer putting the new logic in a **new file** --
a delegate, wrapper, service, or extension class -- over growing an existing CC0 file. All non-static classes use instances and dependency injection.
Reduce the CC0 file to a call into the new type.

Two reasons, both of which apply independently:

1. **Separation of concerns.** The legacy file stays an orchestrator; the new behavior is
   isolated, injectable, and unit-testable without a running game engine.
2. **Licensing.** New files are MIT, so the work stays under this project's own license
   instead of being dedicated to the public domain.

This is a preference, not a prohibition. Bug fixes, small corrections, and changes that
genuinely belong in the existing type should be made in place -- do not create a wrapper
class purely to avoid CC0.

### Architecture

* Always use dependency injection.
* Never instantiate services manually with `new`.
* Avoid Service Locator pattern.
* Use constructor injection.
* Follow SOLID principles.
* Prefer interfaces and services.

### Static Singletons (Map, Game, Common, Settings, RuntimeHandler)

`Map`, `Game`, `Common`, `Settings`, and `RuntimeHandler.Runtime` are static singletons used throughout the codebase (`Map.Instance`, `Game.Instance`, `Common.Civilizations`, `Settings.Instance`, `RuntimeHandler.Runtime`). Do not use them directly inside a class's logic — inject an interface instead, so the class stays testable without a running game engine:

* `Map` editor operations → `IMapEditor` (e.g. `EditorSetTerrain`, `SetStartPosition`, `TryGetStartPosition`).
* `Game` → `IPlayerGame` / `IGame`.
* `Settings` → `ISettings`.
* `RuntimeHandler.Runtime` → `IRuntime`.
* `Common.Civilizations` → inject the `ICivilization[]` collection directly (no wrapper interface needed, `ICivilization` already exists).

Add a constructor parameter for the interface (e.g. `IMapEditor? mapEditor = null`) and resolve it **lazily**, never as an eager default:

```csharp
// BAD: evaluated once at construction time, even if this instance never uses it.
private readonly IMapEditor _mapEditor = mapEditor ?? Map.Instance;

// GOOD: resolved only when actually accessed.
private readonly IMapEditor? _mapEditor = mapEditor;
private IMapEditor MapEditor => _mapEditor ?? Map.Instance;
```

Reason: some of these singletons run expensive or order-dependent static initializers on first touch — e.g. `Reflect.GetAdvances()`/`GetBuildings()`/`GetWonders()` reflect over every loaded assembly and instantiate each type, which requires a registered `IRuntime`. An eager default forces that initialization the moment the class is *constructed*, even in a plain unit test that never needed the dependency, breaking the test. A lazy property defers resolution to first actual use.

### Static Initializers Must Not Need a Runtime

Never add an eagerly initialized `static` field whose initializer reflects, instantiates game types, or touches `RuntimeHandler.Runtime`. Use a lazily initialized cached property instead:

```csharp
// BAD: runs on the first touch of *any* member of this class.
public static IAdvance[] Advances = Reflect.GetAdvances().ToArray();

// GOOD: resolved only when the list is actually used.
private static IAdvance[]? _advances;
public static IAdvance[] Advances => _advances ??= [.. Reflect.GetAdvances()];
```

Why this rule is strict: a static initializer that throws poisons the type for the **entire process**. Once `Common..cctor()` has failed, every later access throws `TypeInitializationException` — including from code and tests that did register a runtime. One test class touching `Common` without a runtime therefore takes down every later test in the same run.

When you see many unrelated tests fail at once with `TypeInitializationException: The type initializer for 'CivOne.Common' threw an exception` and an inner `InvalidOperationException: RuntimeHandler is not initialized`:

* Do not treat it as a flaky or threading problem. Test parallelization is disabled assembly-wide in `xunit/properties/AssemblyInfo.cs`, so it is never a race.
* The intermittency comes from test class **order**, which is not stable between runs. The same filter can pass ten times and then fail.
* The first reported failure is not the culprit. Look for the test class that ran *first* and touched a static without registering a runtime.
* Fix it by making the static lazy, not by adding a runtime to the test that happened to trip over it.

See `REMARKS.md`, chapter "Static Initializers and Tests".

If the concrete class implementing the new interface (e.g. `Map : IMapEditor`) has, or could have, a subclass and cannot be sealed, implement the interface implicitly (public members) rather than explicitly. Explicit interface implementation hides the member from derived classes (flagged by CA1033), and sealing isn't an option once a real subclass exists.

### Factories

* Prefer Factory pattern over direct service instantiation.
* Prefer one factory handling multiple related services.
  * Example: `MyServiceFactory` for `MyCommandService` and `MyQueryService`
* Delegate classes are not services and may be instantiated directly with `new()`.

### Code Smells

* Treat helper classes and pure static utility classes as a code smell.
* Prefer a service when behavior represents a reusable domain capability.
* If a full service is overkill, prefer a dedicated delegate class instead of a helper/static class. See [Delegate chapter](#delegate-pattern).
* Keep behavior behind clear abstractions and keep calling classes focused on orchestration.

### Code Style

* Keep methods small and focused.
* Always use brackets for control flow, even for single statements.
* Prefer `new()` expressions over fully qualified construction syntax.
* Prefer collection expressions:
  * `[ "a", "b", "c" ]` instead of `new string[] { ... }`
* Prefer `[ ..collection ]` over `.ToArray()`.
* Prefer `.Length` instead of `.Any()` when working with arrays.
* Instead of `if (bytes == null) throw new ArgumentNullException(nameof(bytes));`, use `ArgumentNullException.ThrowIfNull(bytes);`.
* Use nullable type if a variable can be null, e.g. `string? name` instead of `string name` if `name` can be null. Use `?` on these fields to access them safely, e.g. `name?.Length` instead of `name.Length` if `name` can be null.
* If a parameter can be null (nullable) make sure to check for null and throw an appropriate exception, e.g. `ArgumentNullException.ThrowIfNull(name);` or provide a default value, e.g. `name = name ?? "default";`. Some services may also have dependency injection parameters that may be null, if so, make sure to use a factory to provide a default service if the injected service is null, e.g. `public MyService(IMyDependency? dependency) { _dependency = dependency ?? MyFactory.Create(); }`.
* When calling method that returns IDisposeable, use `using`. Don't do `using Bytemap unitPicture = ScaleBitmap(movingUnit.ToBitmap(), _tilePixelSize, _tilePixelSize);` but instead `using Bytemap unitSource = movingUnit.ToBitmap(); using Bytemap unitPicture = ScaleBitmap(unitSource, _tilePixelSize, _tilePixelSize);` to immediately dispose the original bitmap after scaling.
* Exception for cached/shared bitmaps: do not use `using` or call `Dispose()` on values returned from sprite caches (`ISprite.Bitmap`, `CachedSpriteCollection` entries, and `UnitExtensions.ToBitmap(...)`). These buffers are owned by the cache and are disposed only by cache clear/dispose.

### Analyzer Suppressions

The projects build with `TreatWarningsAsErrors`, so every warning stops the build. Suppress at the
member with a justification rather than widening the project's `NoWarn` list, so the reason stays
where it applies and the rule keeps working everywhere else.

Two rules are expected to need this.

**CA1822** on a `*Delegate` member that happens not to touch instance data. Delegate classes are
instantiated with `new()` and are deliberately not static utility classes, so making the member
static would defeat the pattern. Follow `PcmMixerDelegate` and `WaveFileWriter`:

```csharp
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
public bool TryParse(ReadOnlySpan<byte> bytes, out LoadedWave wave)
```

**CA1031** where catching every exception is the correct behaviour, above all in a callback that
native code invokes - an exception that escaped it would end the process. See
`SDL.AudioDevice.OnAudioRequested`:

```csharp
[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This method is called from native code. Any exception that escaped it would end the process, so every one of them has to be swallowed here.")]
private void OnAudioRequested(IntPtr userData, IntPtr stream, int length)
```

Do not add either rule to `NoWarn` in a `.csproj`. Outside these two cases both findings are
usually real.

### Culture-invariant formatting and parsing

Use culture-invariant APIs for non-user-facing text, such as identifiers, protocol values, serialization, persistence, logging, and machine-readable data.

Prefer the dedicated invariant casing methods where available:

* `text.ToLowerInvariant()`
* `text.ToUpperInvariant()`

For formatting and parsing, pass `CultureInfo.InvariantCulture` explicitly:

* `value.ToString(CultureInfo.InvariantCulture)`
* `Convert.ToString(value, CultureInfo.InvariantCulture)`
* `double.Parse(text, CultureInfo.InvariantCulture)`
* `int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture)`
* `int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)`

There is no general `ToStringInvariant()` method in .NET 9. Use `ToString(CultureInfo.InvariantCulture)` instead.

Do not use `InvariantCulture` for user-facing text, localized UI, or user input that should follow the current locale. For these cases, use `CultureInfo.CurrentCulture`.

### Resizable Screens

* If a screen must react to window size changes, add `[ScreenResizeable]` to the screen class.
* Problem: without `[ScreenResizeable]`, the screen does not receive resize handling from `BaseScreen` and keeps stale 320x200-era drawing state.
* Problem: with `[ScreenResizeable]` but without redraw handling, the bitmap is recreated on resize but static content and menus may stay at old coordinates or not be redrawn correctly.
* Required fix: make the screen redraw itself after resize.
* Required fix: if the screen uses an `_update` flag or cached UI state, set it so the UI is rebuilt after resize.
* Required fix: if the screen is centered in 320x200 space, apply resize-safe offsets or use menu/dialog centering support so content stays aligned.
* Required fix: preserve menu selection where possible; do not recreate menus on every refresh unless necessary.

## Git

When reviewing staged changes, prefer a single combined diff to minimize token overhead:

```bash
git diff --cached
```

For large staged changes, first request a compact overview:

```bash
git diff --cached --stat
git diff --cached --name-status
```

Then inspect only relevant files or chunks:

```bash
git diff --cached -- path/to/file
```

Prefer compact diffs when full context is unnecessary:

```bash
git diff --cached --unified=1 --find-renames
```

Use `--unified=0` only when the exact changed lines are sufficient. Avoid running separate full diffs for every staged file unless the combined diff is too large or specific files need focused review.

## Build

Use quiet build output and only show final lines.

### PowerShell

```powershell
dotnet build Project.csproj --property WarningLevel=0 -v q 2>&1 | Select-Object -Last 15
```

> Important: The project is configured to treat warnings as errors. Use `--property WarningLevel=0` to suppress warnings during build, to check only for real errors. In the end build or test the project without `--property WarningLevel=0` to ensure no warnings are shown as errors to the user if the project is built.

### Bash

```sh
dotnet build Project.csproj --property WarningLevel=0 -v q 2>&1 | tail -n 15
```

> Important: The project is configured to treat warnings as errors. See PowerShell section for details on suppressing warnings during build.

## Documentation Comments

* Always use English for XML documentation, comments in code, commit messages, and also in README and other markdown files, unless the user explicitly requests otherwise.
* Use XML documentation comments for all public types and members.
* Include:
  * summaries
  * parameter descriptions
  * return descriptions
* Add examples when useful.
* Add line breaks after sentences for readability.
* Avoid comments that explain obvious logic.

## Tests

* Run tests only when necessary.
* Run only tests relevant to the current change.
* Run tests without console logs from CivOne-Code when possible to reduce noise (`-p:SuppressConsoleLogs=true`).
* Run tests with quiet output (`-v q`) and no warnings (`--property WarningLevel=0`) to focus on test results.

Example:

```sh
dotnet test "xunit/CivOne.UnitTests.csproj" --filter "FullyQualifiedName~GameMapViewModeTests" -p:SuppressConsoleLogs=true --property WarningLevel=0 -v q 2>&1 | Select-Object -Last 20
```

## Existing Utilities

Reuse existing systems before creating new implementations.

Available utilities:

* `DebounceServiceFactory` with `IDebounceService`
* `RandomNumberGeneratorFactory` with `IRandomNumberGenerator`

## Delegate Pattern

Use the Delegate Pattern when behavior should be replaceable, injectable, or separated.

Apply when:

* behavior must be passed dynamically
* responsibilities should be decoupled
* multiple behavior implementations may exist
* callbacks, handlers, commands, or strategies are needed
* new behavior would otherwise be added to a CC0 file (see [Licensing](#licensing-cc0-vs-mit))
* the user explicitly requests delegate-based refactoring

### Rules

* Keep the calling class focused on orchestration.
* Move executable behavior into dedicated delegate classes.
* Make behaviors testable and replaceable.
* Use native delegate/function types where appropriate.

### Implementation

* Create classes ending with `Delegate`.
* Store delegate functions inside those classes.
* Instantiate delegate classes directly with `new()` in the calling class.
* Suppress `CA1822` on delegate members that do not touch instance data; see the chapter "Analyzer Suppressions".

## Translation

* Use `ITranslationService` and `TranslationServiceFactory`.
* Prefer existing protected translation properties when available.
* Otherwise inject `ITranslationService`.

### Translation Rules

* Translation keys are the English text itself.
* Never use string interpolation for translations, e.g. `Translate($"Attack at {cityName}")` is not allowed.
* Never use concatenation for translations, e.g. `Translate("Attack at " + cityName)` is not allowed.
* Never use ternary operators for translations, e.g. `Translate(isAttack ? "Attack at {cityName}" : "Defend {cityName}")` is not allowed.
* Never use other method calls inside translation calls, e.g. `Translate(GetAttackMessage(cityName))` is not allowed.
* Never use variables inside translation calls, e.g. `Translate(messageKey)` is not allowed.
* If static or constant fields are used in a translation call, copy the string itself into the translation key, e.g. `Translate(fieldOrConstValue)` is not allowed, but `Translate("Population:")`. Move the `fieldOrConstValue` into the value of the translation entry instead.
* Use `Translate`, `TranslateFormat`, `TranslateArray`, and `TranslateFormattedArray`.

### Multi-line Example

```csharp
Translate("Line 1\nLine 2\nLine 3")
```

### Formatted Example

```csharp
TranslateFormat("Attack at {0}", cityName)
```

### Convenience Wrapper

If many translations exist in a file:

```csharp
private string T(string key) => _translationService.Translate(key);
```

### Translation Extraction

Use:

```sh
translate.ps1
translate.sh
```

This updates:

```txt
translation/all.txt
```

Then manually move entries into language-specific files such as:

```txt
civ_german.txt
```

## rg

The `rg` (ripgrep) command is not available in this environment.

When searching for files or text, use these alternatives instead:

* Find files:
  * `find . -type f`
  * `fd` (if available)

* Search file contents:
  * `grep -R "pattern" .`
  * `grep -rn "pattern" .`

* List directories:
  * `find . -type d`

Do not use `rg` in commands, scripts, or examples unless explicitly confirmed to be installed.
