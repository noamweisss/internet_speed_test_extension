#!/usr/bin/env bash
# Claude Code PreToolUse hook for Bash. Reads the tool call as JSON on stdin and blocks (exit 2)
# commands that would bypass or weaken the project's guards. Applies to the main agent and subagents.
set -u
CMD="$(jq -r '.tool_input.command // empty')"
[ -z "$CMD" ] && exit 0
deny() { echo "BLOCKED by scripts/hooks/guard-bash.sh ($1): $2. See AGENTS.md." >&2; exit 2; }

echo "$CMD" | grep -qE 'git (commit|push)[^|;&]*\s(-n|--no-verify)\b' && deny G1 "bypassing git hooks is not allowed"
echo "$CMD" | grep -qE 'git push.*(--force|-f\b|--force-with-lease)' && deny G2 "force pushes are not allowed"
echo "$CMD" | grep -qE 'git push.*\b(main|master)\b' && deny G3 "pushing to main is not allowed; open a pull request"
echo "$CMD" | grep -qE 'core\.hooksPath|hooksPath' && ! echo "$CMD" | grep -qE 'scripts/setup\.sh' && deny G4 "changing core.hooksPath is not allowed"
echo "$CMD" | grep -qE 'git (checkout|switch)\s+(main|master)\b' && deny G5 "do not work on main; stay on the feature branch"
echo "$CMD" | grep -qE 'git (reset --hard|clean -[a-z]*f|branch -D|rebase)' && deny G6 "destructive git operations need a human; explain what you need instead"
echo "$CMD" | grep -qE 'rm -rf?\s+(/|~|\$HOME|\.git\b|\.githooks|\.claude|scripts)' && deny G7 "deleting repo infrastructure is not allowed"
echo "$CMD" | grep -qE 'git commit --amend' && deny G8 "amending commits is not allowed; make a new commit"
exit 0
