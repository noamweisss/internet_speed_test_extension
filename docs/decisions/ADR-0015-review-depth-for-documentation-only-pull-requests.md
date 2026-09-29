# ADR-0015: Documentation-only pull requests get the documentation rules, not the code rules

Status: accepted · Date: 2026-09-29

## Context
The Code Review Rules in `AGENTS.md` were written for code: every reviewer answers the five safety questions of
`docs/SAFETY-CONTRACT.md` §3 from the diff, and asks for a unit test and an ADR where logic or a decision
changes. Session 5 (PR #19) produced a research document and an owner-facing HTML explainer, with no code. The
code rules had nothing to check there, and reviewers could still ask for a test or an ADR for text, which no
research document can satisfy.

## Decision
`AGENTS.md` gains a subsection, "Documentation-only pull requests". A pull request qualifies only when every file
in its diff is a `.md` at the repository root or under `docs/`, or a `.html` under `docs/`, and none of them is a
rule file (`AGENTS.md`, `CLAUDE.md`, `.github/`, `docs/SAFETY-CONTRACT.md`, `docs/SECURITY.md`,
`docs/REVIEW-PROMPT.md`, `docs/CONVENTIONS.md`, `docs/TESTING.md`). For such a pull request:

- The five safety answers still come from the diff. With only documents in it they are "No", because a document
  cannot reach a host, touch a file, add a dependency, or change a guard.
- Reviewers do not ask for a unit test, a Core-layer move, or an ADR for text. The pull request that implements
  a choice a research document ranks carries the ADR.
- The documentation rules apply in full, plus two: every claim about an external system has a source and a
  confidence label, and an HTML explainer is checked against its `.md`. Its script is reviewed for what it does,
  and loading anything from the network is P1.

## Consequences
- Safety coverage does not shrink. Every change that could make a safety answer "Yes" (a host, a file access, a
  dependency, a capability, a guard) needs a non-document file, a guard file, or a rule file in the diff, and any
  of those takes the pull request out of this subsection and back under every rule.
- The subsection itself is a rule file change. Changing it again goes through the full rules, a `Guard-Change:`
  trailer, and "Safety impact" in the pull request body.
- The definition was narrowed four times during the Codex review of PR #19 (documents only; rule files excluded;
  an explainer's script kept in scope; an exact list of rule files, with the plan and hand-off log counted as
  documents). This ADR records the final form.
