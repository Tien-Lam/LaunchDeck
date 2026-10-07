# LaunchDeck — Widget Experience

<!-- linear-source:095ee650-dc0b-463a-9571-c3889e4399b1 -->

Migrated from Linear on 8 October 2026. This preserves the original specification and dated history; historical workflow instructions and earlier pending statuses describe their original context. Current work is tracked in [GitHub Issues](https://github.com/Tien-Lam/LaunchDeck/issues) and [Projects](https://github.com/users/Tien-Lam/projects/3).

## Outcome

Render the versioned layout faithfully inside Xbox Game Bar across supported widget sizes.

## Scope

- Replace the fixed GridView/ItemsWrapGrid renderer.
- Render variable tile spans using shared placements.
- Preserve icons, launch behavior, feedback, lifecycle, and configuration reloads.
- Add named page navigation, current/default page behavior, and empty states.
- Provide deterministic keyboard/controller directional focus.
- Complete localization, automated view-model coverage, and documentation.

## Sequencing rule

The layout platform gates runtime implementation. Installed-MSIX and Game Bar interaction are isolated to this project's final manual milestone.

Original summary: Variable-size responsive Game Bar grid, page navigation, accessible focus, and runtime hardening.

<details>
<summary>Original metadata and resource index</summary>

```json
{
  "id": "P-TIE-8",
  "uuid": "095ee650-dc0b-463a-9571-c3889e4399b1",
  "icon": "Rocket",
  "color": "#27AE60",
  "name": "LaunchDeck — Widget Experience",
  "summary": "Variable-size responsive Game Bar grid, page navigation, accessible focus, and runtime hardening.",
  "url": "https://linear.app/tienlam/project/launchdeck-widget-experience-d9ec2aa72a08",
  "resourceCount": 0,
  "createdAt": "2026-07-29T08:22:50.666Z",
  "updatedAt": "2026-07-29T08:22:50.666Z",
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
      "id": "2e5b3ac6-d248-4cb2-950c-7bf03fc564de",
      "name": "Phase 1 — Variable Grid Runtime",
      "description": "Widget renders shared placements and variable tile spans while retaining launch, icon, feedback, and lifecycle behavior.",
      "targetDate": null,
      "progress": "0%"
    },
    {
      "id": "72534e2c-5e76-4a2b-8f27-4d4453072aa0",
      "name": "Phase 2 — Pages & Focus Navigation",
      "description": "Named pages, state restoration, empty states, and deterministic keyboard/controller focus are implemented.",
      "targetDate": null,
      "progress": "0%"
    },
    {
      "id": "98694c99-e288-45e2-bd51-da925ee8cb12",
      "name": "Phase 3 — Automated Runtime Hardening",
      "description": "Automated view-model/layout coverage, localization, diagnostics, and runtime documentation are complete.",
      "targetDate": null,
      "progress": "0%"
    },
    {
      "id": "709524fa-27cd-4755-96f1-de68734c536a",
      "name": "Phase 4 — Manual Widget Acceptance",
      "description": "Final project phase. Installed-MSIX, Xbox Game Bar, controller, resize, pinning, and launch checks only.",
      "targetDate": null,
      "progress": "0%"
    }
  ]
}
```

</details>
