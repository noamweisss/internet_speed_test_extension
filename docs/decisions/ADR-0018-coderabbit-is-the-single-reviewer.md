# ADR-0018: CodeRabbit is the single reviewer, on request

Status: accepted · Date: 2026-09-30 · Supersedes ADR-0012 points 1 to 4 and 6 to 9 and ADR-0014; ADR-0016's stop
rule stands, its point 3 (the Claude prompt) is moot

## Context
ADR-0012 replaced CodeRabbit with two reviewers: a Claude workflow on the owner's subscription and Codex through
the owner's ChatGPT plan. The workflow grew to 280 lines over 18 Codex rounds on PR #16, with an environment
secret, a `pull_request_target` trust boundary, and three documents describing it. The owner, who is not a
developer, could no longer say what reviewed a pull request, when, or under which rules, and asked for a review
stack they can read and steer themselves: one product, controlled by plain files in the repository.

What CodeRabbit does on this repository was verified on 2026-09-29 against its documentation and the history of
PRs #1, #7, #11, #15 and #16:
- A public repository under 10 stars gets no automatic review; a review is requested with `@coderabbitai review`.
- The open-source allowance is 1 review per hour on this repository (the bot prints it after every review),
  shared by every open pull request; a trigger inside the hour is refused and costs nothing.
- Thread replies use a separate allowance. A reply that names a pushed commit makes the bot check the head and
  resolve the thread; a reply that says the fix is not yet pushed leaves the thread open for good.
- Its findings were valid: every one on PRs #1 to #15 was fixed, including two bugs the other reviewers missed.
  The complaint behind ADR-0012 point 6 was cadence, not quality.

## Decision
1. CodeRabbit is the only reviewing agent. This pull request removes the Claude review workflow from the
   repository; after the merge the owner deletes the `claude-review` environment with its secret and uninstalls
   the Claude and Codex GitHub Apps (owner-side steps, outside the repository).
2. Automatic reviews are off in `.coderabbit.yaml` (`auto_review.enabled: false`). The building agent requests
   one review when the branch is finished, answers the findings in one push, replies in each thread naming the
   commit, and requests one re-review when the PR is otherwise ready to merge, not because an hour has passed
   (`AGENTS.md` §4 step 6). The ADR-0016 stop rule applies unchanged.
3. The reviewer's rules live in one place, the "Code Review Rules" section of `AGENTS.md`, which CodeRabbit reads
   on its own (its code-guidelines patterns include `AGENTS.md`). `.coderabbit.yaml` points there and adds the
   five safety questions and the guard instruction as path instructions. `docs/REVIEW-PROMPT.md` no longer
   holds a copy of the brief (kept in step by hand, it drifted twice); it becomes the owner's runbook: the
   limits, the controls, how to read a result, when to stop, and a short prompt for a second opinion by hand.
4. `.coderabbit.yaml` is a rule file under R14 (`AGENTS.md` §2, `scripts/safety-impact.sh`): a change to it
   travels alone, like a change to `AGENTS.md`.
5. CodeRabbit's verdicts (approve, request changes) gate nothing: the `main` ruleset requires no approval, only
   resolved threads. The merge stays the owner's decision (ADR-0012 point 4's sentence on that stands in §2).
6. Learnings CodeRabbit stores from thread replies are a fourth control, outside the repository. The owner
   reviews them in the CodeRabbit app when a review cites one that no longer matches the code.

## Alternatives rejected
- Keep the three reviewers. Rejected by the owner: the stack was designed by agents, not by them, and had grown
  past what they could follow. Three reviews per push also produced three sets of threads with the same finding.
- Ten GitHub stars to turn automatic reviews on. The allowance would rise (the documentation says "1-10 per hour,
  varies by star count" without the mapping), but automatic reviews on every push are the opposite of the one
  review per finished branch this repository wants.
- A paid CodeRabbit seat (Essentials, 5 reviews per hour). Not needed while one review per hour fits the
  one-review-per-branch rule; open if the wait ever hurts.

## Consequences
- One review per hour on the whole repository. Two pull requests open at the same time wait for each other; the
  agent triggers once per finished branch, never per push.
- The reviewer replies in threads, so the thread rule in `AGENTS.md` §2 changes: the reviewer resolves what it
  verifies, the owner resolves what it leaves open.
- No workflow reads a secret; the `claude-review` environment goes when the owner deletes it after the merge.
  `docs/SECURITY.md` and `docs/SAFETY-CONTRACT.md` describe one reviewer instead of two; the owner's own reading
  of the "Safety impact" section carries more weight.
- `.github/workflows/review-claude.yml` is gone; ADR-0012 and ADR-0014 record what it was.
