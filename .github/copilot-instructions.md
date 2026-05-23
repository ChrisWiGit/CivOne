# Copilot Instructions

## Chat Answers

* Use Caveman Compression whenever possible.
* Do not narrate progress.
* If progress updates are necessary, keep them very short.

## Documentation

* Always write documentation in English.
* Use concise and clear language.
* Avoid jargon unless necessary.
* Briefly explain technical terms when used.

## C# Coding Guidelines

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

### Code Style

* Keep methods small and focused.
* Prefer `new()` expressions over fully qualified construction syntax.
* Prefer collection expressions:

  * `[ "a", "b", "c" ]` instead of `new string[] { ... }`
* Prefer `[ ..collection ]` over `.ToArray()`.
* Prefer `.Length` instead of `.Any()` when working with arrays.

## Building

Use quiet build output and only show the final lines.

### PowerShell

```powershell
dotnet build CivOne.csproj -v q 2>&1 | Select-Object -Last 15
```

### Bash

```sh
dotnet build CivOne.csproj -v q 2>&1 | tail -n 15
```

## Documentation Comments

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

Example:

```sh
dotnet test xunit/CivOne.UnitTests.csproj --filter "FullyQualifiedName~TranslationFileRepositoryImplTests|FullyQualifiedName~TranslationServiceFactoryTests"
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

* Use `ITranslationService` and `TranslationServiceFactory`.
* Prefer existing protected translation properties when available.
* Otherwise inject `ITranslationService`.

### Translation Rules

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

If a file contains many translation calls:

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
