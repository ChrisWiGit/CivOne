#!/usr/bin/env bash
# Usage: See README.md section "Graphify code graph workflow" for scope details and examples.
# Quick start: ./graphify-fast.sh src | api | both | combined
set -euo pipefail

TARGET_SPEC="${1:-src}"
GRANULARITY="${GRAPHIFY_GRANULARITY:-low}"
ENTRY_POINTS="${GRAPHIFY_ENTRY_POINTS:-app,main,index,root,server,router}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd -P)"
OUTPUT_BASE="${GRAPHIFY_OUTPUT_ROOT:-$SCRIPT_DIR/graphify-out}"

has_graphify() {
  "$1" -c 'import graphify' >/dev/null 2>&1
}

choose_python() {
  local candidates=()

  if [ -n "${GRAPHIFY_PYTHON:-}" ]; then
    candidates+=("$GRAPHIFY_PYTHON")
  fi

  candidates+=("./.venv/bin/python")
  candidates+=("./.venv/Scripts/python.exe")

  if [ -n "${HOME:-}" ]; then
    candidates+=("$HOME/graphify-ext/Scripts/python.exe")
  fi

  if command -v python3 >/dev/null 2>&1; then
    candidates+=("python3")
  fi

  if command -v python >/dev/null 2>&1; then
    candidates+=("python")
  fi

  local candidate
  for candidate in "${candidates[@]}"; do
    if [[ "$candidate" == *"/"* ]]; then
      if [ ! -f "$candidate" ]; then
        continue
      fi
    fi

    if has_graphify "$candidate"; then
      echo "$candidate"
      return 0
    fi
  done

  return 1
}

if ! PYTHON_EXE="$(choose_python)"; then
  echo "No Python with installed 'graphify' found." >&2
  echo "Set GRAPHIFY_PYTHON to an interpreter where 'import graphify' works." >&2
  exit 1
fi

if [[ "$PYTHON_EXE" == *"/"* ]] && [ ! -f "$PYTHON_EXE" ]; then
  echo "Graphify Python not found: $PYTHON_EXE" >&2
  echo "Set GRAPHIFY_PYTHON to your graphify python.exe path." >&2
  exit 1
fi

run_target() {
  local target_path="$1"

  if [ ! -d "$target_path" ]; then
    echo "Target folder not found: $target_path" >&2
    exit 1
  fi

  local target_abs
  if command -v realpath >/dev/null 2>&1; then
    target_abs="$(realpath "$target_path")"
  else
    target_abs="$target_path"
  fi

  local rel_target
  case "$target_abs" in
    "$SCRIPT_DIR")
      rel_target="root"
      ;;
    "$SCRIPT_DIR"/*)
      rel_target="${target_abs#"$SCRIPT_DIR"/}"
      ;;
    *)
      rel_target="$(basename "$target_abs")"
      ;;
  esac

  local dest_output="$OUTPUT_BASE/$rel_target"
  local source_output="$target_abs/graphify-out"

  GRAPHIFY_TARGET="$target_abs" \
  GRAPHIFY_GRANULARITY="$GRANULARITY" \
  GRAPHIFY_ENTRY_POINTS="$ENTRY_POINTS" \
  "$PYTHON_EXE" -c 'from pathlib import Path; import os; import graphify.ai as ai; ai.Summarizer.is_available=lambda self: False; from graphify.watch import _rebuild_code; target=Path(os.environ["GRAPHIFY_TARGET"]); gran=os.environ.get("GRAPHIFY_GRANULARITY","low"); entries=[x for x in os.environ.get("GRAPHIFY_ENTRY_POINTS","app,main,index,root,server,router").split(",") if x]; ok=_rebuild_code(target, granularity=gran, entry_points=entries); raise SystemExit(0 if ok else 1)'

  if [ -d "$source_output" ] && [ "$source_output" != "$dest_output" ]; then
    # Stage outside SOURCE_OUTPUT first: when target is the script root,
    # DEST_OUTPUT is nested inside SOURCE_OUTPUT and a direct mv would fail.
    local stage_output="$target_abs/.graphify-out.stage.$$"
    rm -rf "$stage_output"
    mv "$source_output" "$stage_output"
    rm -rf "$dest_output"
    mkdir -p "$(dirname "$dest_output")"
    mv "$stage_output" "$dest_output"
  fi

  GRAPHIFY_OUTPUT_DIR="$dest_output" \
  "$PYTHON_EXE" - <<'PY'
from pathlib import Path
import json
import os

from graphify.export import to_html

output_dir = Path(os.environ["GRAPHIFY_OUTPUT_DIR"])
html_path = output_dir / "graph.html"
json_path = output_dir / "graph.json"

def build_sample_html() -> bool:
  if not json_path.exists():
    return False

  data = json.loads(json_path.read_text(encoding="utf-8"))
  nodes = data.get("nodes", [])
  if not nodes:
    return False

  edges = data.get("links", data.get("edges", []))
  max_nodes = 4500
  selected_ids = {n.get("id") for n in nodes[:max_nodes] if n.get("id")}

  import networkx as nx
  G = nx.Graph()
  for node in nodes:
    node_id = node.get("id")
    if node_id in selected_ids:
      attrs = dict(node)
      attrs.pop("id", None)
      G.add_node(node_id, **attrs)

  for edge in edges:
    source = edge.get("source", edge.get("from"))
    target = edge.get("target", edge.get("to"))
    if source in selected_ids and target in selected_ids:
      attrs = dict(edge)
      attrs.pop("source", None)
      attrs.pop("target", None)
      attrs.pop("from", None)
      attrs.pop("to", None)
      G.add_edge(source, target, **attrs)

  communities = {}
  for node_id, attrs in G.nodes(data=True):
    community = int(attrs.get("community", 0))
    communities.setdefault(community, []).append(node_id)

  metadata_path = output_dir / "metadata.json"
  labels = {}
  summaries = {}
  if metadata_path.exists():
    metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
    labels = metadata.get("communityLabels", {})
    summaries = metadata.get("communitySummaries", {})

  to_html(
    G,
    communities,
    str(html_path),
    community_labels=labels or None,
    community_summaries=summaries or None,
    granularity="low",
  )
  return True

if not html_path.exists():
  created = build_sample_html()
  if created:
    print("Generated compatibility graph.html from sampled graph.json")
PY

  if [ "$rel_target" = "src" ] || [ "$rel_target" = "root" ]; then
    mkdir -p "$OUTPUT_BASE"
    find "$dest_output" -maxdepth 1 -type f -exec cp -f {} "$OUTPUT_BASE" \;
  fi

  echo "Graphify output: $dest_output"
}

targets=()
case "$TARGET_SPEC" in
  src)
    targets+=("$SCRIPT_DIR/src")
    ;;
  api)
    targets+=("$SCRIPT_DIR/api")
    ;;
  both)
    targets+=("$SCRIPT_DIR/src" "$SCRIPT_DIR/api")
    ;;
  combined)
    targets+=("$SCRIPT_DIR")
    ;;
  *)
    targets+=("$TARGET_SPEC")
    ;;
esac

for t in "${targets[@]}"; do
  run_target "$t"
done
