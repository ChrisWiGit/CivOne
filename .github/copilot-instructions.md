# CoPilot Instructions

## Documentation

All documentation should be written in English (even if the prompt is in another language). Use clear and concise language to explain concepts, code, and processes. Avoid jargon and technical terms unless necessary, and provide definitions when they are used.

## MCP Guidance

For MCP-related tasks, use [MCP.md](../MCP.md) as the canonical reference.
Also follow [.github/instructions/mcp.instructions.md](instructions/mcp.instructions.md) for MCP-specific working rules.

## C# Coding Guidelines

- Always use dependency injection. Never instantiate dependencies manually with new.
- Avoid service locator pattern
- Use constructor injection
- Follow SOLID principles
- Prefer interfaces for services
- Keep methods small and focused