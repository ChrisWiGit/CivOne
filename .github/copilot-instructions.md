# CoPilot Instructions

## Documentation

All documentation should be written in English (even if the prompt is in another language). Use clear and concise language to explain concepts, code, and processes. Avoid jargon and technical terms unless necessary, and provide definitions when they are used.

## C# Coding Guidelines

- Always use dependency injection. Never instantiate dependencies manually with new.
- Avoid service locator pattern
- Use constructor injection
- Follow SOLID principles
- Prefer interfaces for services
- Keep methods small and focused
- Prefer 'new()' expression instead of fully qualified new syntax 

## Existing useful code

* Debounce Service with DebounceServiceFactory and IDebounceService
* Random Number Generator with IRandomNumberGenerator and RandomNumberGeneratorFactory