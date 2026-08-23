#!/usr/bin/env sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
PROJECT_PATH="$SCRIPT_DIR/civtranslate-interactive/civtranslate-interactive.csproj"

if [ "$#" -ne 2 ] || [ "$1" != "--language" ]; then
	echo "Usage: ./translate-interactive.sh --language <postfix>" >&2
	exit 1
fi

POSTFIX="$2"
if [ -z "$POSTFIX" ]; then
	echo "Error: --language requires a postfix value." >&2
	exit 1
fi

case "$POSTFIX" in
	*"/"*|*"\\"*)
		echo "Error: Please pass only a language postfix, not a path." >&2
		exit 1
		;;
esac

TRANSLATION_FILE="civ_$POSTFIX.txt"
TRANSLATION_PATH="$SCRIPT_DIR/translation/$TRANSLATION_FILE"

echo "Step 1/3: Check whether translation/all.txt must be generated."
echo "Detected language file: $TRANSLATION_FILE"
echo "Running translate.sh to generate translation/all.txt ..."
"$SCRIPT_DIR/translate.sh"
echo "translation/all.txt has been written."
echo "Ensure $TRANSLATION_FILE exists (for example by copying all.txt once)."
printf "Press Enter to continue with interactive translation"
read -r _

echo "Step 2/3: Run translate-interactive roundtrip."
if [ ! -f "$TRANSLATION_PATH" ]; then
	echo "Error: Language file not found: $TRANSLATION_PATH" >&2
	exit 1
fi

if dotnet run --project "$PROJECT_PATH" -- --language "$POSTFIX"; then
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
