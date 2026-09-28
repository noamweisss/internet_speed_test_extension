# ADR-0012: Independent review agents and commit identities

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
   by `claude setup-token`) and has no shell: its tools are Read, Glob, and Write for the single path
   `review.md` (no Grep: Codex's seventh review showed its ripgrep subprocess inherits the token and can search
   `/proc/self/environ` past a Read deny), and its settings deny reading `/proc` and `~/.claude` and writing the
   runner's per-step
   command files (Codex's fourth review: a blanket Write could put `BASH_ENV` into `GITHUB_ENV` and so run
   PR-controlled shell in the next trusted step). Symlinks are deleted from the PR checkout before the reviewer
   starts (Codex's fifth review: a committed link to `/proc/self/environ` would be read through a path the
   deny rules do not match). The reviewer's earlier comments handed to it are the last two, capped at 128 KB,
   taken from a bounded window of the PR's last 30 comments in one GraphQL request (Codex's sixth and seventh
   reviews: unbounded history could exhaust the model's context, and unbounded pagination the runner, on a
   long-lived PR). A trusted step writes the diff, the PR metadata, and the reviewer's own
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
2. Codex is told the same rules through `AGENTS.md` "Code Review Rules", which it reads on every review.
3. Each reviewer is independent by construction: a fresh runner or a vendor-hosted session with the diff and
   the prompt, no memory of the building agent's session, no reading of the other reviewers' threads.
4. Reviews are comments, never verdicts: the Claude prompt forbids them, Codex only comments. Merging is the
   owner's decision, and an agent merges only on an explicit, reasoned request for that PR (`AGENTS.md` §2).
   A thread closes when the reviewer that opened it is satisfied. Neither reviewer replies in threads, so the
   building agent resolves only after that reviewer reviewed the fixed commit and did not repeat the finding,
   and names that review. The Claude workflow opens its re-review with Fixed / Not fixed per earlier finding
   (it is handed its own earlier comments); Codex, which posts only P0 and P1 inline findings and ignored a
   request for such a list, signals agreement by not repeating. The `main` ruleset keeps "require
   conversation resolution" on, so this is the merge gate.
6. CodeRabbit is removed (`.coderabbit.yaml` deleted; the owner uninstalls the app). Two reviewers on plans
   the owner already pays for cover the five questions; a third, hourly, manually triggered one added a set of
   threads and a status check without adding a signal.
7. Codex reads its rules only under the exact heading `## Code Review Rules` in `AGENTS.md`, grouped by `###`
   headings. The section was first written as a numbered heading and was ignored; it now uses the exact
   heading and states the severity (P0 for an unanswered safety question or a secret-bearing workflow that
   executes PR-controlled input, P1 for weakened guards and correctness) so the rules survive Codex's
   P0-and-P1-only filter.
8. Reviewers guard maintainability with the same weight as safety. The owner wants the codebase to stay small,
   tested, and cheap to change; the "Design and maintainability" group in `AGENTS.md` lists the concrete
   habits to flag (wrong layer, missing test, duplication, speculative structure, hot-path work, waste on the
   measurement path, state outside the session, poor names), each as P1 / Major, with formatting at P3. The
   Claude prompt and `docs/REVIEW-PROMPT.md` point at that group so all reviewers apply one list.
9. Reviewers also flag documentation and instruction problems (contradictions with the code or between
   documents, instructions readable two ways or naming things that do not exist, changes without their docs
   or ADR, hand-off notes that do not match), always as a list separate from code findings, P1 when they could
   misdirect an agent or a human and P2 otherwise. The prompts carry this as their own part.
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
- Reviews land within minutes of a push. Both reviewers answer the same five questions; agreement between
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
