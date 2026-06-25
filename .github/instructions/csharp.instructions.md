---
applyTo: "**/*.cs"
---

# C# Coding Guidelines

## General Principles
- Always write clean, readable, and maintainable code
- Follow SOLID principles
- Prefer composition over inheritance
- Keep methods small and focused (max ~20-30 lines)

## Collections
- Always use collection expressions [] instead of new List<T>() where possible
- Example: var list = []; instead of new List<string>()

## Dependency Injection
- Always use dependency injection
- Never instantiate dependencies manually using `new` inside classes, except in tests.
- Use constructor injection only
- Do not use service locator pattern

## Interfaces & Architecture
- Always depend on abstractions (interfaces), never concrete implementations
- Define interfaces for services
- Keep business logic out of controllers

## Error Handling
- Use exceptions only for exceptional cases
- Do not swallow exceptions
- Always log meaningful errors

## Naming Conventions
- Use PascalCase for public members
- Use camelCase for local variables
- Use meaningful and descriptive names

## Null Handling
- Avoid null where possible
- Use nullable reference types correctly
- Validate inputs at boundaries

## Logging
- Use structured logging
- Do not log sensitive data

## Code Style
- Use expression-bodied members where it improves readability
- Prefer pattern matching over complex if/else chains
- Avoid deeply nested code

## Comments
- Write code that is self-explanatory
- Only add comments for complex or non-obvious logic


## C# Features

- Use primary constructors most of the time
- Use records for immutable data structures

## Warnings

- Currently all warnings are treated as errors. If you need to suppress a warning, use `[SuppressMessage]` with a clear justification. Do not suppress warnings without a good reason.
- It is a okay reason to suppress warnings if they would create a huge amount of work to fix.


## C# Nullable Reference Types & Nullability Attributes Guidelines

### General Rules

When writing C# code:

* Always enable and respect Nullable Reference Types (`<Nullable>enable</Nullable>`).
* Never suppress warnings with the null-forgiving operator (`!`) unless there is no better solution.
* Prefer explicit nullability contracts over warning suppression.
* Use `System.Diagnostics.CodeAnalysis` nullability attributes whenever they improve compiler flow analysis.

### Required Attributes

#### Guard Methods

For methods that throw when a parameter is null:

```csharp
public static void ThrowIfNull([NotNull] object? value)
```

Use `[NotNull]` on parameters that are guaranteed to be non-null after successful return.

---

#### Try-Pattern Methods

For methods returning `bool` and exposing an `out` parameter:

```csharp
public bool TryGetUser(
    int id,
    [NotNullWhen(true)] out User? user)
```

Rule:

* Use `[NotNullWhen(true)]` when a successful return guarantees a non-null value.
* Use `[MaybeNullWhen(false)]` for generic scenarios where the value may be null on failure.

---

#### Conditional Return Values

For methods whose return nullability depends on an input parameter:

```csharp
[return: NotNullIfNotNull(nameof(input))]
public string? Normalize(string? input)
```

Always use `NotNullIfNotNull` when this relationship exists.

---

#### Generic APIs

When returning generic values that may be null:

```csharp
[return: MaybeNull]
public T Find<T>()
```

Use `[MaybeNull]` instead of incorrectly assuming `T` is always non-null.

---

#### Lazy Initialization

When a method initializes fields or properties:

```csharp
[MemberNotNull(nameof(_service))]
private void EnsureInitialized()
```

Use `[MemberNotNull]` whenever the method guarantees initialization of nullable members.

For conditional initialization:

```csharp
[MemberNotNullWhen(true, nameof(_service))]
private bool TryInitialize()
```

Use `[MemberNotNullWhen]`.

---

#### Property Contracts

If a nullable value may be assigned to a non-nullable property and is handled internally:

```csharp
[AllowNull]
public string Name { get; set; }
```

If a nullable property must never receive null assignments:

```csharp
[DisallowNull]
public string? Name { get; set; }
```

---

### Preferred Design Principles

* Express nullability through type annotations first.
* Use nullability attributes to describe relationships that the compiler cannot infer.
* Prefer compiler-understandable contracts over comments.
* Avoid unnecessary nullable warnings.
* Avoid warning suppression unless technically unavoidable.
* Public APIs should provide precise nullability information.

### Code Review Checklist

Before completing code generation:

* Are all reference types correctly nullable/non-nullable?
* Is every Try-pattern using `NotNullWhen(true)`?
* Are conditional return values using `NotNullIfNotNull`?
* Are guard methods using `NotNull`?
* Are initialization helpers using `MemberNotNull` or `MemberNotNullWhen`?
* Are generic return values using `MaybeNull` when appropriate?
* Can any usage of `!` be replaced with a nullability attribute?
