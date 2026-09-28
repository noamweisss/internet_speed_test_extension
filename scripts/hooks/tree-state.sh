#!/usr/bin/env bash
# Prints the uncommitted state of the working tree and the index: one line per changed or untracked file with
# its status and working-tree content hash, then one line per staged path with its index mode and blob id.
# session-start.sh saves this at session start and stop-check.sh compares against it, so edits the owner made
# before the session started do not count as changes made by the session. The index lines catch a change that
# is only staged (git add of a new hunk in a file that was already partly staged).
set -u
cd "$(git rev-parse --show-toplevel)" || exit 1
git status --porcelain=v1 -z --no-renames --untracked-files=all | while IFS= read -r -d '' entry; do
  printf '%s %s\n' "$entry" "$(git hash-object -- "${entry:3}" 2>/dev/null || echo missing)"
done
git diff-index --cached --no-renames HEAD 2>/dev/null
