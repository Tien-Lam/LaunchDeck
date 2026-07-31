## Linear

- Issue: TIE- <!-- Required: replace with a complete TIE-<number> identifier. -->
- Project / milestone:
- Parent gate:

Confirm before requesting review:

- [ ] The Linear issue is In Review.
- [ ] Its project, milestone, parent, labels, and acceptance criteria are correct.
- [ ] All blocking issues are Done.

## Summary

Describe the user-visible or engineering outcome and any intentionally excluded
scope.

## Automated verification

List exact commands and results:

```text
<command or CI check>: pass/fail
```

- [ ] New or changed behavior has regression coverage where it can be tested
      non-interactively.
- [ ] Relevant documentation and localization resources are updated.
- [ ] Config, migration, IPC, packaging, and release risks are called out below.

## Independent review

- Risk areas:
- Migration / compatibility:
- Follow-up issues:
- Review session:
- Reviewed commit SHA:
- Reviewed tree SHA:
- Review result:

Actionable feedback that expands scope must become a Linear sub-issue or
blocker rather than an untracked checklist item.

- [ ] After implementation and automated verification, a separate review
      session was started automatically without inherited conversation context.
- [ ] Every intended file was committed and `git status --short` was empty
      before review started.
- [ ] The reviewer received the Linear issue, repository instructions, and
      exact commit SHA, and independently inspected that immutable commit.
- [ ] All findings and dispositions are recorded on the Linear issue.
- [ ] The latest independent review has no unresolved findings.
- [ ] The proposed final tree SHA matches the latest independently reviewed
      tree SHA.

## Manual validation

- [ ] Not required for this issue or milestone.
- [ ] Deferred to final-phase issue: TIE-

Do not perform interactive Game Bar, installed-MSIX, touch/controller, or
subjective UX acceptance early. Those checks belong to the final milestone of
the relevant Linear project.

## Completion

- [ ] Final CI evidence will be recorded on the Linear issue.
- [ ] Every blocking issue is Done.
- [ ] The Linear issue will move to Done only after acceptance criteria are met
      and this change is merged.
