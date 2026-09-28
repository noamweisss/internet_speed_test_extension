# ADR-0009: Package identity `InternetSpeedTestExtension`

Status: accepted · Date: 2026-09-24 · Supersedes the "MSIX identity" part of ADR-0005

## Context
ADR-0005 kept the template name `internet_speed_test_extension` for the project, assembly, and MSIX identity.
The first CI build of an actual package (ADR-0008) failed: `MakeAppx` rejects the identity because package
names allow only letters, digits, `-` and `.` (error C00CE169). No package, from Visual Studio or CI, could have
been built with that name.

## Decision
`Identity Name` in `Package.appxmanifest` becomes `InternetSpeedTestExtension`. Everything else in ADR-0005
stays: project, assembly, exe name, publish profiles, and CLSID are unchanged.

## Consequences
- The install script and CI look the package up by the new name.
- Store or WinGet publishing (plan item 3.4) will replace the identity with the one Partner Center assigns.
- `Package.appxmanifest` is safety-sensitive; this change adds no capability and no host.
