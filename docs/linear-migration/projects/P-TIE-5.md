# LaunchDeck — Layout Platform

<!-- linear-source:d52a210c-f004-4a0e-99e9-65043f938fa3 -->

Migrated from Linear on 8 October 2026. This preserves the original specification and dated history; historical workflow instructions and earlier pending statuses describe their original context. Current work is tracked in [GitHub Issues](https://github.com/Tien-Lam/LaunchDeck/issues) and [Projects](https://github.com/users/Tien-Lam/projects/3).

## Outcome

Provide the shared configuration and placement foundation used by both LaunchDeck renderers.

## Scope

- Schema v2 with pages, stable IDs, layout settings, and tile spans.
- Backwards-compatible v1 normalization and safe atomic persistence.
- Desktop serializer and UWP manual-parser parity.
- Deterministic first-fit responsive grid engine.
- Validation, fixtures, migration tests, property/invariant tests, and documentation.

## Sequencing rule

All implementation and automated verification precede the final manual migration milestone. This project blocks widget and editor layout implementation.

Original summary: Schema v2, safe migration, responsive placement engine, and automated layout confidence.

<details>
<summary>Original metadata and resource index</summary>

```json
{
  "id": "P-TIE-5",
  "uuid": "d52a210c-f004-4a0e-99e9-65043f938fa3",
  "icon": "Database",
  "color": "#8B5CF6",
  "name": "LaunchDeck — Layout Platform",
  "summary": "Schema v2, safe migration, responsive placement engine, and automated layout confidence.",
  "url": "https://linear.app/tienlam/project/launchdeck-layout-platform-4454546079d8",
  "resourceCount": 0,
  "createdAt": "2026-07-29T08:22:27.615Z",
  "updatedAt": "2026-07-29T08:22:27.615Z",
  "startedAt": null,
  "completedAt": null,
  "canceledAt": null,
  "startDate": null,
  "startDateResolution": null,
  "targetDate": null,
  "targetDateResolution": null,
  "priority": {
    "value": 2,
    "name": "High"
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
    "id": "bb3163de-7472-4d4e-a7af-5d15edf04b7b",
    "name": "Backlog",
    "type": "backlog"
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
      "id": "5281644f-8a77-4c67-b48f-300c620165fb",
      "name": "Phase 1 — Schema v2 & Persistence Safety",
      "description": "Versioned pages/layout/tile schema, v1 migration, parser parity, validation, and atomic persistence are implemented and automatically verified.",
      "targetDate": null,
      "progress": "0%"
    },
    {
      "id": "3391449c-a625-488a-a1ef-3b6360e67216",
      "name": "Phase 2 — Layout Engine & Automated Verification",
      "description": "Shared deterministic responsive placement engine and automated invariant coverage are complete.",
      "targetDate": null,
      "progress": "0%"
    },
    {
      "id": "b0cfa00e-2aae-4e5d-8f14-bc7dfd39de5d",
      "name": "Phase 3 — Manual Migration Acceptance",
      "description": "Final project phase. Manually inspect representative real-world upgrades and recovery on Windows; contains only interactive acceptance work.",
      "targetDate": null,
      "progress": "0%"
    }
  ]
}
```

</details>
