import { describe, expect, test } from "bun:test";

const {
  findTieReferences,
  isDependabotException,
  validatePullRequest,
} = require("./pr-policy.cjs");

const baseMetadata = {
  actor: "contributor",
  baseRepoFullName: "Tien-Lam/LaunchDeck",
  body: "",
  headRef: "feature/layout",
  headRepoFullName: "contributor/LaunchDeck",
  title: "Improve layout",
};

describe("findTieReferences", () => {
  test("finds an uppercase Linear reference at the start of the title", () => {
    expect(findTieReferences("TIE-253: Validate metadata")).toEqual(["TIE-253"]);
  });

  test.each([
    ["placeholder", "TIE- Improve layout"],
    ["zero", "TIE-0 Improve layout"],
    ["lowercase", "tie-253 Improve layout"],
    ["not first", "Improve layout TIE-253"],
    ["bidi prefix", "\u202eTIE-253 Improve layout"],
    ["bidi identifier", "TIE-\u202e253 Improve layout"],
  ])("rejects %s", (_name, title) => {
    expect(findTieReferences(title)).toEqual([]);
  });
});

describe("Dependabot exception", () => {
  test("allows only same-repository Dependabot branches", () => {
    expect(
      isDependabotException({
        ...baseMetadata,
        actor: "dependabot[bot]",
        headRef: "dependabot/nuget/xunit-3.0.0",
        headRepoFullName: "Tien-Lam/LaunchDeck",
      }),
    ).toBe(true);
  });

  test.each([
    ["wrong actor", { actor: "renovate[bot]" }],
    ["wrong branch", { headRef: "feature/dependency-update" }],
    ["fork repository", { headRepoFullName: "attacker/LaunchDeck" }],
  ])("rejects %s", (_name, override) => {
    expect(
      isDependabotException({
        ...baseMetadata,
        actor: "dependabot[bot]",
        headRef: "dependabot/nuget/xunit-3.0.0",
        headRepoFullName: "Tien-Lam/LaunchDeck",
        ...override,
      }),
    ).toBe(false);
  });
});

describe("validatePullRequest", () => {
  test("accepts an issue in the title", () => {
    expect(
      validatePullRequest({
        ...baseMetadata,
        title: "TIE-253 Require Linear metadata",
      }),
    ).toEqual({
      errors: [],
      exempt: false,
      tieReferences: ["TIE-253"],
    });
  });

  test("rejects body-only references", () => {
    const bodyVariants = [
      "- Issue: TIE-253",
      "[](TIE-253)",
      "[TIE-253]: https://linear.app/example",
      "<details>\nTIE-253\n</details>",
      "```\nTIE-253\n```",
    ];

    for (const body of bodyVariants) {
      expect(
        validatePullRequest({
          ...baseMetadata,
          body,
        }).errors,
      ).toHaveLength(1);
    }
  });

  test("returns actionable guidance when the reference is missing", () => {
    const result = validatePullRequest(baseMetadata);

    expect(result.errors).toHaveLength(1);
    expect(result.errors[0]).toContain("TIE-253");
    expect(result.errors[0]).toContain("Start the pull request title");
  });

  test("rejects the untouched repository pull request template", async () => {
    const template = await Bun.file(
      ".github/pull_request_template.md",
    ).text();
    const result = validatePullRequest({
      ...baseMetadata,
      body: template,
    });

    expect(result.tieReferences).toEqual([]);
    expect(result.errors).toHaveLength(1);
  });

  test("exempts a valid Dependabot pull request", () => {
    expect(
      validatePullRequest({
        ...baseMetadata,
        actor: "dependabot[bot]",
        headRef: "dependabot/nuget/xunit-3.0.0",
        headRepoFullName: "Tien-Lam/LaunchDeck",
      }),
    ).toEqual({
      errors: [],
      exempt: true,
      tieReferences: [],
    });
  });
});
