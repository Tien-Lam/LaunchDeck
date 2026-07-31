"use strict";

const TIE_REFERENCE_PATTERN = /\bTIE-[1-9]\d*\b/g;

function isDependabotException(metadata) {
  return (
    metadata.actor === "dependabot[bot]" &&
    metadata.headRef.startsWith("dependabot/") &&
    metadata.headRepoFullName === metadata.baseRepoFullName
  );
}

function findTieReferences(title, body) {
  return [...new Set(`${title}\n${body}`.match(TIE_REFERENCE_PATTERN) ?? [])];
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
        "title or body. The placeholder `TIE-` is not sufficient.",
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
  validatePullRequest,
};
