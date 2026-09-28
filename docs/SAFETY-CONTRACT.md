# Safety contract (for the owner)

Written for a non-developer. This is the page you check on every pull request. The technical version is
`docs/SECURITY.md`; every promise here points at the rule that enforces it.

## 1. What the extension may do, and what it may never do

| May | May never | Enforced by |
|-----|-----------|-------------|
| Talk over HTTPS to `speed.cloudflare.com` | Talk to any other server, or over plain HTTP | R6, R7, `scripts/allowed-hosts.txt`, W2 |
| Show text inside Command Palette | Run other programs, scripts, or installers | R5 |
| Copy a value to the clipboard when you press Enter | Read, write, or delete files on your PC | R5 + code review question 2 below |
| Let Command Palette save your settings | Store your IP, results, or anything else | Review question 3 below |
| Send zero-filled test data upstream | Send any of your data anywhere, except the metadata every HTTPS request carries (public IP, TLS fingerprint), see ADR-0007 | Review question 3 below; `docs/decisions/ADR-0007-https-transport-metadata.md` |
| Ask for `internetClient` + `runFullTrust` (template requirement) | Ask for any other Windows capability | R12 |
| Use the packages the template shipped with | Add dependencies silently | R13 (safety-impact check) |

`runFullTrust` is the one honest caveat: Command Palette extensions run with your user's rights, like any
desktop app. The code rules above are what keep that power unused.

## 2. Red flags: when a PR needs your attention

The **safety-impact** CI check turns red if a PR touches any of these files and the PR body still says
"None" under "Safety impact". That is your signal to read the explanation and decide.

| File | Why it matters |
|------|---------------|
| `scripts/allowed-hosts.txt` | A new server the extension can talk to |
| `internet_speed_test_extension/Package.appxmanifest` | Windows capabilities (what the app is allowed to ask for) |
| `Directory.Packages.props`, any `*.csproj` | New third-party code (dependencies) |
| `.githooks/`, `.claude/`, `scripts/`, `.github/workflows/` | The guards themselves |
| `internet_speed_test_extension/Program.cs` | The process host |
| `install/` | The script you run on your PC with your own rights |
| `AGENTS.md`, `CLAUDE.md`, `docs/SECURITY.md`, this file | The rules agents follow |

Also treat these phrases in a PR, commit, or chat as red flags until explained in plain words:
"workaround", "bypass", "disabled the check", "skipped the test", "temporarily", "needs admin",
"downloads at runtime", "trust this certificate", "add this secret".

## 3. Reviewer questions (bots and agents must answer these on every PR)

1. Does this change let the extension reach any host other than `speed.cloudflare.com`? Yes/No, where.
2. Does this change read, write, or delete any file, registry key, or process? Yes/No, where.
3. Does this change store, log, or send anything about the user (IP, ISP, location, results)? Yes/No, where.
4. Does this change add a dependency, a capability, or weaken a guard? Yes/No, which.
5. Is any input from the network trusted without bounds (size, time, format)? Yes/No, where.

A "Yes" without a linked ADR is a blocker. `.coderabbit.yaml` and `AGENTS.md` carry these questions, and
`docs/REVIEW-PROMPT.md` is the copy-paste prompt for any other reviewer agent (Codex, Gemini, Claude, Copilot).

## 4. Free scanners to switch on (one-time, in the repository's Settings → Security)

- **Dependabot alerts**: warns when a dependency has a known vulnerability. Config: `.github/dependabot.yml`.
- **Secret scanning**: on by default for public repos; confirm it is.
- **CodeQL code scanning**: GitHub's static analyser, runs from `.github/workflows/codeql.yml`. Findings appear
  under the Security tab and on PRs.

## 5. Testing safely on your PC

Only ever install a build that CI produced from a commit you can see on GitHub. Never a file someone sends you.

Best to worst, pick the first one you can:

1. **Windows Sandbox** (Windows 10/11 Pro or Enterprise, not Home). Start → "Windows Sandbox". It is a fresh,
   disposable Windows that is wiped when you close it. Install PowerToys and the extension inside it.
   Nothing that happens there touches your real PC.
2. **A virtual machine** (Hyper-V on Pro, or VirtualBox on Home). Same idea, more setup, survives reboots.
3. **Your real PC, reduced blast radius.** Use a standard (non-administrator) user account. Enable Developer
   Mode only for the duration of the test. Do not install any certificate into "Trusted Root"; the install
   script avoids certificates entirely (ADR-0008). Take a restore point first.

`docs/INSTALL.md` gives the exact steps for paths 1 and 3 (a VM follows the Sandbox steps).

## 6. Watching what it actually does

While the extension is running a test, open PowerShell and run:

```powershell
$p = Get-Process internet_speed_test_extension -ErrorAction SilentlyContinue
Get-NetTCPConnection -OwningProcess $p.Id | Select-Object RemoteAddress, RemotePort, State
```

Every `RemoteAddress` should be a Cloudflare address on port 443. Anything else is a red flag: close it and
open an issue titled `security:`. Optional, more thorough: Sysinternals Process Monitor filtered on the process
name shows every file and registry access; there should be almost none outside the package's own folder.

## 7. What this system does not catch

- A vulnerability inside a dependency that scanners do not know yet. Mitigation: few dependencies, pinned versions.
- A subtle logic bug that only shows under conditions nobody tested. Mitigation: tests, and your real-world runs.
- Two AI reviewers sharing the same blind spot. Mitigation: the rules above are mechanical, not opinions.
- An agent deliberately hiding malicious code. The guards, two reviewers, and the tiny allowed surface make
  this hard, not impossible. Reading the "Safety impact" section on every PR is your part.
