#!/usr/bin/env bash
# Grep gate (epic-129 test-scenarios TS-129-48). Each row is: id <TAB> extended regex <TAB> pathspecs.
# A row passes when `git grep -nE` finds 0 lines. Add a row per slice as the G-rows are switched on.
set -uo pipefail
cd "$(git rev-parse --show-toplevel)"

ROWS=(
  $'G-17\t\\b(TransformPoint|TransformVector|Inverse|Multiply|Lerp|Distance|FromSurface)\\s*[<(]|operator\\s*[-+*]\tSources/ModelingEvolution.Drawing/Pose3.cs'
  $'G-18\tPose3(<[^>]*>)?\\.(Lerp|Distance|FromSurface)\\b\tSources/'
)

failed=0
for row in "${ROWS[@]}"; do
  IFS=$'\t' read -r id pattern paths <<<"$row"
  # shellcheck disable=SC2086  # pathspecs are space-separated on purpose
  hits=$(git grep -nE "$pattern" -- $paths)
  count=$(printf '%s' "$hits" | grep -c . || true)
  echo "$id: $count line(s)  [git grep -nE '$pattern' -- $paths]"
  if [ "$count" -ne 0 ]; then
    printf '%s\n' "$hits"
    echo "::error::grep gate $id found $count line(s)"
    failed=1
  fi
done
exit $failed
