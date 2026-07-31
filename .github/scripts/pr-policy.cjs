"use strict";

const TIE_TITLE_PATTERN = /^(TIE-[1-9]\d*)(?:$| )/;
const COMMIT_SHA_PATTERN = /^[0-9a-f]{40}$/;
const REVIEW_SESSION_PATTERN =
  /^\/root(?:\/[a-z0-9][a-z0-9_-]*)+$/;
const REVIEW_FIELD_NAMES = [
  "Review session",
  "Reviewed commit SHA",
  "Reviewed tree SHA",
  "Review result",
];

function isDependabotException(metadata) {
  return (
    metadata.actor === "dependabot[bot]" &&
    metadata.headRef.startsWith("dependabot/") &&
    metadata.headRepoFullName === metadata.baseRepoFullName
  );
}

function findTieReferences(title) {
  const match = title.match(TIE_TITLE_PATTERN);
  return match ? [match[1]] : [];
}

function findHeadTreeSha(commits, headSha) {
  const headCommit = commits.find((commit) => commit.sha === headSha);
  return headCommit?.commit?.tree?.sha ?? null;
}

const VOID_HTML_TAGS = new Set([
  "area",
  "base",
  "br",
  "col",
  "embed",
  "hr",
  "img",
  "input",
  "link",
  "meta",
  "param",
  "source",
  "track",
  "wbr",
]);
const DISALLOWED_FIELD_CONTAINERS = new Set([
  "blockquote",
  "details",
  "li",
  "ol",
  "table",
  "template",
  "ul",
]);

function decodeHtmlText(value) {
  return value
    .replace(/&#x([0-9a-f]+);/gi, (_match, digits) =>
      String.fromCodePoint(Number.parseInt(digits, 16)),
    )
    .replace(/&#([0-9]+);/g, (_match, digits) =>
      String.fromCodePoint(Number.parseInt(digits, 10)),
    )
    .replace(
      /&(amp|gt|lt|quot|#39);/g,
      (entity) =>
        ({
          "&amp;": "&",
          "&gt;": ">",
          "&lt;": "<",
          "&quot;": "\"",
          "&#39;": "'",
        })[entity],
    );
}

function tokenizeRenderedHtml(html) {
  const tokens = [];
  const stack = [];
  const pattern = /<\/?([a-z][a-z0-9-]*)(?:\s[^>]*)?>|([^<]+)/gi;

  for (const match of html.matchAll(pattern)) {
    if (match[2] !== undefined) {
      tokens.push({
        ancestors: [...stack],
        end: match.index + match[0].length,
        start: match.index,
        type: "text",
        value: match[2],
      });
      continue;
    }

    const name = match[1].toLowerCase();
    const closing = match[0].startsWith("</");
    tokens.push({
      ancestors: [...stack],
      end: match.index + match[0].length,
      name,
      start: match.index,
      type: closing ? "close" : "open",
    });

    if (closing) {
      const openingIndex = stack.lastIndexOf(name);
      if (openingIndex >= 0) {
        stack.splice(openingIndex);
      }
    } else if (!VOID_HTML_TAGS.has(name) && !match[0].endsWith("/>")) {
      stack.push(name);
    }
  }

  return tokens;
}

function tokenText(tokens, startIndex, endIndex) {
  return decodeHtmlText(
    tokens
      .slice(startIndex + 1, endIndex)
      .filter((token) => token.type === "text")
      .map((token) => token.value)
      .join(""),
  )
    .replace(/\s+/g, " ")
    .trim();
}

function findMatchingClose(tokens, openingIndex) {
  const opening = tokens[openingIndex];
  for (let index = openingIndex + 1; index < tokens.length; index += 1) {
    const token = tokens[index];
    if (
      token.type === "close" &&
      token.name === opening.name &&
      token.ancestors.length === opening.ancestors.length + 1
    ) {
      return index;
    }
  }
  return -1;
}

function extractReviewEvidence(bodyHtml) {
  const tokens = tokenizeRenderedHtml(bodyHtml);
  const headings = tokens
    .map((token, index) => ({ index, token }))
    .filter(
      ({ token }) =>
        token.type === "open" &&
        token.name === "h2" &&
        token.ancestors.length === 0,
    )
    .map(({ index, token }) => {
      const closeIndex = findMatchingClose(tokens, index);
      return {
        closeIndex,
        index,
        start: token.start,
        text: closeIndex < 0 ? "" : tokenText(tokens, index, closeIndex),
      };
    });
  const reviewHeadings = headings.filter(
    (heading) => heading.text === "Independent review",
  );
  const errors = [];

  if (reviewHeadings.length !== 1) {
    errors.push(
      "The rendered PR body must contain exactly one top-level `Independent review` H2 section.",
    );
    return { errors, fields: {} };
  }

  const reviewHeading = reviewHeadings[0];
  const nextHeading = tokens.find(
    (token) =>
      token.type === "open" &&
      (token.name === "h1" || token.name === "h2") &&
      token.ancestors.length === 0 &&
      token.start > reviewHeading.start,
  );
  const sectionEnd = nextHeading?.start ?? bodyHtml.length;
  const listItems = tokens
    .map((token, index) => ({ index, token }))
    .filter(
      ({ token }) =>
        token.type === "open" &&
        token.name === "li" &&
        token.start > reviewHeading.start &&
        token.start < sectionEnd &&
        token.ancestors.length === 1 &&
        (token.ancestors[0] === "ul" || token.ancestors[0] === "ol"),
    )
    .map(({ index }) => {
      const closeIndex = findMatchingClose(tokens, index);
      const nestedContainer =
        closeIndex >= 0 &&
        tokens.slice(index + 1, closeIndex).some(
          (token) =>
            token.type === "open" &&
            DISALLOWED_FIELD_CONTAINERS.has(token.name),
        );
      return {
        nestedContainer,
        text: closeIndex < 0 ? "" : tokenText(tokens, index, closeIndex),
      };
    });
  const fields = {};

  for (const fieldName of REVIEW_FIELD_NAMES) {
    const prefix = `${fieldName}:`;
    const matches = listItems.filter(
      (item) => !item.nestedContainer && item.text.startsWith(prefix),
    );

    if (matches.length !== 1) {
      errors.push(
        `The rendered independent-review section must contain exactly one top-level \`- ${prefix}\` field.`,
      );
      continue;
    }

    const parsedValue = matches[0].text.slice(prefix.length).trim();
    if (!parsedValue) {
      errors.push(`The \`- ${prefix}\` field has an invalid value.`);
      continue;
    }
    fields[fieldName] = parsedValue;
  }

  return { errors, fields };
}

function validatePullRequest(metadata) {
  if (isDependabotException(metadata)) {
    return {
      errors: [],
      exempt: true,
      tieReferences: [],
    };
  }

  const tieReferences = findTieReferences(metadata.title);
  const errors = [];

  if (tieReferences.length === 0) {
    errors.push(
      "Start the pull request title with a Linear issue reference, for " +
        "example `TIE-253 Require Linear metadata`. Body-only references, " +
        "lowercase variants, and the placeholder `TIE-` are not sufficient.",
    );
  }

  let reviewEvidence = null;

  if (!metadata.isDraft) {
    reviewEvidence = extractReviewEvidence(metadata.bodyHtml);
    errors.push(...reviewEvidence.errors);

    const session = reviewEvidence.fields["Review session"];
    const reviewedCommit = reviewEvidence.fields["Reviewed commit SHA"];
    const reviewedTree = reviewEvidence.fields["Reviewed tree SHA"];
    const reviewResult = reviewEvidence.fields["Review result"];

    if (session && !REVIEW_SESSION_PATTERN.test(session)) {
      errors.push(
        "The review session must be a concrete clean-context session path such as `/root/tie248_review`.",
      );
    }
    if (reviewedCommit && !COMMIT_SHA_PATTERN.test(reviewedCommit)) {
      errors.push("The reviewed commit must be a lowercase 40-character SHA.");
    } else if (reviewedCommit && reviewedCommit !== metadata.headSha) {
      errors.push(
        `The reviewed commit ${reviewedCommit} does not match the current PR head ${metadata.headSha}. Start a fresh review for the new head.`,
      );
    }
    if (reviewedTree && !COMMIT_SHA_PATTERN.test(reviewedTree)) {
      errors.push("The reviewed tree must be a lowercase 40-character SHA.");
    } else if (reviewedTree && reviewedTree !== metadata.headTreeSha) {
      errors.push(
        `The reviewed tree ${reviewedTree} does not match the current PR tree ${metadata.headTreeSha}. Start a fresh review for the new tree.`,
      );
    }
    if (reviewResult && reviewResult !== "no findings") {
      errors.push(
        "The latest independent review result must be exactly `no findings`.",
      );
    }
  }

  return {
    errors,
    exempt: false,
    reviewEvidence,
    tieReferences,
  };
}

module.exports = {
  decodeHtmlText,
  extractReviewEvidence,
  findMatchingClose,
  findHeadTreeSha,
  findTieReferences,
  isDependabotException,
  tokenizeRenderedHtml,
  validatePullRequest,
};
