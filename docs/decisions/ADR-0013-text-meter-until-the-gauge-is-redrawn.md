# ADR-0013: v0.1.1 ships the text meter; the SVG gauge leaves the code until it is redrawn

Status: accepted · Date: 2026-09-29 · Supersedes ADR-0011 (ADR-0006 applies again)

## Context
ADR-0011 replaced the text bar of ADR-0006 with an SVG speedometer arc embedded in the markdown. The owner's VM run
on 2026-09-29 (PowerToys 0.101) verified the mechanism: the arcs render and follow the test. The owner's verdict on
the result was that the layout and the animation are "not very good" (plan item 5.1), and they wanted the session 4
measurement fixes (ADR-0010, `Server-Timing`) and the copy commands on the laptop now, as `v0.1.1`, with the
display `v0.1.0` had.

Two ways to ship that: keep `GaugeSvg` in the code with no caller until 5.1, or remove it. The Code Review Rules in
`AGENTS.md` make code without a caller a P1 ("speculative structure"), and a release should not carry it.

## Decision
`MeterMarkdown.AppendMeter` draws the ADR-0006 text bar again, the same line `v0.1.0` shipped. `GaugeSvg`, its tests
and the R6 exemption for that file in `scripts/check.sh` are removed. Rule R6 keeps the single-quote scan it gained
with ADR-0011 and has no exception again.

The gauge is not abandoned. The last commit on `main` that holds it is `6034b22`; plan item 5.1 starts with
`git checkout 6034b22 -- src/SpeedTest.Core/GaugeSvg.cs tests/SpeedTest.Core.Tests/GaugeSvgTests.cs`, re-adds the
R6 exemption from that commit, and records the redrawn gauge in a new ADR that supersedes this one.

## Consequences
- `v0.1.1` looks like `v0.1.0` and measures like session 4. PowerToys 0.95 is no longer a requirement.
- ADR-0011 stays as written (ADRs are not edited after acceptance, `docs/CONVENTIONS.md`); its verification result
  lives in `docs/SESSION-LOG.md`, session 4.
- Every `docs/SAFETY-CONTRACT.md` §3 answer stays "No": the change removes code and an exemption, nothing else.
