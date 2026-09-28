#!/usr/bin/env bash
# R13: a pull request that touches a safety-sensitive file must explain it in its "Safety impact" section.
# Usage: scripts/safety-impact.sh <base-ref> <head-ref>   with the PR body in $PR_BODY.
# Sensitive files are listed in docs/SAFETY-CONTRACT.md §2; keep the two in sync.
set -u
BASE="$1"; HEAD="$2"
CHANGED="$(git diff --name-only "$BASE" "$HEAD")"
SENSITIVE="$(printf '%s\n' "$CHANGED" | grep -E '^(scripts/allowed-hosts\.txt|internet_speed_test_extension/Package\.appxmanifest|Directory\.Packages\.props|.*\.csproj|\.githooks/|\.claude/|scripts/|\.github/workflows/|internet_speed_test_extension/Program\.cs|install/|AGENTS\.md|CLAUDE\.md|docs/SECURITY\.md|docs/SAFETY-CONTRACT\.md)' || true)"
if [ -z "$SENSITIVE" ]; then
  echo "safety-impact: no sensitive files changed."; exit 0
fi
echo "safety-impact: sensitive files changed:"; printf '%s\n' "$SENSITIVE" | sed 's/^/  /'
if ! printf '%s\n' "${PR_BODY:-}" | grep -q '^## Safety impact'; then
  echo "RULE R13: the PR body has no '## Safety impact' section (see .github/pull_request_template.md)." >&2; exit 1
fi
# The section's real content: lines between the heading and the next heading, minus HTML comments,
# blank lines, and the template's "None" line. It must not be empty.
EXPLANATION="$(printf '%s\n' "${PR_BODY:-}" | tr -d '\r' \
  | awk '/^## Safety impact/{f=1; next} /^## /{f=0} f' \
  | sed -E 's/<!--.*-->//g' \
  | grep -vE '^[[:space:]]*$' \
  | grep -vE '^None: no change to what the extension can do' || true)"
if [ -z "$EXPLANATION" ]; then
  echo "RULE R13: sensitive files changed but 'Safety impact' has no explanation (or still says 'None'). Say, in plain language, what the extension can now do that it could not before, and link the ADR." >&2; exit 1
fi
echo "safety-impact: explanation present."; exit 0
