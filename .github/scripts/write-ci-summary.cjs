"use strict";

const fs = require("node:fs");
const path = require("node:path");

function parseTrxCounters(xml) {
  const countersTag = xml.match(/<Counters\b([^>]*)\/?>/);
  if (!countersTag) {
    return null;
  }

  const counters = {};
  for (const match of countersTag[1].matchAll(/([A-Za-z]+)="(\d+)"/g)) {
    counters[match[1]] = Number.parseInt(match[2], 10);
  }

  return {
    failed: counters.failed ?? 0,
    passed: counters.passed ?? 0,
    skipped: Math.max(
      0,
      (counters.total ?? 0) -
        (counters.passed ?? 0) -
        (counters.failed ?? 0),
    ),
    total: counters.total ?? 0,
  };
}

function findFile(root, extension) {
  if (!fs.existsSync(root)) {
    return null;
  }

  for (const entry of fs.readdirSync(root, { withFileTypes: true })) {
    const entryPath = path.join(root, entry.name);
    if (entry.isDirectory()) {
      const nested = findFile(entryPath, extension);
      if (nested) {
        return nested;
      }
    } else if (entry.name.toLowerCase().endsWith(extension)) {
      return entryPath;
    }
  }

  return null;
}

function buildSummary({ counters, retentionDays, steps }) {
  const lines = [
    "# LaunchDeck CI evidence",
    "",
    "| Step | Outcome | Exact command | Captured log |",
    "|---|---|---|---|",
    ...steps.map(
      (step) =>
        `| ${step.name} | ${step.outcome} | \`${step.command}\` | \`${step.log}\` |`,
    ),
    "",
    "## xUnit results",
    "",
  ];

  if (counters) {
    lines.push(
      "| Total | Passed | Failed | Skipped |",
      "|---:|---:|---:|---:|",
      `| ${counters.total} | ${counters.passed} | ${counters.failed} | ${counters.skipped} |`,
    );
  } else {
    lines.push(
      "No TRX counters were produced. Inspect the named build/test outcome and captured log above.",
    );
  }

  lines.push(
    "",
    `The complete \`launchdeck-ci-evidence-<run-attempt>\` artifact is retained for ${retentionDays} days on success and failure.`,
    "",
  );
  return lines.join("\n");
}

function ensureStepLogs(evidenceDirectory, steps) {
  for (const step of steps) {
    const logPath = path.join(evidenceDirectory, step.log);
    if (!fs.existsSync(logPath)) {
      fs.writeFileSync(
        logPath,
        [
          `Command: ${step.command}`,
          `Outcome: ${step.outcome}`,
          "The command did not produce a log. It was skipped or setup failed before execution.",
          "",
        ].join("\n"),
        "utf8",
      );
    }
  }
}

function main() {
  const evidenceDirectory =
    process.env.CI_EVIDENCE_DIRECTORY ?? "artifacts/ci-evidence";
  const trx = findFile(evidenceDirectory, ".trx");
  const counters = trx
    ? parseTrxCounters(fs.readFileSync(trx, "utf8"))
    : null;
  const steps = [
    {
      command:
        "bun test ./.github/scripts/pr-policy.test.ts ./.github/scripts/write-ci-summary.test.ts",
      log: "workflow-script-tests.log",
      name: "Workflow script tests",
      outcome: process.env.POLICY_OUTCOME ?? "unknown",
    },
    {
      command:
        "dotnet build LaunchDeck.Shared/LaunchDeck.Shared.csproj --configuration Release -warnaserror",
      log: "shared-build.log",
      name: "Shared build",
      outcome: process.env.SHARED_OUTCOME ?? "unknown",
    },
    {
      command:
        "dotnet build LaunchDeck.Companion/LaunchDeck.Companion.csproj --configuration Release -warnaserror",
      log: "companion-build.log",
      name: "Companion build",
      outcome: process.env.COMPANION_OUTCOME ?? "unknown",
    },
    {
      command:
        "dotnet build LaunchDeck.Tests/LaunchDeck.Tests.csproj --configuration Release -warnaserror",
      log: "tests-build.log",
      name: "Tests build",
      outcome: process.env.TESTS_BUILD_OUTCOME ?? "unknown",
    },
    {
      command:
        'dotnet test LaunchDeck.Tests/LaunchDeck.Tests.csproj --configuration Release --no-build --verbosity normal --logger "trx;LogFileName=launchdeck-tests.trx" --results-directory artifacts/ci-evidence/test-results',
      log: "xunit-test.log",
      name: "xUnit",
      outcome: process.env.XUNIT_OUTCOME ?? "unknown",
    },
  ];
  const summary = buildSummary({
    counters,
    retentionDays: 14,
    steps,
  });

  fs.mkdirSync(evidenceDirectory, { recursive: true });
  ensureStepLogs(evidenceDirectory, steps);
  fs.writeFileSync(
    path.join(evidenceDirectory, "ci-summary.md"),
    summary,
    "utf8",
  );
  if (process.env.GITHUB_STEP_SUMMARY) {
    fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, summary, "utf8");
  }
}

if (require.main === module) {
  main();
}

module.exports = {
  buildSummary,
  ensureStepLogs,
  findFile,
  parseTrxCounters,
};
