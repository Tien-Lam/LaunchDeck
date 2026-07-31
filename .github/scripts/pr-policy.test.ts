import { describe, expect, test } from "bun:test";

const {
  extractReviewEvidence,
  findTieReferences,
  isDependabotException,
  stripHtmlComments,
  unwrapCodeSpan,
  validatePullRequest,
} = require("./pr-policy.cjs");

const headSha = "1".repeat(40);
const headTreeSha = "2".repeat(40);

const baseMetadata = {
  actor: "contributor",
  baseRepoFullName: "Tien-Lam/LaunchDeck",
  body: "",
  headRef: "feature/layout",
  headRepoFullName: "contributor/LaunchDeck",
  headSha,
  headTreeSha,
  isDraft: true,
  title: "Improve layout",
};

const validReviewBody = `## Independent review

- Risk areas: none
- Migration / compatibility: none
- Follow-up issues: none
- Review session: \`/root/tie248_review\`
- Reviewed commit SHA: \`${headSha}\`
- Reviewed tree SHA: \`${headTreeSha}\`
- Review result: no findings

## Manual validation
`;

describe("findTieReferences", () => {
  test("finds an uppercase Linear reference at the start of the title", () => {
    expect(findTieReferences("TIE-253 Validate metadata")).toEqual(["TIE-253"]);
    expect(findTieReferences("TIE-253")).toEqual(["TIE-253"]);
  });

  test.each([
    ["placeholder", "TIE- Improve layout"],
    ["zero", "TIE-0 Improve layout"],
    ["lowercase", "tie-253 Improve layout"],
    ["not first", "Improve layout TIE-253"],
    ["bidi prefix", "\u202eTIE-253 Improve layout"],
    ["bidi identifier", "TIE-\u202e253 Improve layout"],
    ["colon delimiter", "TIE-253: Improve layout"],
    ["tab delimiter", "TIE-253\tImprove layout"],
  ])("rejects %s", (_name, title) => {
    expect(findTieReferences(title)).toEqual([]);
  });

  test.each([
    ["Latin letter", "é"],
    ["CJK letter", "中"],
    ["combining mark", "\u0301"],
    ["zero-width space", "\u200b"],
    ["left-to-right mark", "\u200e"],
    ["right-to-left override", "\u202e"],
    ["left-to-right isolate", "\u2066"],
    ["right-to-left isolate", "\u2067"],
    ["first strong isolate", "\u2068"],
    ["pop directional isolate", "\u2069"],
    ["byte order mark", "\ufeff"],
    ["NUL", "\u0000"],
    ["DEL", "\u007f"],
  ])("rejects a %s immediately after the identifier", (_name, suffix) => {
    expect(findTieReferences(`TIE-253${suffix} Improve layout`)).toEqual([]);
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

describe("review evidence parsing", () => {
  test("extracts one plain top-level field set", () => {
    expect(extractReviewEvidence(validReviewBody)).toEqual({
      errors: [],
      fields: {
        "Review session": "/root/tie248_review",
        "Reviewed commit SHA": headSha,
        "Reviewed tree SHA": headTreeSha,
        "Review result": "no findings",
      },
    });
  });

  test("rejects duplicate and missing fields", () => {
    const duplicate = validReviewBody.replace(
      "- Review result: no findings",
      "- Review result: no findings\n- Review result: no findings",
    );
    expect(extractReviewEvidence(duplicate).errors).toContain(
      "The independent-review section must contain exactly one `- Review result:` field.",
    );
    expect(
      extractReviewEvidence(
        validReviewBody.replace(`- Reviewed tree SHA: \`${headTreeSha}\`\n`, ""),
      ).errors,
    ).toContain(
      "The independent-review section must contain exactly one `- Reviewed tree SHA:` field.",
    );
  });

  test.each([
    [
      "code fence",
      `\`\`\`\n${validReviewBody}\n\`\`\``,
    ],
    [
      "details block",
      `<details>\n${validReviewBody}\n</details>`,
    ],
    [
      "HTML comment",
      `<!--\n${validReviewBody}\n-->`,
    ],
  ])("rejects evidence hidden in a %s", (_name, body) => {
    expect(extractReviewEvidence(body).errors.length).toBeGreaterThan(0);
  });

  test("unwraps either a plain value or one code span", () => {
    expect(unwrapCodeSpan(" value ")).toBe("value");
    expect(unwrapCodeSpan(" `value` ")).toBe("value");
    expect(unwrapCodeSpan("`broken")).toBeNull();
  });

  test("strips closed and unterminated HTML comments", () => {
    expect(stripHtmlComments("before<!-- hidden -->after")).toBe("beforeafter");
    expect(stripHtmlComments("before<!-- hidden")).toBe("before");
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
      reviewEvidence: null,
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

  test("accepts matching review evidence on a ready PR", () => {
    const result = validatePullRequest({
      ...baseMetadata,
      body: validReviewBody,
      isDraft: false,
      title: "TIE-248 Enforce review evidence",
    });

    expect(result.errors).toEqual([]);
    expect(result.reviewEvidence.fields["Reviewed commit SHA"]).toBe(headSha);
  });

  test("invalidates review evidence after a new head commit", () => {
    const newHead = "3".repeat(40);
    const result = validatePullRequest({
      ...baseMetadata,
      body: validReviewBody,
      headSha: newHead,
      isDraft: false,
      title: "TIE-248 Enforce review evidence",
    });

    expect(result.errors).toContain(
      `The reviewed commit ${headSha} does not match the current PR head ${newHead}. Start a fresh review for the new head.`,
    );
  });

  test("invalidates review evidence after a tree change", () => {
    const newTree = "4".repeat(40);
    const result = validatePullRequest({
      ...baseMetadata,
      body: validReviewBody,
      headTreeSha: newTree,
      isDraft: false,
      title: "TIE-248 Enforce review evidence",
    });

    expect(result.errors).toContain(
      `The reviewed tree ${headTreeSha} does not match the current PR tree ${newTree}. Start a fresh review for the new tree.`,
    );
  });

  test.each([
    ["placeholder session", "/root/"],
    ["noncanonical session", "same-session"],
    ["uppercase session", "/root/TIE248"],
  ])("rejects %s", (_name, session) => {
    const result = validatePullRequest({
      ...baseMetadata,
      body: validReviewBody.replace("/root/tie248_review", session),
      isDraft: false,
      title: "TIE-248 Enforce review evidence",
    });
    expect(result.errors).toContain(
      "The review session must be a concrete clean-context session path such as `/root/tie248_review`.",
    );
  });

  test("requires an exact no-findings result", () => {
    const result = validatePullRequest({
      ...baseMetadata,
      body: validReviewBody.replace("no findings", "approved"),
      isDraft: false,
      title: "TIE-248 Enforce review evidence",
    });
    expect(result.errors).toContain(
      "The latest independent review result must be exactly `no findings`.",
    );
  });
});
