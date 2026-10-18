#!/usr/bin/env sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
PROJECT_PATH="$SCRIPT_DIR/civtext-hashgen/civtext-hashgen.csproj"
OUTPUT_PATH="$SCRIPT_DIR/src/IO/Text/OriginalTextLanguageValidationDefaultDefinitions.cs"

if [ "$#" -lt 1 ] || [ "$#" -gt 2 ]; then
	echo "Usage: ./civtext-hashgen.sh <civ-orig-directory> [--trace]" >&2
	exit 1
fi

CIV_ORIG_DIR="$1"
TRACE_ARG=""
if [ "$#" -eq 2 ]; then
	if [ "$2" != "--trace" ]; then
		echo "Error: Unknown option: $2" >&2
		exit 1
	fi
	TRACE_ARG="--trace"
fi

if [ ! -d "$CIV_ORIG_DIR" ]; then
	echo "Error: Directory not found: $CIV_ORIG_DIR" >&2
	exit 1
fi

echo "Building civtext-hashgen..."
dotnet build "$PROJECT_PATH"

echo "Generating hash definitions into: $OUTPUT_PATH"
if [ -n "$TRACE_ARG" ]; then
	dotnet run --project "$PROJECT_PATH" -- "$CIV_ORIG_DIR" "$TRACE_ARG" --output "$OUTPUT_PATH"
else
	dotnet run --project "$PROJECT_PATH" -- "$CIV_ORIG_DIR" --output "$OUTPUT_PATH"
fi

echo "Done."
