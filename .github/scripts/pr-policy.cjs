"use strict";

const TIE_TITLE_PATTERN = /^(TIE-[1-9]\d*)\b/;

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
