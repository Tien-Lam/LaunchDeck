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
  test("finds unique uppercase Linear references", () => {
    expect(
      findTieReferences("TIE-253: Validate metadata", "Related to TIE-253 and TIE-249"),
    ).toEqual(["TIE-253", "TIE-249"]);
  });

  test("rejects placeholders, zero, and lowercase variants", () => {
    expect(findTieReferences("TIE- and TIE-0", "tie-253")).toEqual([]);
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

  test("accepts an issue in the body", () => {
    expect(
      validatePullRequest({
        ...baseMetadata,
        body: "- Issue: TIE-253",
      }).errors,
    ).toEqual([]);
  });

  test("returns actionable guidance when the reference is missing", () => {
    const result = validatePullRequest(baseMetadata);

    expect(result.errors).toHaveLength(1);
    expect(result.errors[0]).toContain("TIE-253");
    expect(result.errors[0]).toContain("title or body");
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
