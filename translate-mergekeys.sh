#!/usr/bin/env sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
PROJECT_PATH="$SCRIPT_DIR/civtranslate-mergekeys/civtranslate-mergekeys.csproj"
TRANSLATION_DIR="$SCRIPT_DIR/translation"

resolve_translation_path() {
	file_name="$1"

	if [ -z "$file_name" ]; then
		echo "Error: A translation file name is required." >&2
		exit 1
	fi

	case "$file_name" in
		*"/"*|*\*)
			printf '%s\n' "$file_name"
			return
			;;
	esac

	base_name=${file_name%.txt}
	if [ -z "$base_name" ]; then
		echo "Error: Invalid translation file name: $file_name" >&2
		exit 1
	fi

	printf '%s/%s.txt\n' "$TRANSLATION_DIR" "$base_name"
}

if [ "$#" -ne 2 ]; then
	echo "Usage: ./translate-mergekeys.sh <source-file> <target-file>" >&2
	exit 1
fi

SOURCE_PATH=$(resolve_translation_path "$1")
TARGET_PATH=$(resolve_translation_path "$2")

dotnet run --project "$PROJECT_PATH" -- "$SOURCE_PATH" "$TARGET_PATH"