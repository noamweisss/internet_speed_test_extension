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

Part 3. One-line verdict: "No blocking findings" or "Blocking findings: <count>".

Post the result as a review on the pull request (with the gh CLI: gh pr review <PR> --comment --body-file
<file>), or return it as text for a human to post. Do not approve or request changes on behalf of any other
reviewer.
```

## Who reviews, and when

| Reviewer | Identity on GitHub | Trigger | Where its rules live |
|----------|-------------------|---------|----------------------|
| Codex (owner's ChatGPT plan) | `chatgpt-codex-connector` | automatic on every PR; `@codex review` to repeat | `AGENTS.md` "Code Review Rules" |
| Claude (owner's Claude plan) | `github-actions[bot]`, comment headed "Independent review (Claude)" | automatic on open, push, ready-for-review; re-run the workflow to repeat | `.github/workflows/review-claude.yml` (same questions, input as files) |

Neither shares context with the agent that wrote the change or with the other (ADR-0010). Codex posts only
P0 and P1 findings, as inline comments; ask for lower severities in `AGENTS.md` "Code Review Rules" if wanted. The Claude
workflow and its settings always come from `main` (`pull_request_target`); the PR's files are checked out into a
side directory as data, so a PR cannot change its own review or reach the token. The reviewer has no shell:
a trusted step writes the diff, the PR metadata, and the reviewer's own earlier comments to files, the reviewer
reads and writes files only, and another trusted step posts `review.md` after checking it does not contain the
token. Drafts wait until ready; fork PRs get no Claude review (their authors have no write access here);
Dependabot PRs are reviewed.

### Setting up the Claude review workflow (owner, once)

1. Install the Claude GitHub App on this repository: <https://github.com/apps/claude>. Choose "Only select
   repositories" and pick this one.
2. On a machine with Claude Code logged in to the subscription, run `claude setup-token` and copy the token.
3. Repository Settings → Secrets and variables → Actions → New repository secret. Name
   `CLAUDE_CODE_OAUTH_TOKEN`, value the token. Never paste the token anywhere else.
4. Push to any open PR, or re-run the "Review (Claude)" workflow. A comment headed "Independent review (Claude)"
   appears on the PR within a few minutes, posted by `github-actions[bot]`.

To pause it, delete the secret: the job then logs "not set" and exits green. To remove it, delete the workflow
file, the secret, and uninstall the app.

## Reading the results (owner)

- Two different models answering the five questions the same way is the signal to look for. Their agreement
  means "no known problem", not "correct".
- Hand Blockers and Majors to the building agent: "address the review by <reviewer> on PR <PR>". Nits are optional.
- How a reviewer confirms a fix: neither replies in threads. Request a fresh review after the fix
  (`@codex review`, or a push for Claude). The Claude workflow opens its re-review with Fixed / Not fixed per
  earlier finding (it gets its own earlier comments as a file); Codex simply does not repeat what is fixed.
  A review of the fixed commit that does not repeat the finding is agreement, and then the building agent
  resolves the thread, naming that review (`AGENTS.md` §2).
- Every reviewer posts comments, never "Request changes" (the Claude prompt forbids it; Codex only
  comments). Nothing an agent posts blocks a merge: the merge is your
  decision, taken on a green CI and on the findings you chose to have fixed. Keep "Require conversation
  resolution before merging" on in the `main` ruleset: with the resolution rule above, an open thread means
  "the reviewer that found this has not yet seen it fixed", which is the one gate worth keeping.
