# LaunchDeck

[![Microsoft Store](https://img.shields.io/badge/Microsoft_Store-Install-0078D4?style=flat&logo=microsoft)](https://apps.microsoft.com/detail/9PJHCMMVQ6HK)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

An Xbox Game Bar widget that launches apps, URLs, and Store apps from a configurable tile grid overlay.

[Xbox mode](https://support.microsoft.com/en-us/topic/windows-gaming-full-screen-experience-67fb8d12-5467-4a95-8adf-0a10789576ab) (formerly Xbox Full Screen Experience) turns Windows into a console-like fullscreen shell, but its library is game-focused — there's no easy way to launch non-game apps, utilities, or URLs without switching back to the desktop. LaunchDeck fills that gap as a Game Bar widget you can open with Win+G to launch anything without leaving the experience.

<p align="center">
  <img src="https://store-images.s-microsoft.com/image/apps.43906.14282260671513564.52712b09-34b2-435e-9f8c-cb1c54279bd8.9d3a833c-7f68-4f8e-bf0b-b957d3aff6e1" alt="LaunchDeck widget" width="680"/>
</p>
<p align="center">
  <img src="https://store-images.s-microsoft.com/image/apps.54778.14282260671513564.52712b09-34b2-435e-9f8c-cb1c54279bd8.01cc4738-471f-4b6a-8799-0a355c2ac6c5" alt="LaunchDeck config editor" width="680"/>
</p>

## Install

<a href="https://apps.microsoft.com/detail/9PJHCMMVQ6HK">
  <img src="https://get.microsoft.com/images/en-us%20dark.svg" alt="Get it from Microsoft" height="80"/>
</a>

Or build from source — see [Build](#build) and [Deploy](#deploy) below.

## Features

- Launch EXEs, URLs, and Microsoft Store apps from a dark-themed tile grid
- Automatic icon extraction for EXEs, favicons for URLs, and package icons for Store apps
- Built-in config editor with Store app picker — browse installed apps and add them with one click
- Runs as a Game Bar widget — open with Win+G while gaming
- Focuses launched EXE apps by default, with a setting to turn foreground forcing off
- Localized app, widget, and editor strings for English, Spanish, French, German, Japanese, Brazilian Portuguese, Russian, Ukrainian, Simplified Chinese, and Traditional Chinese

## Work Tracking

LaunchDeck uses the
[LaunchDeck initiative in Linear](https://linear.app/tienlam/initiative/launchdeck-b247bff02400)
for bugs, features, implementation, reviews, testing, documentation, and
releases. GitHub is used for source, pull requests, Actions, artifacts, and
release publishing; GitHub Issues are not used for work tracking. Contributors
without Linear workspace access may use the public GitHub issue form as intake;
a maintainer mirrors the report into Linear and closes the intake issue.

Before contributing, read the [development workflow](docs/WORKFLOW.md). Every
change must link a Linear issue and record its automated verification there.
Interactive MSIX, Game Bar, touch/controller, and subjective UX checks are kept
in each project's final manual milestone so they do not block earlier agentic
work.

## Configuration

Items are stored in `%LOCALAPPDATA%\LaunchDeck\config.json`. Use the built-in editor (gear button in the widget) to manage items, or edit the JSON directly:

```json
{
  "focusLaunchedApps": true,
  "items": [
    { "name": "Notepad", "type": "exe", "path": "C:\\Windows\\notepad.exe" },
    { "name": "YouTube", "type": "url", "path": "https://youtube.com" },
    { "name": "Spotify", "type": "store", "path": "shell:AppsFolder\\SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify" },
    { "name": "Dev Server", "type": "exe", "path": "C:\\tools\\server.exe", "args": "--port 8080" },
    { "name": "Discord", "type": "exe", "path": "C:\\Discord\\Discord.exe", "icon": "C:\\icons\\discord.png" }
  ]
}
```

Each item requires `name`, `type`, and `path`. Optional fields: `args` (command-line arguments for EXE items) and `icon` (custom icon image path, overrides auto-extraction). The top-level `focusLaunchedApps` setting defaults to `true`; set it to `false` to leave foreground ownership entirely to Windows, Game Bar, and the launched app. See [`config.sample.json`](config.sample.json) for a full example.

## Building from Source

### Requirements

- Installing and testing the widget: Windows 10 19041+ with Xbox Game Bar
- Full local build: Visual Studio with UWP build tools and Windows SDK
  10.0.26100.0
- Managed-project builds: .NET 10 SDK; use [mise](https://mise.jdx.dev/) on
  macOS

### Architecture

LaunchDeck uses a two-process design to work around UWP sandbox restrictions:

- **Widget** (UWP) — the tile grid UI that runs inside Game Bar
- **Companion** (.NET 10 Win32) — handles file I/O, process launching, icon extraction, and hosts the config editor

The two processes communicate over Windows App Service IPC, packaged together in a single MSIX via a Windows Application Packaging Project.

```
LaunchDeck.Widget/       # UWP XAML widget
LaunchDeck.Companion/    # .NET 10 companion (WPF editor, IPC handlers)
LaunchDeck.Shared/       # Shared library (config models, loader)
LaunchDeck.Tests/        # xUnit tests
LaunchDeck.Package/      # MSIX packaging and manifest
```

### Build

```bash
# Managed projects on Windows
dotnet build LaunchDeck.Shared/LaunchDeck.Shared.csproj
dotnet build LaunchDeck.Companion/LaunchDeck.Companion.csproj
dotnet test LaunchDeck.Tests/

# Full solution on Windows (requires Visual Studio / MSBuild)
msbuild LaunchDeck.sln /p:Configuration=Debug /p:Platform=x64 /restore
msbuild LaunchDeck.sln /p:Configuration=Debug /p:Platform=ARM64 /restore
```

On macOS, use mise rather than installing .NET directly. The portable Shared
project builds locally:

```bash
mise x dotnet@10 -- dotnet build LaunchDeck.Shared/LaunchDeck.Shared.csproj
```

The Windows-targeted Companion and Tests assemblies can be cross-compiled with
`EnableWindowsTargeting=true`, but the tests cannot run on macOS because their
test host requires `Microsoft.WindowsDesktop.App`. The UWP Widget and MSIX
packaging projects require Windows build targets and cannot be built locally on
macOS.

To build the complete signed MSIX from macOS or Linux, run the manual Windows
GitHub Actions workflow:

```bash
gh workflow run build-msix.yml --ref main \
  -f platform=x64 \
  -f configuration=Debug
```

Choose `ARM64` instead of `x64` when needed; `Debug` and `Release` are both
supported. The **Build MSIX** workflow runs the Windows tests, builds the full
solution, and uploads a 14-day artifact containing the development-signed
MSIX, its certificate, and the install/uninstall scripts. It does not publish a
GitHub Release. See [Deployment](docs/DEPLOYMENT.md#remote-msix-build-from-macos-or-linux)
for download and installation details.

Version tags publish only after the tag is verified as reachable from `main`,
managed checks pass, and both x64 and ARM64 full MSIX jobs succeed. Releases
contain separate archives for each architecture; see
[Version-tag releases](docs/DEPLOYMENT.md#version-tag-releases).

### Deploy

```powershell
.\deploy.ps1
.\deploy.ps1 -Platform ARM64
```

Builds the full solution with MSBuild and registers the package via loose-file deployment (no signing needed). The deploy script defaults to `x64`; use `-Platform ARM64` on Windows on Arm devices. Requires Visual Studio with the UWP workload installed. After deploying, open Game Bar (Win+G) and enable the LaunchDeck widget from the widget menu.

### Uninstall

```powershell
.\Uninstall.ps1
```

Or manually:

```powershell
Get-AppxPackage *LaunchDeck* | Remove-AppxPackage
Remove-Item "$env:LOCALAPPDATA\LaunchDeck" -Recurse -Force
```

## Docs

- [Architecture](docs/ARCHITECTURE.md) — system overview, two-process design, project map
- [IPC Protocol](docs/IPC.md) — App Service actions, request/response fields, sequence flows
- [Config](docs/CONFIG.md) — JSON schema, item types, icon resolution
- [UI](docs/UI.md) — dark theme palette, XAML structure, interactive states
- [Deployment](docs/DEPLOYMENT.md) — build pipeline, VS deploy, manifest, troubleshooting
- [Testing](docs/TESTING.md) — test coverage, boundaries, manual test checklist
- [Workflow](docs/WORKFLOW.md) — Linear planning, implementation, review, testing, and release process
