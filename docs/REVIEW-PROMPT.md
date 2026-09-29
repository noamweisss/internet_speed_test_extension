# Review runbook: CodeRabbit, and how the owner steers it

CodeRabbit reviews pull requests on request (ADR-0018). Its brief is the "Code Review Rules" section of
`AGENTS.md`, which it reads on its own, plus the path instructions in `.coderabbit.yaml`. The building agent's
steps are `AGENTS.md` §4 step 6. This file is the owner's side: the limits, the controls, how to read a result,
when to stop.

## The three limits

Verified 2026-09-29 against CodeRabbit's plans page and this repository's history.

- Trigger. A public repository under 10 stars gets no automatic review. Someone comments `@coderabbitai review`
  or ticks "Trigger review" in the bot's status comment. `.coderabbit.yaml` also turns automatic reviews off,
  so this stays true above 10 stars.
- Review slot. One review per hour on this repository, shared by every open pull request. A trigger inside the
  hour is refused with "Review rate limited" and costs nothing. `@coderabbitai rate limit` shows the slot
  without spending it.
- Chat. Replies in review threads have their own allowance, 25 per hour. They never touch the slot.

## The four controls

1. `.coderabbit.yaml` at the repository root. Wins over everything. `profile` (`chill` for fewer findings,
   `assertive` for more), `auto_review.enabled`, `review_status` (the "review skipped" comment on every PR),
   `path_instructions` (the checklist per path), `chat.auto_reply`. A rule file: a change to it travels alone
   (R14).
2. Comments on the pull request: `@coderabbitai review` (new commits only), `full review` (everything again),
   `pause` and `resume`, `resolve` (closes every CodeRabbit thread; use only when you agree), `rate limit`,
   `configuration` (prints the settings it resolved, which proves the file is read), `help`.
3. The "Code Review Rules" section of `AGENTS.md`. Also a rule file.
4. Learnings, in the CodeRabbit app. The bot stores one when a thread reply teaches it something and applies it
   to later reviews of similar code. Delete a learning when the code it names is gone.

## Reading a result

- Findings arrive 5 to 15 minutes after the trigger, as review threads. The bot's Critical, Major, Minor and
  Trivial are `AGENTS.md`'s Blocker, Major, Minor and Nit. The five safety answers are in its summary. The
  "Summary by CodeRabbit" it writes into the PR description is a walkthrough, not a verdict.
- The bot also posts Approve or Request changes. Neither gates a merge: the `main` ruleset requires resolved
  threads, not approvals. You merge on a green CI and the findings you chose to have fixed.
- A fix is confirmed in the thread. The building agent pushes, then replies naming the commit; the bot checks
  the head, answers fixed or still an issue, and resolves what it agrees with. A thread it leaves open is
  yours: resolve, dismiss, or hand it back to the agent. Order matters: the bot believes the reply, so "not yet
  pushed" leaves the thread open for good.
- When to stop (`AGENTS.md` §4 step 6, ADR-0016): the first review, one fix push, one re-review once the PR is
  otherwise ready to merge. Another round only for a Blocker or Major in code, a safety finding, or a thread
  you hand back. A reviewer that reads prose always finds a sentence that lags a change; that is not a reason
  for a round. "Agree" means no open Blocker or Major, not a round with zero comments.
- Rule changes come alone (R14): a PR that changes a rule or guard file changes nothing else except the session
  log, the plan, the changelog and ADRs. The "Safety impact declared" CI check fails and names the files to
  move.

## A prompt for a second opinion

To have another model review a pull request by hand (Gemini, Copilot, a Claude chat), paste this with `<PR>`
replaced. It gives the outside reviewer the same job as CodeRabbit, so the answers can be compared.

```
You are reviewing pull request <PR> in the GitHub repository noamweisss/internet_speed_test_extension.
Review the diff between the base branch (main) and the PR's head. Read docs/SAFETY-CONTRACT.md,
docs/SECURITY.md, and the "Code Review Rules" section of AGENTS.md first, and apply every rule there. Do not
read other reviewers' comments or the author's replies before forming your own findings. Do not change any code.
Answer the five safety questions of docs/SAFETY-CONTRACT.md section 3 on one line, then list code findings and,
separately, documentation findings, one bullet each: severity (Blocker, Major, Minor, Nit), file:line, what
goes wrong and under which input, the smallest fix. End with "No blocking findings" or "Blocking findings: N".
```
