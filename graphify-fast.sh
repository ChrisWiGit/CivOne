#!/usr/bin/env bash
# Builds the graphify code graph without AI (tree-sitter only).
# Usage: ./graphify-fast.sh [--ai] [path]     (default path: repository root)
# Output always goes to <repository root>/graphify-out/, where the agent instructions expect it
# (override with GRAPHIFY_OUT).
# --ai (or GRAPHIFY_AI=1) also names the graph communities through the Claude Code CLI,
# which uses the claude.ai subscription instead of an API key.
# Optional: GRAPHIFY_AI_BACKEND (claude, openai, gemini, ollama, ...), GRAPHIFY_CLAUDE_CLI_MODEL (sonnet, haiku).
# If graphify is neither in ./.venv nor on PATH, it is installed into ./.venv.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd -P)"
VENV="$SCRIPT_DIR/.venv"
USE_AI="${GRAPHIFY_AI:-0}"
AI_BACKEND="${GRAPHIFY_AI_BACKEND:-claude-cli}"
TARGET="$SCRIPT_DIR"
export GRAPHIFY_OUT="${GRAPHIFY_OUT:-$SCRIPT_DIR/graphify-out}"

for arg in "$@"; do
  case "$arg" in
    --ai) USE_AI=1 ;;
    --no-ai) USE_AI=0 ;;
    *) TARGET="$arg" ;;
  esac
done

find_graphify() {
  if [ -x "$VENV/bin/graphify" ]; then
    echo "$VENV/bin/graphify"
  elif command -v graphify >/dev/null 2>&1; then
    command -v graphify
  else
    echo "graphify not found, installing graphifyy into $VENV ..." >&2
    python3 -m venv "$VENV" >&2
    "$VENV/bin/pip" install --quiet graphifyy >&2
    echo "$VENV/bin/graphify"
  fi
}

# graphify's claude-cli backend needs "claude" on PATH; fall back to the binary bundled with the VS Code extension.
ensure_claude_cli() {
  if command -v claude >/dev/null 2>&1; then
    return 0
  fi

  local bundled
  bundled="$(ls -d "${HOME:-}"/.vscode/extensions/anthropic.claude-code-*/resources/native-binary/claude 2>/dev/null | sort -V | tail -n 1)"
  if [ -z "$bundled" ]; then
    echo "Claude Code CLI not found. Install it or set GRAPHIFY_AI_BACKEND to another backend." >&2
    exit 1
  fi

  export PATH="$(dirname "$bundled"):$PATH"
}

GRAPHIFY="$(find_graphify)"
"$GRAPHIFY" update "$TARGET"

if [ "$USE_AI" = "1" ]; then
  if [ "$AI_BACKEND" = "claude-cli" ]; then
    ensure_claude_cli
  fi
  "$GRAPHIFY" label "$TARGET" --backend="$AI_BACKEND"
fi
