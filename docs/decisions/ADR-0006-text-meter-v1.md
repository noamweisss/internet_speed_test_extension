# ADR-0006: Draw the meter dashboard as text in markdown for v1

Status: accepted · Date: 2026-09-24

## Context
Command Palette pages render markdown (`MarkdownContent`) or lists; there is no gauge control. Markdown images work
from `data:` URIs, but whether SVG data URIs render is unverified, and generating PNGs in-process needs extra
Windows imaging code. An unverifiable UI choice made in a Linux container is a bad bet for the first release.

## Decision
v1 renders the meter as markdown text: phase header, big value, and a Unicode bar (`▰▰▰▱▱▱`) scaled to a
documented maximum. It is deterministic, testable in Core, and cannot fail to render. Image gauges are a
session-3 experiment (`docs/PLAN.md` 3.1) once a human can see the result.

## Consequences
Less visual polish than Raycast's speedometer at first. Zero rendering risk and one more thing covered by tests.
