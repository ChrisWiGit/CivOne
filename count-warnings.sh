#!/usr/bin/env bash
set -o pipefail

export LANG=C.UTF-8
export LC_ALL=C.UTF-8

BUILD_LOG="build.log"
OUTPUT_FILE="warning-summary.txt"
MOST_WARNINGS_FILE="warning-most-summary.txt"
HISTORY_FILE="warning-history.csv"

echo "Running dotnet build..."

rm -f "$BUILD_LOG"

dotnet build \
  --no-incremental \
  -fl \
  "-flp:logfile=$BUILD_LOG;encoding=UTF-8;verbosity=normal"

BUILD_EXIT_CODE=$?

if [ "$BUILD_EXIT_CODE" -ne 0 ]; then
  echo "WARNING: Build returned exit code $BUILD_EXIT_CODE" >&2
fi

if [ ! -f "$BUILD_LOG" ]; then
  echo "Build log was not created: $BUILD_LOG" >&2
  exit 1
fi

echo "Analyzing warnings..."

# Normalize optional MSBuild node prefix like "5>", deduplicate warning lines
WARNING_LINES="$(grep -E ':\s*warning\s+[A-Z]+[0-9]+\s*:' "$BUILD_LOG" \
  | sed -E 's/^[[:space:]]*[0-9]+>[[:space:]]*//' \
  | sed -E 's/^[[:space:]]*//' \
  | sort -u)"

TOTAL_WARNINGS=0

if [ -n "$WARNING_LINES" ]; then
  TOTAL_WARNINGS="$(printf '%s\n' "$WARNING_LINES" | wc -l | tr -d ' ')"
fi

# Summary grouped by warning code + description
RESULT_TEXT="$(
  if [ -n "$WARNING_LINES" ]; then
    printf '%s\n' "$WARNING_LINES" \
      | sed -E 's/.*:[[:space:]]*warning[[:space:]]+([A-Z]+[0-9]+)[[:space:]]*:[[:space:]]*(.*)([[:space:]]+\[[^]]+\])?$/\1\t\2/' \
      | awk -F '\t' '
          {
            key = $1 "\t" $2
            count[key]++
            code[key] = $1
            desc[key] = $2
          }
          END {
            printf "%-8s %-12s %s\n", "Count", "Warning", "Description"
            printf "%-8s %-12s %s\n", "-----", "-------", "-----------"

            for (k in count) {
              printf "%08d\t%d\t%s\t%s\n", count[k], count[k], code[k], desc[k]
            }
          }
        ' \
      | sort -r \
      | awk -F '\t' '
          NR <= 2 { print; next }
          {
            printf "%-8s %-12s %s\n", $2, $3, $4
          }
        '
  else
    echo "No warnings found."
  fi
)"

{
  echo "Warning Analysis"
  echo "================"
  echo
  echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
  echo
  echo "$RESULT_TEXT"
  echo
  echo "Total warnings: $TOTAL_WARNINGS"
} > "$OUTPUT_FILE"

# Files with most warnings
if [ -n "$WARNING_LINES" ]; then
  MOST_WARNINGS="$(
    printf '%s\n' "$WARNING_LINES" \
      | sed -nE 's/^(.*\.cs)\([0-9]+,[0-9]+\):[[:space:]]*warning[[:space:]]+[A-Z]+[0-9]+[[:space:]]*:.*$/\1/p' \
      | sort \
      | uniq -c \
      | sort -k1,1nr -k2 \
      | awk '{ count=$1; $1=""; sub(/^ /, ""); print $0 ": " count }'
  )"

  if [ -n "$MOST_WARNINGS" ]; then
    printf '%s\n' "$MOST_WARNINGS" > "$MOST_WARNINGS_FILE"
  else
    echo "No warning files found." > "$MOST_WARNINGS_FILE"
  fi
else
  echo "No warning files found." > "$MOST_WARNINGS_FILE"
fi

# History tracking
LAST_WARNING_COUNT=""

if [ -f "$HISTORY_FILE" ]; then
  LAST_WARNING_COUNT="$(tail -n 1 "$HISTORY_FILE" | awk -F ',' '{print $2}')"
fi

if [ -z "$LAST_WARNING_COUNT" ] || [ "$LAST_WARNING_COUNT" = "WarningCount" ]; then
  DELTA=0
else
  DELTA=$((TOTAL_WARNINGS - LAST_WARNING_COUNT))
fi

if [ ! -f "$HISTORY_FILE" ]; then
  echo "Date,WarningCount,Delta" > "$HISTORY_FILE"
fi

echo "$(date '+%Y-%m-%d %H:%M:%S'),$TOTAL_WARNINGS,$DELTA" >> "$HISTORY_FILE"

echo
echo "====================================="
echo "Total warnings : $TOTAL_WARNINGS"
echo "Delta          : $DELTA"
echo "Summary        : $OUTPUT_FILE"
echo "Most warnings  : $MOST_WARNINGS_FILE"
echo "History        : $HISTORY_FILE"
echo "====================================="