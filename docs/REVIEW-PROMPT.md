# Review runbook: CodeRabbit, and how the owner steers it

One reviewer, CodeRabbit, reviews pull requests on request (ADR-0018). Its brief is the "Code Review Rules"
section of `AGENTS.md`, which it reads on its own, plus the path instructions in `.coderabbit.yaml` (the five
safety questions and the guard rule). The building agent's steps are `AGENTS.md` §4 step 6. This file is the
owner's side: what limits apply, where the controls are, how to read a result, and when to stop.

## The three limits (verified 2026-09-29 against CodeRabbit's plans page and this repository's history)

- **Trigger.** A public repository under 10 stars gets no automatic review. Someone comments `@coderabbitai
  review`, or ticks "Trigger review" in the bot's status comment. `.coderabbit.yaml` also turns automatic
  reviews off, so this stays true above 10 stars.
- **Review slot.** One review per hour on this repository (the bot prints "Your plan provides up to 1 included
  review per hour" after each review). The slot is shared by every open pull request; a trigger inside the
  hour is refused with "Review rate limited" and costs nothing. `@coderabbitai rate limit` shows the slot
  without spending it.
- **Chat.** Replies in review threads have their own allowance (25 per hour). They never touch the slot.

## The four controls

1. `.coderabbit.yaml` at the repository root. Wins over everything. `profile` (`chill` for fewer findings,
   `assertive` for more), `auto_review.enabled`, `review_status` (the "review skipped" comment on every PR),
   `path_instructions` (the reviewer's checklist per path), `chat.auto_reply`. A change to it is a rule change
   and travels alone (R14).
2. Comments on the pull request: `@coderabbitai review` (new commits only), `full review` (everything again),
   `pause` and `resume`, `resolve` (closes every CodeRabbit thread, use only when you agree), `rate limit`,
   `configuration` (prints the settings it resolved, the way to check that the file is read), `help`.
3. The "Code Review Rules" section of `AGENTS.md`: the rules themselves. Also a rule file (R14).
4. Learnings, in the CodeRabbit app (Learnings page). The bot stores one when a thread reply teaches it
   something, and applies it to later reviews of similar code. Delete a learning when the code it names is gone.

## Reading a result

- Findings arrive 5 to 15 minutes after the trigger, as review threads with a severity label (the bot's
  Critical, Major, Minor, Trivial map to `AGENTS.md`'s Blocker, Major, Minor, Nit). The five safety answers are
  in its summary. The "Summary by CodeRabbit" it writes into the PR description is a walkthrough, not a verdict.
- The bot also posts Approve or Request changes. Neither gates a merge: the `main` ruleset requires resolved
  threads, not approvals. The merge is your decision, on a green CI and the findings you chose to have fixed.
- How a fix is confirmed: the building agent pushes, then replies in the thread naming the commit. The bot
  checks the head, answers "fixed" or "still an issue", and resolves what it agrees with. A thread it leaves
  open after that reply is yours to resolve or dismiss. Order matters: the bot believes the reply, so a reply
  written before the push ("not yet pushed") leaves the thread open for good.
- When to stop (`AGENTS.md` §4 step 6, ADR-0016): the first review, one fix push, one re-review once the PR is
  otherwise ready to merge (the hour passing is not the trigger, readiness is). Another round only for a
  Blocker or Major in code or a safety finding, or when you hand an open thread back. A reviewer that reads prose will
  always find a sentence that lags a change; that is not a reason for a round. Asking the reviewer to "agree"
  means no open Blocker or Major, not a round with zero comments.
- Rule changes come alone: a PR that changes a rule or guard file changes nothing else except the session log,
  the plan, the changelog and ADRs (R14; the "Safety impact declared" CI check fails and names the files to move).

## A prompt for a second opinion

When you want another model to review a pull request by hand (Gemini, Copilot, a Claude chat), paste this after
replacing `<PR>`; it gives the outside reviewer the same job as CodeRabbit, so the answers can be compared:

```
You are reviewing pull request <PR> in the GitHub repository noamweisss/internet_speed_test_extension.
Review the diff between the base branch (main) and the PR's head. Read docs/SAFETY-CONTRACT.md,
docs/SECURITY.md, and the "Code Review Rules" section of AGENTS.md first, and apply every rule there. Do not
read other reviewers' comments or the author's replies before forming your own findings. Do not change any code.
Answer the five safety questions of docs/SAFETY-CONTRACT.md §3 on one line, then list code findings and,
separately, documentation findings, one bullet each: severity (Blocker, Major, Minor, Nit), file:line, what
goes wrong and under which input, the smallest fix. End with "No blocking findings" or "Blocking findings: N".
```
