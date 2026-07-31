import { describe, expect, test } from "bun:test";

const {
  decodeHtmlText,
  extractReviewEvidence,
  findHeadTreeSha,
  findTieReferences,
  isDependabotException,
  tokenizeRenderedHtml,
  validatePullRequest,
} = require("./pr-policy.cjs");

const headSha = "1".repeat(40);
const headTreeSha = "2".repeat(40);

const baseMetadata = {
  actor: "contributor",
  baseRepoFullName: "Tien-Lam/LaunchDeck",
  bodyHtml: "",
  headRef: "feature/layout",
  headRepoFullName: "contributor/LaunchDeck",
  headSha,
  headTreeSha,
  isDraft: true,
  title: "Improve layout",
};

const reviewFieldsHtml = `<ul>
<li>Risk areas: none</li>
<li>Migration / compatibility: none</li>
<li>Follow-up issues: none</li>
<li>Review session: <code>/root/tie248_review</code></li>
<li>Reviewed commit SHA: <code>${headSha}</code></li>
<li>Reviewed tree SHA: <code>${headTreeSha}</code></li>
<li>Review result: no findings</li>
</ul>`;

const validReviewHtml = `<h2>Independent review</h2>
${reviewFieldsHtml}
<h2>Manual validation</h2>`;

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

describe("pull request commit evidence", () => {
  test("selects the live head tree from base-scoped pull commits", () => {
    expect(
      findHeadTreeSha(
        [
          { sha: "older", commit: { tree: { sha: "old-tree" } } },
          { sha: headSha, commit: { tree: { sha: headTreeSha } } },
        ],
        headSha,
      ),
    ).toBe(headTreeSha);
  });

  test("fails closed when the head or tree is unavailable", () => {
    expect(findHeadTreeSha([], headSha)).toBeNull();
    expect(findHeadTreeSha([{ sha: headSha, commit: {} }], headSha)).toBeNull();
  });
});

describe("rendered review evidence", () => {
  test("extracts one top-level rendered field set", () => {
    expect(extractReviewEvidence(validReviewHtml)).toEqual({
      errors: [],
      fields: {
        "Review session": "/root/tie248_review",
        "Reviewed commit SHA": headSha,
        "Reviewed tree SHA": headTreeSha,
        "Review result": "no findings",
      },
    });
  });

  test("decodes rendered entities and tokenizes tag ancestry", () => {
    expect(decodeHtmlText("a&amp;b &#x2f; &#47;")).toBe("a&b / /");
    const heading = tokenizeRenderedHtml(
      "<details><h2>Independent review</h2></details>",
    ).find((token) => token.type === "open" && token.name === "h2");
    expect(heading.ancestors).toEqual(["details"]);
  });

  test.each([
    ["collapsed details", `<details>${validReviewHtml}</details>`],
    ["template", `<template>${validReviewHtml}</template>`],
    ["blockquote", `<blockquote>${validReviewHtml}</blockquote>`],
    ["list item", `<ul><li>${validReviewHtml}</li></ul>`],
    [
      "code block",
      `<pre><code>## Independent review\n- Review result: no findings</code></pre>`,
    ],
  ])("rejects a section rendered inside %s", (_name, html) => {
    expect(extractReviewEvidence(html).errors).toContain(
      "The rendered PR body must contain exactly one top-level `Independent review` H2 section.",
    );
  });

  test("rejects duplicate top-level sections", () => {
    expect(
      extractReviewEvidence(`${validReviewHtml}${validReviewHtml}`).errors,
    ).toContain(
      "The rendered PR body must contain exactly one top-level `Independent review` H2 section.",
    );
  });

  test("stops at the next rendered H1 or H2", () => {
    const html = `<h2>Independent review</h2><h1>Other</h1>${reviewFieldsHtml}`;
    expect(extractReviewEvidence(html).errors.length).toBe(4);
  });

  test("rejects fields in nested lists or hidden containers", () => {
    const nestedList = `<h2>Independent review</h2><ul><li><ul>
      <li>Review session: /root/tie248_review</li>
      <li>Reviewed commit SHA: ${headSha}</li>
      <li>Reviewed tree SHA: ${headTreeSha}</li>
      <li>Review result: no findings</li>
    </ul></li></ul>`;
    expect(extractReviewEvidence(nestedList).errors.length).toBe(4);

    const hiddenValue = validReviewHtml.replace(
      "/root/tie248_review",
      "<details>/root/tie248_review</details>",
    );
    expect(extractReviewEvidence(hiddenValue).errors).toContain(
      "The rendered independent-review section must contain exactly one top-level `- Review session:` field.",
    );
  });

  test("rejects duplicate and missing fields", () => {
    const duplicate = validReviewHtml.replace(
      "<li>Review result: no findings</li>",
      "<li>Review result: no findings</li><li>Review result: no findings</li>",
    );
    expect(extractReviewEvidence(duplicate).errors).toContain(
      "The rendered independent-review section must contain exactly one top-level `- Review result:` field.",
    );

    const missing = validReviewHtml.replace(
      `<li>Reviewed tree SHA: <code>${headTreeSha}</code></li>`,
      "",
    );
    expect(extractReviewEvidence(missing).errors).toContain(
      "The rendered independent-review section must contain exactly one top-level `- Reviewed tree SHA:` field.",
    );
  });
});

describe("validatePullRequest", () => {
  test("accepts a TIE title while a PR is draft", () => {
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

  test("returns actionable guidance when the reference is missing", () => {
    const result = validatePullRequest(baseMetadata);
    expect(result.errors).toHaveLength(1);
    expect(result.errors[0]).toContain("Start the pull request title");
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

  test("accepts matching rendered review evidence on a ready PR", () => {
    const result = validatePullRequest({
      ...baseMetadata,
      bodyHtml: validReviewHtml,
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
      bodyHtml: validReviewHtml,
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
      bodyHtml: validReviewHtml,
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
      bodyHtml: validReviewHtml.replace("/root/tie248_review", session),
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
      bodyHtml: validReviewHtml.replace("no findings", "approved"),
      isDraft: false,
      title: "TIE-248 Enforce review evidence",
    });
    expect(result.errors).toContain(
      "The latest independent review result must be exactly `no findings`.",
    );
  });
});
