import { describe, expect, test } from "bun:test";

const {
  findTieReferences,
  isDependabotException,
  stripHtmlComments,
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
      findTieReferences(
        "TIE-253: Validate metadata",
        "- Issue: TIE-253, TIE-249",
      ),
    ).toEqual(["TIE-253", "TIE-249"]);
  });

  test("rejects placeholders, zero, and lowercase variants", () => {
    expect(findTieReferences("TIE- and TIE-0", "tie-253")).toEqual([]);
  });

  test("ignores complete identifiers inside HTML comments", () => {
    expect(
      findTieReferences("Improve layout", "<!-- Example: TIE-253 -->\nIssue: TIE-"),
    ).toEqual([]);
  });

  test("ignores Markdown reference definitions outside the Issue field", () => {
    expect(
      findTieReferences("Improve layout", "[TIE-253]: https://linear.app/example"),
    ).toEqual([]);
  });

  test("ignores an Issue field inside an unterminated HTML comment", () => {
    expect(
      findTieReferences("Improve layout", "<!-- hidden\n- Issue: TIE-253"),
    ).toEqual([]);
  });

  test("ignores visible references outside the configured Issue field", () => {
    expect(
      findTieReferences("Improve layout", "Related work: TIE-253"),
    ).toEqual([]);
  });
});

describe("stripHtmlComments", () => {
  test("removes multiline comments without removing visible content", () => {
    expect(
      stripHtmlComments("before<!-- hidden\nTIE-253 -->after"),
    ).toBe("beforeafter");
  });

  test("removes an unterminated comment through end of input", () => {
    expect(stripHtmlComments("before<!-- hidden TIE-253")).toBe("before");
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
    expect(result.errors[0]).toContain("`- Issue:` field");
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
