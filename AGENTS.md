# Agent Instructions

## Non-Interactive Shell Commands

Always use non-interactive flags to avoid hanging:
```bash
cp -f source dest        # NOT: cp source dest
mv -f source dest        # NOT: mv source dest
rm -rf directory         # NOT: rm -r directory
```

## .NET Toolchain

- On macOS, manage .NET through the existing `mise` installation. Do not use
  `dotnet-install.sh`, Homebrew, a system installer, or a manually unpacked SDK.
- Run .NET 10 commands on macOS with
  `mise x dotnet@10 -- dotnet <command>`.
- The Shared project can build on macOS. The Companion and Tests projects can
  cross-compile with `EnableWindowsTargeting=true`, but the tests cannot run
  because they require the Windows Desktop runtime.

## Full MSIX Builds

- The UWP Widget and WAPPROJ package require Windows MSBuild, Windows XAML
  targets, and the Desktop Bridge targets; they cannot build locally on macOS.
- When explicitly authorized to run a remote build, use the manual
  `.github/workflows/build-msix.yml` workflow from the default branch:

  ```bash
  gh workflow run build-msix.yml --ref main \
    -f platform=x64 \
    -f configuration=Debug
  ```

- Valid workflow inputs are `x64` or `ARM64` and `Debug` or `Release`.
- Monitor the run through completion with `gh run watch <run-id> --exit-status`.
  Do not report success until the artifact upload step completes.
- The resulting Actions artifact contains the signed development MSIX,
  certificate, `Install.ps1`, and `Uninstall.ps1`. Installation and Game Bar
  testing still require Windows.
