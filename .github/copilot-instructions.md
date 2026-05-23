# Copilot Instructions

## Response Style

* Use Caveman Compression whenever possible.
* Do not narrate progress.
* Keep status updates minimal and short.

## Documentation

* Always write documentation in English.
* Keep wording concise and easy to understand.
* Avoid jargon unless necessary.
* Explain technical terms briefly when used.

## C# Guidelines

### Architecture

* Use dependency injection only.
* Never instantiate services manually with `new`.
* Avoid Service Locator.
* Use constructor injection.
* Follow SOLID.
* Prefer interfaces and services.

### Factories

* Prefer factories over direct service creation.
* Prefer one factory handling multiple related services.
* Delegate classes are not services and may be instantiated directly with `new()`.

### Code Style

* Keep methods small and focused.
* Prefer `new()` over explicit type construction.
* Prefer collection expressions:

  * `[ "a", "b" ]` instead of `new string[] { ... }`
* Prefer `[ ..collection ]` over `.ToArray()`.
* Use `.Length` instead of `.Any()` for arrays.

## Build

Use quiet build output and only show final lines.

### PowerShell

```powershell
dotnet build Project.csproj -v q 2>&1 | Select-Object -Last 15
```

### Bash

```sh
dotnet build Project.csproj -v q 2>&1 | tail -n 15
```

## Documentation Comments

* Add XML docs to all public types and members.
* Include summaries, params, and return descriptions.
* Add examples when useful.
* Add line breaks after sentences for readability.
* Avoid redundant inline comments.

## Tests

* Run tests only when needed.
* Run only relevant tests.
* Run tests without console logs from CivOne-Code when possible to reduce noise (` -p:SuppressConsoleLogs=true`).

Example:

```sh
dotnet test Tests.csproj --filter "FullyQualifiedName~MyTest" -p:SuppressConsoleLogs=true
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

Then manually move entries into language files like:

```txt
civ_german.txt
```
