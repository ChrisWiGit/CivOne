#!/usr/bin/env sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
PROJECT_PATH="$SCRIPT_DIR/civtranslate-interactive/civtranslate-interactive.csproj"

if [ "$#" -lt 1 ] || [ -z "$1" ]; then
	echo "Error: A translation file name is required (example: story_german or civ_german)." >&2
	exit 1
fi

TRANSLATION_INPUT="$1"

case "$TRANSLATION_INPUT" in
	*"/"*|*"\\"*)
		echo "Error: Please pass only a file name from the translation folder, not a path." >&2
		exit 1
		;;
esac

BASE_NAME="${TRANSLATION_INPUT%.txt}"
TRANSLATION_FILE="$BASE_NAME.txt"
TRANSLATION_PATH="$SCRIPT_DIR/translation/$TRANSLATION_FILE"

echo "Step 1/3: Check whether translation/all.txt must be generated."
case "$BASE_NAME" in
	civ*)
		echo "Detected civ* file: $TRANSLATION_FILE"
		echo "Running translate.sh to generate translation/all.txt ..."
		"$SCRIPT_DIR/translate.sh"
		echo "translation/all.txt has been written."
		echo "You can now copy or adjust a civ_xy.txt file in the translation folder."
		printf "Press Enter to continue with interactive translation"
		read -r _
		;;
	*)
		echo "Skipped. The provided file name does not start with civ."
		;;
esac

echo "Step 2/3: Run translate-interactive roundtrip."
if [ ! -f "$TRANSLATION_PATH" ]; then
	echo "Error: Translation file not found: $TRANSLATION_PATH" >&2
	exit 1
fi

if dotnet run --project "$PROJECT_PATH" -- "$TRANSLATION_PATH"; then
	:
else
	step2_exit=$?
	echo "Step 2/3 failed with exit code $step2_exit."
	echo "Step 3/3 is skipped."
	exit "$step2_exit"
fi

echo "Step 3/3: Copy translations to target directory."
"$SCRIPT_DIR/copy-translations.sh"

echo "Done."
