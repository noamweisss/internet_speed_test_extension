# ADR-0016: One fix round per review, and rule changes in their own pull request

Status: accepted · Date: 2026-09-29

## Context
PR #19 (a research document, its HTML explainer, and a new subsection in `AGENTS.md`) went through 15 Codex
review rounds. No round found anything in code; the substantive findings came in rounds 1, 4, 6 and 10, the rest
were units and hedging. The causes:

1. The building agent did not follow the stop rule already in `docs/REVIEW-PROMPT.md` ("a review round with no
   Blocker or Major in code ends the loop"). The owner had asked for both reviewers to "agree on everything", and
   the agent read that as "a round with zero comments", so it requested `@codex review` after every fix.
2. The pull request mixed a rule change (`AGENTS.md`) with content. Reviewers treat guard changes as P0/P1, and
   four rounds went only to the rule's wording.
3. Each finding was fixed in one sentence, so the next round found the next occurrence of the same concept.
4. The documentation rules make an inconsistency P1 when it could send an agent the wrong way, and the two
   documents repeated each other's claims.

## Decision
1. Stop rule, written (`AGENTS.md` §4 step 6 for the building agent, "Code Review Rules" / Completeness for
   reviewers, `docs/REVIEW-PROMPT.md` "Reading the results" for the owner). The first review, one fix push, one
   re-review. Another round only after a round that reports a Blocker or Major (P0/P1) in code, or a safety
   finding (a Yes to a question of `docs/SAFETY-CONTRACT.md` §3, or a weakened guard). Documentation findings
   after the fix round are fixed if cheap and answered once, never with a review request. "Agree" means no open
   Blocker or Major. Before pushing a fix for a documentation finding, the agent fixes every occurrence of the
   same concept in the diff. This rule cannot be checked by a script: it is about what the agent posts.
2. Rule changes alone, checked (R14, `scripts/safety-impact.sh`). A pull request that changes a rule or guard
   file (`AGENTS.md`, `CLAUDE.md`, `.github/`, `.githooks/`, `.claude/`, `scripts/`, `docs/SAFETY-CONTRACT.md`,
   `docs/SECURITY.md`, `docs/REVIEW-PROMPT.md`, `docs/CONVENTIONS.md`, `docs/TESTING.md`) changes nothing else
   except `docs/SESSION-LOG.md`, `docs/PLAN.md`, `CHANGELOG.md` and `docs/decisions/`. ADRs are allowed because
   the Guards rule asks a guard change to name one.

Why a check and not only a sentence for point 2: the stop rule that PR #19 ignored was a sentence. The check is
ten lines in a script that already runs on every pull request (R13) and already reads the diff's file list; no
new job, no new workflow. The CI job keeps its name, "Safety impact declared", so a required-check setting on it
keeps working; the log line starts with `RULE R14`.

## Consequences
- Replayed on the merged history: PRs #18, #20 and #21 (workflow fixes), the Dependabot PRs #2, #3, #4 and #6,
  and the code-only PRs pass. PRs #15 and #17 would have failed (a `scripts/check.sh` exemption shipped with the
  gauge code), and PR #16 too (`.coderabbit.yaml` deleted next to rule files). Those now take two pull requests;
  when the content depends on the rule (a check exemption, a new host in `scripts/allowed-hosts.txt`), the rule
  pull request merges first.
- A merge of `main` into a branch does not trip the check: CI compares the pull request's merge commit with its
  base, so only the branch's own changes count.
- PR #19, still open, fails R14 once this merges: its `AGENTS.md`, `docs/CONVENTIONS.md` and
  `docs/REVIEW-PROMPT.md` changes and its ADR-0015 move to their own pull request, or PR #19 merges first.
  This ADR takes number 0016 because PR #19 already holds 0015.
- Documentation threads can stay open after the stop. The owner resolves or dismisses them (`AGENTS.md` §2).
- Every `docs/SAFETY-CONTRACT.md` §3 answer stays "No": the change adds a check and weakens none.
