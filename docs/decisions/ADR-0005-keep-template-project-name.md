# ADR-0005: Keep the template project/assembly name, rename the types

Status: accepted · Date: 2026-09-24

## Context
The template generated `internet_speed_test_extension` as the project, assembly, MSIX identity, exe name, namespace,
and class names. Snake_case type names are non-idiomatic C#, but the project/assembly/identity name is wired into
the solution, the manifest, and the publish profiles, and renaming it buys nothing for users.

## Decision
Keep the project, assembly, MSIX identity, and repository name. Change the root namespace to `SpeedTest.Extension`
and name types idiomatically (`SpeedTestExtension`, `SpeedTestCommandsProvider`, `MeterPage`, `DetailsPage`).
`Program.cs` is updated once for the new type names in this initial commit; after that it is protected (W1).

## Consequences
Readable code, no churn in build plumbing. The CLSID stays the template's GUID and is checked by R10.
