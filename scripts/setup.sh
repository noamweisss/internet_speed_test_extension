#!/usr/bin/env bash
# One-time (idempotent) repo setup: point git at the versioned hooks. Run by humans once and by the
# Claude Code SessionStart hook on every session. Safe to re-run.
set -eu
ROOT="$(git rev-parse --show-toplevel)"
cd "$ROOT"
git config core.hooksPath .githooks
chmod +x .githooks/* scripts/*.sh scripts/hooks/*.sh 2>/dev/null || true
echo "git hooks: $(git config core.hooksPath)"
