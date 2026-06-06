# Copilot Instructions

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

Whenever a diff deletes a line that contains any of `index++`, `++index`, `i++` (in subscripts), `ref `, `out `, `Read…(`, `Write…(`, `Dispose(`, `+=`, `-=`, treat it as suspicious and re-verify behaviour before accepting.

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

### Architecture

* Always use dependency injection.
* Never instantiate services manually with `new`.
* Avoid Service Locator pattern.
* Use constructor injection.
* Follow SOLID principles.
* Prefer interfaces and services.

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

* Culture rule:
  * Use `CultureInfo.InvariantCulture` for stable, culture-insensitive behavior.
  * Use it for serialization, logs, config files, protocol values, IDs, and tests.
  * Examples: `ToLower(..., CultureInfo.InvariantCulture)`, `ToUpper(..., CultureInfo.InvariantCulture)`, `value.ToString(CultureInfo.InvariantCulture)`, `Convert.ToString(value, CultureInfo.InvariantCulture)`, `double.Parse(text, CultureInfo.InvariantCulture)`, `int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture)`, `int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)`.
  * Do not use `InvariantCulture` for user-facing text, localized UI, or user input that should follow current locale.
  * For UI text/input, use `CultureInfo.CurrentCulture`.



### Resizable Screens

* If a screen must react to window size changes, add `[ScreenResizeable]` to the screen class.
* Problem: without `[ScreenResizeable]`, the screen does not receive resize handling from `BaseScreen` and keeps stale 320x200-era drawing state.
* Problem: with `[ScreenResizeable]` but without redraw handling, the bitmap is recreated on resize but static content and menus may stay at old coordinates or not be redrawn correctly.
* Required fix: make the screen redraw itself after resize.
* Required fix: if the screen uses an `_update` flag or cached UI state, set it so the UI is rebuilt after resize.
* Required fix: if the screen is centered in 320x200 space, apply resize-safe offsets or use menu/dialog centering support so content stays aligned.
* Required fix: preserve menu selection where possible; do not recreate menus on every refresh unless necessary.

## Build

Use quiet build output and only show final lines.

### PowerShell

```powershell
dotnet build Project.csproj --property WarningLevel=0 -v q 2>&1 | Select-Object -Last 15
```

### Bash

```sh
dotnet build Project.csproj --property WarningLevel=0 -v q 2>&1 | tail -n 15
```

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
* Add XML docs to all public types and members.
* Include summaries, params, and return descriptions.
* Add examples when useful.
* Add line breaks after sentences for readability.
* Avoid redundant inline comments.

## Tests

* Run tests only when needed.
* Run only relevant tests.
* Run tests without console logs from CivOne-Code when possible to reduce noise (` -p:SuppressConsoleLogs=true`).
* Run tests with quiet output (`-v q`) and no warnings (`--property WarningLevel=0`) to focus on test results.

Example:

```sh
dotnet test "xunit/CivOne.UnitTests.csproj" --filter "FullyQualifiedName~GameMapViewModeTests" -p:SuppressConsoleLogs=true --property WarningLevel=0 -v q 2>&1 | Select-Object -Last 20
```

## Existing Useful Code

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

## Translation

```sh
dotnet test "xunit/CivOne.UnitTests.csproj" --filter "FullyQualifiedName~GameMapViewModeTests" -p:SuppressConsoleLogs=true -p:NoWarn="*" -v q 2>&1 | Select-Object -Last 20
```

## Existing Utilities

Available reusable systems:

* `DebounceServiceFactory` + `IDebounceService`
* `RandomNumberGeneratorFactory` + `IRandomNumberGenerator`

Reuse existing implementations before creating new ones.

## Delegate Pattern

Use when behavior should be replaceable, injected, or separated.

Apply for:

* callbacks
* handlers
* strategies
* interchangeable logic
* delegate-based refactors

Rules:

* Calling class handles orchestration only.
* Move behavior into `*Delegate` classes.
* Use native delegate/function types when appropriate.
* Instantiate delegate classes directly with `new()`.

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

### Rules

* Translation keys are the English text itself.
* Never use string interpolation for translations.
* Use:

  * `Translate`
  * `TranslateFormat`
  * `TranslateArray`
  * `TranslateFormattedArray`

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
