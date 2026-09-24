# ADR-0001: Record architecture decisions as ADRs

Status: accepted · Date: 2026-09-24

## Context
Several agents (possibly from different vendors) and reviewers will touch this project across sessions. Decisions
made in one conversation are invisible to the next unless written down where the code lives.

## Decision
Every decision a reviewer could reasonably question is recorded in `docs/decisions/ADR-NNNN-kebab-title.md` with
Context, Decision, Consequences. ADRs are immutable once accepted; a new ADR supersedes an old one.

## Consequences
Slightly more writing per decision; in exchange, reviewers and future agents can trace *why* without asking.
