# GitHub work and delivery

Use the following GitHub products for **LaunchDeck**. [The migration index](linear-migration/README.md) maps historical Linear IDs and preserves specifications and dated history.

| Product                                                                           | Responsibility                                                                                      |
| --------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| [GitHub Issues](https://github.com/Tien-Lam/LaunchDeck/issues)                    | Scope, bugs/features, ownership, decisions, native sub-issues/dependencies and acceptance evidence. |
| [GitHub Projects — LaunchDeck](https://github.com/users/Tien-Lam/projects/3)      | Status, priority and workstream. Use this existing board for the repository.                        |
| [GitHub repository milestones](https://github.com/Tien-Lam/LaunchDeck/milestones) | Delivery phases and their exit gates.                                                               |
| [GitHub Pull Requests](https://github.com/Tien-Lam/LaunchDeck/pulls)              | Review and merge of code and Markdown documentation.                                                |
| [GitHub Actions](https://github.com/Tien-Lam/LaunchDeck/actions)                  | Automated checks, builds and authorized publication through the repository's existing workflows.    |
| [GitHub Releases](https://github.com/Tien-Lam/LaunchDeck/releases)                | Versioned release notes and distributable artifacts when the product has a release.                 |
| [GitHub Wiki](https://github.com/Tien-Lam/LaunchDeck/wiki)                        | Navigation to canonical repository docs and work tracking.                                          |

## Issue lifecycle

- Read the issue and discussion before implementing. Confirm workstream, milestone, parent gate, labels, priority, acceptance criteria and native blocked-by relations.
- Keep independently deliverable work in native sub-issues. Add native issue dependencies for prerequisites; do not start work while required blockers remain unfinished.
- Assign active work and set Project Status to In Progress. Use a short-lived codex/ branch containing the GitHub issue number.
- Record scope changes, decisions and blockers on the GitHub issue. Keep acceptance evidence and capability limits explicit. A migration or merge alone does not satisfy acceptance.
- Commit intended files and record exact commit/tree SHAs plus appropriate verification before In Review. Use the repository’s independent review requirements and resolve findings.
- Close as completed and set Done only after required acceptance, checks, review and merge. Canceled work closes as not planned; retain its reason and replacement. Keep unresolved acceptance gaps open.
- Manual validation remains in the workstream’s final milestone, after its automated gates. Preserve the repository’s native input, installed-device and release authorization boundaries.

## Pull requests

Link the owning GitHub issue and summarize the concrete outcome, verification, risks and review evidence. Legacy TIE identifiers are historical aliases; the cross-reference maps them to GitHub. New work uses GitHub issue numbers. Full temporary reports and captures remain in ignored local work folders; save concise durable evidence on the issue and PR. Do not add transient status tables outside the project board.

## Documentation

Keep README.md, AGENTS.md and repository Markdown documentation (normally docs/) canonical and review changes through GitHub Pull Requests. Update the existing document alongside the related implementation; keep specifications, architecture, setup, testing, review and publishing procedures with the source. Operational runbooks belong in Tien-Lam/runbooks. An existing GitHub Wiki is a navigation index linking to these documents, GitHub Issues and GitHub Projects; do not duplicate specifications, workflows or live task status there. Put temporary plans, full QA captures and full review reports in ignored local work folders, and concise durable acceptance evidence on the issue and PR.

GitHub Issues and GitHub Projects own current work. Do not create or update Linear tasks or mirror contributor reports into Linear. Preserve historical imported documents, IDs, attribution and dated evidence unchanged; the migration index resolves old IDs.

## Imported history

Original author/date/state information is attributed explicitly because GitHub creation timestamps are migration timestamps. The archived label preserves Linear’s archive flag. Native sub-issues, dependencies and related links retain execution structure. Historical source documents retain their original context, including earlier workflow wording and bounded acceptance evidence. Current policy is this document plus AGENTS.md and the repository’s canonical testing/review/publishing guides.
