# ADR-0014: The Claude review workflow uses its own token, not the Claude GitHub App

Status: superseded by ADR-0018 on 2026-09-30 (the workflow is removed) · Date: 2026-09-29 · Superseded the token
and permission points of ADR-0012

## Context
ADR-0012 set up `.github/workflows/review-claude.yml` with `anthropics/claude-code-action`, which by default
exchanges the runner's OIDC token for a Claude GitHub App token. So the owner installed the Claude GitHub App, and
the job asked for `id-token: write` (ADR-0012, Consequences: "The Claude GitHub App gets the permission set of the
app ... `id-token: write`").

The first run that got past parsing (PR #19, 2026-09-29) failed in that exchange: "App token exchange failed: 401
Unauthorized - Invalid OIDC token". The exchange endpoint rejects runs whose event is `pull_request_target`
(anthropics/claude-code-action issue 713, open since 2025-12-02). The workflow uses that event on purpose
(ADR-0012: the workflow, the prompt and `.claude/` settings come from `main`), and `pull_request` is not an option,
because the `claude-review` environment secret is restricted to `main`.

## Decision
The action step passes `github_token: ${{ github.token }}`. At the pinned action commit (756cc22e),
`setupGitHubToken()` in `src/github/token.ts` returns that input before requesting any OIDC token, so the exchange
never happens. The job drops `id-token: write`. The Claude GitHub App is no longer needed; `docs/REVIEW-PROMPT.md`
no longer asks for it.

## Consequences
- The job's token is limited to `contents: read`, `pull-requests: write`, `issues: read`. The app's permission set
  is no longer in play.
- The action process gets nothing new: `action.yml` already passed the same token to it as
  `DEFAULT_WORKFLOW_TOKEN`. The reviewer's tools are unchanged (Read, Glob, Write to one file; no shell, no Grep),
  so it cannot use the token; the trusted posting step already used it.
- If the action is re-pinned, check that `github_token` still skips the exchange.
- Every `docs/SAFETY-CONTRACT.md` §3 answer stays "No": one permission fewer, no new secret.
