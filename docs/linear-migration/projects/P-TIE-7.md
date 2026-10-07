# LaunchDeck — Live Grid Editor

<!-- linear-source:f9203484-a125-48ed-8454-e27876c58be9 -->

Migrated from Linear on 8 October 2026. This preserves the original specification and dated history; historical workflow instructions and earlier pending statuses describe their original context. Current work is tracked in [GitHub Issues](https://github.com/Tien-Lam/LaunchDeck/issues) and [Projects](https://github.com/users/Tien-Lam/projects/3).

## Outcome

Replace the ordered card list with a live editor that previews the same layout the widget will render.

## Scope

- Page sidebar, live preview, and selected-item inspector.
- Inline and suggested naming.
- Drag reorder, keyboard movement, resize presets/handles, and undo/redo.
- Layout controls and preview-width modes.
- Page create/rename/reorder/delete/duplicate and cross-page movement.
- Save/dirty-state integrity, lazy icon loading, localization, tests, and documentation.

## Sequencing rule

Shared schema and layout behavior are prerequisites. Mouse/touch/keyboard usability validation is kept in the final manual milestone.

Original summary: Three-pane live editor with direct naming, drag movement, resizing, page management, and undo/redo.

<details>
<summary>Original metadata and resource index</summary>

```json
{
  "id": "P-TIE-7",
  "uuid": "f9203484-a125-48ed-8454-e27876c58be9",
  "icon": "Rocket",
  "color": "#5E6AD2",
  "name": "LaunchDeck — Live Grid Editor",
  "summary": "Three-pane live editor with direct naming, drag movement, resizing, page management, and undo/redo.",
  "url": "https://linear.app/tienlam/project/launchdeck-live-grid-editor-c5378a958725",
  "resourceCount": 0,
  "createdAt": "2026-07-29T08:22:48.782Z",
  "updatedAt": "2026-07-29T08:22:48.782Z",
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
      "id": "220ea761-72c4-4602-a250-d41e845f8d79",
      "name": "Phase 1 — Live Editor Shell",
      "description": "Three-pane WPF shell, shared-layout preview, selection, inspector, and active-page icon loading are implemented.",
      "targetDate": null,
      "progress": "0%"
    },
    {
      "id": "9c600fc3-3ec2-4530-b476-57303926643b",
      "name": "Phase 2 — Direct Manipulation",
      "description": "Inline naming, drag/keyboard movement, tile resizing, and undo/redo are implemented.",
      "targetDate": null,
      "progress": "0%"
    },
    {
      "id": "ed48b974-6dd2-4dc8-9c73-6364119d6106",
      "name": "Phase 3 — Page & Layout Management",
      "description": "Page lifecycle, cross-page movement, layout controls, responsive preview modes, and save integration are implemented.",
      "targetDate": null,
      "progress": "0%"
    },
    {
      "id": "7eab275d-5661-459e-a127-ed80d8974739",
      "name": "Phase 4 — Automated Editor Hardening",
      "description": "Automated editor behavior coverage, localization, performance safeguards, accessibility metadata, and documentation are complete.",
      "targetDate": null,
      "progress": "0%"
    },
    {
      "id": "fed1e957-76e2-48d0-82c0-c5b0f574d236",
      "name": "Phase 5 — Manual Editor Acceptance",
      "description": "Final project phase. Mouse, touch, keyboard, scaling, localization, and subjective usability checks only.",
      "targetDate": null,
      "progress": "0%"
    }
  ]
}
```

</details>
