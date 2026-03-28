# LaunchPad - Xbox Game Bar Widget

## CRITICAL: Workflow Rules

**Before writing ANY code**, you MUST:
1. Run `export PATH="$PATH:/c/Users/lamti/AppData/Local/Programs/bd:/c/Program Files/dolt/bin"`
2. Create a beads issue: `bd create --title="..." --type=bug|task|feature --priority=2`
3. Claim it: `bd update <id> --claim`

**After completing code for an issue**: `bd close <id>`

**NEVER use** TodoWrite, TaskCreate, or markdown task lists. Beads is the ONLY tracking system.

## Project Overview

Game Bar widget that launches apps (EXEs, URLs, Store apps) from a configurable grid overlay. UWP XAML widget + Win32 companion process (App Service IPC) in a single MSIX package.

## Docs

- [Architecture](docs/ARCHITECTURE.md) -- system overview, two-process design, project map
- [IPC Protocol](docs/IPC.md) -- App Service actions, request/response fields, sequence flows
- [Config](docs/CONFIG.md) -- JSON schema, item types, icon resolution, UWP path workaround
- [UI](docs/UI.md) -- dark theme palette, XAML structure, interactive states
- [Deployment](docs/DEPLOYMENT.md) -- build pipeline, VS deploy, manifest, troubleshooting
- [Testing](docs/TESTING.md) -- test coverage, boundaries, manual test checklist

## Beads Quick Reference

```bash
bd prime                 # Full workflow context (run at session start)
bd ready                 # Find unblocked work
bd create --title="..." --type=task --priority=2  # Create issue
bd update <id> --claim   # Claim and start work
bd close <id>            # Complete work
bd show <id>             # View issue details
```

## Tech Stack

- C# / UWP XAML (widget) / .NET 8 (companion) / .NET Standard 2.0 (shared)
- Microsoft.Gaming.XboxGameBar NuGet
- Visual Studio 2022 + UWP workload + Windows SDK 19041+
- Windows Application Packaging Project (WAPPROJ)

## Build

```bash
# Non-UWP projects (shared, companion, tests)
dotnet build LaunchPad.Shared/LaunchPad.Shared.csproj
dotnet build LaunchPad.Companion/LaunchPad.Companion.csproj
dotnet test LaunchPad.Tests/

# Full solution (requires VS / MSBuild)
msbuild LaunchPad.sln /p:Configuration=Debug /p:Platform=x64 /restore
```

## Deploy

Deploy via Visual Studio (F5). Command-line `Add-AppxPackage` does not reliably register Game Bar widgets.
