# Development Workflow

LaunchDeck uses [Linear](https://linear.app/tienlam/initiative/launchdeck-b247bff02400)
as the source of truth for product planning, implementation, reviews, testing,
documentation, and releases. GitHub remains the source host, pull-request
review surface, CI runner, artifact store, and release publisher; GitHub Issues
are not used for work tracking.

## Linear hierarchy

Work belongs to the **LaunchDeck** initiative and one of these projects:

- [Layout Platform](https://linear.app/tienlam/project/launchdeck-layout-platform-4454546079d8)
  owns configuration, migration, persistence, shared placement, and automated
  layout confidence.
- [Widget Experience](https://linear.app/tienlam/project/launchdeck-widget-experience-d9ec2aa72a08)
  owns the Game Bar renderer, pages, focus navigation, and widget runtime.
- [Live Grid Editor](https://linear.app/tienlam/project/launchdeck-live-grid-editor-c5378a958725)
  owns the WPF editing experience, direct manipulation, layout controls, and
  page management.
- [Delivery & Quality](https://linear.app/tienlam/project/launchdeck-delivery-and-quality-419de99cfa4e)
  owns workflow, CI/review gates, cross-project release readiness, and final
  release acceptance.

Each project is organized as:

1. Ordered milestones describe delivery phases.
2. A parent **Gate:** issue defines each milestone's exit criteria.
3. Independently deliverable work is represented by sub-issues under the gate.
4. `blocked by` relations encode both phase order and cross-project
   prerequisites.
5. The project's last milestone contains manual and interactive acceptance
   only.

Do not create an unparented implementation issue when an existing milestone
gate covers the work. Add a sub-issue or reparent the issue to the appropriate
gate.

## Labels

- `Agentic`: can be implemented and verified non-interactively.
- `Manual validation`: requires a person, installed MSIX, Game Bar, physical
  input, platform hardware, or subjective UX acceptance.
- `Architecture`, `Testing`, `Review`, and `Documentation`: identify the
  discipline involved.
- `Feature`, `Improvement`, and `Bug`: retain their normal product meaning.

`Manual validation` and `Agentic` are mutually exclusive. A manual issue must
be in the final milestone of its project and must be blocked by that project's
automated hardening gate. Finding a manual requirement during earlier work
does not authorize doing it early; create or update the final-phase issue.

## Status workflow

| Status | Meaning |
|--------|---------|
| Backlog | Valid work that is not ready because prioritization or blockers remain. |
| Todo | Unblocked, scoped, and ready to start. |
| In Progress | Implementation is active. |
| In Review | Implementation and required automated verification are complete; an independent clean-context review is active or awaiting disposition. |
| Done | Acceptance criteria are met, every blocker is Done, the change is merged where applicable, evidence is recorded, and the latest independent review has no unresolved findings. |
| Canceled | Work will not be done; the reason and replacement, if any, are recorded. |
| Duplicate | Another Linear issue is the source of truth. |

A blocked dependent issue stays in Backlog. Investigate or fix the dependency
on the blocking issue; do not move or work the dependent issue while any
blocker remains open. Do not start a later milestone merely because an
individual task looks implementable.

## Starting work

Before changing code, tests, workflow files, or living documentation:

1. Find or create the Linear issue.
2. Confirm its initiative, project, milestone, parent gate, labels, priority,
   and acceptance criteria.
3. Confirm every `blocked by` issue is Done.
4. Split the issue if it contains independently reviewable outcomes.
5. Assign the issue and move it to In Progress.
6. Use Linear's generated branch name when creating a branch.

Small review fixes may remain in the current issue when they are necessary to
meet its existing acceptance criteria. New behavior, broader cleanup, or a
separately reviewable change becomes a sub-issue and may block the current
gate.

## Implementation and commits

- Keep one coherent Linear issue per branch unless a gate issue explicitly
  coordinates a tightly coupled change.
- Include the issue identifier in branch and pull-request metadata.
- Before review, commit every intended file on Linear's generated issue branch
  and confirm `git status --short` is empty. Untracked or modified files are not
  a reviewable delivery state.
- Preserve the repository's existing build, runtime, and non-interactive shell
  rules from `AGENTS.md`.
- Update the issue when scope, risk, or a discovered dependency changes.
- Add a blocker relation instead of writing "blocked" only in prose.
- Do not close a gate until every required sub-issue is Done.

## Automated testing evidence

Every implementation issue must state the regression its tests prevent and the
verification appropriate to its layer. Before moving to In Review, add a
Linear comment containing:

```text
Commit: <immutable commit SHA>
Tree: <git rev-parse <commit>^{tree}>
Automated verification:
- <exact command or CI check>: pass/fail
- <exact command or CI check>: pass/fail
Not run:
- <command or environment>: <reason and tracking issue>
Risks / follow-ups:
- <issue links or none>
```

On macOS, use `mise` for .NET. Portable Shared builds and Windows-targeted
cross-compilation are agentic work. Running Windows Desktop tests, building the
full MSIX, installing a package, opening Game Bar, using touch/controller
input, and subjective UX review are different boundaries:

- Authorized Windows CI or a remote build is still automated evidence.
- The required remote MSIX dispatch runs from `main`, so it is evidence only
  for the exact reviewed commit after that commit is present on `main`. It is
  not evidence for an unmerged issue branch; record the full MSIX build as not
  run for that revision.
- Installation and interaction remain manual evidence.
- Do not trigger the manual Build MSIX workflow unless the user explicitly
  authorizes it.

See [Testing](TESTING.md) for the command and platform matrix.

## Pull requests and review

Every pull request must:

- include at least one complete uppercase `TIE-n` identifier in its title or
  body and link its Linear issue;
- summarize the accepted scope;
- list exact automated checks and results;
- identify config, IPC, localization, packaging, or migration risk;
- link any deferred manual validation issue;
- update living documentation when behavior or contracts change.

The required `pr-policy` check accepts the identifier only in the visible pull
request title or the template's explicit `- Issue:` field. Tokens elsewhere in
the body, Markdown reference definitions, HTML comments, placeholders such as
`TIE-`, and lowercase variants do not pass. The check does not call Linear or
use Linear credentials. It runs from the trusted base revision on
`pull_request_target` and never checks out or executes fork code.

The only automatic exception is a pull request authored by
`dependabot[bot]`, from a `dependabot/` branch in this repository. Forks,
similarly named bots, and other automation must provide a TIE issue like any
other contributor.

When implementation and automated verification are complete:

1. Commit every intended change on Linear's generated issue branch.
2. Confirm `git status --short` is empty, then record the immutable commit SHA,
   its tree SHA, and automated evidence in Linear.
3. Move the issue to In Review.
4. Automatically start a separate review session with no inherited
   conversation history. Do not use the implementation session as the sole
   reviewer.
5. Give the reviewer only the Linear issue and acceptance criteria, repository
   instructions, and the exact commit SHA to inspect. The reviewer resolves the
   tree from that commit and does not review a mutable working-tree diff. Do not
   prime it with the implementation session's reasoning or conclusions.
6. The review session reads the relevant code and tests independently and
   reports findings ordered by severity with file and line evidence. It does
   not edit the work unless it is later assigned a separate implementation
   task.
7. Record the review session identity, inspected commit and tree SHAs, findings
   (including an explicit no-findings result), and disposition in a Linear
   comment.

For every review finding:

- fix an in-scope correctness problem on the current issue and return it to In
  Progress while the fix is active;
- create a sub-issue for expanded scope or an independently shippable
  follow-up;
- add blocker relations when the follow-up must land before the current issue
  or gate can complete;
- when a new blocker is added, move the current issue to Backlog and do not
  resume implementation or review until every blocker is Done;
- link the review thread or summarize the decision in Linear.

After any content change, commit it, rerun the required automated verification,
and automatically start another new clean-context review session. The earlier
reviewer context must not be reused. Repeat until the latest review has no
unresolved findings and its reviewed tree SHA matches the proposed final tree.

If the available tooling cannot start a separate clean-context session, keep
the issue in In Review, record the blocker, and request the missing capability.
Do not substitute self-review by the implementation session.

After merge, confirm every blocker is Done and the resulting source tree SHA
matches the tree approved by the latest independent review. Record the
merge/commit and final CI evidence before moving the issue to Done. Reopened
work returns to In Progress with the reason recorded.

## Manual acceptance

Manual acceptance is deliberately deferred so it cannot hold up agentic work.
It begins only after the corresponding automated hardening gate is Done.

Manual issues must record:

- artifact and source commit;
- OS, architecture, Game Bar, package, and relevant hardware versions;
- exact checklist and observed result;
- screenshots/logs where useful;
- a linked Bug issue for each failure.

Do not implement fixes inside a manual acceptance issue. Create a Bug
sub-issue, relate it as a blocker, and return the affected gate to the
appropriate active state.

## Releases

The automated integrated-release gate depends on the automated gates from the
layout, widget, editor, and delivery projects. It intentionally does not wait
for their interactive checks.

The final release gate depends on:

- the automated release candidate;
- Layout Platform manual migration acceptance;
- Widget Experience manual Game Bar acceptance;
- Live Grid Editor manual usability acceptance.

Release approval identifies the final commit and artifacts, dispositions all
release-blocking defects, posts project/initiative status updates, and only
then publishes through the normal release workflow.

## GitHub issue routing

Use GitHub for pull requests, code review, Actions, artifacts, and releases.
Workspace members create new bugs, features, chores, test gaps, documentation
gaps, and review follow-ups directly in Linear.

The public GitHub issue form is an intake bridge for contributors who cannot
access the Linear workspace. For every GitHub intake issue, a maintainer must:

1. create the correctly scoped Linear issue;
2. preserve a link to the original report on the Linear issue;
3. reply with the Linear issue link; and
4. close the GitHub issue without using it for status, planning, or completion.

Maintainers must not use GitHub's privileged blank-issue option to bypass this
route. If a blank GitHub issue is created, mirror and close it using the same
procedure.
