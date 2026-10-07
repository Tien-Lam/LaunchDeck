# GitHub work and delivery

[Issues](https://github.com/Tien-Lam/LaunchDeck/issues) own scope, decisions, ownership and acceptance evidence. [The project board](https://github.com/users/Tien-Lam/projects/3) owns status, priority and workstream. Repository milestones retain delivery phases. Pull requests, checks and releases remain the code and publication record. [Migration index](linear-migration/README.md) maps historical Linear IDs and preserves specifications and dated history.

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

## Imported history

Original author/date/state information is attributed explicitly because GitHub creation timestamps are migration timestamps. The archived label preserves Linear’s archive flag. Native sub-issues, dependencies and related links retain execution structure. Historical source documents retain their original context, including earlier workflow wording and bounded acceptance evidence. Current policy is this document plus AGENTS.md and the repository’s canonical testing/review/publishing guides.
