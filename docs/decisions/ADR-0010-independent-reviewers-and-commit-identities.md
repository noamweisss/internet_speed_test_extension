# ADR-0010: Independent review agents and commit identities

Status: accepted · Date: 2026-09-28

## Context
CodeRabbit is the only reviewer the repository configures, and on the free plan it reviews this repository only
on request, about once per hour. Codex already reviews every pull request through the owner's ChatGPT plan
(installed as a GitHub App by the owner, configured outside the repository). The owner also pays for Claude and
wants it as a third, independent reviewer, without waiting hours per pull request.

The owner also needs to tell apart, in the history and on a pull request, what they did themselves, what a
building agent did on their behalf, and what a reviewing agent said.

## Decision
1. `.github/workflows/review-claude.yml` runs `anthropics/claude-code-action` on every pull request. It
   authenticates with the owner's Claude subscription (`CLAUDE_CODE_OAUTH_TOKEN` repository secret, generated
   by `claude setup-token`) and has no shell: its tools are Read, Glob, Grep, and Write, and its settings deny
   reading `/proc` and `~/.claude`. A trusted step writes the diff, the PR metadata, and the reviewer's own
   earlier comments to files; the reviewer writes `review.md` outside the checkout; another trusted step posts
   that fixed file to the fixed pull request after checking it does not contain the token. Codex's second and
   third reviews of PR #16 showed why: with `gh pr comment` allowed, a prompt injection could make the
   reviewer post `/proc/self/environ`; with any `gh` command allowed, shell expansion such as
   `"${CLAUDE_CODE_OAUTH_TOKEN:0:12}"` inside it prints token fragments into the public workflow log, past
   both GitHub's secret masking and a verbatim grep. It never pushes. Until the secret exists the job does
   nothing.
   The event is `pull_request_target`: the workflow file, the prompt, and the `.claude/` settings the reviewer
   loads always come from `main`; the pull request's files are checked out into a side directory (`pr-head`) as
   data. Codex's review of PR #16 showed why a plain `pull_request` trigger is not enough: a PR from a repository
   branch could edit this workflow and the edited copy would run with the token, so the PR could skip its own
   review or read the credential. With `pull_request_target` a PR controls only text the reviewer reads.
2. Codex is told the same rules through `AGENTS.md` §5, which it reads on every review.
3. Each reviewer is independent by construction: a fresh runner or a vendor-hosted session with the diff and
   the prompt, no memory of the building agent's session, no reading of the other reviewers' threads.
4. Reviews are comments, never verdicts. CodeRabbit posts comments instead of "Request changes"
   (`request_changes_workflow: false`), the Claude prompt forbids verdicts, Codex only comments. Merging is the
   owner's decision, and an agent merges only on an explicit, reasoned request for that PR (`AGENTS.md` §2).
   A thread closes when the reviewer that opened it is satisfied: CodeRabbit resolves its own; for Codex and
   Claude, which never reply in threads, the building agent resolves only after that reviewer reviewed the
   fixed commit and did not repeat the finding, and names that review. Both are asked to open a re-review with
   Fixed / Not fixed per earlier finding (`AGENTS.md` §5); the Claude workflow is handed its own earlier
   comments for that. The `main` ruleset keeps "require conversation resolution" on, so this is the merge gate.
5. Commit identity: a human commits as themselves. A building agent commits with author name `Claude Code`
   (set by the `env` block in `.claude/settings.json`) while the committer stays the human whose credentials
   push. Reviewing agents never commit. Every identity uses the owner's GitHub noreply address.

## Alternatives rejected
- Claude's managed Code Review product: Team and Enterprise plans only, billed per review.
- Gemini Code Assist on GitHub: the app is offered to Google Cloud customers (a project with a billing
  account), not through a consumer Gemini plan. Two config files away if the owner ever sets that up.
- A separate GitHub account for the agent: another login and token to protect, for a distinction the author
  field already expresses.
- Rewriting history to change past commit emails: forbidden by the project's own rules and pointless for an
  address that is already indexed.

## Consequences
- Reviews land within minutes of a push. Three reviewers answer the same five questions; agreement between
  two different models is the signal the owner looks for (`docs/REVIEW-PROMPT.md`).
- The workflow, `.claude/settings.json`, and `AGENTS.md` are guard files: changes need the `Guard-Change`
  trailer and a "Safety impact" note (R9, R13).
- Each Claude review spends the owner's subscription quota, not API credit. Drafts wait until ready and fork
  pull requests are skipped; Dependabot pull requests are reviewed.
- Text in a pull request reaches the reviewer as data. A prompt injection there can at most make the review
  wrong: the reviewer has no shell and no network, only file reads and one file write, the posting step is
  fixed, and the posted body is checked for the token first.
- The comment is posted by `github-actions[bot]` under the heading "Independent review (Claude)", not by
  `claude[bot]`: the reviewer no longer holds a posting tool.
- The Claude GitHub App gets the permission set of the app, not only what the workflow uses; the workflow's own
  token is limited to `contents: read`, `pull-requests: write`, `issues: read`, `id-token: write`.
