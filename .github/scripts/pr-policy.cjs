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

function stripHtmlComments(value) {
  return value.replace(/<!--[\s\S]*?(?:-->|$)/g, "");
}

function unwrapCodeSpan(value) {
  const trimmed = value.trim();
  if (trimmed.startsWith("`") || trimmed.endsWith("`")) {
    if (
      trimmed.length < 3 ||
      !trimmed.startsWith("`") ||
      !trimmed.endsWith("`")
    ) {
      return null;
    }
    return trimmed.slice(1, -1);
  }
  return trimmed;
}

function updateFenceState(fence, line) {
  if (fence) {
    const closing = line.match(/^ {0,3}(`{3,}|~{3,})[ \t]*$/);
    if (
      closing &&
      closing[1][0] === fence.marker &&
      closing[1].length >= fence.length
    ) {
      return null;
    }
    return fence;
  }

  const opening = line.match(/^ {0,3}(`{3,}|~{3,})(.*)$/);
  if (
    !opening ||
    (opening[1][0] === "`" && opening[2].includes("`"))
  ) {
    return null;
  }

  return {
    length: opening[1].length,
    marker: opening[1][0],
  };
}

function extractReviewEvidence(body) {
  const visibleBody = stripHtmlComments(body);
  const lines = visibleBody.split(/\r?\n/);
  const sectionIndexes = lines
    .map((line, index) => (line === "## Independent review" ? index : -1))
    .filter((index) => index >= 0);
  const errors = [];

  if (sectionIndexes.length !== 1) {
    errors.push(
      "The PR body must contain exactly one top-level `## Independent review` section.",
    );
    return { errors, fields: {} };
  }

  const sectionStart = sectionIndexes[0];
  let fence = null;
  let detailsDepth = 0;

  for (let index = 0; index <= sectionStart; index += 1) {
    const line = lines[index];
    fence = updateFenceState(fence, line);
    if (/<details(?:\s|>)/i.test(line)) {
      detailsDepth += 1;
    }
    if (/<\/details>/i.test(line)) {
      detailsDepth = Math.max(0, detailsDepth - 1);
    }
  }

  if (fence || detailsDepth > 0) {
    errors.push(
      "The `## Independent review` section must not be inside a code fence or collapsed details block.",
    );
    return { errors, fields: {} };
  }

  const sectionEndOffset = lines
    .slice(sectionStart + 1)
    .findIndex((line) => /^## /.test(line));
  const sectionEnd =
    sectionEndOffset < 0
      ? lines.length
      : sectionStart + 1 + sectionEndOffset;
  const sectionLines = lines.slice(sectionStart + 1, sectionEnd);
  const fields = {};

  if (
    sectionLines.some(
      (line) =>
        /^\s*(```|~~~)/.test(line) ||
        /<\/?(?:details|template)(?:\s|>)/i.test(line),
    )
  ) {
    errors.push(
      "The independent-review evidence fields must not be inside code, details, or template blocks.",
    );
  }

  for (const fieldName of REVIEW_FIELD_NAMES) {
    const prefix = `- ${fieldName}:`;
    const matches = sectionLines.filter((line) => line.startsWith(prefix));

    if (matches.length !== 1) {
      errors.push(
        `The independent-review section must contain exactly one \`${prefix}\` field.`,
      );
      continue;
    }

    const parsedValue = unwrapCodeSpan(matches[0].slice(prefix.length));
    if (!parsedValue) {
      errors.push(`The \`${prefix}\` field has an invalid value.`);
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
    reviewEvidence = extractReviewEvidence(metadata.body);
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
  extractReviewEvidence,
  findTieReferences,
  isDependabotException,
  stripHtmlComments,
  updateFenceState,
  unwrapCodeSpan,
  validatePullRequest,
};
