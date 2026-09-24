# ADR-0003: Keep measurement logic in a cross-platform Core library

Status: accepted · Date: 2026-09-24

## Context
The extension project targets `net10.0-windows` with MSIX tooling and only builds on Windows. Agents usually work in
Linux containers. Logic that only compiles on Windows cannot be unit-tested where the agents run.

## Decision
`src/SpeedTest.Core` (plain `net10.0`, no Windows APIs) holds the measurer, result model, formatting, and the text
meter. `tests/SpeedTest.Core.Tests` (xUnit) tests it with a faked `HttpMessageHandler`. The extension project
references Core and contains only Command Palette adapters. CI compiles the extension on a Windows runner.

## Consequences
Two projects instead of one, but almost all behaviour is testable on any OS, and the Windows-only surface stays
small enough to review by eye.
