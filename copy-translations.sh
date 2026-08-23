#!/usr/bin/env sh
# Copies generated translation .txt files from the repository translation folder
# into the active CivOne translations directory.
# Excludes all.txt and overwrites existing target files.
# Normalizes target file names to lowercase.

set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
SOURCE_DIR="$SCRIPT_DIR/translation"
TARGET_DIR="${1:-${XDG_DATA_HOME:-$HOME/.local/share}/CivOne/translations}"

if [ ! -d "$SOURCE_DIR" ]; then
	echo "Source directory not found: $SOURCE_DIR" >&2
	exit 1
fi

mkdir -p "$TARGET_DIR"

count=0
for file in "$SOURCE_DIR"/*.txt; do
	[ -e "$file" ] || continue
	name=$(basename "$file")
	lower_name=$(printf '%s' "$name" | tr '[:upper:]' '[:lower:]')
	if [ "$lower_name" = "all.txt" ]; then
		continue
	fi
	cp "$file" "$TARGET_DIR/$lower_name"
	echo "Copied $name -> $lower_name"
	count=$((count + 1))
done

echo "Done: $count file(s) copied to $TARGET_DIR"