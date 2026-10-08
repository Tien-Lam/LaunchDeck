# Agent Instructions

## GitHub Work Management

- GitHub Issues and [Projects](https://github.com/users/Tien-Lam/projects/3) own planning, implementation, review, testing, documentation and release status. Follow docs/WORKFLOW.md and docs/GITHUB_WORKFLOW.md.
- Before changes, read the owning issue and confirm workstream, milestone, parent gate, labels, priority, acceptance criteria and blockers. Do not start blocked work. Assign it, set In Progress and use a codex/ branch containing the GitHub issue number.
- Keep independently deliverable outcomes in native sub-issues. Add native blocked-by relations for prerequisites.
- Before In Review, commit every intended file, confirm a clean working tree and record exact verification commands/results plus immutable commit and tree SHAs on the GitHub issue.
- Automatically start a separate clean-context read-only review session with the issue, repository instructions and exact commit. The implementer must not be the sole reviewer or prime the review with its conclusions. Record session identity, findings and dispositions on the issue.
- Fix actionable findings and repeat verification/fresh review after content changes. Expanded scope becomes a linked sub-issue; blockers return the dependent issue to Backlog. The final tree must equal the independently approved tree.
- Ready PRs must fill the Independent review fields with the fresh /root/... session, exact lowercase commit/tree SHAs and result no findings. The pr-policy check rejects stale or hidden evidence. New titles use #<GitHub issue number>; historical TIE aliases remain accepted for migrated work.
- Close as completed/Done only after matching acceptance, blockers completed, merge, required checks and final review evidence. Preserve canceled/unfinished dispositions and capability gaps.
- Manual validation issues belong to the workstream’s final milestone. Do not perform installed-MSIX, Game Bar, touch/controller, hardware or subjective UX checks early.
- GitHub Issues accept contributor reports directly; triage them into the same project, milestone and dependency model. No Linear mirroring is required.

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
- Because the required dispatch uses `--ref main`, cite the remote MSIX build as
  evidence only for the exact reviewed commit after it is present on `main`.
  Never use a `main` run as evidence for an unmerged issue branch.
- The resulting Actions artifact contains the signed development MSIX,
  certificate, `Install.ps1`, and `Uninstall.ps1`. Installation and Game Bar
  testing still require Windows.

## GitHub products and documentation

Use GitHub Issues for scope and acceptance, GitHub Projects for status and priority, GitHub Pull Requests for review, GitHub Actions for automated verification and authorized publishing, and GitHub Releases for versioned releases. Keep README.md, AGENTS.md and repository Markdown docs canonical; an existing GitHub Wiki is a navigation index. Follow [the product map and documentation policy](docs/GITHUB_WORKFLOW.md).
