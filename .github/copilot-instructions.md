# CoPilot Instructions

## Chat Answers

* Use [Caveman Compression](./instructions/cavemen.instructions.md) to compress your answers when possible.
* Do not comment on current progress or if necessary only use short phrases in Caveman style.

## Documentation

All documentation should be written in English (even if the prompt is in another language). Use clear and concise language to explain concepts, code, and processes. Avoid jargon and technical terms unless necessary, and provide definitions when they are used.



## C# Coding Guidelines

- Always use dependency injection. Never instantiate dependencies manually with new.
- Avoid service locator pattern
- Use constructor injection
- Follow SOLID principles
- Prefer interfaces and services
- Prefer Factory pattern instead of new for services.
  - Prefer one factory for multiple services instead of one factory per service (e.g. MyServiceFooFactory for MyCommandFooService, MyQueryFooService)
- Keep methods small and focused
- Prefer 'new()' expression instead of fully qualified new syntax 
- Instead of .toArray() use [.. collection] to create a new array from an IEnumerable
- Instead of `new string[] { "a", "b", "c" }` use `[ "a", "b", "c" ]` for better readability and less verbosity.
- Instead of .Any() use .Length when working with arrays for better readability and performance.

## Building

Use build command with quiet verbosity and only show the last lines of output to confirm successful build without overwhelming with details.

Example with PowerShell and with the last 15 lines of output:

```powershell
dotnet build CivOne.csproj -v q 2>&1 | Select-Object -Last 15
```

Example with bash and with the last 15 lines of output:
```sh
dotnet build CivOne.csproj -v q 2>&1 | tail -n 15
```

## Documentation

- Use XML documentation comments for all public members and classes.
- Provide clear summaries, parameter descriptions, and return value explanations.
- Use examples in documentation where helpful.
- After each sentence add a line break to improve readability.

## Tests

* Run tests only when necessary.
* Run only the tests that are relevant to the current change.
* e.g. `dotnet test xunit/CivOne.UnitTests.csproj --filter "FullyQualifiedName~TranslationFileRepositoryImplTests|FullyQualifiedName~TranslationServiceFactoryTests"`

## Existing useful code

- Debounce Service with DebounceServiceFactory and IDebounceService
- Random Number Generator with IRandomNumberGenerator and RandomNumberGeneratorFactory

## Delegate Pattern

Use the Delegate Pattern when the user requests refactoring logic into delegates, callbacks, handlers, or interchangeable behaviors.

The Delegate Pattern should be applied when:

* behavior needs to be passed dynamically to another class or method
* responsibilities should be separated to reduce coupling
* different implementations of the same behavior may exist
* event handling, callbacks, command execution, or strategy-like behavior is needed
* the user explicitly mentions “delegate”, “delegation”, “refactor into delegates”

The implementation should:

* keep the calling class focused on orchestration
* move executable behavior into dedicated delegate functions/classes
* make behaviors replaceable and testable
* use language-native delegate/function types where appropriate

Implementation:

* Create a class ending with "Delegate" that contains the delegate function(s).
* Instantiate the delegate class within the calling class using new() and call the delegate function(s) where the behavior is needed.