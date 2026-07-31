import { describe, expect, test } from "bun:test";

const {
  buildSummary,
  parseTrxCounters,
} = require("./write-ci-summary.cjs");

describe("parseTrxCounters", () => {
  test("extracts readable xUnit totals", () => {
    expect(
      parseTrxCounters(
        '<ResultSummary><Counters total="91" executed="90" passed="88" failed="2" notExecuted="1" notRunnable="0" disconnected="0" /></ResultSummary>',
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
