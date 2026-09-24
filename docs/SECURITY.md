# Security

## Threat model (short)

The extension runs as a full-trust, out-of-process COM server that PowerToys loads (template requirement:
`runFullTrust`). Anything it does, it does with the user's rights. Therefore the design keeps its behaviour small
and provable:

| Concern | Position |
|---------|----------|
| Code execution | No process spawning, no native interop, no `unsafe`, no reflection loading, no downloaded binaries. (R5) |
| Network | HTTPS only, to `speed.cloudflare.com` only. Hosts are allowlisted in `scripts/allowed-hosts.txt`. (R6, R7) |
| TLS | .NET default validation. Overrides are forbidden. (R5) |
| Data in | Response bodies are read and discarded, bounded in size and time. JSON from `/meta` is parsed into a fixed record with `System.Text.Json` source generation; unknown fields ignored. |
| Data out | Upload bodies are zero-filled buffers. No user data leaves the machine except what any HTTPS request carries (IP, TLS fingerprint). |
| Privacy | Public IP and ISP are shown to the user, never logged or persisted. Only settings are persisted, by the host. |
| Supply chain | NuGet packages: only those from the template plus xUnit for tests. Central version pinning in `Directory.Packages.props`. `NuGetAuditMode` on. New packages need an ADR. |
| Capabilities | `internetClient` and `runFullTrust` only (`Package.appxmanifest`). |
| Secrets | None exist. Certificates and env files are refused by the hooks. (R2, R4) |

## Enforced rules (ids printed when a rule fires)

Git hooks, `.githooks/` (any git user, installed by `scripts/setup.sh`):

| Id | Rule |
|----|------|
| R1 | No commits on `main`. |
| R2 | No secret or certificate file types. |
| R3 | No files over 1 MB. |
| R4 | No secret-looking strings. |
| R5 | No process spawning, interop, `unsafe`, reflection loading, TLS overrides in C#. |
| R6 | No plain `http://` in C#. |
| R7 | Every `https://` host in C# is in `scripts/allowed-hosts.txt`. |
| R8 | Code changes are committed together with a `CHANGELOG.md` change. |
| R9 | Guard-file changes carry a `Guard-Change:` trailer (commit-msg hook). |
| R10 | The COM CLSID is identical in the manifest and the extension class. |
| C1 | Conventional Commits first line. |
| P1 | No pushes to `main`. |
| P2 | No non-fast-forward pushes. |
| P3 | No auto-generated branch names (`claude/<word>-<word>-<id>`). |

Claude Code hooks, `.claude/settings.json` (main agent and subagents):

| Id | Rule |
|----|------|
| G1 | No hook bypass flag on commit or push. |
| G2 | No force flags on push. |
| G3 | No push targeting `main`. |
| G4 | No `core.hooksPath` change outside `scripts/setup.sh`. |
| G5 | No switching to `main`. |
| G6 | No hard reset, forced clean, branch delete, or rebase. |
| G7 | No deleting repo infrastructure. |
| G8 | No commit amending. |
| W1 | No edits to `Program.cs`. |
| W2 | No edits to `scripts/allowed-hosts.txt` without a human decision and ADR. |
| S1 | A session that changed the repo must update `docs/SESSION-LOG.md` before it ends. |

What the hooks cannot do: they do not stop a determined human with shell access, and they are not a substitute for
review. They exist to make accidental or careless violations impossible and deliberate ones visible.

## Reporting

Open a GitHub issue titled `security: <summary>`. There is no bug bounty.
