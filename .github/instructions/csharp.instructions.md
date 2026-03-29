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