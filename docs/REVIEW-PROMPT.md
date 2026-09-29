# Review prompt for any reviewer agent

Paste the block below into any code-review agent (Codex, Gemini, Claude, Copilot, a human) after replacing
`<PR>`. It gives every reviewer the same job, so their answers can be compared. The reviewer must look at the
code, not at other reviewers' threads or the author's replies; independence is the point.

```
You are reviewing pull request <PR> in the GitHub repository noamweisss/internet_speed_test_extension.
Review the diff between the base branch (main) and the PR's head. Read docs/SAFETY-CONTRACT.md,
docs/SECURITY.md, and the "Code Review Rules" section of AGENTS.md first. Do not read other reviewers' comments
or the author's replies before forming your own findings. Do not change any code.

Part 1. Answer these five questions with Yes/No and a file:line for every Yes:
1. Can the extension reach any host other than speed.cloudflare.com?
2. Does it read, write, or delete files, registry keys, or processes?
3. Does it store, log, or send anything about the user (IP, ISP, location, results)? (ADR-0007 accepts the
   metadata every HTTPS request carries; anything beyond that is a Yes.)
4. Does it add a dependency, a Windows capability, or weaken a guard (.githooks, .claude, scripts, workflows)?
5. Is any network input used without bounds on size, time, or format?
A Yes without a linked ADR in docs/decisions/ is a Blocker.

Part 2. List correctness, stability, security, and maintainability findings. Maintainability means the "Design
and maintainability" rules in AGENTS.md (wrong layer, missing test, duplication, speculative structure, hot-path
work, waste on the measurement path, state outside the session, poor names); each of those is Major. For each
finding: severity (Blocker, Major, Minor, Nit), file:line, what goes wrong and under which input (or what gets
harder to change, and the smaller alternative), and the smallest fix. Verify every finding against the actual
code before reporting it. Style remarks are Nits.

Part 3. Documentation and instructions, as a separate list, never mixed with Part 2: apply the "Documentation
and instructions" rules in AGENTS.md (a document that contradicts the code, another document, or an ADR; an
instruction an agent or human could read two ways or that names something that does not exist; a behaviour or
guard change without its docs, changelog, or ADR; a hand-off note or plan status that does not match the change).
Severity Major when it could send an agent or a human the wrong way, Minor otherwise, with file:line and the fix.

Part 4. One-line verdict: "No blocking findings" or "Blocking findings: <count>".

Post the result as a review on the pull request (with the gh CLI: gh pr review <PR> --comment --body-file
<file>), or return it as text for a human to post. Do not approve or request changes on behalf of any other
reviewer.
```

## Who reviews, and when

| Reviewer | Identity on GitHub | Trigger | Where its rules live |
|----------|-------------------|---------|----------------------|
| Codex (owner's ChatGPT plan) | `chatgpt-codex-connector` | automatic on every PR; `@codex review` to repeat (when: `AGENTS.md` §4) | `AGENTS.md` "Code Review Rules" |
| Claude (owner's Claude plan) | `github-actions[bot]`, comment headed "Independent review (Claude)" | automatic on open, push, ready-for-review; re-run the workflow to repeat (when: `AGENTS.md` §4) | `.github/workflows/review-claude.yml` (same questions, input as files) |

The Claude workflow reviews pull requests into `main` only (its token lives in an environment restricted to
`main`). Neither reviewer shares context with the agent that wrote the change or with the other (ADR-0012). Codex posts P0 and
P1 findings by default, as inline comments, plus the P2 findings the "Code Review Rules" in `AGENTS.md` ask for
(documentation); anything else below P1 needs an explicit rule there. The Claude
workflow and its settings always come from `main` (`pull_request_target`); the PR's files are checked out into a
side directory as data, so a PR cannot change its own review or reach the token. The reviewer has no shell:
a trusted step writes the diff, the PR metadata, and the reviewer's own earlier comments to files, the reviewer
reads and writes files only, and another trusted step posts `review.md` after checking it does not contain the
token. Drafts wait until ready; fork PRs get no Claude review (their authors have no write access here).
A Dependabot PR is reviewed when GitHub grants the `claude-review` environment to its run; if such a PR shows
the job's "not set" line, it did not, and that PR is left to Codex (step 5 below). A review is also re-run
when the PR body is edited, since the "Safety impact" section lives there.

### Setting up the Claude review workflow (owner, once)

1. No GitHub App is needed. The workflow hands the action its own token (`github_token`), so the exchange of the
   runner's OIDC token for a Claude GitHub App token never happens; that exchange rejects `pull_request_target`
   runs (anthropics/claude-code-action issue 713), the event this workflow uses. An installed app is unused (ADR-0014).
2. On a machine with Claude Code logged in to the subscription, run `claude setup-token` and copy the token.
3. Repository Settings → Environments → New environment, name `claude-review`. Under "Deployment branches and
   tags" choose "Selected branches and tags" and add `main`. Then, in that environment, "Add environment
   secret": name `CLAUDE_CODE_OAUTH_TOKEN`, value the token. Never paste the token anywhere else, and do not
   store it as a repository secret: a repository secret is readable by a workflow on any branch, an
   environment secret restricted to `main` is not.
4. Push to any open PR into `main`, or re-run the "Review (Claude)" workflow. A comment headed
   "Independent review (Claude)" appears on the PR within a few minutes, posted by `github-actions[bot]`.
5. Optional, for Dependabot PRs: if their runs log "CLAUDE_CODE_OAUTH_TOKEN is not set", GitHub is withholding
   the environment from that run; leave those PRs to Codex.

To pause it, delete the environment secret: the job then logs "not set" and exits green. To remove it, delete the workflow
file and the secret.

## Reading the results (owner)

- Two different models answering the five questions the same way is the signal to look for. Their agreement
  means "no known problem", not "correct".
- Hand Blockers and Majors to the building agent: "address the review by <reviewer> on PR <PR>". Nits are optional.
- When to stop (`AGENTS.md` §4, ADR-0016): the first review, one fix push, one re-review. That ends the loop,
  unless a round reports a Blocker or Major in code or a safety finding (a Yes to one of the five questions, or
  a weakened guard). Documentation findings after the fix round are fixed if cheap and answered once; the
  building agent does not request another review for them and reports the PR as ready; you merge. A reviewer
  that reads prose will always find a sentence that lags a change; that is not a reason for a round. Asking the
  reviewers to "agree" means no open Blocker or Major, not a round with zero comments. Threads left open after
  the stop are yours to resolve or dismiss.
- Rule changes come alone: a PR that changes a rule or guard file changes nothing else except the session log,
  the plan, the changelog and ADRs (R14; the "Safety impact declared" CI check fails and names the files to
  move). Your merges of `main` into a branch and Dependabot PRs pass it.
- How a reviewer confirms a fix: neither replies in threads. The one re-review after the fix push does it
  (`@codex review`, and the push itself for Claude). The Claude workflow opens its re-review with Fixed /
  Not fixed per earlier finding (it gets its own earlier comments as a file); Codex simply does not repeat what
  is fixed.
  A review of the fixed commit that does not repeat the finding is agreement, and then the building agent
  resolves the thread, naming that review (`AGENTS.md` §2).
- Every reviewer posts comments, never "Request changes" (the Claude prompt forbids it; Codex only
  comments). Nothing an agent posts blocks a merge: the merge is your
  decision, taken on a green CI and on the findings you chose to have fixed. Keep "Require conversation
  resolution before merging" on in the `main` ruleset: with the resolution rule above, an open thread means
  "the reviewer that found this has not yet seen it fixed", which is the one gate worth keeping.
