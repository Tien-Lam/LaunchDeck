"use strict";

const TIE_REFERENCE_PATTERN = /\bTIE-[1-9]\d*\b/g;

function stripHtmlComments(value) {
  return value.replace(/<!--[\s\S]*?(?:-->|$)/g, "");
}

function isDependabotException(metadata) {
  return (
    metadata.actor === "dependabot[bot]" &&
    metadata.headRef.startsWith("dependabot/") &&
    metadata.headRepoFullName === metadata.baseRepoFullName
  );
}

function findTieReferences(title, body) {
  const titleReferences = title.match(TIE_REFERENCE_PATTERN) ?? [];
  const visibleBody = stripHtmlComments(body);
  const issueFieldReferences = [
    ...visibleBody.matchAll(/^\s*-\s+Issue:\s*(.*)$/gm),
  ].flatMap((match) => match[1].match(TIE_REFERENCE_PATTERN) ?? []);

  return [...new Set([...titleReferences, ...issueFieldReferences])];
}

function validatePullRequest(metadata) {
  if (isDependabotException(metadata)) {
    return {
      errors: [],
      exempt: true,
      tieReferences: [],
    };
  }

  const tieReferences = findTieReferences(metadata.title, metadata.body);
  const errors = [];

  if (tieReferences.length === 0) {
    errors.push(
      "Add a Linear issue reference such as TIE-253 to the pull request " +
        "title or the template's `- Issue:` field. The placeholder `TIE-` " +
        "and references elsewhere in the body are not sufficient.",
    );
  }

  return {
    errors,
    exempt: false,
    tieReferences,
  };
}

module.exports = {
  findTieReferences,
  isDependabotException,
  stripHtmlComments,
  validatePullRequest,
};
