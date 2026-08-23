#!/usr/bin/env sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
PROJECT_PATH="$SCRIPT_DIR/civtranslate/civtranslate.csproj"
INPUT_PATH="$SCRIPT_DIR/src"
OUTPUT_DIR="$SCRIPT_DIR/translation"
OUTPUT_PATH="$OUTPUT_DIR/all.txt"

mkdir -p "$OUTPUT_DIR"

dotnet run --project "$PROJECT_PATH" -- "$INPUT_PATH" --output "$OUTPUT_PATH"
