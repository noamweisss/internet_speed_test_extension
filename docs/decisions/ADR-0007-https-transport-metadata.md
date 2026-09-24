# ADR-0007: Accept the metadata that any HTTPS request carries

Status: accepted · Date: 2026-09-24

## Context
`docs/SAFETY-CONTRACT.md` promises that the extension sends none of the user's data. Any HTTPS request, by
its nature, reveals to the server the client's public IP address, TLS client fingerprint, and TCP/IP transport
details. Cloudflare's speed test reads exactly these to report ISP, location, and the serving data centre back
to the user. There is no way to measure a connection without making requests over it.

## Decision
The contract's "no user data" promise excludes the metadata inherent to HTTPS transport: public IP, TLS
fingerprint, and connection timing. Nothing else is sent: upload bodies are zero-filled, no identifiers, no
settings, no results. The extension displays what Cloudflare reports and never stores or logs it.

## Consequences
The contract states the exception explicitly instead of implying an impossible promise. Reviewer question 3
("does it store, log, or send anything about the user?") is answered "No, beyond ADR-0007".
