# Copilot Instructions – MCP Usage

## Purpose

Ensure MCP-related tasks use consistent rules and point to the canonical reference.

## Rules

- For MCP protocol, lifecycle, transport, authentication, and tool behavior, use [MCP.md](../../MCP.md) as the source of truth.
- Use MCP tools directly whenever MCP is available.
- Do not create intermediary scripts (for example Python, PowerShell, or shell scripts) to call MCP unless there is a clear, unavoidable need.
- If a script-based fallback is truly necessary, explicitly justify why direct MCP usage is not possible.
- If the MCP server cannot be reached or is unavailable, clearly state this to the user.
- Keep implementation changes aligned with documented MCP method names and payload shapes.
- When adding or changing MCP tools, update [MCP.md](../../MCP.md) in the same change.
- Prefer concise examples that match current behavior.

## Scope

Applies whenever work includes MCP server behavior, MCP tool handlers, or MCP documentation updates.