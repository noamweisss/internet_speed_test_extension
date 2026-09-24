#!/usr/bin/env bash
# Deterministic project checks. Used by .githooks/pre-commit (staged files) and CI (all files).
# Usage: scripts/check.sh staged | scripts/check.sh all
# Every rule prints "RULE <id>: <message>" on failure so a reader can trace which rule fired.
set -u
MODE="${1:-staged}"
ROOT="$(git rev-parse --show-toplevel)"
cd "$ROOT"
FAIL=0
fail() { echo "RULE $1: $2" >&2; FAIL=1; }

if [ "$MODE" = "staged" ]; then
  FILES="$(git diff --cached --name-only --diff-filter=ACMR)"
  show() { git show ":$1"; }
else
  FILES="$(git ls-files)"
  show() { cat "$1"; }
fi
[ -z "$FILES" ] && exit 0

# R1: never commit on the default branch (main). Work happens on feature branches.
if [ "$MODE" = "staged" ]; then
  BRANCH="$(git symbolic-ref --short HEAD 2>/dev/null || echo detached)"
  case "$BRANCH" in main|master) fail R1 "commits on '$BRANCH' are not allowed; create a feature branch (docs/CONVENTIONS.md)";; esac
fi

CODE_CHANGED=0
for f in $FILES; do
  # R2: forbidden file types (secrets, certificates, local env files).
  case "$f" in
    *.pfx|*.p12|*.snk|*.pem|*.key|*.cer|.env|.env.*|*id_rsa*) fail R2 "forbidden file type: $f";;
  esac
  # R3: no files over 1 MB (keeps the repo lean; binaries go through releases, not git).
  if [ "$MODE" = "staged" ]; then SIZE=$(git cat-file -s ":$f" 2>/dev/null || echo 0); else SIZE=$(stat -c %s "$f" 2>/dev/null || echo 0); fi
  [ "$SIZE" -gt 1048576 ] && fail R3 "file over 1 MB: $f"

  case "$f" in
    *.cs|*.csproj|*.props|*.json|*.yml|*.yaml|*.sh|*.md|*.txt|*.appxmanifest|*.sln)
      CONTENT="$(show "$f")"
      # R4: secret patterns.
      if printf '%s' "$CONTENT" | grep -nE -- '-----BEGIN [A-Z ]*PRIVATE KEY-----|ghp_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}|AKIA[0-9A-Z]{16}|sk-[A-Za-z0-9]{32,}' >/dev/null; then
        fail R4 "possible secret in $f"
      fi
      ;;
  esac

  case "$f" in
    *.cs)
      case "$f" in src/*|internet_speed_test_extension/*) CODE_CHANGED=1;; esac
      CONTENT="$(show "$f")"
      # R5: no process spawning, native interop, reflection loading, or TLS validation overrides in this project.
      #     Program.cs (the template's COM host) is the only file allowed to use the WinRT server APIs.
      if printf '%s' "$CONTENT" | grep -nE 'Process\.Start|System\.Diagnostics\.Process|DllImport|LibraryImport|\bunsafe\b|Assembly\.Load|Reflection\.Emit|DangerousAcceptAnyServerCertificateValidator|ServerCertificateCustomValidationCallback|CheckCertificateRevocationList\s*=\s*false' >/dev/null; then
        fail R5 "forbidden API usage in $f (docs/SECURITY.md, rule R5)"
      fi
      # R6: no plain-http URLs.
      if printf '%s' "$CONTENT" | grep -nE '"http://' >/dev/null; then fail R6 "plain http:// URL in $f"; fi
      # R7: every https host in C# must be in scripts/allowed-hosts.txt.
      for host in $(printf '%s' "$CONTENT" | grep -oE 'https://[A-Za-z0-9.-]+' | sed 's#https://##' | sort -u); do
        grep -qxF "$host" scripts/allowed-hosts.txt || fail R7 "host '$host' in $f is not in scripts/allowed-hosts.txt"
      done
      ;;
  esac
done

# R8: code changes must be accompanied by a CHANGELOG.md entry in the same commit.
if [ "$MODE" = "staged" ] && [ "$CODE_CHANGED" = 1 ]; then
  printf '%s\n' "$FILES" | grep -qx 'CHANGELOG.md' || fail R8 "code changed but CHANGELOG.md is not staged (docs/CONVENTIONS.md)"
fi

# R9: guard files (hooks, checks, CI, agent rules) may only change with a 'Guard-Change:' commit trailer. Checked in commit-msg.

# R10: the COM CLSID must be identical in the extension class and the manifest (template requirement).
if [ -f internet_speed_test_extension/Package.appxmanifest ]; then
  IDS="$(grep -ohE '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}' internet_speed_test_extension/Package.appxmanifest internet_speed_test_extension/*.cs 2>/dev/null | sort -u | wc -l)"
  [ "$IDS" != "1" ] && fail R10 "CLSID mismatch between Package.appxmanifest and the extension class"
fi

exit $FAIL
