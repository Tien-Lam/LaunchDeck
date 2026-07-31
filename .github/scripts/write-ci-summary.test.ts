import { afterEach, describe, expect, test } from "bun:test";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";

const {
  buildSummary,
  ensureStepLogs,
  parseTrxCounters,
} = require("./write-ci-summary.cjs");

const temporaryDirectories: string[] = [];

afterEach(() => {
  for (const directory of temporaryDirectories.splice(0)) {
    fs.rmSync(directory, { force: true, recursive: true });
  }
});

describe("parseTrxCounters", () => {
  test("extracts readable xUnit totals", () => {
    expect(
      parseTrxCounters(
        '<ResultSummary><Counters total="91" executed="90" passed="88" failed="2" notExecuted="0" notRunnable="0" disconnected="0" /></ResultSummary>',
      ),
    ).toEqual({
      failed: 2,
      passed: 88,
      skipped: 1,
      total: 91,
    });
  });

  test("returns null when no TRX counters exist", () => {
    expect(parseTrxCounters("<ResultSummary />")).toBeNull();
  });
});

describe("ensureStepLogs", () => {
  test("creates an explanatory log only for a command that did not run", () => {
    const directory = fs.mkdtempSync(
      path.join(os.tmpdir(), "launchdeck-ci-evidence-"),
    );
    temporaryDirectories.push(directory);
    const existingLog = path.join(directory, "existing.log");
    fs.writeFileSync(existingLog, "real command output", "utf8");
    const steps = [
      {
        command: "dotnet build Existing.csproj",
        log: "existing.log",
        name: "Existing",
        outcome: "success",
      },
      {
        command: "dotnet test Skipped.csproj",
        log: "skipped.log",
        name: "Skipped",
        outcome: "skipped",
      },
    ];

    ensureStepLogs(directory, steps);

    expect(fs.readFileSync(existingLog, "utf8")).toBe("real command output");
    const placeholder = fs.readFileSync(
      path.join(directory, "skipped.log"),
      "utf8",
    );
    expect(placeholder).toContain("Command: dotnet test Skipped.csproj");
    expect(placeholder).toContain("Outcome: skipped");
    expect(placeholder).toContain("did not produce a log");
  });
});

describe("buildSummary", () => {
  test("names the failed command, log, totals, and retention", () => {
    const summary = buildSummary({
      counters: { failed: 1, passed: 48, skipped: 0, total: 49 },
      retentionDays: 14,
      steps: [
        {
          command: "dotnet build Example.csproj -warnaserror",
          log: "example-build.log",
          name: "Example build",
          outcome: "failure",
        },
      ],
    });

    expect(summary).toContain("| Example build | failure |");
    expect(summary).toContain("dotnet build Example.csproj -warnaserror");
    expect(summary).toContain("example-build.log");
    expect(summary).toContain("| 49 | 48 | 1 | 0 |");
    expect(summary).toContain("retained for 14 days");
  });

  test("explains a missing TRX without hiding build outcomes", () => {
    const summary = buildSummary({
      counters: null,
      retentionDays: 14,
      steps: [],
    });
    expect(summary).toContain("No TRX counters were produced");
  });
});
