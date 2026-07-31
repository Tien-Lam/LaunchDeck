# Agent Instructions

## Linear Work Management

- Linear is the source of truth for all LaunchDeck planning, implementation,
  review, testing, documentation, and release work. Use the
  [LaunchDeck initiative](https://linear.app/tienlam/initiative/launchdeck-b247bff02400)
  and follow `docs/WORKFLOW.md`.
- Before changing code, tests, workflow files, or living documentation, find or
  create the Linear issue and confirm its project, milestone, parent gate,
  labels, priority, acceptance criteria, and blockers.
- Do not start a blocked issue. Assign the issue, move it to `In Progress`, and
  use Linear's generated branch name when work begins.
- Keep independently deliverable work in sub-issues. If review or implementation
  discovers broader scope, create a linked sub-issue and add a blocking relation
  when it must land first.
- Move work to `In Review` only after implementation and required automated
  verification are complete. First commit every intended file, confirm the
  working tree is clean, and record exact commands/results plus the immutable
  commit SHA and tree SHA in a Linear comment.
- On entering `In Review`, automatically start a separate review session with
  no inherited conversation history. Give it only the Linear issue, repository
  instructions, and the exact commit SHA to inspect. The implementation session
  must not act as the sole reviewer or prime the reviewer with its reasoning.
- The review session reports findings without editing the work. Record its
  session identity, inspected commit and tree SHAs, findings, and disposition
  in Linear.
  In-scope findings return the issue to `In Progress`; after fixes and automated
  verification, start another fresh review session. A newly discovered blocker
  moves the issue to `Backlog` until every blocker is Done. If a separate
  session cannot be started, keep the work in review and report the blocker
  instead of self-approving.
- Any content change after review requires a new commit, automated verification,
  and fresh clean-context review. Before merge or completion, confirm the final
  tree SHA exactly matches the tree SHA approved by the latest review session.
- Before making a pull request ready for review, fill its `Independent review`
  fields with the fresh `/root/...` session, exact lowercase commit and tree
  SHAs, and the exact result `no findings`. The `pr-policy` check compares those
  SHAs with the live PR head; every new push invalidates the evidence until a
  new clean-context review is recorded.
- Move work to `Done` only after acceptance criteria are met, changes are merged
  where applicable, final evidence is recorded, and the latest independent
  review session has no unresolved findings. Every blocking issue must be Done.
- Issues labeled `Manual validation` must be in the final milestone of their
  project and must not block earlier agentic work. Do not perform installed-MSIX,
  Game Bar, touch/controller, hardware, or subjective UX checks early.
- Use GitHub for source, pull requests, review threads, Actions, artifacts, and
  releases. GitHub Issues are public intake only: mirror each report into Linear,
  post the Linear link, and close the GitHub issue. Do not track work there.

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
