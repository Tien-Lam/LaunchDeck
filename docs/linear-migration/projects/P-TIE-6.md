# LaunchDeck — Delivery & Quality

<!-- linear-source:504f38b9-b940-4fc8-a23f-e37c0e123c94 -->

Migrated from Linear on 8 October 2026. This preserves the original specification and dated history; historical workflow instructions and earlier pending statuses describe their original context. Current work is tracked in [GitHub Issues](https://github.com/Tien-Lam/LaunchDeck/issues) and [Projects](https://github.com/users/Tien-Lam/projects/3).

## Outcome

Make Linear the repository's source of truth and turn automated review/testing evidence into release gates.

## Scope

- Repository workflow and agent instructions.
- Pull request and GitHub issue-routing templates.
- Linear-linked branch, review, testing, blocker, and completion conventions.
- CI/release evidence, documentation audit, localization completeness, and release notes.
- Cross-project integration gate and final signed-MSIX acceptance.

## Sequencing rule

Process adoption begins immediately. Final interactive release acceptance remains last and cannot block agentic implementation in the feature projects.

Original summary: Linear-first workflow, review and CI gates, documentation, release readiness, and final acceptance.

<details>
<summary>Original metadata and resource index</summary>

```json
{
  "id": "P-TIE-6",
  "uuid": "504f38b9-b940-4fc8-a23f-e37c0e123c94",
  "icon": "Rocket",
  "color": "#F2994A",
  "name": "LaunchDeck — Delivery & Quality",
  "summary": "Linear-first workflow, review and CI gates, documentation, release readiness, and final acceptance.",
  "url": "https://linear.app/tienlam/project/launchdeck-delivery-and-quality-419de99cfa4e",
  "resourceCount": 0,
  "createdAt": "2026-07-29T08:22:47.010Z",
  "updatedAt": "2026-07-29T10:33:13.033Z",
  "startedAt": "2026-07-29T10:33:13.031Z",
  "completedAt": null,
  "canceledAt": null,
  "startDate": "2026-07-29",
  "startDateResolution": null,
  "targetDate": null,
  "targetDateResolution": null,
  "priority": {
    "value": 3,
    "name": "Medium"
  },
  "labels": [],
  "initiatives": [
    {
      "id": "f8b44f52-4575-43a2-be83-ac977f7c6326",
      "name": "LaunchDeck"
    }
  ],
  "lead": {
    "id": "9eae46f7-c527-49bc-8d6a-465118651013",
    "name": "Tien Long Lam"
  },
  "leadTeam": {
    "id": "7a88d71d-eac6-404e-a83d-f3b37d5c6efa",
    "name": "Tien's Team",
    "key": "TIE"
  },
  "status": {
    "id": "01fba55f-4f73-49d5-98ad-fb7eae790683",
    "name": "In Progress",
    "type": "started"
  },
  "teams": [
    {
      "id": "7a88d71d-eac6-404e-a83d-f3b37d5c6efa",
      "name": "Tien's Team",
      "key": "TIE"
    }
  ],
  "members": [
    {
      "id": "9eae46f7-c527-49bc-8d6a-465118651013",
      "name": "Tien Long Lam",
      "email": "lamtienlong9@gmail.com"
    }
  ],
  "milestones": [
    {
      "id": "319eb131-56ce-4b5d-a94d-0ee6c31d7f8f",
      "name": "Phase 1 — Linear Workflow Adoption",
      "description": "Repository instructions, templates, and documentation make Linear the source of truth.",
      "targetDate": null,
      "progress": "100%"
    },
    {
      "id": "d2a8347f-246f-4ccb-996c-bc8cfb6ea1f6",
      "name": "Phase 2 — Automated Review & Test Gates",
      "description": "CI, PR evidence, automated validation, and review-follow-up conventions are enforced.",
      "targetDate": null,
      "progress": "100%"
    },
    {
      "id": "6f5caf65-d6b0-4a9f-b1b1-1b02ec389a49",
      "name": "Phase 3 — Integrated Release Readiness",
      "description": "Cross-project automated integration, docs, localization, migration notes, and release packaging are ready.",
      "targetDate": null,
      "progress": "0%"
    },
    {
      "id": "7d04d11d-71aa-40ce-9d26-b7ac7802ae8f",
      "name": "Phase 4 — Manual Release Acceptance",
      "description": "Final project phase. Signed MSIX installation, Game Bar end-to-end acceptance, and release approval only.",
      "targetDate": null,
      "progress": "0%"
    }
  ]
}
```

</details>

## Dated status updates

### 2026-07-29T10:33:17.065Z — Tien Long Lam

<!-- linear-source:78e06bfc-e1e3-48d4-841e-8def4bb9b4f2 -->

Phase 1 is active.

The Linear hierarchy is complete (TIE-250). Repository rules, workflow documentation, PR template, GitHub issue routing, and testing evidence policy are implemented under TIE-247. Local diff validation passes; the issue remains open until review and merge.

Changes recorded with this update:

**Status**: In Progress
**Priority**: Medium
**Lead**: Tien Long Lam assigned
**Start date** set to Jul 29th

_Progress since Jul 29_:
— **Phase 1 — Linear Workflow Adoption** added: 50%

<details>
<summary>Update provenance</summary>

```json
{
  "id": "78e06bfc-e1e3-48d4-841e-8def4bb9b4f2",
  "health": "onTrack",
  "url": "https://linear.app/tienlam/project/launchdeck-delivery-and-quality-419de99cfa4e/activity#project-update-78e06bfc",
  "createdAt": "2026-07-29T10:33:17.065Z",
  "updatedAt": "2026-07-29T10:33:18.980Z",
  "editedAt": null,
  "archivedAt": null,
  "isDiffHidden": false,
  "user": {
    "id": "9eae46f7-c527-49bc-8d6a-465118651013",
    "name": "Tien Long Lam"
  },
  "type": "project",
  "project": {
    "id": "504f38b9-b940-4fc8-a23f-e37c0e123c94",
    "name": "LaunchDeck — Delivery & Quality"
  }
}
```

</details>

---

### 2026-07-31T00:39:00.135Z — Tien Long Lam

<!-- linear-source:1ec29f31-df54-492d-85bc-8d0915ea1d16 -->

Phase 1 — Linear Workflow Adoption is complete. TIE-247 merged through GitHub PR #1 with successful CI and an independently approved tree match; gate TIE-204 is Done. Phase 2 gate TIE-202 is unblocked and in Todo.

Changes recorded with this update:

_Progress since Jul 29_:
— **Phase 1 — Linear Workflow Adoption**: 50% → 100%

<details>
<summary>Update provenance</summary>

```json
{
  "id": "1ec29f31-df54-492d-85bc-8d0915ea1d16",
  "health": "onTrack",
  "url": "https://linear.app/tienlam/project/launchdeck-delivery-and-quality-419de99cfa4e/activity#project-update-1ec29f31",
  "createdAt": "2026-07-31T00:39:00.135Z",
  "updatedAt": "2026-07-31T00:39:01.725Z",
  "editedAt": null,
  "archivedAt": null,
  "isDiffHidden": false,
  "user": {
    "id": "9eae46f7-c527-49bc-8d6a-465118651013",
    "name": "Tien Long Lam"
  },
  "type": "project",
  "project": {
    "id": "504f38b9-b940-4fc8-a23f-e37c0e123c94",
    "name": "LaunchDeck — Delivery & Quality"
  }
}
```

</details>
