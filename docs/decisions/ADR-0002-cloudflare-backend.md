# ADR-0002: Measure natively against Cloudflare instead of running the Ookla CLI

Status: accepted (owner chose "Option A" on 2026-09-24) · Date: 2026-09-24

## Context
The Raycast extension downloads the Ookla `speedtest` CLI at runtime, verifies a SHA-256, accepts Ookla's license and
GDPR terms on the user's behalf, spawns the process, and parses its JSON stream. Doing the same here means shipping
code that downloads and executes a third-party binary from a full-trust process, which is the single largest security
surface such an extension could have, plus zip extraction, hash checking, and license handling code.

Cloudflare exposes public, unauthenticated endpoints used by its own speed test: `GET /__down?bytes=N`,
`POST /__up`, and `GET /meta` on `speed.cloudflare.com`. They return ISP, IP, and location in headers/JSON.

## Decision
Implement the measurement in C# with `HttpClient` against `speed.cloudflare.com` only. No external executables.

## Consequences
- Far less code and no runtime downloads; the whole network surface is one allowlisted host.
- Numbers are comparable to Cloudflare's speed test, not to speedtest.net. No speedtest.net share link, no server
  selection, no packet-loss figure. Documented in README.
- If Cloudflare changes or removes the endpoints, the extension breaks until updated; the error path must be clear.
